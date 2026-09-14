using DiceRoller.Core;
using DiceRoller.Physics;
using DiceRoller.Session;
using Godot;

#nullable enable

public partial class DiceTableApp : Node3D
{
    // Phase 3: the `roll` action (Space / R) spawns a standard numeric d6.
    // Phase 6/7 will switch Space back to RollAll.
    DiceManager _manager = null!;
    Label _outcomeLabel = null!;
    RollOutcome? _lastOutcome;

    public override void _Ready()
    {
        _manager = GetNode<DiceManager>("DiceManager");
        _outcomeLabel = GetNode<Label>("Hud/OutcomeLabel");
        var session = GetNode<DiceSession>("/root/DiceSession");
        session.Recorded += OnRecorded;
        _manager.Spawned += _ => RefreshHud();
        RefreshHud();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Mouse hover/drag/lock belongs to DiceInteractionController; do not consume pointer events here.
        if (@event.IsActionPressed("roll"))
        {
            _manager.SpawnStandard(HullKind.D6);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("lock_dice"))
        {
            _manager.LockValid();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("clear"))
        {
            _manager.Clear();
            _lastOutcome = null;
            RefreshHud();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.F2)
            {
                ReplaceLastDieSlot6();
                GetViewport().SetInputAsHandled();
            }
            else if (key.Keycode == Key.F3)
            {
                DropLastDie();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    void OnRecorded(RollOutcome outcome)
    {
        _lastOutcome = outcome;
        RefreshHud();
    }

    void ReplaceLastDieSlot6()
    {
        var die = _manager.LastSpawned;
        if (die is null)
            return;
        die.Layout.Replace(new FaceSlotId(6), new FaceContent { Id = "sword", Label = "Sword" });
        RefreshHud();
    }

    void DropLastDie()
    {
        var die = _manager.LastSpawned;
        if (die is null)
            return;
        var pos = die.GlobalPosition;
        die.GlobalPosition = new Vector3(pos.X, -5f, pos.Z);
    }

    void RefreshHud()
    {
        string outcomeLine = "Space: spawn d6";
        if (_lastOutcome is { } outcome)
        {
            outcomeLine = outcome.IsValid
                ? $"{outcome.Hull}  slot={outcome.Slot.Value}  {outcome.Content.Label}"
                : $"{outcome.Hull}  invalid  ?";
        }

        string layout6 = "-";
        if (_manager.LastSpawned is { } die)
        {
            try
            {
                layout6 = die.Layout[new FaceSlotId(6)].Id;
            }
            catch (System.Exception)
            {
                layout6 = "n/a";
            }
        }

        _outcomeLabel.Text = $"{outcomeLine}\nlayout[6]={layout6}";
    }
}
