using DiceRoller.Core;
using Xunit;

namespace DiceRoller.Tests;

public class DieDefinitionTests
{
    [Fact]
    public void CloneForSpawn_layout_is_independent_of_source()
    {
        var source = new DieDefinition
        {
            Hull = HullKind.D6,
            Layout = FaceLayouts.StandardNumeric(HullKind.D6),
        };

        var spawned = source.CloneForSpawn();
        spawned.Layout.Replace(new FaceSlotId(6), new FaceContent { Id = "sword", Label = "Sword" });

        Assert.Equal(HullKind.D6, spawned.Hull);
        Assert.Equal("sword", spawned.Layout[new FaceSlotId(6)].Id);
        Assert.Equal("6", source.Layout[new FaceSlotId(6)].Id);
    }
}
