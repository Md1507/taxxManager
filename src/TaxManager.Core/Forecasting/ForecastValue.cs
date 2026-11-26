namespace TaxManager.Core.Forecasting;

public enum ForecastStatus { Actual, Planned, Assumed }

/// <summary>A supplied value and its provenance; a missing value is represented by null.</summary>
public sealed record ForecastValue<T>
{
    public T Value { get; }
    public ForecastStatus Status { get; }
    public string? Source { get; }
    public string? Note { get; }
    public DateTimeOffset EnteredAt { get; }

    public ForecastValue(T value, ForecastStatus status, string? source, string? note, DateTimeOffset enteredAt)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (enteredAt == default) throw new ArgumentException("Entry time is required.", nameof(enteredAt));
        if (status == ForecastStatus.Assumed && string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("An assumption must explain how it was formed.", nameof(note));
        Value = value;
        Status = status;
        Source = source;
        Note = note;
        EnteredAt = enteredAt;
    }
}
