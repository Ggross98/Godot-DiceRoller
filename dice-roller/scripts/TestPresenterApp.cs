using DiceRoller.Core;
using DiceRoller.Physics;
using DiceRoller.Presentation;
using Godot;

#nullable enable

public partial class TestPresenterApp : Node3D
{
    const string Hint = "左键拖动，双击抛出";

    Label _label = null!;

    public override void _Ready()
    {
        IDiceTable table = GetNode<DiceTable>("DiceTable");
        _label = GetNode<Label>("Hud/Status");
        _label.Text = Hint;
        table.Rolled += OnRolled;

        var layout = FaceLayouts.StandardNumeric(HullKind.D6);
        SetFace(layout, 1, "gem-red", "res://assets/sprites/GemRed.png");
        SetFace(layout, 2, "gem-green", "res://assets/sprites/GemGreen.png");
        SetFace(layout, 3, "gem-blue", "res://assets/sprites/GemBlue.png");
        SetFace(layout, 4, "gem-yellow", "res://assets/sprites/GemYellow.png");
        SetFace(layout, 5, "gem-red", "res://assets/sprites/GemRed.png");
        SetFace(layout, 6, "gem-blue", "res://assets/sprites/GemBlue.png");

        table.Spawn(
            new DieDefinition { Hull = HullKind.D6, Layout = layout },
            new ImageFacePresenter { HideBakedNumerals = true });
    }

    static void SetFace(FaceLayout layout, int slot, string id, string textureKey)
    {
        layout.Replace(new FaceSlotId(slot), new FaceContent
        {
            Id = id,
            Label = id,
            TextureKey = textureKey,
        });
    }

    void OnRolled(RollOutcome outcome)
    {
        _label.Text = outcome.IsValid ? outcome.Content.Id : "?";
    }
}
