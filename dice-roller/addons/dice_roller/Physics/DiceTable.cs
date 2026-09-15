using System;
using DiceRoller.Core;
using Godot;

#nullable enable

namespace DiceRoller.Physics;

[GlobalClass]
public partial class DiceTable : Node3D, IDiceTable
{
    DiceManager _manager = null!;

    public override void _Ready()
    {
        _manager = GetNode<DiceManager>("DiceManager");
        _manager.Rolled += outcome => Rolled?.Invoke(outcome);
    }

    public int Count => _manager.Count;

    public event Action<RollOutcome>? Rolled;

    public DieBody Spawn(DieDefinition definition) => _manager.Spawn(definition);

    public DieBody SpawnStandard(HullKind hull) => _manager.SpawnStandard(hull);

    public DieBody SpawnStandard(string hullKind) => _manager.SpawnStandard(hullKind);

    public void Clear() => _manager.Clear();

    public void Clear(HullKind hull) => _manager.Clear(hull);

    public void RollAll() => _manager.RollAll();

    public void RollInvalid() => _manager.RollInvalid();

    public void LockValid() => _manager.LockValid();
}
