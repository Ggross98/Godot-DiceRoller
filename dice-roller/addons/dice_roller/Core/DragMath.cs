using Godot;

#nullable enable

namespace DiceRoller.Core;

public readonly record struct AxisAngle(Vector3 Axis, float Angle);

public static class DragMath
{
    public static Vector3 IntersectYPlane(Vector3 rayOrigin, Vector3 rayDirection, float planeY)
    {
        if (rayDirection.Y == 0f)
            return Vector3.Zero;

        float distance = (-rayOrigin.Y + planeY) / rayDirection.Y;
        return rayOrigin + rayDirection * distance;
    }

    public static Vector3 ClampHorizontalThrow(Vector3 mouseMinusDie, float maxSpeed)
    {
        var throwVec = mouseMinusDie * new Vector3(100f, 0f, 100f);
        float length = throwVec.Length();
        if (length == 0f)
            return Vector3.Zero;
        return Mathf.Clamp(length, 0f, maxSpeed) * throwVec.Normalized();
    }

    public static AxisAngle? RotationToAlignUp(Vector3 sideDirection)
    {
        if (sideDirection.IsEqualApprox(Vector3.Up))
            return null;

        var axis = sideDirection.Cross(Vector3.Up);
        if (axis.LengthSquared() == 0f)
            return null;

        return new AxisAngle(axis.Normalized(), sideDirection.AngleTo(Vector3.Up));
    }

    public static float PutDownOriginY(float originToLowestY, float? hitDieHighestY) =>
        hitDieHighestY is float highest ? originToLowestY + highest : originToLowestY;

    public static float NextDraggingHeight(float current, int minOverlaps, int maxOverlaps, float minHeight)
    {
        if (minOverlaps > 1)
            return current + 0.1f;
        if (maxOverlaps <= 1 && current > minHeight)
            return current - 0.1f;
        return current;
    }

    public static float GrabHeightFromRadius(float radius) => radius / 2f;
}
