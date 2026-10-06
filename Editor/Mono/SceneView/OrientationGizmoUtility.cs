// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

// View-direction math shared by OrientationGizmoElement and the Scene View orientation shortcuts.
static class OrientationGizmoUtility
{
    public const int PositiveAxisCount = 3;

    public static readonly Vector3[] AxisDirections =
    {
        Vector3.right, Vector3.up, Vector3.forward,
        Vector3.left, Vector3.down, Vector3.back
    };

    public static Vector3 ProjectAxisToCameraSpace(Vector3 worldAxis, Quaternion cameraRotation)
    {
        return Quaternion.Inverse(cameraRotation) * worldAxis;
    }

    public static Vector2 AxisScreenPosition(Vector3 worldAxis, Quaternion cameraRotation, Vector2 center, float radius)
    {
        var local = ProjectAxisToCameraSpace(worldAxis, cameraRotation);
        return center + new Vector2(local.x, -local.y) * radius;
    }

    public static float AxisDepth(Vector3 worldAxis, Quaternion cameraRotation)
    {
        return ProjectAxisToCameraSpace(worldAxis, cameraRotation).z;
    }

    public static Quaternion RotationForAxis(Vector3 worldAxis)
    {
        return Quaternion.LookRotation(-worldAxis);
    }

    // Keeps the current heading in the x-z plane, seen partway from above, so this never looks
    // like a no-op when the view was already axis-aligned.
    public static Quaternion NiceAngleRotation(Quaternion currentRotation)
    {
        var dir = currentRotation * Vector3.forward;
        dir.y = 0f;
        dir = dir == Vector3.zero ? Vector3.forward : dir.normalized;
        dir.y = -0.5f;
        return Quaternion.LookRotation(dir);
    }

    public static bool IsAxisAligned(Vector3 worldAxis, Quaternion cameraRotation, float dotThreshold = 0.999f)
    {
        var forward = cameraRotation * Vector3.forward;
        return Vector3.Dot(forward, -worldAxis) > dotThreshold;
    }

    public static int AlignedAxisIndex(Quaternion cameraRotation, float dotThreshold = 0.999f)
    {
        for (var i = 0; i < AxisDirections.Length; i++)
        {
            if (IsAxisAligned(AxisDirections[i], cameraRotation, dotThreshold))
                return i;
        }

        return -1;
    }

    public static float BehindCenterFadeAlpha(float depth, float fadeStartDepth = 0.3f)
    {
        if (depth <= fadeStartDepth)
            return 1f;

        return Mathf.Clamp01(1f - (depth - fadeStartDepth) / (1f - fadeStartDepth));
    }

    public static int PickNearestAxis(Vector2 localPoint, Vector2[] axisScreenPositions, float hitRadius)
    {
        var best = -1;
        var bestSqrDistance = hitRadius * hitRadius;
        for (var i = 0; i < axisScreenPositions.Length; i++)
        {
            var sqrDistance = (axisScreenPositions[i] - localPoint).sqrMagnitude;
            if (sqrDistance <= bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                best = i;
            }
        }
        return best;
    }

    // Picks the axis closest to a "goal" vector built from the swipe and aimed partway back toward
    // the camera, favoring the nearer axis over the one 90 degrees away.
    public static int AxisForSwipe(Vector2 delta, Quaternion cameraRotation)
    {
        var goalVector = cameraRotation * (-SwipeDirection(delta) - Vector3.forward * 0.9f);

        var best = 0;
        var bestDot = 0f;
        for (var i = 0; i < AxisDirections.Length; i++)
        {
            var dot = Vector3.Dot(AxisDirections[i], goalVector);
            if (dot > bestDot)
            {
                bestDot = dot;
                best = i;
            }
        }
        return best;
    }

    // Inverted so a swipe down looks from above, consistent with orbit controls.
    static Vector3 SwipeDirection(Vector2 delta)
    {
        if (delta.y > 0f)
            return Vector3.up;
        if (delta.y < 0f)
            return -Vector3.up;

        return delta.x < 0f ? Vector3.right : -Vector3.right;
    }
}
