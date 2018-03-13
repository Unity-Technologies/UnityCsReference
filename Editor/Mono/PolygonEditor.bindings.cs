// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/PolygonEditor.h")]
    internal partial class PolygonEditor
    {
        [NativeThrows]
        extern public static void StartEditing(Collider2D collider);
        [NativeThrows]
        extern public static void ApplyEditing(Collider2D collider);

        [StaticAccessor("PolygonEditor::Get()", StaticAccessorType.Dot)]
        extern public static void StopEditing();

        [StaticAccessor("PolygonEditor::Get()", StaticAccessorType.Dot)]
        extern public static bool GetNearestPoint(Vector2 point, out int pathIndex, out int pointIndex, out float distance);

        [StaticAccessor("PolygonEditor::Get()", StaticAccessorType.Dot)]
        extern public static bool GetNearestEdge(Vector2 point, out int pathIndex, out int pointIndex0, out int pointIndex1, out float distance, bool loop);

        [StaticAccessor("PolygonEditor::Get()", StaticAccessorType.Dot)]
        extern public static int GetPathCount();

        [StaticAccessor("PolygonEditor::Get()", StaticAccessorType.Dot)]
        extern public static int GetPointCount(int pathIndex);

        [StaticAccessor("PolygonEditor::Get()", StaticAccessorType.Dot)]
        extern public static bool GetPoint(int pathIndex, int pointIndex, out Vector2 point);

        [StaticAccessor("PolygonEditor::Get()", StaticAccessorType.Dot)]
        extern public static void SetPoint(int pathIndex, int pointIndex, Vector2 value);

        [StaticAccessor("PolygonEditor::Get()", StaticAccessorType.Dot)]
        extern public static void InsertPoint(int pathIndex, int pointIndex, Vector2 value);

        [StaticAccessor("PolygonEditor::Get()", StaticAccessorType.Dot)]
        extern public static void RemovePoint(int pathIndex, int pointIndex);

        [StaticAccessor("PolygonEditor::Get()", StaticAccessorType.Dot)]
        extern public static void TestPointMove(int pathIndex, int pointIndex, Vector2 movePosition, out bool leftIntersect, out bool rightIntersect, bool loop);
    }
}
