# ADR 0016 — Immutable protected monthly calculation history

Status: implementation, synthetic checks and independent Bint review 1 complete with no findings. Human security review remains required before real data.
No Web, Identity, legal verification or new personal identifiers are introduced.

## Contract and source boundary

`CalculationHistoryService.CreateMonthlyAsync(profileId, year, month, sourceMode)` reads
the persisted monthly income, applicable dependent intervals and registered tax rules,
then calls the actual Core `MonthlySalaryCalculator`. It never accepts caller-supplied
amounts, rules or results. The context must have no pending writes. Missing profile,
income, catalog or registration is an error; nothing is seeded or substituted.

SourceMode is mandatory: Official applies the existing strict technical source gate to
the entire catalog; References requires the exact supported 2025/2026 catalog. Both
reuse the loader's policy and graph/storage validators inside the service transaction.
References does not promote Unverified/Missing to Verified. Registration PeriodFrom/To
is independent of legal effective dates; FromYear is a label. Loading verified metadata
does not establish that the legislation is correct or applicable to a real taxpayer.

Scope is one ordinary, fully taxable monthly salary with manually entered insurance.
Automatic insurance, rounding policies, annual calculations, bonuses, extras, casual
payments, multiple payers, charitable contributions and retirement graphs remain
unsupported and are rejected by the existing persistence adapters. ExemptIncome is
explicitly zero. ActualWithheldTax is stored for reconciliation, **not** passed into
the monthly PIT calculation. Dependent intervals include both boundary months and
count distinct dependent IDs, scoped to the selected profile; names are not copied.

`ReadAsync(profileId, historyId)` requires both nonempty IDs and validates the key-check
even for an empty lookup. It returns null for no matching pair, including a history
owned by a different profile. It decrypts the saved document without joining current
sources or invoking the current calculator. IDs and AuditWriteContext.Actor are
untrusted caller metadata, not authentication or authorization. DbContext can access
all records; a future authorized application must enforce access before these calls.

## Document and storage

CalculationHistory stores Id, logical ProfileId, Year, Month, SchemaVersion,
CreatedAtUtc and PayloadEncrypted. Index: ProfileId/Year/Month/CreatedAtUtc. Repeated
calculations for the same month are separate immutable records. There are no FKs to
income, dependent, profile or rule rows that could rewrite or cascade old history.

The protected schema-v1 document contains:

- The header again, explicit source mode, gross/manual insurance/actual withheld/
  exempt income, active dependent IDs/intervals and count.
- RuleId, Version, Period, FromYear, registration dates, deductions and ordered
  brackets with nullable final bound. All LegalBasis fields are preserved, including
  nulls, status, verification date and legal effective dates for each role/bracket.
- Every Core result amount, intermediate pre-clamp value, taxable gross, PIT taxable
  amount, total and each portion's bounds/rate/taxable amount/tax. Each amount/portion
  carries its exact LegalBasis and references the single Rules snapshot in the same
  document. Gross/manual insurance keep Core's Missing basis.
- UTC creation time from the context clock, schema version separate from encryption
  envelope version, calculator name and contract version, actual Core assembly version,
  informational version and module version ID. This identifies the binary used;
  it does not archive the binary or promise reproducibility by a future engine.

DTOs use immutable arrays and immutable Core Bracket/LegalBasis values; modifying a
record copy does not alter the saved snapshot. Ciphertext access returns a defensive
copy. Service-returned DTOs outlive their context. `CalculationRecord.Snapshot` is a
protected live-context accessor and reauthenticates rather than trusting a cached
plaintext document; use the service DTO outside the context lifetime.

JSON numbers retain decimal values exactly, with no double/REAL conversion or added
rounding. Reads reject nonrepresentable numbers, missing/duplicate/unrecognized fields,
invalid enum/schema/structure and header mismatches. Unknown extensions are errors,
not discarded graphs. The 4 MiB protector limit also applies to the encrypted audit
payload, which includes the snapshot; oversize documents fail atomically. Parse errors
are replaced with a generic DataProtectionException without source text/inner JSON.

## Protection and append-only boundary

Use the external FileKeyRing/sentinel/AES-GCM envelope from ADR0015. History AAD binds
database/profile/entity/history ID/field and canonical year/month/schema/UTC header.
The document's duplicated header is checked after authentication. No key is stored in
the DB or repository. Input/result/provenance/engine/source mode are inside ciphertext;
history contains no names. Audit Add carries the full snapshot and actor/purpose/
correlation inside its own encrypted payload, with only opaque IDs/action/time in its
headers. SQL writes send encrypted BLOBs, never plaintext personal amounts/provenance.
Do not enable sensitive data logging or serialize snapshots into application logs.

The context grants an internal, reference-scoped append only to the service's exact
new record after Core calculation. SaveChanges and both bool sync/async overloads
reject caller Adds, Modified entries, detached Update and direct Delete. Private
setters are not the enforcement boundary: the persistence guard is. Profile purge is
the only supported erasure operation. Tracking reads/no-op saves remain supported.

This protects supported application writes; arbitrary SQL, ExecuteUpdate/Delete,
reflection, tracker OriginalValues manipulation, mutating/suppressing interceptors,
concurrent context use and SQLite administration are outside that API. It does not
prevent replay of an old authenticated row in its original context or restoring an
old whole database. Metadata reveals relationships, periods, timing, ciphertext sizes
and key IDs. Public rule tables remain plaintext; memory contains decrypted data.

## Transaction, failure and retry

Create owns a transaction or a whole-operation savepoint inside the caller transaction.
It checks SQLite foreign keys and rejects ReadUncommitted; a no-op write promotes the
writer lock **before** any source read. All inputs and the full catalog are read
AsNoTracking in that transaction, avoiding stale identity-map values. The source loader
does not start a nested transaction. History and Add audit use the existing atomic
coordinator; the outer operation covers read/calculate/save and commits last.

Validation, overflow, size/crypto, SQL or cancellation errors roll back the operation,
detach internal history/audit entries and do not flush caller work. Caller transactions
are never committed or disposed; their earlier SQL work survives a failed operation.
After caller rollback use a fresh context/reload as in ADR0015. No automatic retry or
exactly-once claim is added. Use a stable explicit OperationId for reconciliation;
reuse after a recorded operation is rejected. Commit exceptions report possibly
unknown outcome. Inspect/decrypt OperationId events and matching history in a fresh
context before deciding whether a retry is appropriate, including exceptions raised
after the actual commit. An omitted OperationId is generated by the audit coordinator
and cannot be used by the caller to identify an uncertain operation in advance.

## Retention, purge and migration

Q-1 decision: ordinary profile deletion retains encrypted history using an intentional
logical ProfileId relation, exactly as retained audit. These are managed retained
records, not accidental unowned rows. Read still requires ProfileId+HistoryId even
after the profile is gone; no new calculation can be created for a deleted profile.
Purge removes **all** target history and audit, even for an already-deleted profile,
alongside the active business graph, in the same transaction/savepoint. It emits no
new receipt linking the erased target and leaves other profiles untouched. No legal
retention period is invented. Shared keys, WAL/free pages, backups and historical
copies mean this is active-table erasure, not a forensic erasure promise.

Migration `20261123045552_AddCalculationHistory` is additive and preserves all six old
migration/designer files byte-for-byte. It does not seed/init keys or run at startup.
Downgrade refuses before altering schema; restore a matching 2.3 backup/application
instead. Tests migrate a genuinely populated 2.3 schema, reopen original external
keys, compare old ciphertext/sentinel/audit/rules and check all four overlap triggers.
The legacy synthetic copy-forward upgrader still accepts only its previous legacy
source formats and now creates the latest four-migration target. No production
upgrader, real DB operation or automatic key provisioning is introduced.

## Evidence and review boundary

Behavioral tests live in Data.Tests/CalculationHistory: persisted Core calculation,
full field/provenance audit comparison, fresh key/context roundtrip after supported
source edits/deletion, exact decimals/cultures/clamp, explicit ownership/source policy,
immutability and context/header/row/field/database tampering, authenticated malformed
documents, wrong/missing keys, SQL/commit/cancel faults, caller transactions/savepoints,
operation reconciliation, oversize/overflow, writer-lock competition, stale tracked
inputs, retained history/purge failures/cancellation and confidentiality scans including
actual SQL parameters. Every DB/key is synthetic in TEMP. Counts and final commands
are recorded in PLAN; scans do not establish safety of arbitrary historical media.

Independent Bint review must use the task snapshot diff for
01M836AAX9R6EPSHC16WVB85R1, preserving the preexisting dirty 2.1/2.2/2.3 baseline.
This ADR does not assert independent review or human security approval has happened.
The user self-commits after review; HEAD/index and Bint model defaults stay unchanged.
