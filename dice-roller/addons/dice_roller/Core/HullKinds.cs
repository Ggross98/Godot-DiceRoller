using System;

#nullable enable

namespace DiceRoller.Core;

public static class HullKinds
{
    public static bool TryParse(string text, out HullKind hull)
    {
        hull = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        switch (text.Trim().ToLowerInvariant())
        {
            case "d4":
                hull = HullKind.D4;
                return true;
            case "d6":
                hull = HullKind.D6;
                return true;
            case "d8":
                hull = HullKind.D8;
                return true;
            case "d10":
                hull = HullKind.D10;
                return true;
            case "d12":
                hull = HullKind.D12;
                return true;
            case "d20":
                hull = HullKind.D20;
                return true;
            default:
                return false;
        }
    }

    public static HullKind Parse(string text)
    {
        if (TryParse(text, out var hull))
            return hull;
        throw new ArgumentException($"Unknown hull '{text}'.", nameof(text));
    }
}
