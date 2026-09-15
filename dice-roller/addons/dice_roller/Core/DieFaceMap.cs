using System;
using System.Collections.Generic;
using Godot;

namespace DiceRoller.Core;

public readonly record struct UpSlotResult(bool IsValid, FaceSlotId Slot);

public sealed class DieFaceMap
{
    public HullKind Hull { get; }
    public float MaxSideHeightDifference { get; }
    public IReadOnlyDictionary<FaceSlotId, Vector3> Samples { get; }

    public Vector3 this[FaceSlotId slot] => Samples[slot];

    DieFaceMap(HullKind hull, float maxSideHeightDifference, Dictionary<FaceSlotId, Vector3> samples)
    {
        Hull = hull;
        MaxSideHeightDifference = maxSideHeightDifference;
        Samples = samples;
    }

    public static DieFaceMap For(HullKind hull) => hull switch
    {
        HullKind.D4 => D4,
        HullKind.D6 => D6,
        HullKind.D8 => D8,
        HullKind.D10 => D10,
        HullKind.D12 => D12,
        HullKind.D20 => D20,
        _ => throw new ArgumentOutOfRangeException(nameof(hull), hull, "Unknown hull."),
    };

    public UpSlotResult ReadUpSlot(Transform3D transform)
    {
        float highestY = float.NegativeInfinity;
        var highest = FaceSlotId.None;
        var heights = new List<float>(Samples.Count);
        foreach (var (slot, local) in Samples)
        {
            float height = (transform * local).Y;
            heights.Add(height);
            if (height > highestY)
            {
                highestY = height;
                highest = slot;
            }
        }

        heights.Sort();
        if (heights[^1] - heights[^2] < MaxSideHeightDifference)
            return new UpSlotResult(false, FaceSlotId.None);

        return new UpSlotResult(true, highest);
    }

    static FaceSlotId S(int value) => new(value);

    // Sample vectors copied verbatim from godot/dice/scripts/dN.gd. Do not normalize.
    static DieFaceMap D4 { get; } = new(
        HullKind.D4,
        0.6f,
        new Dictionary<FaceSlotId, Vector3>
        {
            [S(1)] = -new Vector3(0.471405f, 0.333333f, 0.816496f),
            [S(2)] = -new Vector3(0.471404f, 0.333333f, -0.816497f),
            [S(3)] = -new Vector3(-0.942809f, 0.333334f, -0f),
            [S(4)] = -new Vector3(0f, -1f, 0f),
        });

    static DieFaceMap D6 { get; } = new(
        HullKind.D6,
        0.4f,
        new Dictionary<FaceSlotId, Vector3>
        {
            [S(1)] = Vector3.Down,
            [S(2)] = Vector3.Back,
            [S(3)] = Vector3.Left,
            [S(4)] = Vector3.Right,
            [S(5)] = Vector3.Forward,
            [S(6)] = Vector3.Up,
        });

    static DieFaceMap D8 { get; } = new(
        HullKind.D8,
        0.3f,
        new Dictionary<FaceSlotId, Vector3>
        {
            [S(1)] = new Vector3(0.57735f, -0.57735f, 0.57735f),
            [S(2)] = new Vector3(-0.57735f, 0.57735f, 0.57735f),
            [S(3)] = new Vector3(-0.57735f, -0.57735f, 0.57735f),
            [S(4)] = new Vector3(0.57735f, 0.57735f, 0.57735f),
            [S(5)] = new Vector3(-0.57735f, -0.57735f, -0.57735f),
            [S(6)] = new Vector3(0.57735f, 0.57735f, -0.57735f),
            [S(7)] = new Vector3(0.57735f, -0.57735f, -0.57735f),
            [S(8)] = new Vector3(-0.57735f, 0.57735f, -0.57735f),
        });

    static DieFaceMap D10 { get; } = new(
        HullKind.D10,
        0.1f,
        new Dictionary<FaceSlotId, Vector3>
        {
            [S(1)] = new Vector3(0.772135f, -0.583838f, -0.250882f),
            [S(2)] = new Vector3(-0.477205f, 0.583838f, -0.656817f),
            [S(3)] = new Vector3(-0.477206f, -0.583837f, 0.656817f),
            [S(4)] = new Vector3(0.772135f, 0.583838f, 0.250882f),
            [S(5)] = new Vector3(-0.772135f, -0.583837f, -0.250882f),
            [S(6)] = new Vector3(0.477205f, 0.583838f, -0.656817f),
            [S(7)] = new Vector3(0.477206f, -0.583837f, 0.656817f),
            [S(8)] = new Vector3(-0.772135f, 0.583837f, 0.250882f),
            [S(9)] = new Vector3(-0f, -0.583837f, -0.811871f),
            [S(10)] = new Vector3(-0f, 0.583837f, 0.811871f),
        });

    static DieFaceMap D12 { get; } = new(
        HullKind.D12,
        0.15f,
        new Dictionary<FaceSlotId, Vector3>
        {
            [S(1)] = new Vector3(-0.525731f, -0.850651f, 0f),
            [S(2)] = new Vector3(0f, -0.525731f, 0.850651f),
            [S(3)] = new Vector3(-0.850651f, 0f, 0.525731f),
            [S(4)] = new Vector3(0.525731f, -0.850651f, 0f),
            [S(5)] = new Vector3(-0.850651f, 0f, -0.525731f),
            [S(6)] = new Vector3(0f, -0.525731f, -0.850651f),
            [S(7)] = new Vector3(0f, 0.525731f, 0.850651f),
            [S(8)] = new Vector3(0.850651f, 0f, 0.525731f),
            [S(9)] = new Vector3(-0.525731f, 0.850651f, 0f),
            [S(10)] = new Vector3(0.850651f, 0f, -0.525731f),
            [S(11)] = new Vector3(0f, 0.525731f, -0.850651f),
            [S(12)] = new Vector3(0.525731f, 0.850651f, 0f),
        });

    static DieFaceMap D20 { get; } = new(
        HullKind.D20,
        0.15f,
        new Dictionary<FaceSlotId, Vector3>
        {
            [S(1)] = new Vector3(0f, -0.934172f, -0.356822f),
            [S(2)] = new Vector3(-0.57735f, 0.57735f, 0.57735f),
            [S(3)] = new Vector3(0.934172f, -0.356822f, 0f),
            [S(4)] = new Vector3(-0.57735f, 0.57735f, -0.57735f),
            [S(5)] = new Vector3(-0.934172f, -0.356822f, 0f),
            [S(6)] = new Vector3(0.57735f, 0.57735f, -0.57735f),
            [S(7)] = new Vector3(0f, -0.934172f, 0.356822f),
            [S(8)] = new Vector3(0.57735f, 0.57735f, 0.57735f),
            [S(9)] = new Vector3(0.356822f, 0f, -0.934172f),
            [S(10)] = new Vector3(0.356822f, 0f, 0.934172f),
            [S(11)] = new Vector3(-0.356822f, 0f, -0.934172f),
            [S(12)] = new Vector3(-0.356822f, 0f, 0.934172f),
            [S(13)] = new Vector3(-0.57735f, -0.57735f, -0.57735f),
            [S(14)] = new Vector3(0f, 0.934172f, -0.356822f),
            [S(15)] = new Vector3(-0.57735f, -0.57735f, 0.57735f),
            [S(16)] = new Vector3(0.934172f, 0.356822f, 0f),
            [S(17)] = new Vector3(0.57735f, -0.57735f, 0.57735f),
            [S(18)] = new Vector3(-0.934172f, 0.356822f, 0f),
            [S(19)] = new Vector3(0.57735f, -0.57735f, -0.57735f),
            [S(20)] = new Vector3(0f, 0.934172f, 0.356822f),
        });
}
