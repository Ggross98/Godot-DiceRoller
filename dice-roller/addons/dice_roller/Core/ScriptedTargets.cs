using System;

#nullable enable

namespace DiceRoller.Core;

public static class ScriptedTargets
{
    public static FaceSlotId Resolve(DieFaceMap map, FaceSlotId? sticky, int randomIndex)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (sticky is { } slot)
        {
            map.RequireSlot(slot);
            return slot;
        }

        int n = map.Samples.Count;
        if (n == 0)
            throw new InvalidOperationException("DieFaceMap has no slots.");

        int index = randomIndex % n;
        if (index < 0)
            index += n;

        int i = 0;
        foreach (var key in map.Samples.Keys)
        {
            if (i++ == index)
                return key;
        }

        throw new InvalidOperationException("DieFaceMap has no slots.");
    }
}
