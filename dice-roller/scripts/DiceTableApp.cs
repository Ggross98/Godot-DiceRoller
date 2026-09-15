using DiceRoller.Core;
using DiceRoller.Physics;
using DiceRoller.Presentation;
using DiceRoller.Session;
using DiceRoller.Settings;
using Godot;

#nullable enable

public partial class DiceTableApp : Node3D
{
    // Roll / lock / clear / add-die live on DiceHud so Space is RollAll, not spawn.
    DiceManager _manager = null!;
    ArenaController _arena = null!;
    Camera3D _camera = null!;
    Timer _resizeTicks = null!;
    Timer _zoomTicks = null!;
    SettingsStore _settings = null!;
    Label _outcomeLabel = null!;
    RollOutcome? _lastOutcome;
    bool _sizeUp;
    bool _zoomIn;

    public override void _Ready()
    {
        _manager = GetNode<DiceManager>("DiceManager");
        _arena = GetNode<ArenaController>("Box");
        _camera = GetNode<Camera3D>("Camera3D");
        _resizeTicks = GetNode<Timer>("Box/ResizeTicks");
        _zoomTicks = GetNode<Timer>("Camera3D/ZoomTicks");
        _settings = GetNode<SettingsStore>("/root/SettingsStore");
        _outcomeLabel = GetNode<Label>("Hud/OutcomeLabel");
        _resizeTicks.Timeout += OnResizeTicksTimeout;
        _zoomTicks.Timeout += OnZoomTicksTimeout;
        var session = GetNode<DiceSession>("/root/DiceSession");
        session.Recorded += OnRecorded;
        _settings.Changed += OnSettingChanged;
        DiceTheme.Apply(_settings);
        RefreshHud();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        HandleZoomEvent(@event);

        if (@event.IsActionPressed("zoom_in"))
        {
            _zoomIn = true;
            Zoom();
            _zoomTicks.Start();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("zoom_out"))
        {
            _zoomIn = false;
            Zoom();
            _zoomTicks.Start();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionReleased("zoom_in") || @event.IsActionReleased("zoom_out"))
        {
            _zoomTicks.Stop();
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
            else if (key.Keycode == Key.F4)
            {
                _settings.Set("body_color", "#e040a0");
                GetViewport().SetInputAsHandled();
            }
            else if (key.Keycode == Key.Bracketleft)
            {
                NudgeGravity(-0.5f);
                GetViewport().SetInputAsHandled();
            }
            else if (key.Keycode == Key.Bracketright)
            {
                NudgeGravity(0.5f);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    void HandleZoomEvent(InputEvent @event)
    {
        if (@event is not InputEventWithModifiers)
            return;

        float scrollInversion = _settings.Get("invert_scroll", false) ? -1f : 1f;
        float resizeInversion = _settings.Get("invert_resize", false) ? -1f : 1f;

        if (@event is InputEventMagnifyGesture magnify)
        {
            if (magnify.AltPressed || magnify.ShiftPressed)
            {
                if (magnify.Factor > 1f)
                {
                    _sizeUp = true;
                    _arena.Resize(true, (magnify.Factor - 1f) * 5f);
                }
                else if (magnify.Factor < 1f)
                {
                    _sizeUp = false;
                    _arena.Resize(false, (magnify.Factor - 1f) * -5f);
                }
            }
            else if (magnify.Factor > 1f)
            {
                _zoomIn = true;
                Zoom((magnify.Factor - 1f) * 5f);
            }
            else if (magnify.Factor < 1f)
            {
                _zoomIn = false;
                Zoom((magnify.Factor - 1f) * -5f);
            }

            return;
        }

        if (@event is not InputEventMouseButton mouse)
            return;

        if (mouse.AltPressed || mouse.ShiftPressed)
        {
            if (@event.IsActionPressed("scroll_up"))
            {
                _sizeUp = true;
                _arena.Resize(true, resizeInversion);
            }
            else if (@event.IsActionPressed("scroll_down"))
            {
                _sizeUp = false;
                _arena.Resize(false, resizeInversion);
            }

            return;
        }

        if (@event.IsActionPressed("scroll_up"))
        {
            _zoomIn = true;
            Zoom(scrollInversion);
        }
        else if (@event.IsActionPressed("scroll_down"))
        {
            _zoomIn = false;
            Zoom(scrollInversion);
        }
    }

    void OnResizeTicksTimeout() => _arena.Resize(_sizeUp);

    void OnZoomTicksTimeout() => Zoom();

    void Zoom(float zoomFactor = 1f)
    {
        _camera.Position = ArenaMath.ZoomCamera(_camera.Position, _zoomIn, zoomFactor);
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
        die.PollNow();
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
        string last = _lastOutcome is { } outcome
            ? outcome.IsValid
                ? $"{outcome.Hull} {outcome.Content.Label}"
                : $"{outcome.Hull} ?"
            : "-";
        _outcomeLabel.Text = $"F2 sword  F3 drop  F4 color  [ ] gravity  |  {last}";
    }

    void OnSettingChanged(string key, object _)
    {
        if (key.Contains("color"))
            DiceTheme.Apply(_settings);
    }

    void NudgeGravity(float delta)
    {
        float gravity = Mathf.Clamp(_settings.Get("gravity", 4f) + delta, 0.5f, 10f);
        _settings.Set("gravity", gravity);
    }
}
