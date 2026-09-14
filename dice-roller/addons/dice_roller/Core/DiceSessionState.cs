using System.Collections.Generic;

namespace DiceRoller.Core;

public sealed class DiceSessionState
{
    readonly Dictionary<ulong, RollOutcome> _byInstance = new();

    public int Count => _byInstance.Count;

    public void Record(RollOutcome outcome) => _byInstance[outcome.InstanceId] = outcome;

    public void Forget(ulong instanceId) => _byInstance.Remove(instanceId);

    public bool TryGet(ulong instanceId, out RollOutcome outcome) =>
        _byInstance.TryGetValue(instanceId, out outcome);
}
