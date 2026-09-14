#nullable enable

namespace DiceRoller.Core;

public readonly record struct RollOutcome(
    ulong InstanceId,
    HullKind Hull,
    bool IsValid,
    FaceSlotId Slot,
    FaceContent Content);

public static class FaceReader
{
    public static RollOutcome Read(
        DieFaceMap map,
        FaceLayout layout,
        Godot.Transform3D transform,
        ulong instanceId = 0)
    {
        var up = map.ReadUpSlot(transform);
        if (!up.IsValid)
            return new RollOutcome(instanceId, map.Hull, false, FaceSlotId.None, FaceContent.None);

        return new RollOutcome(instanceId, map.Hull, true, up.Slot, layout[up.Slot]);
    }
}
