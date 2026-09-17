using System;
using System.Collections.Generic;
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
    const float ScriptedMinAirTime = 0.2f;
    const float ScriptedTimeout = 2.5f;
    const float ScriptedLinearSleep = 0.15f;
    const float ScriptedAngularSleep = 0.4f;
    const float ScriptedSnapHold = 0.08f;

    [Export]
    public HullKind Hull { get; set; } = HullKind.D6;

    public FaceLayout Layout { get; private set; } = FaceLayouts.StandardNumeric(HullKind.D6);

    public bool IsInvalid { get; private set; }

    public bool Locked { get; private set; }

    public InteractionState Interaction { get; set; }

    public DieFaceMap FaceMap => _map;

    public FaceSlotId? ScriptedSlot => _scriptedSlot;

    public event Action<RollOutcome>? Rolled;

    public event Action<DieBody>? Died;

    public event Action<DieBody>? RespawnRequested;

    DieFaceMap _map = DieFaceMap.For(HullKind.D6);
    IFacePresenter _presenter = new BakedNumeralPresenter();
    SettingsStore? _settings;
    MeshInstance3D? _mesh;
    MeshInstance3D? _border;
    Material? _lockedMaterial;
    readonly MeshDataTool _meshTool = new();
    float _pollTime;
    bool _configured;
    bool _despawning;
    bool _layoutHooked;
    FaceSlotId? _scriptedSlot;
    FaceSlotId _flightSlot;
    bool _scriptedFlight;
    float _scriptedElapsed;
    float _snapHold;

    public void Configure(DieDefinition definition, IFacePresenter presenter, SettingsStore? settings)
    {
        UnhookLayout();
        Hull = definition.Hull;
        Layout = definition.Layout;
        _map = DieFaceMap.For(Hull);
        _settings = settings;
        _configured = true;
        ClearScriptedRoll();
        CancelScriptedFlight();
        SetPresenter(presenter);
        HookLayout();
    }

    public override void _Ready()
    {
        InputRayPickable = true;
        _mesh = GetNode<MeshInstance3D>("Mesh");
        _border = GetNode<MeshInstance3D>("Mesh/Border");
        _lockedMaterial = GD.Load<Material>(DiceAssets.BodyLockedMaterial);
        if (_border.Mesh is ArrayMesh arrayMesh)
            _meshTool.CreateFromSurface(arrayMesh, 0);

        if (!_configured)
        {
            Configure(
                new DieDefinition { Hull = Hull, Layout = FaceLayouts.StandardNumeric(Hull) },
                new BakedNumeralPresenter(),
                GetNodeOrNull<SettingsStore>("/root/SettingsStore"));
        }
        else
        {
            BindPresenter();
            CallDeferred(MethodName.BindPresenter);
        }
    }

    public override void _ExitTree()
    {
        UnhookLayout();
        _presenter.Unbind();
    }

    public override void _Process(double delta)
    {
        if (_despawning)
            return;

        if (_snapHold > 0f)
        {
            _snapHold -= (float)delta;
            if (_snapHold <= 0f && !Locked)
                Freeze = false;
        }

        if (_scriptedFlight)
            TickScriptedFlight((float)delta);
        else
        {
            _pollTime += (float)delta;
            if (_pollTime > 1f)
            {
                _pollTime = 0f;
                PollNow();
            }
        }

        if (GlobalPosition.Y < -4f)
            RequestRespawn();
    }

    public RollOutcome PollNow()
    {
        if (_scriptedFlight)
            CompleteScriptedFlight();

        var outcome = FaceReader.Read(_map, Layout, GlobalTransform, GetInstanceId());
        SetInvalid(!outcome.IsValid);
        Rolled?.Invoke(outcome);
        return outcome;
    }

    public void ScriptNextRoll(FaceSlotId slot)
    {
        _map.RequireSlot(slot);
        _scriptedSlot = slot;
    }

    public void ScriptNextRollByContentId(string contentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentId);
        if (!Layout.TryGetSlotByContentId(contentId, out var slot))
            throw new KeyNotFoundException($"FaceLayout has no content id '{contentId}'.");
        ScriptNextRoll(slot);
    }

    public void ClearScriptedRoll() => _scriptedSlot = null;

    public void BeginScriptedFlight(FaceSlotId slot)
    {
        _map.RequireSlot(slot);
        _flightSlot = slot;
        _scriptedFlight = true;
        _scriptedElapsed = 0f;
        _pollTime = 0f;
    }

    public void CancelScriptedFlight() => _scriptedFlight = false;

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

    public float GetOriginToLowestYHeight()
    {
        float originY = ToGlobal(Vector3.Zero).Y;
        float lowestY = originY;
        int count = _meshTool.GetVertexCount();
        for (int i = 0; i < count; i++)
        {
            float vertexY = ToGlobal(_meshTool.GetVertex(i)).Y;
            if (vertexY < lowestY)
                lowestY = vertexY;
        }

        return originY - lowestY;
    }

    public float GetHighestYPositionGlobal()
    {
        float highestY = ToGlobal(Vector3.Zero).Y;
        int count = _meshTool.GetVertexCount();
        for (int i = 0; i < count; i++)
        {
            float vertexY = ToGlobal(_meshTool.GetVertex(i)).Y;
            if (vertexY > highestY)
                highestY = vertexY;
        }

        return highestY;
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

    void SetPresenter(IFacePresenter presenter)
    {
        if (_mesh is not null)
            _presenter.Unbind();
        _presenter = presenter;
        BindPresenter();
    }

    void BindPresenter()
    {
        if (_mesh is null)
            return;
        _presenter.Bind(_mesh, _map);
        _presenter.Apply(Layout);
    }

    void HookLayout()
    {
        UnhookLayout();
        Layout.Changed += OnLayoutChanged;
        _layoutHooked = true;
        _presenter.Apply(Layout);
    }

    void UnhookLayout()
    {
        if (_layoutHooked)
            Layout.Changed -= OnLayoutChanged;
        _layoutHooked = false;
    }

    void OnLayoutChanged() => _presenter.Apply(Layout);

    void TickScriptedFlight(float delta)
    {
        if (Interaction is InteractionState.Dragged or InteractionState.Clicked)
        {
            CancelScriptedFlight();
            return;
        }

        _scriptedElapsed += delta;
        if (_scriptedElapsed >= ScriptedTimeout || HasScriptedSettled())
            PollNow();
    }

    bool HasScriptedSettled()
    {
        if (_scriptedElapsed < ScriptedMinAirTime)
            return false;
        if (Sleeping)
            return true;
        return LinearVelocity.Length() < ScriptedLinearSleep
            && AngularVelocity.Length() < ScriptedAngularSleep;
    }

    void CompleteScriptedFlight()
    {
        if (!_scriptedFlight)
            return;

        var slot = _flightSlot;
        _scriptedFlight = false;

        Freeze = true;
        GlobalTransform = _map.AlignSlotToWorldUp(GlobalTransform, slot);
        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;
        Sleeping = true;
        _snapHold = ScriptedSnapHold;
    }
}
