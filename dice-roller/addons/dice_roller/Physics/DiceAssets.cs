using DiceRoller.Core;

#nullable enable

namespace DiceRoller.Physics;

public static class DiceAssets
{
    public const string Root = "res://addons/dice_roller/assets/";
    public const string DiceDir = Root + "dice/";
    public const string WallScene = Root + "world/wall.tscn";
    public const string BodyMaterial = DiceDir + "materials/Body.material";
    public const string BodyLockedMaterial = DiceDir + "materials/BodyLocked.tres";
    public const string NumbersMaterial = DiceDir + "materials/Numbers.material";
    public const string OutlineMaterial = DiceDir + "materials/outline.tres";

    public static string DieScene(HullKind hull) => hull switch
    {
        HullKind.D4 => DiceDir + "d4.tscn",
        HullKind.D6 => DiceDir + "d6.tscn",
        HullKind.D8 => DiceDir + "d8.tscn",
        HullKind.D10 => DiceDir + "d10.tscn",
        HullKind.D12 => DiceDir + "d12.tscn",
        HullKind.D20 => DiceDir + "d20.tscn",
        _ => DiceDir + "d6.tscn",
    };
}
