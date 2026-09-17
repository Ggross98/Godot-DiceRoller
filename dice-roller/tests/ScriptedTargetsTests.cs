using DiceRoller.Core;
using Xunit;

namespace DiceRoller.Tests;

public class ScriptedTargetsTests
{
    [Fact]
    public void Resolve_uses_sticky_slot()
    {
        var map = DieFaceMap.For(HullKind.D20);
        var slot = ScriptedTargets.Resolve(map, new FaceSlotId(20), randomIndex: 0);

        Assert.Equal(20, slot.Value);
    }

    [Fact]
    public void Resolve_without_sticky_picks_by_index()
    {
        var map = DieFaceMap.For(HullKind.D6);
        var slot = ScriptedTargets.Resolve(map, sticky: null, randomIndex: 5);

        Assert.Equal(6, slot.Value);
    }

    [Fact]
    public void Resolve_wraps_random_index()
    {
        var map = DieFaceMap.For(HullKind.D6);
        var slot = ScriptedTargets.Resolve(map, sticky: null, randomIndex: 7);

        Assert.Equal(2, slot.Value);
    }

    [Fact]
    public void Resolve_rejects_sticky_none()
    {
        var map = DieFaceMap.For(HullKind.D6);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ScriptedTargets.Resolve(map, FaceSlotId.None, 0));
    }
}
