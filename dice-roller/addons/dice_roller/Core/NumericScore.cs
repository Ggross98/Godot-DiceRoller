using System.Collections.Generic;

#nullable enable

namespace DiceRoller.Core;

public static class NumericScore
{
    public static int Sum(IEnumerable<RollOutcome> latest)
    {
        int sum = 0;
        foreach (var outcome in latest)
        {
            if (!outcome.IsValid)
                continue;
            if (outcome.Content.NumericValue is int value)
                sum += value;
        }

        return sum;
    }
}
