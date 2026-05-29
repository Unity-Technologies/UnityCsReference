// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [Flags]
    public enum DrivenTransformProperties
    {
        None = 0,
        All = ~None,
        AnchoredPositionX = 1 << 1,
        AnchoredPositionY = 1 << 2,

        AnchoredPositionZ = 1 << 3,
        Rotation = 1 << 4,
        ScaleX = 1 << 5,
        ScaleY = 1 << 6,
        ScaleZ = 1 << 7,
        AnchorMinX = 1 << 8,
        AnchorMinY = 1 << 9,
        AnchorMaxX = 1 << 10,
        AnchorMaxY = 1 << 11,
        SizeDeltaX = 1 << 12,
        SizeDeltaY = 1 << 13,
        PivotX = 1 << 14,
        PivotY = 1 << 15,

        AnchoredPosition = AnchoredPositionX | AnchoredPositionY,

        AnchoredPosition3D = AnchoredPositionX | AnchoredPositionY | AnchoredPositionZ,
        Scale = ScaleX | ScaleY | ScaleZ,
        AnchorMin = AnchorMinX | AnchorMinY,
        AnchorMax = AnchorMaxX | AnchorMaxY,
        Anchors = AnchorMin | AnchorMax,
        SizeDelta = SizeDeltaX | SizeDeltaY,
        Pivot = PivotX | PivotY
    }

    [NativeHeader("Editor/Src/Animation/AnimationModeSnapshot.h")]
    [NativeHeader("Editor/Src/Undo/PropertyUndoManager.h")]
    public struct DrivenRectTransformTracker
    {
        private List<RectTransform> m_Tracked;

        internal static bool CanRecordModifications()
        {
            // The DrivenRectTransformTracker should not record undo by itself but always
            // as part of another undoable action. We therefore prevent recording undo if there
            // is no undo recordings yet. This fixes many situations where the main scene is
            // marked as dirty without user interaction as a side effect the Add() and Clear()
            // below being called either when rebuilding auto layouted during ui rendering or when
            // layoutgroup is being disabled when merging Prefab instances. Fixes case 1268783.
            return !IsInAnimationMode() && (IsUndoingOrRedoing() || HasUndoRecordObjects());
        }

        [FreeFunction("GetAnimationModeSnapshot().IsInAnimationMode")]
        static extern bool IsInAnimationMode();

        [FreeFunction("GetPropertyUndoManager().HasRecordings")]
        static extern bool HasUndoRecordObjects();

        [FreeFunction("GetPropertyUndoManager().IsUndoingOrRedoing")]
        static extern bool IsUndoingOrRedoing();

        private static bool s_BlockUndo;

        public static void StopRecordingUndo() { s_BlockUndo = true; }

        public static void StartRecordingUndo() { s_BlockUndo = false; }

        public void Add(Object driver, RectTransform rectTransform, DrivenTransformProperties drivenProperties)
        {
            if (m_Tracked == null)
                m_Tracked = new List<RectTransform>();

            if (!Application.isPlaying && CanRecordModifications() && !s_BlockUndo)
                RuntimeUndo.RecordObject(rectTransform, "Driving RectTransform");

            // Ensure the driven properties are cleared if the driver is different.
            if (rectTransform.drivenByObject != driver)
                rectTransform.drivenProperties = DrivenTransformProperties.None;

            rectTransform.drivenByObject = driver;
            rectTransform.drivenProperties = rectTransform.drivenProperties | drivenProperties;

            m_Tracked.Add(rectTransform);
        }

        [Obsolete("revertValues parameter is ignored. Please use Clear() instead.")]
        public void Clear(bool revertValues)
        {
            Clear();
        }

        public void Clear()
        {
            if (m_Tracked != null)
            {
                for (int i = 0; i < m_Tracked.Count; i++)
                {
                    if (m_Tracked[i] != null)
                    {
                        if (!Application.isPlaying && CanRecordModifications() && !s_BlockUndo)
                            RuntimeUndo.RecordObject(m_Tracked[i], "Driving RectTransform");

                        m_Tracked[i].drivenByObject = null;
                        m_Tracked[i].drivenProperties = DrivenTransformProperties.None;
                    }
                }
                m_Tracked.Clear();
            }
        }
    }

    [NativeHeader("Runtime/Transform/RectTransform.h"),
     NativeClass("UI::RectTransform")]
    [UIModuleHelpURL("class-RectTransform")]
    public sealed class RectTransform : Transform
    {
        public enum Edge { Left = 0, Right = 1, Top = 2, Bottom = 3 }
        public enum Axis { Horizontal = 0, Vertical = 1 }
        public enum FitResult
        {
            Success = 0,
            AlreadyInside = 1,
            FailLargerThanTarget = 2,
            FailNotCoplanar = 3,
            FailZRotationMismatch = 4,
            FailInvalidSizeTarget = 5,
        }

        public delegate void ReapplyDrivenProperties(RectTransform driven);
        public static event ReapplyDrivenProperties reapplyDrivenProperties;

        public extern Rect rect { get; }
        public extern Vector2 anchorMin { get; set; }
        public extern Vector2 anchorMax { get; set; }
        public extern Vector2 anchoredPosition { get; set; }
        public extern Vector2 sizeDelta { get; set; }
        public extern Vector2 pivot { get; set; }

        public Vector3 anchoredPosition3D
        {
            get
            {
                Vector2 pos2 = anchoredPosition;
                return new Vector3(pos2.x, pos2.y, localPosition.z);
            }
            set
            {
                anchoredPosition = new Vector2(value.x, value.y);
                Vector3 pos3 = localPosition;
                pos3.z = value.z;
                localPosition = pos3;
            }
        }

        public Vector2 offsetMin
        {
            get
            {
                return anchoredPosition - Vector2.Scale(sizeDelta, pivot);
            }
            set
            {
                Vector2 offset = value - (anchoredPosition - Vector2.Scale(sizeDelta, pivot));
                sizeDelta -= offset;
                anchoredPosition += Vector2.Scale(offset, Vector2.one - pivot);
            }
        }

        public Vector2 offsetMax
        {
            get
            {
                return anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot);
            }
            set
            {
                Vector2 offset = value - (anchoredPosition + Vector2.Scale(sizeDelta, Vector2.one - pivot));
                sizeDelta += offset;
                anchoredPosition += Vector2.Scale(offset, pivot);
            }
        }

        extern public Object drivenByObject { get; internal set; }
        extern internal DrivenTransformProperties drivenProperties { get; set; }
        extern public bool sendChildDimensionsChange { get; set; }

        [NativeMethod("UpdateIfTransformDispatchIsDirty")] public extern void ForceUpdateRectTransforms();

        public void GetLocalCorners(Vector3[] fourCornersArray)
        {
            if (fourCornersArray == null || fourCornersArray.Length < 4)
            {
                Debug.LogError("Calling GetLocalCorners with an array that is null or has less than 4 elements.");
                return;
            }

            Rect tmpRect = rect;
            float x0 = tmpRect.x;
            float y0 = tmpRect.y;
            float x1 = tmpRect.xMax;
            float y1 = tmpRect.yMax;

            fourCornersArray[0] = new Vector3(x0, y0, 0f);
            fourCornersArray[1] = new Vector3(x0, y1, 0f);
            fourCornersArray[2] = new Vector3(x1, y1, 0f);
            fourCornersArray[3] = new Vector3(x1, y0, 0f);
        }

        public void GetLocalCorners(Span<Vector3> fourCorners)
        {
            if (fourCorners.Length < 4)
            {
                Debug.LogError("Calling GetLocalCorners with a Span<Vector3> that has less than 4 elements.");
                return;
            }

            Rect tmpRect = rect;
            float x0 = tmpRect.x;
            float y0 = tmpRect.y;
            float x1 = tmpRect.xMax;
            float y1 = tmpRect.yMax;

            fourCorners[0] = new Vector3(x0, y0, 0f);
            fourCorners[1] = new Vector3(x0, y1, 0f);
            fourCorners[2] = new Vector3(x1, y1, 0f);
            fourCorners[3] = new Vector3(x1, y0, 0f);
        }

        public void GetWorldCorners(Vector3[] fourCornersArray)
        {
            if (fourCornersArray == null || fourCornersArray.Length < 4)
            {
                Debug.LogError("Calling GetWorldCorners with an array that is null or has less than 4 elements.");
                return;
            }

            GetLocalCorners(fourCornersArray);

            Matrix4x4 mat = localToWorldMatrix;
            for (int i = 0; i < 4; i++)
                fourCornersArray[i] = mat.MultiplyPoint(fourCornersArray[i]);
        }

        public void GetWorldCorners(Span<Vector3> fourCorners)
        {
            if (fourCorners.Length < 4)
            {
                Debug.LogError("Calling GetWorldCorners with Span<Vector3> that has less than 4 elements.");
                return;
            }

            Rect r = rect;
            Matrix4x4 m = localToWorldMatrix;
            fourCorners[0] = m.MultiplyPoint(new Vector3(r.xMin, r.yMin));
            fourCorners[1] = m.MultiplyPoint(new Vector3(r.xMin, r.yMax));
            fourCorners[2] = m.MultiplyPoint(new Vector3(r.xMax, r.yMax));
            fourCorners[3] = m.MultiplyPoint(new Vector3(r.xMax, r.yMin));
        }

        public Rect GetWorldRect()
        {
            Span<Vector3> c = stackalloc Vector3[4];
            GetWorldCorners(c);

            Vector3 min = Vector3.Min(Vector3.Min(c[0], c[1]), Vector3.Min(c[2], c[3]));
            Vector3 max = Vector3.Max(Vector3.Max(c[0], c[1]), Vector3.Max(c[2], c[3]));

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        public bool Contains(RectTransform other)
        {
            Rect worldRect = GetWorldRect();
            Rect otherRect = other.GetWorldRect();
            return worldRect.xMin <= otherRect.xMin
             && worldRect.xMax >= otherRect.xMax
             && worldRect.yMin <= otherRect.yMin
             && worldRect.yMax >= otherRect.yMax;
        }

        public float GetLocalTop() => GetRectInParentSpace().yMax;
        public float GetLocalBottom() => GetRectInParentSpace().y;
        public float GetLocalLeft() => GetRectInParentSpace().x;
        public float GetLocalRight() => GetRectInParentSpace().xMax;

        public void SetLocalTop(float value, bool preserveSize = true)
        {
            if (preserveSize)
            {
                anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y + (value - GetLocalTop()));
            }
            else
            {
                float delta = value - GetLocalTop();
                sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y + delta);
                anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y + pivot.y * delta);
            }
        }

        public void SetLocalBottom(float value, bool preserveSize = true)
        {
            if (preserveSize)
            {
                anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y + (value - GetLocalBottom()));
            }
            else
            {
                float delta = value - GetLocalBottom();
                sizeDelta = new Vector2(sizeDelta.x, sizeDelta.y - delta);
                anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y + (1f - pivot.y) * delta);
            }
        }

        public void SetLocalLeft(float value, bool preserveSize = true)
        {
            if (preserveSize)
            {
                anchoredPosition = new Vector2(anchoredPosition.x + (value - GetLocalLeft()), anchoredPosition.y);
            }
            else
            {
                float delta = value - GetLocalLeft();
                sizeDelta = new Vector2(sizeDelta.x - delta, sizeDelta.y);
                anchoredPosition = new Vector2(anchoredPosition.x + (1f - pivot.x) * delta, anchoredPosition.y);
            }
        }

        public void SetLocalRight(float value, bool preserveSize = true)
        {
            if (preserveSize)
            {
                anchoredPosition = new Vector2(anchoredPosition.x + (value - GetLocalRight()), anchoredPosition.y);
            }
            else
            {
                float delta = value - GetLocalRight();
                sizeDelta = new Vector2(sizeDelta.x + delta, sizeDelta.y);
                anchoredPosition = new Vector2(anchoredPosition.x + pivot.x * delta, anchoredPosition.y);
            }
        }

        public void SetAnchors(Vector2 position)
        {
            anchorMin = position;
            anchorMax = position;
        }

        public void SetPivotAndAnchors(Vector2 position)
        {
            pivot = position;
            anchorMin = position;
            anchorMax = position;
        }

        public void SetInsetAndSizeFromParentEdge(Edge edge, float inset, float size)
        {
            int axis = (edge == Edge.Top || edge == Edge.Bottom) ? 1 : 0;
            bool end = (edge == Edge.Top || edge == Edge.Right);

            // Set anchorMin and anchorMax to be anchored to the chosen edge.
            float anchorValue = end ? 1 : 0;
            Vector2 anchor = anchorMin;
            anchor[axis] = anchorValue;
            anchorMin = anchor;
            anchor = anchorMax;
            anchor[axis] = anchorValue;
            anchorMax = anchor;

            // Set size. Since anchors are together, size and sizeDelta means the same in this case.
            Vector2 sizeD = sizeDelta;
            sizeD[axis] = size;
            sizeDelta = sizeD;

            // Set inset.
            Vector2 positionCopy = anchoredPosition;
            positionCopy[axis] = end ? -inset - size * (1 - pivot[axis]) : inset + size * pivot[axis];
            anchoredPosition = positionCopy;
        }

        public void SetSizeWithCurrentAnchors(Axis axis, float size)
        {
            int i = (int)axis;
            Vector2 sizeD = sizeDelta;
            sizeD[i] = size - GetParentSize()[i] * (anchorMax[i] - anchorMin[i]);
            sizeDelta = sizeD;
        }

        [NativeMethod("SetPivotWithCounterAdjust")]
        private extern void Internal_SetPivotWithCounterAdjust(Vector2 newPivot, bool adjustChildren);

        public void SetPivotWithCounterAdjust(Vector2 newPivot, bool adjustChildren = false)
        {
            Internal_SetPivotWithCounterAdjust(newPivot, adjustChildren);
        }

        [RequiredByNativeCode]
        internal static void SendReapplyDrivenProperties(RectTransform driven)
        {
            reapplyDrivenProperties?.Invoke(driven);
        }

        // Return rect relative to lower left corner of parent rect
        internal Rect GetRectInParentSpace()
        {
            Rect rectResult = rect;
            Vector2 offset = offsetMin + Vector2.Scale(pivot, rectResult.size);
            RectTransform parentRectTransform = parent as RectTransform;
            if (parentRectTransform)
            {
                offset += Vector2.Scale(anchorMin, parentRectTransform.rect.size);
            }

            rectResult.x += offset.x;
            rectResult.y += offset.y;
            return rectResult;
        }

        private Vector2 GetParentSize()
        {
            RectTransform parentRect = parent as RectTransform;
            if (!parentRect)
                return Vector2.zero;
            return parentRect.rect.size;
        }

        public bool IsCoplanarWith(RectTransform target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            return Internal_IsCoplanarWith(target);
        }

        [NativeMethod("IsCoplanarWith")]
        private extern bool Internal_IsCoplanarWith(RectTransform target);

        public FitResult FitInsideCoplanarRectTransform(RectTransform target, bool allowShrink = false)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            return Internal_FitInsideCoplanarRectTransform(target, allowShrink);
        }

        [NativeMethod("FitInsideCoplanarRectTransform")]
        private extern FitResult Internal_FitInsideCoplanarRectTransform(RectTransform target, bool allowShrink);

        public FitResult TryFitInsideCoplanarRectTransform(RectTransform target, bool allowShrink = false)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            return Internal_TryFitInsideCoplanarRectTransform(target, allowShrink);
        }

        [NativeMethod("TryFitInsideCoplanarRectTransform")]
        private extern FitResult Internal_TryFitInsideCoplanarRectTransform(RectTransform target, bool allowShrink);
    }
}
