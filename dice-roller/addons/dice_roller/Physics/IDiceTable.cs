using System;
using DiceRoller.Core;
using DiceRoller.Presentation;

#nullable enable

namespace DiceRoller.Physics;

public interface IDiceTable
{
    DieBody Spawn(DieDefinition definition);
    DieBody Spawn(DieDefinition definition, IFacePresenter presenter);
    DieBody SpawnStandard(HullKind hull);
    DieBody SpawnStandard(string hullKind);
    void Clear();
    void Clear(HullKind hull);
    void RollAll();
    void RollInvalid();
    void LockValid();
    int Count { get; }
    event Action<RollOutcome> Rolled;
}
