// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    internal class TransformManipulator
    {
        private struct TransformData
        {
            public static Quaternion[] s_Alignments = new Quaternion[]
            {
                Quaternion.LookRotation(Vector3.right, Vector3.up),
                Quaternion.LookRotation(Vector3.right, Vector3.forward),
                Quaternion.LookRotation(Vector3.up, Vector3.forward),
                Quaternion.LookRotation(Vector3.up, Vector3.right),
                Quaternion.LookRotation(Vector3.forward, Vector3.right),
                Quaternion.LookRotation(Vector3.forward, Vector3.up)
            };

            public Transform transform;
            public Vector3 position;
            public Vector3 localPosition;
            public Quaternion rotation;
            public Vector3 scale;
            public RectTransform rectTransform;
            public Rect rect;
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;

            public static TransformData GetData(Transform t)
            {
                TransformData data = new TransformData();
                data.SetupTransformValues(t);

                data.rectTransform = t.GetComponent<RectTransform>();
                if (data.rectTransform != null)
                {
                    data.sizeDelta = data.rectTransform.sizeDelta;
                    data.rect = data.rectTransform.rect;
                    data.anchoredPosition = data.rectTransform.anchoredPosition;
                }
                else
                {
                    SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.drawMode != SpriteDrawMode.Simple)
                        data.sizeDelta = sr.size;
                }
                return data;
            }

            private Quaternion GetRefAlignment(Quaternion targetRotation, Quaternion ownRotation)
            {
                float biggestDot = Mathf.NegativeInfinity;
                Quaternion refAlignment = Quaternion.identity;
                for (int i = 0; i < s_Alignments.Length; i++)
                {
                    float dot = Mathf.Min(
                            Mathf.Abs(Vector3.Dot(targetRotation * Vector3.right, ownRotation * s_Alignments[i] * Vector3.right)),
                            Mathf.Abs(Vector3.Dot(targetRotation * Vector3.up, ownRotation * s_Alignments[i] * Vector3.up)),
                            Mathf.Abs(Vector3.Dot(targetRotation * Vector3.forward, ownRotation * s_Alignments[i] * Vector3.forward))
                            );
                    if (dot > biggestDot)
                    {
                        biggestDot = dot;
                        refAlignment = s_Alignments[i];
                    }
                }
                return refAlignment;
            }

            private void SetupTransformValues(Transform t)
            {
                transform = t;
                position = t.position;
                localPosition = t.localPosition;
                rotation = t.rotation;
                scale = t.localScale;
            }

            private void SetScaleValue(Vector3 scale)
            {
                transform.localScale = scale;
            }

            public void SetScaleDelta(Vector3 scaleDelta, Vector3 scalePivot, Quaternion scaleRotation, bool preferRectResize)
            {
                SetPosition(scaleRotation * Vector3.Scale(Quaternion.Inverse(scaleRotation) * (position - scalePivot), scaleDelta) + scalePivot);

                Vector3 minDifference = ManipulationToolUtility.minDragDifference;
                if (transform.parent != null)
                {
                    minDifference.x /= transform.parent.lossyScale.x;
                    minDifference.y /= transform.parent.lossyScale.y;
                    minDifference.z /= transform.parent.lossyScale.z;
                }

                Quaternion ownRotation = (Tools.rectBlueprintMode && UnityEditorInternal.InternalEditorUtility.SupportsRectLayout(transform) ? transform.parent.rotation : rotation);
                Quaternion refAlignment = GetRefAlignment(scaleRotation, ownRotation);
                scaleDelta = refAlignment * scaleDelta;
                scaleDelta = Vector3.Scale(scaleDelta, refAlignment * Vector3.one);

                if (preferRectResize)
                {
                    if (rectTransform != null)
                    {
                        Vector2 newSizeDelta = sizeDelta + Vector2.Scale(rect.size, scaleDelta) - rect.size;
                        newSizeDelta.x = MathUtils.RoundBasedOnMinimumDifference(newSizeDelta.x, minDifference.x);
                        newSizeDelta.y = MathUtils.RoundBasedOnMinimumDifference(newSizeDelta.y, minDifference.y);
                        rectTransform.sizeDelta = newSizeDelta;
                        if (rectTransform.drivenByObject != null)
                            RectTransform.SendReapplyDrivenProperties(rectTransform);
                        return;
                    }
                    else
                    {
                        SpriteRenderer sr = transform.GetComponent<SpriteRenderer>();
                        if (sr != null && sr.drawMode != SpriteDrawMode.Simple)
                        {
                            sr.size = Vector2.Scale(sizeDelta, scaleDelta);
                            return;
                        }
                    }
                }

                SetScaleValue(Vector3.Scale(scale, scaleDelta));
            }

            private void SetPosition(Vector3 newPosition)
            {
                SetPositionDelta(newPosition - position, true);
            }

            public void SetPositionDelta(Vector3 positionDelta, bool applySmartRounding)
            {
                Vector3 localPositionDelta = positionDelta;
                Vector3 minDifference = ManipulationToolUtility.minDragDifference;
                if (transform.parent != null)
                {
                    localPositionDelta = transform.parent.InverseTransformVector(localPositionDelta);
                    minDifference.x /= transform.parent.lossyScale.x;
                    minDifference.y /= transform.parent.lossyScale.y;
                    minDifference.z /= transform.parent.lossyScale.z;
                }

                // For zero delta, we don't want to change the value so we ignore rounding
                bool zeroXDelta = Mathf.Approximately(localPositionDelta.x, 0f);
                bool zeroYDelta = Mathf.Approximately(localPositionDelta.y, 0f);
                bool zeroZDelta = Mathf.Approximately(localPositionDelta.z, 0f);

                if (rectTransform == null)
                {
                    Vector3 newLocalPosition = localPosition + localPositionDelta;

                    if (applySmartRounding)
                    {
                        newLocalPosition.x = zeroXDelta ? localPosition.x : MathUtils.RoundBasedOnMinimumDifference(newLocalPosition.x, minDifference.x);
                        newLocalPosition.y = zeroYDelta ? localPosition.y : MathUtils.RoundBasedOnMinimumDifference(newLocalPosition.y, minDifference.y);
                        newLocalPosition.z = zeroZDelta ? localPosition.z : MathUtils.RoundBasedOnMinimumDifference(newLocalPosition.z, minDifference.z);
                    }

                    transform.localPosition = newLocalPosition;
                }
                else
                {
                    // Set position.z
                    Vector3 newLocalPosition = localPosition + localPositionDelta;

                    if (applySmartRounding)
                        newLocalPosition.z = zeroZDelta ? localPosition.z : MathUtils.RoundBasedOnMinimumDifference(newLocalPosition.z, minDifference.z);

                    transform.localPosition = newLocalPosition;

                    // Set anchoredPosition
                    Vector2 newAnchoredPosition = anchoredPosition + (Vector2)localPositionDelta;

                    if (applySmartRounding)
                    {
                        newAnchoredPosition.x = zeroXDelta ? anchoredPosition.x : MathUtils.RoundBasedOnMinimumDifference(newAnchoredPosition.x, minDifference.x);
                        newAnchoredPosition.y = zeroYDelta ? anchoredPosition.y : MathUtils.RoundBasedOnMinimumDifference(newAnchoredPosition.y, minDifference.y);
                    }

                    rectTransform.anchoredPosition = newAnchoredPosition;

                    if (rectTransform.drivenByObject != null)
                        RectTransform.SendReapplyDrivenProperties(rectTransform);
                }
            }

            public void DebugAlignment(Quaternion targetRotation)
            {
                Quaternion refAlignment = Quaternion.identity;
                if (!TransformManipulator.individualSpace)
                    refAlignment = GetRefAlignment(targetRotation, rotation);

                Vector3 dir;
                Vector3 pos = transform.position;
                float size = HandleUtility.GetHandleSize(pos) * 0.25f;
                Color oldColor = Handles.color;

                Handles.color = Color.red;
                dir = rotation * refAlignment * Vector3.right * size;
                Handles.DrawLine(pos - dir, pos + dir);

                Handles.color = Color.green;
                dir = rotation * refAlignment * Vector3.up * size;
                Handles.DrawLine(pos - dir, pos + dir);

                Handles.color = Color.blue;
                dir = rotation * refAlignment * Vector3.forward * size;
                Handles.DrawLine(pos - dir, pos + dir);

                Handles.color = oldColor;
            }
        }

        static EventType s_EventTypeBefore = EventType.Ignore;
        static TransformData[] s_MouseDownState = null;
        static Vector3 s_StartHandlePosition = Vector3.zero;
        public static Vector3 mouseDownHandlePosition { get { return s_StartHandlePosition; } }
        static Vector3 s_StartLocalHandleOffset = Vector3.zero;
        static int s_HotControl = 0;
        static bool s_LockHandle = false;

        public static bool active { get { return s_MouseDownState != null; } }

        public static bool individualSpace { get { return Tools.pivotRotation == PivotRotation.Local && Tools.pivotMode == PivotMode.Pivot; } }

        private static void BeginEventCheck()
        {
            s_EventTypeBefore = Event.current.GetTypeForControl(s_HotControl);
        }

        private static EventType EndEventCheck()
        {
            EventType usedEvent = (s_EventTypeBefore != Event.current.GetTypeForControl(s_HotControl) ? s_EventTypeBefore : EventType.Ignore);
            s_EventTypeBefore = EventType.Ignore;
            if (usedEvent == EventType.MouseDown)
                s_HotControl = GUIUtility.hotControl;
            else if (usedEvent == EventType.MouseUp)
                s_HotControl = 0;
            return usedEvent;
        }

        public static void BeginManipulationHandling(bool lockHandleWhileDragging)
        {
            BeginEventCheck();
            s_LockHandle = lockHandleWhileDragging;
        }

        public static EventType EndManipulationHandling()
        {
            EventType usedEvent = EndEventCheck();

            if (usedEvent == EventType.MouseDown)
            {
                RecordMouseDownState(Selection.transforms);
                s_StartHandlePosition = Tools.handlePosition;
                s_StartLocalHandleOffset = Tools.localHandleOffset;
                if (s_LockHandle)
                    Tools.LockHandlePosition();
                Tools.LockHandleRectRotation();
            }
            else if (s_MouseDownState != null && (usedEvent == EventType.MouseUp || GUIUtility.hotControl != s_HotControl))
            {
                s_MouseDownState = null;
                if (s_LockHandle)
                    Tools.UnlockHandlePosition();
                Tools.UnlockHandleRectRotation();
                ManipulationToolUtility.DisableMinDragDifference();
            }

            return usedEvent;
        }

        private static void RecordMouseDownState(Transform[] transforms)
        {
            s_MouseDownState = new TransformData[transforms.Length];
            for (int i = 0; i < transforms.Length; i++)
            {
                s_MouseDownState[i] = TransformData.GetData(transforms[i]);
            }
        }

        private static void SetLocalHandleOffsetScaleDelta(Vector3 scaleDelta, Quaternion pivotRotation)
        {
            Quaternion refAlignment = Quaternion.Inverse(Tools.handleRotation) * pivotRotation;
            Tools.localHandleOffset = Vector3.Scale(Vector3.Scale(s_StartLocalHandleOffset, refAlignment * scaleDelta), refAlignment * Vector3.one);
        }

        public static void SetScaleDelta(Vector3 scaleDelta, Quaternion pivotRotation)
        {
            if (s_MouseDownState == null)
                return;

            SetLocalHandleOffsetScaleDelta(scaleDelta, pivotRotation);

            for (int i = 0; i < s_MouseDownState.Length; i++)
            {
                var cur = s_MouseDownState[i];
                Undo.RecordObject(cur.transform, "Scale");
            }

            Vector3 point = Tools.handlePosition;
            for (int i = 0; i < s_MouseDownState.Length; i++)
            {
                // Scale about handlePosition or local pivot based on pivotMode
                if (Tools.pivotMode == PivotMode.Pivot)
                    point = s_MouseDownState[i].position;
                if (individualSpace)
                    pivotRotation = s_MouseDownState[i].rotation;
                s_MouseDownState[i].SetScaleDelta(scaleDelta, point, pivotRotation, false);
            }
        }

        public static void SetResizeDelta(Vector3 scaleDelta, Vector3 pivotPosition, Quaternion pivotRotation)
        {
            if (s_MouseDownState == null)
                return;

            SetLocalHandleOffsetScaleDelta(scaleDelta, pivotRotation);

            for (int i = 0; i < s_MouseDownState.Length; i++)
            {
                var cur = s_MouseDownState[i];
                if (cur.rectTransform != null)
                    Undo.RecordObject((Object)cur.rectTransform, "Resize");
                else
                {
                    SpriteRenderer sr = cur.transform.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.drawMode != SpriteDrawMode.Simple)
                        Undo.RecordObjects(new Object[] {(Object)sr, (Object)cur.transform}, "Resize");
                    else
                        Undo.RecordObject((Object)cur.transform, "Resize");
                }
            }

            for (int i = 0; i < s_MouseDownState.Length; i++)
            {
                s_MouseDownState[i].SetScaleDelta(scaleDelta, pivotPosition, pivotRotation, true);
            }
        }

        public static void SetPositionDelta(Vector3 positionDelta)
        {
            if (s_MouseDownState == null)
                return;

            if (positionDelta.magnitude == 0)
                return;

            for (int i = 0; i < s_MouseDownState.Length; i++)
            {
                var cur = s_MouseDownState[i];
                Undo.RecordObject((cur.rectTransform != null ? (Object)cur.rectTransform : (Object)cur.transform), "Move");
            }

            if (s_MouseDownState.Length > 0)
            {
                s_MouseDownState[0].SetPositionDelta(positionDelta, true);
                Vector3 firstDelta = s_MouseDownState[0].transform.position - s_MouseDownState[0].position;

                for (int i = 1; i < s_MouseDownState.Length; i++)
                {
                    s_MouseDownState[i].SetPositionDelta(firstDelta, false);
                }
            }
        }

        public static void DebugAlignment(Quaternion targetRotation)
        {
            if (s_MouseDownState == null)
                return;
            for (int i = 0; i < s_MouseDownState.Length; i++)
                s_MouseDownState[i].DebugAlignment(targetRotation);
        }
    }
} // namespace
