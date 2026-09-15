using DiceRoller.Core;
using Xunit;

namespace DiceRoller.Tests;

public class HullKindsTests
{
    [Theory]
    [InlineData("d4", HullKind.D4)]
    [InlineData("D6", HullKind.D6)]
    [InlineData("d8", HullKind.D8)]
    [InlineData("d10", HullKind.D10)]
    [InlineData("d12", HullKind.D12)]
    [InlineData("d20", HullKind.D20)]
    public void Parse_accepts_standard_names(string text, HullKind expected)
    {
        Assert.Equal(expected, HullKinds.Parse(text));
    }

    [Fact]
    public void TryParse_rejects_unknown()
    {
        Assert.False(HullKinds.TryParse("d7", out _));
        Assert.Throws<ArgumentException>(() => HullKinds.Parse("d7"));
    }
}
