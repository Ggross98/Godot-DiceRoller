using DiceRoller.Core;
using DiceRoller.Physics;
using DiceRoller.Presentation;
using Godot;

#nullable enable

public partial class HeadlessTableApp : Node3D
{
    IDiceTable _table = null!;
    Label _label = null!;
    DieBody _die = null!;
    string _layoutId = "";
    string _lastOutcome = "waiting";

    public override void _Ready()
    {
        _table = GetNode<DiceTable>("DiceTable");
        _label = GetNode<Label>("Hud/Status");
        _table.Rolled += OnRolled;

        var layout = FaceLayouts.StandardNumeric(HullKind.D6);
        var slot6 = new FaceSlotId(6);
        layout.Replace(slot6, new FaceContent
        {
            Id = "sword",
            Label = "Sword",
            TextureKey = DiceAssets.ExampleFace,
        });
        _die = _table.Spawn(
            new DieDefinition { Hull = HullKind.D6, Layout = layout },
            new ImageFacePresenter());
        _layoutId = _die.Layout[slot6].Id;
        _die.PollNow();
        Refresh();
        GD.Print($"headless: count={_table.Count} layout[6]={_layoutId}");
    }

    void OnRolled(RollOutcome outcome)
    {
        _lastOutcome = outcome.IsValid
            ? $"{outcome.Hull} slot={outcome.Slot.Value} id={outcome.Content.Id}"
            : $"{outcome.Hull} invalid";
        Refresh();
    }

    void Refresh()
    {
        _label.Text =
            $"headless table  count={_table.Count}  layout[6]={_layoutId}  last={_lastOutcome}";
    }
}
