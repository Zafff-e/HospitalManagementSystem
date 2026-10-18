using FluentAssertions;
using Hms.Domain.Exceptions;
using Hms.Domain.ValueObjects;
using Xunit;

namespace Hms.UnitTests;

public class ValueObjectsTests
{
    [Theory]
    [InlineData("01712345678", "+8801712345678")]
    [InlineData("+8801712345678", "+8801712345678")]
    [InlineData("8801712345678", "+8801712345678")]
    [InlineData("01312345678", "+8801312345678")]
    [InlineData("01812345678", "+8801812345678")]
    [InlineData("01912345678", "+8801912345678")]
    public void Phone_ValidInputs_ShouldNormalizeToE164(string input, string expected)
    {
        var phone = Phone.Create(input);
        phone.Value.Should().Be(expected);
        Phone.IsValid(input).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("01212345678")] // 012 is not a valid BD mobile operator
    [InlineData("0171234567")]   // 10 digits instead of 11
    [InlineData("017123456789")] // 12 digits
    [InlineData("abcdefghijk")]
    public void Phone_InvalidInputs_ShouldThrowDomainException(string input)
    {
        var act = () => Phone.Create(input);
        act.Should().Throw<DomainException>();
        Phone.IsValid(input).Should().BeFalse();
    }

    [Theory]
    [InlineData("1234567890", "enc_10", "****7890")]
    [InlineData("1234567890123", "enc_13", "****0123")]
    [InlineData("12345678901234567", "enc_17", "****4567")]
    public void NationalId_ValidLengths_ShouldCreateAndMask(string input, string encrypted, string expectedMasked)
    {
        var nid = NationalId.Create(input, encrypted);
        nid.EncryptedValue.Should().Be(encrypted);
        nid.Last4.Should().Be(input[^4..]);
        nid.ToMaskedDisplay().Should().Be(expectedMasked);
        NationalId.IsValidFormat(input).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456789")]          // 9 digits
    [InlineData("12345678901")]        // 11 digits
    [InlineData("12345678901234")]      // 14 digits
    [InlineData("123456789012345678")]  // 18 digits
    [InlineData("123456789a")]         // non-digit
    public void NationalId_InvalidLengths_ShouldThrowDomainException(string input)
    {
        var act = () => NationalId.Create(input, "enc_mock");
        act.Should().Throw<DomainException>();
        NationalId.IsValidFormat(input).Should().BeFalse();
    }

    [Fact]
    public void Money_Operations_ShouldCalculateAndFormatCorrectly()
    {
        var m1 = Money.BDT(100.50m);
        var m2 = Money.BDT(50.25m);

        var sum = m1 + m2;
        sum.Amount.Should().Be(150.75m);
        sum.Currency.Should().Be("BDT");

        var diff = m1 - m2;
        diff.Amount.Should().Be(50.25m);

        m1.ToDisplay().Should().Contain("100.50");
        m1.ToDisplay(useBanglaDigits: true).Should().Contain("৳");
    }
}
