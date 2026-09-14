using System;
using DiceRoller.Core;
using Godot;

#nullable enable

namespace DiceRoller.Session;

public partial class DiceSession : Node
{
    public DiceSessionState State { get; } = new();

    public event Action<RollOutcome>? Recorded;

    public int Count => State.Count;

    public void Record(RollOutcome outcome)
    {
        State.Record(outcome);
        Recorded?.Invoke(outcome);
    }

    public void Forget(ulong instanceId) => State.Forget(instanceId);

    public bool TryGet(ulong instanceId, out RollOutcome outcome) =>
        State.TryGet(instanceId, out outcome);
}
