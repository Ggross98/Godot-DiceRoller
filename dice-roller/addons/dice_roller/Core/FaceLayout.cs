using System;
using System.Collections.Generic;

#nullable enable

namespace DiceRoller.Core;

public sealed class FaceLayout
{
    readonly Dictionary<FaceSlotId, FaceContent> _slots;

    public FaceLayout(IReadOnlyDictionary<FaceSlotId, FaceContent> slots)
    {
        _slots = new Dictionary<FaceSlotId, FaceContent>(slots);
    }

    public FaceContent this[FaceSlotId slot]
    {
        get
        {
            if (slot == FaceSlotId.None)
                throw new ArgumentOutOfRangeException(nameof(slot), "None is not a layout key.");
            if (!_slots.TryGetValue(slot, out var content))
                throw new KeyNotFoundException($"FaceLayout has no content for slot {slot.Value}.");
            return content;
        }
    }

    public event Action? Changed;

    public void Replace(FaceSlotId slot, FaceContent content)
    {
        if (slot == FaceSlotId.None)
            throw new ArgumentOutOfRangeException(nameof(slot), "None is not a layout key.");
        if (!_slots.ContainsKey(slot))
            throw new KeyNotFoundException($"FaceLayout has no slot {slot.Value} to replace.");
        ArgumentNullException.ThrowIfNull(content);
        _slots[slot] = content;
        Changed?.Invoke();
    }

    public FaceLayout Clone() => new(_slots);
}

public static class FaceLayouts
{
    public static FaceLayout StandardNumeric(HullKind hull)
    {
        var map = DieFaceMap.For(hull);
        var slots = new Dictionary<FaceSlotId, FaceContent>(map.Samples.Count);
        foreach (var slot in map.Samples.Keys)
            slots[slot] = FaceContent.Numeric(slot.Value);
        return new FaceLayout(slots);
    }
}
