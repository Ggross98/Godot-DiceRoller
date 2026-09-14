#nullable enable

namespace DiceRoller.Core;

public sealed class FaceContent
{
    public static FaceContent None { get; } = new() { Id = "", Label = "?", NumericValue = null };

    public required string Id { get; init; }
    public string Label { get; init; } = "";
    public int? NumericValue { get; init; }
    public string? TextureKey { get; init; }
    public object? Payload { get; init; }

    public static FaceContent Numeric(int n) => new()
    {
        Id = $"{n}",
        Label = $"{n}",
        NumericValue = n,
    };
}
