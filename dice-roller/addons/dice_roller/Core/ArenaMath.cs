using Godot;

#nullable enable

namespace DiceRoller.Core;

public static class ArenaMath
{
    public const float MinScale = 0.15f;
    public const float MaxScale = 1.4f;
    public const float ScaleStep = 0.02f;
    public const float MinCameraDistance = 4f;
    public const float MaxCameraDistance = 60f;

    public static float NextAreaScale(float current, bool sizeUp, float factor = 1f)
    {
        if (sizeUp && current < MaxScale)
            return current + ScaleStep * factor;
        if (current > MinScale)
            return current - ScaleStep * factor;
        return current;
    }

    public static Vector3 ZoomCamera(Vector3 translation, bool zoomIn, float zoomFactor)
    {
        float length = translation.Length();
        if (length == 0f)
            return translation;

        zoomFactor += length / 200f;
        var offset = translation.Normalized() * zoomFactor;
        if (zoomIn)
        {
            if (length > MinCameraDistance)
                return translation - offset;
        }
        else if (length < MaxCameraDistance)
        {
            return translation + offset;
        }

        return translation;
    }

    public static int ScaledHalfExtent(float shapeHalf, float areaScale) =>
        (int)(shapeHalf * areaScale);
}
