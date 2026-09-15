using System.Collections.Generic;
using DiceRoller.Core;
using DiceRoller.Physics;
using DiceRoller.Session;
using Godot;

#nullable enable

namespace DiceRoller.Ui;

[GlobalClass]
public partial class DiceHud : CanvasLayer
{
    [Export]
    public DiceManager? Manager { get; set; }

    [Export]
    public ArenaController? Arena { get; set; }

    [Export]
    public Camera3D? Camera { get; set; }

    DiceManager _manager = null!;
    ArenaController _arena = null!;
    Camera3D _camera = null!;
    DiceSession _session = null!;
    VBoxContainer _selects = null!;
    Button _addCurrent = null!;
    Label _countLabel = null!;
    Label _sumLabel = null!;
    Control _totals = null!;
    VBoxContainer _hullList = null!;
    HullKind _selected = HullKind.D6;
    bool _sizeUp;
    bool _zoomIn;
    readonly Dictionary<HullKind, Control> _rows = new();

    public override void _Ready()
    {
        _manager = Manager ?? GetParent()!.GetNode<DiceManager>("DiceManager");
        _arena = Arena ?? GetParent()!.GetNode<ArenaController>("Box");
        _camera = Camera ?? GetParent()!.GetNode<Camera3D>("Camera3D");
        _session = GetNode<DiceSession>("/root/DiceSession");

        GetNode<Control>("Root").MouseFilter = Control.MouseFilterEnum.Ignore;
        GetNode<Control>("Root/Left").MouseFilter = Control.MouseFilterEnum.Stop;
        GetNode<Control>("Root/Right").MouseFilter = Control.MouseFilterEnum.Stop;

        _selects = GetNode<VBoxContainer>("Root/Left/Margin/Column/DiceSelects");
        _addCurrent = GetNode<Button>("Root/Left/Margin/Column/Actions/AddDie");
        _totals = GetNode<Control>("Root/Right/Margin/Column/Totals");
        _countLabel = GetNode<Label>("Root/Right/Margin/Column/Totals/Count");
        _sumLabel = GetNode<Label>("Root/Right/Margin/Column/Totals/Sum");
        _hullList = GetNode<VBoxContainer>("Root/Right/Margin/Column/HullStats");

        var packed = GD.Load<PackedScene>("res://addons/dice_roller/Ui/DiceSelect.tscn");
        foreach (var hull in HullUi.PlayOrder)
        {
            var row = packed.Instantiate<DiceSelect>();
            row.Name = HullUi.TypeName(hull);
            row.Hull = hull;
            row.AddDice += OnAddDice;
            row.RemoveDice += OnRemoveDice;
            _selects.AddChild(row);
        }

        _addCurrent.Pressed += () => SpawnAmount(_selected, 1);
        GetNode<Button>("Root/Left/Margin/Column/Actions/ClearAll").Pressed += () => _manager.Clear();
        GetNode<Button>("Root/Left/Margin/Column/Actions/RollAll").Pressed += () => _manager.RollAll();
        GetNode<Button>("Root/Left/Margin/Column/Actions/RollInvalid").Pressed += () => _manager.RollInvalid();
        GetNode<Button>("Root/Left/Margin/Column/Actions/LockValid").Pressed += () => _manager.LockValid();

        WireHoldButton("Root/Left/Margin/Column/View/SizeUp", true, resize: true);
        WireHoldButton("Root/Left/Margin/Column/View/SizeDown", false, resize: true);
        WireHoldButton("Root/Left/Margin/Column/View/ZoomIn", true, resize: false);
        WireHoldButton("Root/Left/Margin/Column/View/ZoomOut", false, resize: false);

        ApplyIcons();
        SetSelected(HullKind.D6);

        GetNode<Timer>("ResizeTicks").Timeout += OnResizeTicks;
        GetNode<Timer>("ZoomTicks").Timeout += OnZoomTicks;
        var refresh = GetNode<Timer>("Root/Right/Refresh");
        refresh.Timeout += RefreshStats;
        RefreshStats();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("number_shortcut") && @event is InputEventKey number && !number.Echo)
        {
            int digit = DigitOf(number.Keycode);
            if (digit >= 1)
            {
                int index = Mathf.Clamp(digit, 1, HullUi.PlayOrder.Length) - 1;
                SetSelected(HullUi.PlayOrder[index]);
                SpawnAmount(_selected, 1);
                GetViewport().SetInputAsHandled();
                return;
            }
        }

        if (@event.IsActionPressed("add_die"))
        {
            SpawnAmount(_selected, 1);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("roll_invalid_only"))
        {
            _manager.RollInvalid();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed("roll"))
        {
            _manager.RollAll();
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
            if (GetViewport().GuiGetFocusOwner() is NumberInput)
                return;
            _manager.Clear();
            GetViewport().SetInputAsHandled();
        }
    }

    void OnAddDice(HullKind hull, int amount)
    {
        SetSelected(hull);
        SpawnAmount(hull, amount);
    }

    void OnRemoveDice(HullKind hull) => _manager.Clear(hull);

    void SpawnAmount(HullKind hull, int amount)
    {
        int n = Mathf.Max(1, amount);
        for (int i = 0; i < n; i++)
            _manager.SpawnStandard(hull);
    }

    void SetSelected(HullKind hull)
    {
        _selected = hull;
        _addCurrent.Icon = HullUi.Icon(hull);
        _addCurrent.TooltipText = $"Add {HullUi.TypeName(hull)}";
    }

    void ApplyIcons()
    {
        SetButtonIcon("Root/Left/Margin/Column/Actions/ClearAll", "clear", "Clear all");
        SetButtonIcon("Root/Left/Margin/Column/Actions/RollAll", "roll", "Roll all");
        SetButtonIcon("Root/Left/Margin/Column/Actions/RollInvalid", "undo", "Roll invalid");
        SetButtonIcon("Root/Left/Margin/Column/Actions/LockValid", "locked", "Lock valid faces");
        SetButtonIcon("Root/Left/Margin/Column/View/SizeUp", "sizeup", "Enlarge arena");
        SetButtonIcon("Root/Left/Margin/Column/View/SizeDown", "sizedown", "Shrink arena");
        SetButtonIcon("Root/Left/Margin/Column/View/ZoomIn", "zoomin", "Zoom in");
        SetButtonIcon("Root/Left/Margin/Column/View/ZoomOut", "zoomout", "Zoom out");
        GetNode<TextureRect>("Root/Right/Margin/Column/Totals/CountIcon").Texture = HullUi.Icon("d6");
        GetNode<TextureRect>("Root/Right/Margin/Column/Totals/SumIcon").Texture = HullUi.Icon("sum");
    }

    void SetButtonIcon(string path, string stem, string tooltip)
    {
        var button = GetNode<Button>(path);
        button.Icon = HullUi.Icon(stem);
        button.ExpandIcon = true;
        button.TooltipText = tooltip;
    }

    void WireHoldButton(string path, bool positive, bool resize)
    {
        var button = GetNode<Button>(path);
        var ticks = GetNode<Timer>(resize ? "ResizeTicks" : "ZoomTicks");
        button.ButtonDown += () =>
        {
            if (resize)
            {
                _sizeUp = positive;
                _arena.Resize(_sizeUp);
            }
            else
            {
                _zoomIn = positive;
                Zoom();
            }

            ticks.Start();
        };
        button.ButtonUp += () => ticks.Stop();
    }

    void OnResizeTicks() => _arena.Resize(_sizeUp);

    void OnZoomTicks() => Zoom();

    void Zoom(float zoomFactor = 1f)
    {
        _camera.Position = ArenaMath.ZoomCamera(_camera.Position, _zoomIn, zoomFactor);
    }

    void RefreshStats()
    {
        int count = _session.Count;
        _totals.Visible = count > 0;
        _hullList.Visible = count > 0;
        _countLabel.Text = $"{count}";
        _sumLabel.Text = $"{NumericScore.Sum(_session.Outcomes)}";

        var groups = SessionStats.ByHull(_session.Outcomes);
        var present = new HashSet<HullKind>();
        int order = 0;
        foreach (var group in groups)
        {
            present.Add(group.Hull);
            if (!_rows.TryGetValue(group.Hull, out var row))
            {
                row = CreateHullRow(group.Hull);
                _rows[group.Hull] = row;
                _hullList.AddChild(row);
            }

            UpdateHullRow(row, group);
            _hullList.MoveChild(row, order++);
        }

        var stale = new List<HullKind>();
        foreach (var hull in _rows.Keys)
        {
            if (!present.Contains(hull))
                stale.Add(hull);
        }

        foreach (var hull in stale)
        {
            _rows[hull].QueueFree();
            _rows.Remove(hull);
        }
    }

    Control CreateHullRow(HullKind hull)
    {
        var root = new VBoxContainer { Name = HullUi.TypeName(hull) };
        var header = new HBoxContainer { Name = "Header" };
        header.AddChild(new TextureRect
        {
            Texture = HullUi.Icon(hull),
            CustomMinimumSize = new Vector2(28, 28),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TooltipText = HullUi.TypeName(hull),
        });
        header.AddChild(new Label { Name = "Count" });
        header.AddChild(new Label { Name = "Sum" });
        var fold = new Button { Name = "Fold", Text = "▸", ToggleMode = true, CustomMinimumSize = new Vector2(28, 28) };
        var faces = new VBoxContainer { Name = "Faces", Visible = false };
        fold.Toggled += on =>
        {
            faces.Visible = on;
            fold.Text = on ? "▾" : "▸";
            RefreshStats();
        };
        header.AddChild(fold);
        root.AddChild(header);
        root.AddChild(faces);
        return root;
    }

    static void UpdateHullRow(Control row, HullBreakdown group)
    {
        row.GetNode<Label>("Header/Count").Text = $"{group.Count} x";
        row.GetNode<Label>("Header/Sum").Text = $"{group.NumericSum}";
        var faces = row.GetNode<VBoxContainer>("Faces");
        if (!faces.Visible)
            return;

        for (int i = faces.GetChildCount() - 1; i >= 0; i--)
            faces.GetChild(i).Free();

        foreach (var face in group.Faces)
            faces.AddChild(new Label { Text = $"{face.Occurrences} x [{face.Id}]" });
        if (group.InvalidCount > 0)
            faces.AddChild(new Label { Text = $"{group.InvalidCount} x [?]" });
    }

    static int DigitOf(Key keycode) => keycode switch
    {
        Key.Key1 => 1,
        Key.Key2 => 2,
        Key.Key3 => 3,
        Key.Key4 => 4,
        Key.Key5 => 5,
        Key.Key6 => 6,
        Key.Key7 => 7,
        Key.Key8 => 8,
        Key.Key9 => 9,
        _ => 0,
    };
}
