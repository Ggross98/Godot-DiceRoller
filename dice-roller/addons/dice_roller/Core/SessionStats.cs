using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace DiceRoller.Core;

public readonly record struct FaceCount(string Id, string Label, int Occurrences);

public sealed class HullBreakdown
{
    public required HullKind Hull { get; init; }
    public int Count { get; init; }
    public int NumericSum { get; init; }
    public int InvalidCount { get; init; }
    public IReadOnlyList<FaceCount> Faces { get; init; } = [];
}

public static class SessionStats
{
    public static IReadOnlyList<HullBreakdown> ByHull(IEnumerable<RollOutcome> outcomes)
    {
        var grouped = new Dictionary<HullKind, List<RollOutcome>>();
        foreach (var outcome in outcomes)
        {
            if (!grouped.TryGetValue(outcome.Hull, out var list))
            {
                list = [];
                grouped[outcome.Hull] = list;
            }

            list.Add(outcome);
        }

        return grouped
            .OrderBy(pair => FaceCountOf(pair.Key))
            .Select(pair => ToBreakdown(pair.Key, pair.Value))
            .ToList();
    }

    static HullBreakdown ToBreakdown(HullKind hull, List<RollOutcome> latest)
    {
        var faces = new Dictionary<string, FaceCount>();
        int invalid = 0;
        foreach (var outcome in latest)
        {
            if (!outcome.IsValid)
            {
                invalid++;
                continue;
            }

            string id = outcome.Content.Id;
            if (faces.TryGetValue(id, out var existing))
                faces[id] = existing with { Occurrences = existing.Occurrences + 1 };
            else
                faces[id] = new FaceCount(id, outcome.Content.Label, 1);
        }

        return new HullBreakdown
        {
            Hull = hull,
            Count = latest.Count,
            NumericSum = NumericScore.Sum(latest),
            InvalidCount = invalid,
            Faces = faces.Values.ToList(),
        };
    }

    static int FaceCountOf(HullKind hull) => hull switch
    {
        HullKind.D4 => 4,
        HullKind.D6 => 6,
        HullKind.D8 => 8,
        HullKind.D10 => 10,
        HullKind.D12 => 12,
        HullKind.D20 => 20,
        _ => int.MaxValue,
    };
}
