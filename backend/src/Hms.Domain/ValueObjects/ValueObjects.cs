using System.Text.RegularExpressions;

namespace Hms.Domain.ValueObjects;

/// <summary>
/// Phone number value object with E.164 normalization for Bangladesh.
/// Accepts: 01XXXXXXXXX, +8801XXXXXXXXX
/// Stores: +8801XXXXXXXXX
/// </summary>
public sealed partial class Phone : IEquatable<Phone>
{
    public string Value { get; }

    private Phone(string value) => Value = value;

    /// <summary>
    /// Creates a Phone from user input, normalizing to E.164 format.
    /// </summary>
    public static Phone Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new DomainException("Phone number is required.");

        var cleaned = input.Trim().Replace(" ", "").Replace("-", "");

        // Bangladesh: accept 01X... and normalize to +880...
        if (cleaned.StartsWith("01") && cleaned.Length == 11)
            cleaned = "+880" + cleaned;
        else if (cleaned.StartsWith("8801") && cleaned.Length == 13)
            cleaned = "+" + cleaned;
        else if (!cleaned.StartsWith("+8801"))
            throw new DomainException($"Invalid Bangladesh phone number format: {input}");

        if (!BdPhoneRegex().IsMatch(cleaned))
            throw new DomainException($"Invalid Bangladesh phone number: {input}. Expected format: 01XXXXXXXXX or +8801XXXXXXXXX");

        return new Phone(cleaned);
    }

    /// <summary>
    /// Creates a Phone from a stored E.164 value (no validation needed).
    /// </summary>
    public static Phone FromStored(string stored) => new(stored);

    /// <summary>
    /// Validates whether a string is a valid BD phone number.
    /// </summary>
    public static bool IsValid(string input)
    {
        try { Create(input); return true; }
        catch { return false; }
    }

    /// <summary>
    /// Returns the phone number in display format: 01X-XXXX-XXXX
    /// </summary>
    public string ToDisplayFormat()
    {
        var local = Value.Replace("+880", "0");
        if (local.Length == 11)
            return $"{local[..3]}-{local[3..7]}-{local[7..]}";
        return local;
    }

    [GeneratedRegex(@"^\+8801[3-9]\d{8}$")]
    private static partial Regex BdPhoneRegex();

    public bool Equals(Phone? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Phone other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(Phone phone) => phone.Value;
}

/// <summary>
/// National ID value object with length validation and encryption support.
/// Bangladesh NID: 10, 13, or 17 digits.
/// </summary>
public sealed class NationalId : IEquatable<NationalId>
{
    public string EncryptedValue { get; }
    public string Last4 { get; }

    private NationalId(string encrypted, string last4)
    {
        EncryptedValue = encrypted;
        Last4 = last4;
    }

    /// <summary>
    /// Creates a NationalId from plain text, validating format.
    /// The encrypted value must be provided by the caller (application-level encryption).
    /// </summary>
    public static NationalId Create(string plainText, string encryptedValue)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            throw new DomainException("NID is required when provided.");

        var cleaned = plainText.Trim();

        if (!cleaned.All(char.IsDigit))
            throw new DomainException("NID must contain only digits.");

        if (cleaned.Length != 10 && cleaned.Length != 13 && cleaned.Length != 17)
            throw new DomainException($"NID must be 10, 13, or 17 digits. Got {cleaned.Length} digits.");

        var last4 = cleaned[^4..];

        return new NationalId(encryptedValue, last4);
    }

    /// <summary>
    /// Restores from storage.
    /// </summary>
    public static NationalId FromStored(string encrypted, string last4) => new(encrypted, last4);

    /// <summary>
    /// Validates NID format without creating the object.
    /// </summary>
    public static bool IsValidFormat(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var cleaned = input.Trim();
        return cleaned.All(char.IsDigit) && (cleaned.Length == 10 || cleaned.Length == 13 || cleaned.Length == 17);
    }

    /// <summary>
    /// Returns masked display: ****XXXX
    /// </summary>
    public string ToMaskedDisplay() => $"****{Last4}";

    public bool Equals(NationalId? other) => other is not null && Last4 == other.Last4;
    public override bool Equals(object? obj) => obj is NationalId other && Equals(other);
    public override int GetHashCode() => Last4.GetHashCode();
    public override string ToString() => ToMaskedDisplay();
}

/// <summary>
/// Money value object for BDT with proper decimal handling.
/// </summary>
public sealed class Money : IEquatable<Money>, IComparable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; } = "BDT";

    private Money(decimal amount, string currency = "BDT")
    {
        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency;
    }

    public static Money BDT(decimal amount) => new(amount);
    public static Money Zero => new(0);

    public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount);
    public static Money operator -(Money a, Money b) => new(a.Amount - b.Amount);
    public static Money operator *(Money a, decimal factor) => new(a.Amount * factor);
    public static bool operator >(Money a, Money b) => a.Amount > b.Amount;
    public static bool operator <(Money a, Money b) => a.Amount < b.Amount;
    public static bool operator >=(Money a, Money b) => a.Amount >= b.Amount;
    public static bool operator <=(Money a, Money b) => a.Amount <= b.Amount;

    /// <summary>
    /// Format as BDT with the ৳ symbol.
    /// </summary>
    public string ToDisplay(bool useBanglaDigits = false)
    {
        var formatted = Amount.ToString("N2");
        if (useBanglaDigits)
        {
            formatted = string.Concat(formatted.Select(c => c >= '0' && c <= '9' ? (char)('০' + c - '0') : c));
        }
        return $"৳{formatted}";
    }

    public bool Equals(Money? other) => other is not null && Amount == other.Amount && Currency == other.Currency;
    public override bool Equals(object? obj) => obj is Money other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
    public int CompareTo(Money? other) => other is null ? 1 : Amount.CompareTo(other.Amount);
    public override string ToString() => ToDisplay();
}
