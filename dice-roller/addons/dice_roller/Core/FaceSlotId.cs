namespace DiceRoller.Core;

public readonly record struct FaceSlotId(int Value)
{
    public static FaceSlotId None { get; } = new(0);
}
