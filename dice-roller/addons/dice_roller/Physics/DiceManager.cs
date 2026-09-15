using System;
using System.Collections.Generic;
using DiceRoller.Core;
using DiceRoller.Presentation;
using DiceRoller.Session;
using DiceRoller.Settings;
using Godot;

#nullable enable

namespace DiceRoller.Physics;

[GlobalClass]
public partial class DiceManager : Node3D, IDiceTable
{
    static readonly IFacePresenter DefaultPresenter = new BakedNumeralPresenter();

    static readonly Dictionary<HullKind, string> ScenePaths = new()
    {
        [HullKind.D4] = "res://dice/d4.tscn",
        [HullKind.D6] = "res://dice/d6.tscn",
        [HullKind.D8] = "res://dice/d8.tscn",
        [HullKind.D10] = "res://dice/d10.tscn",
        [HullKind.D12] = "res://dice/d12.tscn",
        [HullKind.D20] = "res://dice/d20.tscn",
    };

    [Export]
    public CollisionShape3D? FloorCollision { get; set; }

    [Export]
    public float AreaScale { get; set; } = 0.2f;

    public DieBody? LastSpawned { get; private set; }

    public int Count
    {
        get
        {
            int n = 0;
            foreach (var _ in Dice())
                n++;
            return n;
        }
    }

    public event Action<DieBody>? Spawned;

    public event Action<RollOutcome>? Rolled;

    SettingsStore? _settings;
    DiceSession? _session;
    bool _diceLocked;

    public override void _Ready()
    {
        FloorCollision ??= GetNodeOrNull<CollisionShape3D>("../Box/Floor/CollisionShape3D");
        _settings = GetNode<SettingsStore>("/root/SettingsStore");
        _session = GetNode<DiceSession>("/root/DiceSession");
        _settings.Changed += OnSettingChanged;
    }

    public override void _ExitTree()
    {
        if (_settings is not null)
            _settings.Changed -= OnSettingChanged;
    }

    public DieBody SpawnStandard(HullKind hull) =>
        Spawn(new DieDefinition
        {
            Hull = hull,
            Layout = FaceLayouts.StandardNumeric(hull),
        });

    public DieBody SpawnStandard(string hullKind) => SpawnStandard(HullKinds.Parse(hullKind));

    public DieBody Spawn(DieDefinition definition)
    {
        var spawn = definition.CloneForSpawn();
        var scene = GD.Load<PackedScene>(ScenePaths[spawn.Hull]);
        var die = scene.Instantiate<DieBody>();
        die.Configure(spawn, DefaultPresenter, _settings);
        AddChild(die);
        die.GravityScale = _settings?.Get("gravity", 4f) ?? 4f;
        die.Rolled += OnDieRolled;
        die.Died += OnDieDied;
        die.RespawnRequested += OnDieRespawnRequested;
        RandomizeThrow(die);
        LastSpawned = die;
        Spawned?.Invoke(die);
        return die;
    }

    public void RollAll()
    {
        foreach (var die in Dice())
            Roll(die);
    }

    public void RollInvalid()
    {
        foreach (var die in Dice())
        {
            if (die.IsInvalid)
                Roll(die);
        }
    }

    public void Roll(DieBody die)
    {
        // Original roll() leaves MODE_STATIC (locked) dice stuck; restore rigid first.
        if (die.Locked || die.Freeze)
            die.SetLocked(false);

        int maxUp = (int)Math.Ceiling(AreaScale * 5f) * 5;
        die.LinearVelocity = new Vector3(
            RandomValueInRange(1, 3, true),
            RandomValueInRange(10, maxUp),
            RandomValueInRange(1, 3, true));
        die.AngularVelocity = new Vector3(
            RandomValueInRange(5, 10, true),
            RandomValueInRange(5, 10, true),
            RandomValueInRange(5, 10, true));
    }

    public void LockValid()
    {
        if (!_diceLocked)
        {
            foreach (var die in Dice())
            {
                die.PollNow();
                if (!die.IsInvalid)
                    die.SetLocked(true);
            }

            _diceLocked = true;
        }
        else
        {
            foreach (var die in Dice())
                die.SetLocked(false);
            _diceLocked = false;
        }
    }

    public void Clear() => RemoveDice(_ => true);

    public void Clear(HullKind hull) => RemoveDice(die => die.Hull == hull);

    void RemoveDice(Func<DieBody, bool> predicate)
    {
        foreach (var die in Dice())
        {
            if (predicate(die))
                die.Die();
        }

        if (LastSpawned is { } last && last.IsQueuedForDeletion())
            LastSpawned = null;
    }

    public void WakeAll()
    {
        foreach (var die in Dice())
            die.Sleeping = false;
    }

    public void RandomizeThrow(DieBody die)
    {
        float floorHalfX = 25f;
        float floorHalfZ = 25f;
        if (FloorCollision?.Shape is BoxShape3D box)
        {
            floorHalfX = box.Size.X / 2f;
            floorHalfZ = box.Size.Z / 2f;
        }

        int areaSizeX = ArenaMath.ScaledHalfExtent(floorHalfX, AreaScale);
        int areaSizeZ = ArenaMath.ScaledHalfExtent(floorHalfZ, AreaScale);
        int minThrowHeight = (int)Math.Ceiling(AreaScale * 10f);
        die.GlobalPosition = new Vector3(
            RandomValueInRange(0, areaSizeX / 2, true),
            RandomValueInRange(minThrowHeight, minThrowHeight + 3),
            RandomValueInRange(0, areaSizeZ / 2, true));
        die.LinearVelocity = new Vector3(
            RandomValueInRange(2, 3, true),
            0,
            RandomValueInRange(2, 3, true));
        die.AngularVelocity = new Vector3(
            RandomValueInRange(5, 10, true),
            RandomValueInRange(5, 10, true),
            RandomValueInRange(5, 10, true));
        die.Sleeping = false;
    }

    void OnDieRolled(RollOutcome outcome)
    {
        _session?.Record(outcome);
        Rolled?.Invoke(outcome);
    }

    void OnDieDied(DieBody die)
    {
        _session?.Forget(die.GetInstanceId());
        if (LastSpawned == die)
            LastSpawned = null;
    }

    void OnDieRespawnRequested(DieBody dying)
    {
        Spawn(new DieDefinition
        {
            Hull = dying.Hull,
            Layout = dying.Layout,
        });
    }

    void OnSettingChanged(string key, object _)
    {
        if (key != "gravity")
            return;
        float gravity = _settings?.Get("gravity", 4f) ?? 4f;
        foreach (var die in Dice())
            die.GravityScale = gravity;
    }

    IEnumerable<DieBody> Dice()
    {
        foreach (var child in GetChildren())
        {
            if (child is DieBody die && !die.IsQueuedForDeletion())
                yield return die;
        }
    }

    // Copied from original DiceManager.random_value_in_range: (randi() % (max+1)) + min, even when min > max.
    static int RandomValueInRange(int minimum, int maximum, bool randSign = false)
    {
        int value = (int)(GD.Randi() % (uint)(maximum + 1)) + minimum;
        if (randSign && GD.Randi() % 2 == 1)
            value *= -1;
        return value;
    }
}
