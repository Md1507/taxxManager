namespace TaxManager.Data.Protection;

public sealed class DataProtectionException : Exception
{
    public DataProtectionException(string message) : base(message) { }
}
