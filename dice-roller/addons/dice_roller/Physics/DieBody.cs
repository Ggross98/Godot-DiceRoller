using System;
using DiceRoller.Core;
using DiceRoller.Presentation;
using DiceRoller.Settings;
using Godot;

#nullable enable

namespace DiceRoller.Physics;

[GlobalClass]
public partial class DieBody : RigidBody3D
{
    static readonly Vector3 ShrunkMesh = new(0.9f, 0.9f, 0.9f);

    [Export]
    public HullKind Hull { get; set; } = HullKind.D6;

    public FaceLayout Layout { get; private set; } = FaceLayouts.StandardNumeric(HullKind.D6);

    public bool IsInvalid { get; private set; }

    public bool Locked { get; private set; }

    public event Action<RollOutcome>? Rolled;

    public event Action<DieBody>? Died;

    public event Action<DieBody>? RespawnRequested;

    DieFaceMap _map = DieFaceMap.For(HullKind.D6);
    IFacePresenter _presenter = new BakedNumeralPresenter();
    SettingsStore? _settings;
    MeshInstance3D? _mesh;
    MeshInstance3D? _border;
    Material? _lockedMaterial;
    float _pollTime;
    bool _configured;
    bool _despawning;
    bool _layoutHooked;

    public void Configure(DieDefinition definition, IFacePresenter presenter, SettingsStore? settings)
    {
        Hull = definition.Hull;
        Layout = definition.Layout;
        _map = DieFaceMap.For(Hull);
        _presenter = presenter;
        _settings = settings;
        _configured = true;
        HookLayout();
        if (IsNodeReady())
            _presenter.Apply(Layout);
    }

    public override void _Ready()
    {
        _mesh = GetNode<MeshInstance3D>("Mesh");
        _border = GetNode<MeshInstance3D>("Mesh/Border");
        _lockedMaterial = GD.Load<Material>("res://dice/materials/BodyLocked.material");

        if (!_configured)
        {
            Configure(
                new DieDefinition { Hull = Hull, Layout = FaceLayouts.StandardNumeric(Hull) },
                new BakedNumeralPresenter(),
                GetNodeOrNull<SettingsStore>("/root/SettingsStore"));
        }
        else
        {
            _presenter.Apply(Layout);
        }
    }

    public override void _ExitTree()
    {
        if (_layoutHooked)
            Layout.Changed -= OnLayoutChanged;
        _layoutHooked = false;
    }

    public override void _Process(double delta)
    {
        if (_despawning)
            return;

        _pollTime += (float)delta;
        if (_pollTime > 1f)
        {
            _pollTime = 0f;
            PollNow();
        }

        if (GlobalPosition.Y < -4f)
            RequestRespawn();
    }

    public RollOutcome PollNow()
    {
        var outcome = FaceReader.Read(_map, Layout, GlobalTransform, GetInstanceId());
        SetInvalid(!outcome.IsValid);
        Rolled?.Invoke(outcome);
        return outcome;
    }

    public void SetLocked(bool locked)
    {
        Locked = locked;
        if (_mesh is null)
            return;

        if (locked)
        {
            _mesh.SetSurfaceOverrideMaterial(0, _lockedMaterial);
            if (_settings?.Get("allow_locked_move", false) == true)
            {
                AxisLockAngularX = true;
                AxisLockAngularY = true;
                AxisLockAngularZ = true;
            }
            else
            {
                FreezeMode = FreezeModeEnum.Static;
                Freeze = true;
                Sleeping = true;
            }
        }
        else
        {
            _mesh.SetSurfaceOverrideMaterial(0, null);
            Freeze = false;
            AxisLockAngularX = false;
            AxisLockAngularY = false;
            AxisLockAngularZ = false;
            Sleeping = false;
        }
    }

    public void Die()
    {
        if (_despawning)
            return;
        _despawning = true;
        Died?.Invoke(this);
        QueueFree();
    }

    void RequestRespawn()
    {
        if (_despawning)
            return;
        _despawning = true;
        RespawnRequested?.Invoke(this);
        Died?.Invoke(this);
        QueueFree();
    }

    void SetInvalid(bool invalid)
    {
        IsInvalid = invalid;
        if (_border is not null)
            _border.Visible = invalid;
        if (_mesh is not null)
            _mesh.Scale = invalid ? ShrunkMesh : Vector3.One;
    }

    void HookLayout()
    {
        if (_layoutHooked)
            Layout.Changed -= OnLayoutChanged;
        Layout.Changed += OnLayoutChanged;
        _layoutHooked = true;
        _presenter.Apply(Layout);
    }

    void OnLayoutChanged() => _presenter.Apply(Layout);
}
