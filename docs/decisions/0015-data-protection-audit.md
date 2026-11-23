# ADR 0015 — Protected SQLite fields, atomic audit and synthetic upgrade

Status: implemented for synthetic data; independent Bint review and the user's security
review before real data are still required. This does not add Identity, authorization,
legal retention periods or calculation history.

## Protection boundary

Use BCL .NET AES-256-GCM with a fresh random 12-byte nonce and 16-byte authentication
tag per encryption. No custom cipher, KDF or MAC. The versioned BLOB envelope contains
magic/version, UTF-8 key ID, nonce, ciphertext and tag. Header and canonical length-prefixed
context are authenticated. Payloads are limited to 4 MiB.

AAD binds application purpose/version, database instance UUID, profile UUID, entity,
complete row key and field. Audit additionally authenticates event/operation IDs, ordinal,
action, entity key and UTC timestamp. Different rows, fields, profiles, databases or audit
headers cannot exchange envelopes. Replaying an old envelope in its original context, or
restoring a whole old database, is outside this protection.

UserProfile/Dependent names and the three personal income amounts are encrypted. Names
retain null versus empty and Unicode; money is JSON decimal without double conversion.
Readback rejects numbers that cannot be represented exactly, missing/wrong payload types,
negative stored income, invalid/unknown envelope versions and failed authentication.
There is no plaintext fallback, implicit zero or key generation when a key is missing.

Plaintext metadata deliberately remains: opaque PK/FK and profile relationships, income
year/month, dependent eligibility months, database ID, audit IDs/ordinal/action/entity/key/
UTC, key IDs, envelope versions and ciphertext sizes. Public tax rules, their provenance
and amounts remain plaintext. Actor, purpose/context, correlation and before/after values
are inside the audit ciphertext. Metadata still permits relationship, timing and size
analysis. This is field encryption, not whole-file encryption or protection against a
compromised process, a SQLite administrator, arbitrary SQL, or a person with both DB and keys.
Plaintext exists in application memory.

## Keys, initialization and recovery

FileKeyRing loads an explicitly operator-provisioned JSON file:

```json
{"activeKeyId":"operator-key-id","keys":{"operator-key-id":"BASE64_OF_32_BYTES"}}
```

This placeholder is not a usable key. Runtime code never creates a key or a missing file.
Absolute paths are required. Key paths containing filesystem links, paths within the
repository or database directory, duplicate IDs, bad key lengths and missing active keys
are rejected. Callers must supply the actual absolute repository and DB directory to
Load; do not substitute unrelated directories to bypass separation checks. Protect the
key directory with OS ACLs, keep it off source control and DB backups, and never put
key bytes in logs, arguments, SQL or source. Configuration may contain the key file path.
The application owner controls the FileKeyRing lifetime and disposes it after contexts.

On the first protected write to a fresh empty database, the save transaction inserts
a random database UUID and authenticated key-check sentinel. Migrations themselves do
not provision keys, seed data or initialize a sentinel. Materializing a protected entity
and saving protected data validate the sentinel. Call ValidateProtection() when reopening
an initialized DB, including an empty/ordinary-deleted DB: metadata-only queries such as
Count/ID projections do not materialize protected fields and do not themselves validate
keys. Missing sentinel in a populated DB is an error, not a reason to reset encryption.

For rotation, provision a new active ID/key outside the app, retaining all old keys.
New/changed rows use the active key; existing rows, the sentinel and old backups still
need their previous keys. No automatic re-encryption/retirement utility is provided.
Do not remove an old key until every dependent DB/backup has been assessed and restore
tested. Losing the required key is unrecoverable by changing activeKeyId or resetting DB
metadata. Keep a separately secured key backup and restore the DB instance metadata
unchanged with its correct ring.

## EF contract

Mapped storage is required BLOB columns, with no mapped plaintext/shadow name or amount
columns. Public values are unmapped and hydrated by a stateless materialization
interceptor using the current context's session, never a key captured by EF's model
cache. Tracking, AsNoTracking, client projections and Reload are covered by tests.
Encrypted-property replacement invalidates cached plaintext. Public setters notify the
tracker, and acceptance/reload invalidates the cache. The coordinator still compares
the actual stored and proposed values to discard false modifications.

SQL predicates, ordering and aggregation on unmapped sensitive values are unsupported;
load a bounded set by plaintext metadata and calculate in Core. Scalar ID/count queries
are metadata access. Reload/discard or use a new context after conflicts or caller
transaction rollback. Do not change mapped encrypted storage or OriginalValues directly.

## Audit and atomic saves

Supported writes are tracked SaveChanges/SaveChangesAsync, including their bool overloads,
ProfileDeletionService and ProfilePurgeService. Before a real protected change, set an
AuditWriteContext with nonblank Actor and Context; Correlation and a stable OperationId
are optional. No OS username/default actor is inferred. Actor is untrusted caller metadata,
not an authenticated identity. TaxRules-only writes and no-ops need no actor.

One event is produced per actual add/update/delete of profile, monthly income, dependent
or eligibility period. Auditing profiles in addition to the required entities keeps name
changes and profile deletion explicit. Stored before-state is read under the SQLite write
transaction. An existing tracked original that differs from storage rejects the write;
a detached Update without stored ciphertext uses current DB before-state. Same values,
change-then-revert, Modified-only flags and add-then-remove do not create events.

The save coordinator acquires a write lock before before-state/cascade/TaxRules validation.
It creates its own transaction or a savepoint in the caller's transaction covering the
entire preparation and SQL. Business and audit inserts use one base SaveChanges(false).
Owned commits precede tracker acceptance. Caller transactions are never committed or
disposed by SaveChanges. Save errors/cancellation roll back the owned transaction or
preparation savepoint, keeping earlier caller work. AutoTransactionBehavior.Never or
disabled EF auto-savepoints do not disable the coordinator's explicit transaction.

After successful SaveChanges(false), accept all changes or clear/reopen before saving
again; internal audit entries are detached so they cannot be accidentally reinserted.
After an outer rollback, the tracker does not undo accepted state: reopen/reload.
Interceptors that mutate/suppress application writes during the save pipeline, concurrent
use of a DbContext, manipulating tracker OriginalValues and bulk/raw SQL writes are outside
the supported business API.

No automatic retry is performed. SQLITE_BUSY or stale conflicts require rollback and a
fresh context. OperationId+ordinal is unique; reuse for any non-no-op batch is rejected,
including repeat Adds. This deliberately requires reconciliation instead of claiming
transparent replay/exactly-once behavior. Before retrying an uncertain commit, use a fresh
context to inspect/decrypt its OperationId events and the stored business state. Commit
exceptions explicitly report possible unknown outcome; exceptions after actual commit
are tested. An omitted OperationId is generated for that save, so callers needing
cross-attempt reconciliation must supply one before the first attempt.

AuditEvent has no mutable public DbSet; reads use Set<AuditEvent>().AsNoTracking() and
ReadAudit(event). A guard rejects caller tracked add/update/delete of AuditEvent or
DataProtectionMetadata. Purge is the separate explicit erasure API. These are application
invariants, not a SQLite permissions boundary: Database SQL/ExecuteUpdate/ExecuteDelete,
arbitrary interceptors, reflection and DBA tools can bypass application policy.

## Ordinary delete versus purge

Ordinary Remove(profile/dependent), or ProfileDeletionService, enumerates persisted
children even when not loaded, records their actual encrypted delete before-state and
deletes the business graph atomically. Audit.ProfileId is a logical association without
FK/cascade, so audit survives and remains decryptable. Ordinary deletion is not erasure.

ProfilePurgeService requires a fresh context, explicit profile and nonblank caller/purpose.
It deletes target audit plus the business graph in one transaction/savepoint, including
audit of a profile already ordinary-deleted. It stores no receipt linking back to the
target, actor or correlation. Other profiles are preserved. There is no retention timer,
auth check or legal-policy decision here; the caller/operator chooses the appropriate API
under a reviewed policy. Shared keys mean purge is not cryptographic erasure. Free pages,
WAL/journals, snapshots and backups can retain older bytes.

## Migrations and development upgrade

Both old migrations and their designers remain byte-identical. The third migration is
AddDataProtectionAndAudit. It checks all legacy sensitive tables are empty before any
destructive schema SQL. Populated direct upgrade rejects with the
RequireCopyForwardForPopulatedDatabase constraint and leaves data/history intact.
Empty/fresh upgrade recreates the business tables with required encrypted columns and
the original FK/check constraints and dependent overlap triggers; tax tables and their
two overlap triggers are preserved. Down throws before modifying protected schema.

SyntheticDatabaseUpgrader is an explicit development API, restricted to absolute,
unlinked synthetic files under TEMP, with syntheticDataOnly=true and a distinct,
nonexistent target. It rejects taxmanager.local.db. It is not a production migration tool.
The source must contain exactly the two legacy migrations. The read-only source
connection holds one consistent snapshot; the operator must stop writers for an offline
upgrade. Raw legacy readers preserve exact decimals/nulls/keys/dates, validate FK and
integer storage, and reuse strict raw tax-rule validation.

No source bytes are copied into the target. A unique pending file is migrated fresh;
all business entities and complete public rule/basis/bracket/registration rows are saved
together. Import events identify synthetic-upgrader and the current import time, never
pretend to reconstruct historical actors/events. A fresh context then compares every
scalar/PK and sensitive value, decrypts audit, checks FK/trigger presence and absence of
legacy plaintext columns. Connections close after checkpoint/journal normalization.
Only then does a no-overwrite rename publish the target. Failure/cancellation preserves
the source and existing target and cleans only that attempt's pending files.

Retain and secure the plaintext source as rollback material until the target and separately
backed-up keys are verified. After a reviewed cutover, inventory old source/WAL/journal/
temporary files/backups and dispose according to the chosen policy. UPDATE, secure_delete
or VACUUM do not prove secure erasure on SSDs, filesystem snapshots or backup media.
This task does not run on the user's DB or real keys.

## Evidence and next step

Behavior tests cover exact decimal/culture and Unicode roundtrips, independent key rings,
nonce/header tampering, field/row/profile/database swaps, sentinel errors, DB/WAL/log
marker scans, durable reopen and SQLite backup/restore; sync/async SQL/commit/cancellation
faults, caller savepoints, stale/detached writes, save(false), uncertain commits, competing
writers, unloaded cascade and purge; populated migration rejection, synthetic copy,
readback/key/publish/cancellation failures and EF model consistency. Marker scans only
establish confidentiality on those new fixtures, not forensic erasure of historic media.
Current full counts/review status are recorded in PLAN.

Step 2.4 remains planned: immutable input/result snapshots, full RuleId+Version/provenance,
engine version and UTC, protected using 2.3. Its deletion/purge participation must be
implemented with that feature; no history table or API is added here.

Technical references: [.NET AesGcm](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm?view=net-10.0),
[EF transactions/savepoints](https://learn.microsoft.com/en-us/ef/core/saving/transactions),
[materialization interceptors](https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors),
[SQLite BackupDatabase](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup).
