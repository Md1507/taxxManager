using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TaxManager.Data.Infrastructure;

namespace TaxManager.Data.TaxRules;

internal static partial class TaxRuleStorageValidator
{
    [GeneratedRegex(@"^[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?$")]
    private static partial Regex DecimalPattern();

    private static string Normalize(string value)
    {
        var parts = value.ToLowerInvariant().Split('e');
        var exponent = parts.Length == 2 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 0;
        var mantissa = parts[0];
        var negative = mantissa.StartsWith('-');
        mantissa = mantissa.TrimStart('+', '-');
        var point = mantissa.IndexOf('.');
        if (point >= 0)
        {
            exponent -= mantissa.Length - point - 1;
            mantissa = mantissa.Remove(point, 1);
        }
        mantissa = mantissa.TrimStart('0');
        if (mantissa.Length == 0) return "0";
        var trimmed = mantissa.TrimEnd('0');
        exponent += mantissa.Length - trimmed.Length;
        return $"{(negative ? "-" : "")}{trimmed}e{exponent}";
    }

    private static void ValidateDecimal(DbDataReader reader, int column)
    {
        if (reader.IsDBNull(column)) return;
        _ = ParseExactDecimal(reader.GetString(column));
    }

    internal static decimal ParseExactDecimal(string text)
    {
        try
        {
            if (!DecimalPattern().IsMatch(text) ||
                !decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                Normalize(text) != Normalize(value.ToString("G29", CultureInfo.InvariantCulture)))
                throw new InvalidOperationException("Tax rule decimal is not exactly representable.");
            return value;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw new InvalidOperationException("Tax rule decimal is invalid.", ex);
        }
    }

    internal static async Task ValidateAsync(TaxManagerDbContext context, CancellationToken cancellationToken)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = """
            SELECT EXISTS(SELECT 1 FROM TaxRuleSnapshot
                WHERE typeof(FromYear) <> 'integer' OR FromYear NOT BETWEEN 1 AND 9999)
                OR EXISTS(SELECT 1 FROM TaxRuleLegalBasis
                WHERE typeof(Status) <> 'integer' OR Status NOT BETWEEN 0 AND 2)
                OR EXISTS(SELECT 1 FROM TaxRuleBracket
                WHERE typeof(Ordinal) <> 'integer' OR Ordinal NOT BETWEEN 0 AND 2147483647)
            """;
        if (Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) != 0)
            throw new InvalidOperationException("Tax rule integer storage is invalid.");
        command.CommandText = """
            SELECT PersonalDeduction, DependentDeduction FROM TaxRuleSnapshot
            UNION ALL SELECT UpperBound, Rate FROM TaxRuleBracket
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ValidateDecimal(reader, 0);
            ValidateDecimal(reader, 1);
        }
    }
}
