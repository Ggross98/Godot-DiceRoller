using DiceRoller.Core;
using Godot;

#nullable enable

namespace DiceRoller.Ui;

public static class HullUi
{
    public static readonly HullKind[] PlayOrder =
    [
        HullKind.D4,
        HullKind.D6,
        HullKind.D8,
        HullKind.D10,
        HullKind.D12,
        HullKind.D20,
    ];

    public static string TypeName(HullKind hull) => hull switch
    {
        HullKind.D4 => "d4",
        HullKind.D6 => "d6",
        HullKind.D8 => "d8",
        HullKind.D10 => "d10",
        HullKind.D12 => "d12",
        HullKind.D20 => "d20",
        _ => "d6",
    };

    public static Texture2D? Icon(HullKind hull) =>
        GD.Load<Texture2D>($"res://Interface/Icons/{TypeName(hull)}.png");

    public static Texture2D? Icon(string fileStem) =>
        GD.Load<Texture2D>($"res://Interface/Icons/{fileStem}.png");
}
