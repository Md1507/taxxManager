using System.Globalization;
using System.Text.RegularExpressions;

namespace TaxManager.Web;

/// <summary>Explicit input formats keep a grouped amount distinct from legacy dot decimals.</summary>
public static class MoneyInput
{
    public const string GroupedFormat = "vi-VN";

    public static bool TryParse(string text, bool grouped, out decimal value)
    {
        value = 0;
        var pattern = grouped
            ? @"^(0|[1-9][0-9]*|[1-9][0-9]{0,2}(\.[0-9]{3})+)(,[0-9]{1,28})?$"
            : @"^(0|[1-9][0-9]*)([,.][0-9]{1,28})?$";
        if (!Regex.IsMatch(text, pattern, RegexOptions.CultureInvariant)) return false;
        var canonical = (grouped ? text.Replace(".", "") : text).Replace(',', '.');
        return decimal.TryParse(canonical, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value) &&
            value.ToString(CultureInfo.InvariantCulture) == canonical;
    }
}
