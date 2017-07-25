// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor
{
    internal abstract class ManipulationTool
    {
        protected virtual void OnToolGUI(SceneView view)
        {
            if (!Selection.activeTransform || Tools.s_Hidden)
                return;

            bool isStatic = (!Tools.s_Hidden && EditorApplication.isPlaying && GameObjectUtility.ContainsStatic(Selection.gameObjects));
            using (new EditorGUI.DisabledScope(isStatic))
            {
                Vector3 handlePosition = Tools.handlePosition;

                ToolGUI(view, handlePosition, isStatic);

                Handles.ShowStaticLabelIfNeeded(handlePosition);
            }
        }

        public abstract void ToolGUI(SceneView view, Vector3 handlePosition, bool isStatic);
    }

    internal class ManipulationToolUtility
    {
        public static Vector3 minDragDifference { get; set; }
        public static void SetMinDragDifferenceForPos(Vector3 position)
        {
            minDragDifference = Vector3.one * (HandleUtility.GetHandleSize(position) / 80f);
        }

        public static void SetMinDragDifferenceForPos(Vector3 position, float multiplier)
        {
            minDragDifference = Vector3.one * (HandleUtility.GetHandleSize(position) * multiplier / 80f);
        }

        public static void DisableMinDragDifference()
        {
            minDragDifference = Vector3.zero;
        }

        public static void DisableMinDragDifferenceForAxis(int axis)
        {
            Vector2 diff = minDragDifference;
            diff[axis] = 0;
            minDragDifference = diff;
        }

        public static void DisableMinDragDifferenceBasedOnSnapping(Vector3 positionBeforeSnapping, Vector3 positionAfterSnapping)
        {
            for (int axis = 0; axis < 3; axis++)
                if (positionBeforeSnapping[axis] != positionAfterSnapping[axis])
                    DisableMinDragDifferenceForAxis(axis);
        }

        public delegate void HandleDragChange(string handleName, bool dragging);
        public static HandleDragChange handleDragChange;
        public static void BeginDragging(string handleName)
        {
            if (handleDragChange != null)
                handleDragChange(handleName, true);
        }

        public static void EndDragging(string handleName)
        {
            if (handleDragChange != null)
                handleDragChange(handleName, false);
        }

        public static void DetectDraggingBasedOnMouseDownUp(string handleName, EventType typeBefore)
        {
            if (typeBefore == EventType.MouseDrag && Event.current.type != EventType.MouseDrag)
                BeginDragging(handleName);
            else if (typeBefore == EventType.MouseUp && Event.current.type != EventType.MouseUp)
                EndDragging(handleName);
        }
    }

    internal class TransformTool : ManipulationTool
    {
        static TransformTool s_Instance;
        static Vector3 s_Scale;

        public static void OnGUI(SceneView view)
        {
            if (s_Instance == null)
                s_Instance = new TransformTool();
            s_Instance.OnToolGUI(view);
        }

        public override void ToolGUI(SceneView view, Vector3 handlePosition, bool isStatic)
        {
            var ids = Handles.TransformHandleIds.Default;
            TransformManipulator.BeginManipulationHandling(false);

            // Lock position when scaling or rotating
            if (ids.scale.Has(GUIUtility.hotControl)
                || ids.rotation.Has(GUIUtility.hotControl))
                Tools.LockHandlePosition();
            else
                Tools.UnlockHandlePosition();

            EditorGUI.BeginChangeCheck();

            if (Event.current.type == EventType.MouseDown)
                s_Scale = Vector3.one;

            var startPosition = handlePosition;
            var endPosition = startPosition;
            var startRotation = Tools.handleRotation;
            var endRotation = startRotation;
            var startScale = s_Scale;
            var endScale = startScale;
            Handles.TransformHandle(ids, ref endPosition, ref endRotation, ref endScale, Handles.TransformHandleParam.Default);
            s_Scale = endScale;

            if (EditorGUI.EndChangeCheck() && !isStatic)
            {
                Undo.RecordObjects(Selection.transforms, "Transform Manipulation");

                Vector3 deltaPosition = endPosition - TransformManipulator.mouseDownHandlePosition;
                if (deltaPosition != Vector3.zero)
                {
                    ManipulationToolUtility.SetMinDragDifferenceForPos(handlePosition);
                    TransformManipulator.SetPositionDelta(deltaPosition);
                }

                Quaternion deltaRotation = Quaternion.Inverse(startRotation) * endRotation;
                float angle;
                Vector3 axis;
                deltaRotation.ToAngleAxis(out angle, out axis);
                if (!Mathf.Approximately(angle, 0))
                {
                    foreach (Transform t in Selection.transforms)
                    {
                        // Rotate around handlePosition (Global or Local axis).
                        if (Tools.pivotMode == PivotMode.Center)
                            t.RotateAround(handlePosition, startRotation * axis, angle);
                        // Local rotation (Pivot mode with Local axis).
                        else if (TransformManipulator.individualSpace)
                            t.Rotate(t.rotation * axis, angle, Space.World);
                        // Pivot mode with Global axis.
                        else
                            t.Rotate(startRotation * axis, angle, Space.World);

                        // sync euler hints after a rotate tool update tyo fake continuous rotation
                        t.SetLocalEulerHint(t.GetLocalEulerAngles(t.rotationOrder));

                        if (t.parent != null)
                            t.SendTransformChangedScale(); // force scale update, needed if tr has non-uniformly scaled parent.
                    }
                    Tools.handleRotation = endRotation;
                }

                if (endScale != startScale)
                    TransformManipulator.SetScaleDelta(endScale, endRotation);
            }
            TransformManipulator.EndManipulationHandling();
        }
    }

    internal class MoveTool : ManipulationTool
    {
        private static MoveTool s_Instance;
        public static void OnGUI(SceneView view)
        {
            if (s_Instance == null)
                s_Instance = new MoveTool();
            s_Instance.OnToolGUI(view);
        }

        public override void ToolGUI(SceneView view, Vector3 handlePosition, bool isStatic)
        {
            TransformManipulator.BeginManipulationHandling(false);
            EditorGUI.BeginChangeCheck();
            Vector3 pos2 = Handles.PositionHandle(handlePosition, Tools.handleRotation);
            if (EditorGUI.EndChangeCheck() && !isStatic)
            {
                Vector3 delta = pos2 - TransformManipulator.mouseDownHandlePosition;
                ManipulationToolUtility.SetMinDragDifferenceForPos(handlePosition);

                if (Tools.vertexDragging)
                    ManipulationToolUtility.DisableMinDragDifference();

                TransformManipulator.SetPositionDelta(delta);
            }
            TransformManipulator.EndManipulationHandling();
        }
    }

    internal class RotateTool : ManipulationTool
    {
        private static RotateTool s_Instance;
        public static void OnGUI(SceneView view)
        {
            if (s_Instance == null)
                s_Instance = new RotateTool();
            s_Instance.OnToolGUI(view);
        }

        public override void ToolGUI(SceneView view, Vector3 handlePosition, bool isStatic)
        {
            Quaternion before = Tools.handleRotation;

            EditorGUI.BeginChangeCheck();
            Quaternion after = Handles.RotationHandle(before, handlePosition);

            if (EditorGUI.EndChangeCheck() && !isStatic)
            {
                Quaternion delta = Quaternion.Inverse(before) * after;
                float angle;
                Vector3 axis;
                delta.ToAngleAxis(out angle, out axis);

                Undo.RecordObjects(Selection.transforms, "Rotate");
                foreach (Transform t in Selection.transforms)
                {
                    // Rotate around handlePosition (Global or Local axis).
                    if (Tools.pivotMode == PivotMode.Center)
                    {
                        t.RotateAround(handlePosition, before * axis, angle);
                    }
                    // Local rotation (Pivot mode with Local axis).
                    else if (TransformManipulator.individualSpace)
                    {
                        t.Rotate(t.rotation * axis, angle, Space.World);
                    }
                    // Pivot mode with Global axis.
                    else
                    {
                        t.Rotate(before * axis, angle, Space.World);
                    }

                    // sync euler hints after a rotate tool update tyo fake continuous rotation
                    t.SetLocalEulerHint(t.GetLocalEulerAngles(t.rotationOrder));

                    if (t.parent != null)
                        t.SendTransformChangedScale(); // force scale update, needed if tr has non-uniformly scaled parent.
                }
                Tools.handleRotation = after;
            }
        }
    }

    internal class ScaleTool : ManipulationTool
    {
        private static ScaleTool s_Instance;
        public static void OnGUI(SceneView view)
        {
            if (s_Instance == null)
                s_Instance = new ScaleTool();
            s_Instance.OnToolGUI(view);
        }

        private static Vector3 s_CurrentScale = Vector3.one;

        public override void ToolGUI(SceneView view, Vector3 handlePosition, bool isStatic)
        {
            // Allow global space scaling for multi-selection but not for a single object
            Quaternion handleRotation = Selection.transforms.Length > 1 ?
                Tools.handleRotation : Tools.handleLocalRotation;

            TransformManipulator.DebugAlignment(handleRotation);

            if (Event.current.type == EventType.MouseDown)
                s_CurrentScale = Vector3.one;

            EditorGUI.BeginChangeCheck();

            TransformManipulator.BeginManipulationHandling(true);
            s_CurrentScale = Handles.ScaleHandle(s_CurrentScale, handlePosition, handleRotation, HandleUtility.GetHandleSize(handlePosition));
            TransformManipulator.EndManipulationHandling();

            if (EditorGUI.EndChangeCheck() && !isStatic)
            {
                TransformManipulator.SetScaleDelta(s_CurrentScale, handleRotation);
            }
        }
    }

    internal class RectTool : ManipulationTool
    {
        private static RectTool s_Instance;

        internal const string kChangingLeft = "ChangingLeft";
        internal const string kChangingRight = "ChangingRight";
        internal const string kChangingTop = "ChangingTop";
        internal const string kChangingBottom = "ChangingBottom";
        internal const string kChangingPosX = "ChangingPosX";
        internal const string kChangingPosY = "ChangingPosY";
        internal const string kChangingWidth = "ChangingWidth";
        internal const string kChangingHeight = "ChangingHeight";
        internal const string kChangingPivot = "ChangingPivot";

        const float kMinVisibleSize = 0.2f;

        public static void OnGUI(SceneView view)
        {
            if (s_Instance == null)
                s_Instance = new RectTool();
            s_Instance.OnToolGUI(view);
        }

        public static Vector2 GetLocalRectPoint(Rect rect, int index)
        {
            switch (index)
            {
                case (0): return new Vector2(rect.xMin, rect.yMax);
                case (1): return new Vector2(rect.xMax, rect.yMax);
                case (2): return new Vector2(rect.xMax, rect.yMin);
                case (3): return new Vector2(rect.xMin, rect.yMin);
            }
            return Vector3.zero;
        }

        public override void ToolGUI(SceneView view, Vector3 handlePosition, bool isStatic)
        {
            Rect rect = Tools.handleRect;
            Quaternion rectRotation = Tools.handleRectRotation;

            // Draw rect
            Vector3[] verts = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 pos = GetLocalRectPoint(rect, i);
                verts[i] = rectRotation * pos + handlePosition;
            }
            RectHandles.RenderRectWithShadow(false, verts);

            // Handle fading
            Color oldColor = GUI.color;
            float faded = 1;
            if (Camera.current)
            {
                Vector3 viewDir = Camera.current.orthographic ?
                    Camera.current.transform.forward :
                    (handlePosition + rectRotation * rect.center - Camera.current.transform.position);
                Vector3 rectRight = rectRotation * Vector3.right * rect.width;
                Vector3 rectUp = rectRotation * Vector3.up * rect.height;
                float visibleSize = Mathf.Sqrt(Vector3.Cross(Vector3.ProjectOnPlane(rectRight, viewDir), Vector3.ProjectOnPlane(rectUp, viewDir)).magnitude);
                visibleSize /= HandleUtility.GetHandleSize(handlePosition);
                faded = Mathf.Clamp01((visibleSize - kMinVisibleSize) / kMinVisibleSize * 2);
                Color fadedColor = oldColor;
                fadedColor.a *= faded;
                GUI.color = fadedColor;
            }

            Vector3 oldPivot = Tools.GetHandlePosition();

            // Pivot handle
            if (!Tools.vertexDragging)
            {
                RectTransform rectTransform = Selection.activeTransform.GetComponent<RectTransform>();
                bool groupPivot = Selection.transforms.Length > 1;
                bool rectTransformPivot = !groupPivot && Tools.pivotMode == PivotMode.Pivot && rectTransform != null;
                using (new EditorGUI.DisabledScope(!groupPivot && !rectTransformPivot))
                {
                    EditorGUI.BeginChangeCheck();
                    Vector3 newPivot = PivotHandleGUI(rect, oldPivot, rectRotation);
                    if (EditorGUI.EndChangeCheck() && !isStatic)
                    {
                        if (groupPivot)
                        {
                            Tools.localHandleOffset += Quaternion.Inverse(Tools.handleRotation) * (newPivot - oldPivot);
                        }
                        else if (rectTransformPivot)
                        {
                            Transform tr = Selection.activeTransform;
                            Undo.RecordObject(rectTransform, "Move Rectangle Pivot");
                            Transform space = Tools.rectBlueprintMode && UnityEditorInternal.InternalEditorUtility.SupportsRectLayout(tr) ? tr.parent : tr;
                            Vector2 offset = space.InverseTransformVector(newPivot - oldPivot);
                            offset.x /= rectTransform.rect.width;
                            offset.y /= rectTransform.rect.height;
                            Vector2 pivot = rectTransform.pivot + offset;

                            RectTransformEditor.SetPivotSmart(rectTransform, pivot.x, 0, true, space != rectTransform.transform);
                            RectTransformEditor.SetPivotSmart(rectTransform, pivot.y, 1, true, space != rectTransform.transform);
                        }
                    }
                }
            }

            TransformManipulator.BeginManipulationHandling(true);
            if (!Tools.vertexDragging)
            {
                // Resize handles
                EditorGUI.BeginChangeCheck();
                Vector3 scalePivot = handlePosition;
                Vector3 scale = ResizeHandlesGUI(rect, handlePosition, rectRotation, out scalePivot);
                if (EditorGUI.EndChangeCheck() && !isStatic)
                {
                    TransformManipulator.SetResizeDelta(scale, scalePivot, rectRotation);
                }

                bool enableRotation = true;
                if (Tools.rectBlueprintMode)
                {
                    foreach (Transform t in Selection.transforms)
                    {
                        if (t.GetComponent<RectTransform>() != null)
                            enableRotation = false;
                    }
                }

                if (enableRotation)
                {
                    // Rotation handles
                    EditorGUI.BeginChangeCheck();
                    Quaternion after = RotationHandlesGUI(rect, handlePosition, rectRotation);
                    if (EditorGUI.EndChangeCheck() && !isStatic)
                    {
                        Quaternion delta = Quaternion.Inverse(rectRotation) * after;
                        float angle;
                        Vector3 axis;
                        delta.ToAngleAxis(out angle, out axis);
                        axis = rectRotation * axis;

                        Undo.RecordObjects(Selection.transforms, "Rotate");
                        foreach (Transform t in Selection.transforms)
                        {
                            t.RotateAround(handlePosition, axis, angle);

                            // sync euler hints after a rotate tool update tyo fake continuous rotation
                            t.SetLocalEulerHint(t.GetLocalEulerAngles(t.rotationOrder));

                            if (t.parent != null)
                                t.SendTransformChangedScale(); // force scale update, needed if transform has non-uniformly scaled parent.
                        }
                        Tools.handleRotation = Quaternion.AngleAxis(angle, axis) * Tools.handleRotation;
                    }
                }
            }
            TransformManipulator.EndManipulationHandling();

            TransformManipulator.BeginManipulationHandling(false);
            // Move handle
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = MoveHandlesGUI(rect, handlePosition, rectRotation);
            if (EditorGUI.EndChangeCheck() && !isStatic)
            {
                if (GridSnapping.active)
                    newPos = GridSnapping.Snap(newPos);

                Vector3 delta = newPos - TransformManipulator.mouseDownHandlePosition;
                TransformManipulator.SetPositionDelta(delta);
            }
            TransformManipulator.EndManipulationHandling();

            GUI.color = oldColor;
        }

        private static int s_ResizeHandlesHash = "ResizeHandles".GetHashCode();
        private static int s_RotationHandlesHash = "RotationHandles".GetHashCode();
        private static int s_MoveHandleHash = "MoveHandle".GetHashCode();
        private static int s_PivotHandleHash = "PivotHandle".GetHashCode();
        private static Rect s_StartRect = new Rect();

        private static Vector3 GetRectPointInWorld(Rect rect, Vector3 pivot, Quaternion rotation, int xHandle, int yHandle)
        {
            Vector3 pos = new Vector2(
                    pos.x = Mathf.Lerp(rect.xMin, rect.xMax, xHandle * 0.5f),
                    pos.y = Mathf.Lerp(rect.yMin, rect.yMax, yHandle * 0.5f));
            return rotation * pos + pivot;
        }

        static Vector3 ResizeHandlesGUI(Rect rect, Vector3 pivot, Quaternion rotation, out Vector3 scalePivot)
        {
            if (Event.current.type == EventType.MouseDown)
                s_StartRect = rect;

            scalePivot = pivot;
            Vector3 scale = Vector3.one;

            Quaternion inverseRotation = Quaternion.Inverse(rotation);
            // Loop through the 8 handles (sides and corners) using a nested loop.
            // (The loop covers 9 combinations, but the center position is ignored.)
            for (int xHandle = 0; xHandle <= 2; xHandle++)
            {
                for (int yHandle = 0; yHandle <= 2; yHandle++)
                {
                    // Ignore center
                    if (xHandle == 1 && yHandle == 1)
                        continue;

                    Vector3 origPos = GetRectPointInWorld(s_StartRect, pivot, rotation, xHandle, yHandle);
                    Vector3 curPos = GetRectPointInWorld(rect, pivot, rotation, xHandle, yHandle);

                    float size = 0.05f * HandleUtility.GetHandleSize(curPos);
                    int id = GUIUtility.GetControlID(s_ResizeHandlesHash, FocusType.Passive);
                    if (GUI.color.a > 0 || GUIUtility.hotControl == id)
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector3 newPos;

                        EventType typeBefore = Event.current.type;

                        if (xHandle == 1 || yHandle == 1)
                        {
                            // Side resizer (1D)
                            Vector3 sideDir = (xHandle == 1 ? rotation * Vector3.right * rect.width : rotation * Vector3.up * rect.height);
                            Vector3 slideDir = (xHandle == 1 ? rotation * Vector3.up : rotation * Vector3.right);
                            newPos = RectHandles.SideSlider(id, curPos, sideDir, slideDir, size, null, 0);
                        }
                        else
                        {
                            // Corner handle (2D)
                            Vector3 outwardsA = rotation * Vector3.right * (xHandle - 1);
                            Vector3 outwardsB = rotation * Vector3.up * (yHandle - 1);
                            newPos = RectHandles.CornerSlider(id, curPos, rotation * Vector3.forward, outwardsA, outwardsB, size, RectHandles.RectScalingHandleCap, Vector2.zero);
                        }

                        // Calculate snapping values if applicable
                        bool supportsRectSnapping = Selection.transforms.Length == 1 &&
                            UnityEditorInternal.InternalEditorUtility.SupportsRectLayout(Selection.activeTransform) &&
                            Selection.activeTransform.parent.rotation == rotation;
                        if (supportsRectSnapping)
                        {
                            Transform transform = Selection.activeTransform;
                            RectTransform rectTransform = transform.GetComponent<RectTransform>();
                            Transform transformParent = transform.parent;
                            RectTransform rectTransformParent = transformParent.GetComponent<RectTransform>();
                            if (typeBefore == EventType.MouseDown && Event.current.type != EventType.MouseDown)
                                RectTransformSnapping.CalculateOffsetSnapValues(transformParent, transform, rectTransformParent, rectTransform, xHandle, yHandle);
                        }

                        if (EditorGUI.EndChangeCheck())
                        {
                            // Resize handles require more fine grained rounding of values than other tools.
                            // With other tools, the slight rounding is not notizable as long as it's just sub-pixel,
                            // because the manipulated object is being moved at the same time.
                            // However, with resize handles, when dragging one edge or corner,
                            // the opposite is standing still, and even slight rounding can cause shaking/vibration.
                            // At a fraction of the normal rounding, the shaking is very unlikely to happen though.
                            ManipulationToolUtility.SetMinDragDifferenceForPos(curPos, 0.1f);

                            if (supportsRectSnapping)
                            {
                                Transform transformParent = Selection.activeTransform.parent;
                                RectTransform rectParent = transformParent.GetComponent<RectTransform>();

                                Vector2 snapSize = Vector2.one * HandleUtility.GetHandleSize(newPos) * RectTransformSnapping.kSnapThreshold;
                                snapSize.x /= (inverseRotation * transformParent.TransformVector(Vector3.right)).x;
                                snapSize.y /= (inverseRotation * transformParent.TransformVector(Vector3.up)).y;

                                Vector3 newPosInParent = transformParent.InverseTransformPoint(newPos) - (Vector3)rectParent.rect.min;
                                Vector3 newPosInParentSnapped = (Vector3)RectTransformSnapping.SnapToGuides(newPosInParent, snapSize) + Vector3.forward * newPosInParent.z;
                                ManipulationToolUtility.DisableMinDragDifferenceBasedOnSnapping(newPosInParent, newPosInParentSnapped);
                                newPos = transformParent.TransformPoint(newPosInParentSnapped + (Vector3)rectParent.rect.min);
                            }

                            bool scaleFromPivot = Event.current.alt;
                            bool squashing = EditorGUI.actionKey;
                            bool uniformScaling = Event.current.shift && !squashing;

                            if (!scaleFromPivot)
                                scalePivot = GetRectPointInWorld(s_StartRect, pivot, rotation, 2 - xHandle, 2 - yHandle);

                            if (uniformScaling)
                                newPos = Vector3.Project(newPos - scalePivot, origPos - scalePivot) + scalePivot;

                            Vector3 sizeBefore = inverseRotation * (origPos - scalePivot);
                            Vector3 sizeAfter = inverseRotation * (newPos - scalePivot);
                            if (xHandle != 1)
                                scale.x = sizeAfter.x / sizeBefore.x;
                            if (yHandle != 1)
                                scale.y = sizeAfter.y / sizeBefore.y;

                            if (uniformScaling)
                            {
                                float refScale = (xHandle == 1 ? scale.y : scale.x);
                                scale = Vector3.one * refScale;
                            }

                            if (squashing && xHandle == 1)
                            {
                                if (Event.current.shift)
                                    scale.x = scale.z = 1 / Mathf.Sqrt(Mathf.Max(scale.y, 0.0001f));
                                else
                                    scale.x = 1 / Mathf.Max(scale.y, 0.0001f);
                            }

                            if (uniformScaling)
                            {
                                float refScale = (xHandle == 1 ? scale.y : scale.x);
                                scale = Vector3.one * refScale;
                            }

                            if (squashing && xHandle == 1)
                            {
                                if (Event.current.shift)
                                    scale.x = scale.z = 1 / Mathf.Sqrt(Mathf.Max(scale.y, 0.0001f));
                                else
                                    scale.x = 1 / Mathf.Max(scale.y, 0.0001f);
                            }
                            if (squashing && yHandle == 1)
                            {
                                if (Event.current.shift)
                                    scale.y = scale.z = 1 / Mathf.Sqrt(Mathf.Max(scale.x, 0.0001f));
                                else
                                    scale.y = 1 / Mathf.Max(scale.x, 0.0001f);
                            }
                        }

                        if (xHandle == 0)
                            ManipulationToolUtility.DetectDraggingBasedOnMouseDownUp(kChangingLeft, typeBefore);
                        if (xHandle == 2)
                            ManipulationToolUtility.DetectDraggingBasedOnMouseDownUp(kChangingRight, typeBefore);
                        if (xHandle != 1)
                            ManipulationToolUtility.DetectDraggingBasedOnMouseDownUp(kChangingWidth, typeBefore);
                        if (yHandle == 0)
                            ManipulationToolUtility.DetectDraggingBasedOnMouseDownUp(kChangingBottom, typeBefore);
                        if (yHandle == 2)
                            ManipulationToolUtility.DetectDraggingBasedOnMouseDownUp(kChangingTop, typeBefore);
                        if (yHandle != 1)
                            ManipulationToolUtility.DetectDraggingBasedOnMouseDownUp(kChangingHeight, typeBefore);
                    }
                }
            }

            return scale;
        }

        private static Vector3 s_StartMouseWorldPos;
        private static Vector3 s_StartPosition;
        private static Vector2 s_StartMousePos;
        private static Vector3 s_StartRectPosition;
        private static Vector2 s_CurrentMousePos;
        private static bool s_Moving = false;
        private static int s_LockAxis = -1;

        static Vector3 MoveHandlesGUI(Rect rect, Vector3 pivot, Quaternion rotation)
        {
            int id = GUIUtility.GetControlID(s_MoveHandleHash, FocusType.Passive);

            Vector3 newPos = pivot;
            float discSize = HandleUtility.GetHandleSize(pivot) * 0.2f;
            float discOpacity = (1 - GUI.color.a);

            Vector3[] corners = new Vector3[4];
            corners[0] = rotation * new Vector2(rect.x, rect.y) + pivot;
            corners[1] = rotation * new Vector2(rect.xMax, rect.y) + pivot;
            corners[2] = rotation * new Vector2(rect.xMax, rect.yMax) + pivot;
            corners[3] = rotation * new Vector2(rect.x, rect.yMax) + pivot;

            VertexSnapping.HandleKeyAndMouseMove(id);

            bool supportsRectSnapping = Selection.transforms.Length == 1 &&
                UnityEditorInternal.InternalEditorUtility.SupportsRectLayout(Selection.activeTransform) &&
                Selection.activeTransform.parent.rotation == rotation;

            Event evt = Event.current;
            EventType eventType = evt.GetTypeForControl(id);
            Plane guiPlane = new Plane(corners[0], corners[1], corners[2]);
            switch (eventType)
            {
                case EventType.MouseDown:
                {
                    bool acceptClick = false;

                    if (Tools.vertexDragging)
                    {
                        acceptClick = true;
                    }
                    else
                    {
                        acceptClick =
                            evt.button == 0 &&
                            evt.modifiers == 0 &&
                            RectHandles.RaycastGUIPointToWorldHit(evt.mousePosition, guiPlane, out s_StartMouseWorldPos) &&
                            (
                                SceneViewDistanceToRectangle(corners, evt.mousePosition) == 0f ||
                                (discOpacity > 0 && SceneViewDistanceToDisc(pivot, rotation * Vector3.forward, discSize, evt.mousePosition) == 0f)
                            );
                    }

                    if (acceptClick)
                    {
                        s_StartPosition = pivot;
                        s_StartMousePos = s_CurrentMousePos = evt.mousePosition;
                        s_Moving = false;
                        s_LockAxis = -1;
                        GUIUtility.hotControl = GUIUtility.keyboardControl = id;
                        EditorGUIUtility.SetWantsMouseJumping(1);
                        HandleUtility.ignoreRaySnapObjects = null;
                        evt.Use();

                        // Calculate snapping values if applicable
                        if (supportsRectSnapping)
                        {
                            Transform transform = Selection.activeTransform;
                            RectTransform rectTransform = transform.GetComponent<RectTransform>();
                            Transform transformParent = transform.parent;
                            RectTransform rectTransformParent = transformParent.GetComponent<RectTransform>();

                            s_StartRectPosition = rectTransform.anchoredPosition;

                            RectTransformSnapping.CalculatePositionSnapValues(transformParent, transform, rectTransformParent, rectTransform);
                        }
                    }
                    break;
                }
                case EventType.MouseDrag:
                {
                    if (GUIUtility.hotControl == id)
                    {
                        s_CurrentMousePos += evt.delta;
                        if (!s_Moving && (s_CurrentMousePos - s_StartMousePos).magnitude > 3f)
                        {
                            s_Moving = true;
                            // Re-raycast to get start mouse pos when effective dragging starts.
                            // This prevents a sudden unsnap when the dragging is enabled.
                            RectHandles.RaycastGUIPointToWorldHit(s_CurrentMousePos, guiPlane, out s_StartMouseWorldPos);
                        }
                        if (s_Moving)
                        {
                            if (Tools.vertexDragging)
                            {
                                if (HandleUtility.ignoreRaySnapObjects == null)
                                    Handles.SetupIgnoreRaySnapObjects();
                                Vector3 near;
                                if (HandleUtility.FindNearestVertex(s_CurrentMousePos, null, out near))
                                {
                                    // Snap position based on found near vertex
                                    newPos = near;
                                    GUI.changed = true;
                                }
                                ManipulationToolUtility.minDragDifference = Vector2.zero;
                            }
                            else
                            {
                                ManipulationToolUtility.SetMinDragDifferenceForPos(pivot);
                                Vector3 pos;
                                if (RectHandles.RaycastGUIPointToWorldHit(s_CurrentMousePos, guiPlane, out pos))
                                {
                                    Vector3 offset = pos - s_StartMouseWorldPos;

                                    // Snap to axis
                                    if (evt.shift)
                                    {
                                        // Get offset in rect handles space
                                        offset = Quaternion.Inverse(rotation) * offset;
                                        // Determine lock axis if not already set
                                        if (s_LockAxis == -1)
                                            s_LockAxis = Mathf.Abs(offset.x) > Mathf.Abs(offset.y) ? 0 : 1;
                                        // Cancel mocement on other axis
                                        offset[1 - s_LockAxis] = 0;
                                        // Put offset back in world space
                                        offset = rotation * offset;
                                    }
                                    else
                                    {
                                        s_LockAxis = -1;
                                    }

                                    if (supportsRectSnapping)
                                    {
                                        Transform transformParent = Selection.activeTransform.parent;
                                        Vector3 rectPosition = s_StartRectPosition + transformParent.InverseTransformVector(offset);
                                        rectPosition.z = 0;

                                        Quaternion inverseRotation = Quaternion.Inverse(rotation);
                                        Vector2 snapSize = Vector2.one * HandleUtility.GetHandleSize(newPos) * RectTransformSnapping.kSnapThreshold;
                                        snapSize.x /= (inverseRotation * transformParent.TransformVector(Vector3.right)).x;
                                        snapSize.y /= (inverseRotation * transformParent.TransformVector(Vector3.up)).y;

                                        Vector3 newRectPosition = RectTransformSnapping.SnapToGuides(rectPosition, snapSize);
                                        ManipulationToolUtility.DisableMinDragDifferenceBasedOnSnapping(rectPosition, newRectPosition);
                                        offset = transformParent.TransformVector(newRectPosition - s_StartRectPosition);
                                    }

                                    newPos = s_StartPosition + offset;

                                    GUI.changed = true;
                                }
                            }
                        }
                        evt.Use();
                    }
                    break;
                }
                case EventType.MouseUp:
                {
                    if (GUIUtility.hotControl == id)
                    {
                        if (!s_Moving)
                            Selection.activeGameObject = SceneViewPicking.PickGameObject(evt.mousePosition);
                        GUIUtility.hotControl = 0;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        HandleUtility.ignoreRaySnapObjects = null;
                        evt.Use();
                    }
                    break;
                }
                case EventType.Repaint:
                {
                    if (Tools.vertexDragging)
                    {
                        RectHandles.RectScalingHandleCap(id, pivot, rotation, 1, EventType.Repaint);
                    }
                    else
                    {
                        Handles.color = Handles.secondaryColor * new Color(1, 1, 1, 1.5f * discOpacity);
                        Handles.CircleHandleCap(id, pivot, rotation, discSize, EventType.Repaint);
                        Handles.color = Handles.secondaryColor * new Color(1, 1, 1, 0.3f * discOpacity);
                        Handles.DrawSolidDisc(pivot, rotation * Vector3.forward, discSize);
                    }
                    break;
                }
            }

            ManipulationToolUtility.DetectDraggingBasedOnMouseDownUp(kChangingPosX, eventType);
            ManipulationToolUtility.DetectDraggingBasedOnMouseDownUp(kChangingLeft, eventType);
            ManipulationToolUtility.DetectDraggingBasedOnMouseDownUp(kChangingRight, eventType);
            ManipulationToolUtility.DetectDraggingBasedOnMouseDownUp(kChangingPosY, eventType);
            ManipulationToolUtility.DetectDraggingBasedOnMouseDownUp(kChangingTop, eventType);
            ManipulationToolUtility.DetectDraggingBasedOnMouseDownUp(kChangingBottom, eventType);

            return newPos;
        }

        static float SceneViewDistanceToDisc(Vector3 center, Vector3 normal, float radius, Vector2 mousePos)
        {
            Plane plane = new Plane(normal, center);
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            float dist;
            if (plane.Raycast(ray, out dist))
            {
                Vector3 worldMousePos = ray.GetPoint(dist);
                return Mathf.Max(0, (worldMousePos - center).magnitude - radius);
            }
            return Mathf.Infinity;
        }

        // Determine the distance from the mouse position to the world rectangle specified by the 4 points.
        static float SceneViewDistanceToRectangle(Vector3[] worldPoints, Vector2 mousePos)
        {
            Vector2[] screenPoints = new Vector2[4];
            for (int i = 0; i < 4; ++i)
                screenPoints[i] = HandleUtility.WorldToGUIPoint(worldPoints[i]);
            return DistanceToRectangle(screenPoints, mousePos);
        }

        static float DistancePointToLineSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            float l2 = (b - a).sqrMagnitude;
            if (l2 == 0f)
                return (point - a).magnitude;
            float t = Vector2.Dot(point - a, b - a) / l2;
            if (t < 0f)
                return (point - a).magnitude;
            else if (t > 1f)
                return (point - b).magnitude;
            Vector2 projection = a + t * (b - a);
            return (point - projection).magnitude;
        }

        static float DistanceToRectangle(Vector2[] screenPoints, Vector2 mousePos)
        {
            bool oddNodes = false;
            int j = 4;

            for (int i = 0; i < 5; i++)
            {
                Vector3 v0 = screenPoints[i % 4];
                Vector3 v1 = screenPoints[j % 4];

                if ((v0.y > mousePos.y) != (v1.y > mousePos.y))
                {
                    if (mousePos.x < (v1.x - v0.x) * (mousePos.y - v0.y) / (v1.y - v0.y) + v0.x)
                    {
                        oddNodes = !oddNodes;
                    }
                }
                j = i;
            }

            if (!oddNodes)
            {
                float dist, closestDist = -1f;

                for (int i = 0; i < 4; i++)
                {
                    Vector3 v0 = screenPoints[i];
                    Vector3 v1 = screenPoints[(i + 1) % 4];

                    dist = DistancePointToLineSegment(mousePos, v0, v1);

                    if (dist < closestDist || closestDist < 0f) closestDist = dist;
                }
                return closestDist;
            }
            else return 0f;
        }

        static Quaternion RotationHandlesGUI(Rect rect, Vector3 pivot, Quaternion rotation)
        {
            Vector3 euler = rotation.eulerAngles;
            // Loop through the 4 corner handles
            for (int xHandle = 0; xHandle <= 2; xHandle += 2)
            {
                for (int yHandle = 0; yHandle <= 2; yHandle += 2)
                {
                    Vector3 curPos = GetRectPointInWorld(rect, pivot, rotation, xHandle, yHandle);

                    float size = 0.05f * HandleUtility.GetHandleSize(curPos);
                    int id = GUIUtility.GetControlID(s_RotationHandlesHash, FocusType.Passive);
                    if (GUI.color.a > 0 || GUIUtility.hotControl == id)
                    {
                        EditorGUI.BeginChangeCheck();
                        Vector3 outwardsA = rotation * Vector3.right * (xHandle - 1);
                        Vector3 outwardsB = rotation * Vector3.up * (yHandle - 1);
                        float angle = RectHandles.RotationSlider(id, curPos, euler.z, pivot, rotation * Vector3.forward, outwardsA, outwardsB, size, null, Vector2.zero);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (Event.current.shift)
                                angle = Mathf.Round((angle - euler.z) / 15f) * 15f + euler.z;
                            euler.z = angle;
                            rotation = Quaternion.Euler(euler);
                        }
                    }
                }
            }
            return rotation;
        }

        static Vector3 PivotHandleGUI(Rect rect, Vector3 pivot, Quaternion rotation)
        {
            int id = GUIUtility.GetControlID(s_PivotHandleHash, FocusType.Passive);
            EventType eventType = Event.current.GetTypeForControl(id);
            if (GUI.color.a > 0 || GUIUtility.hotControl == id)
            {
                EventType typeBefore = eventType;

                EditorGUI.BeginChangeCheck();
                Vector3 newPivot = Handles.Slider2D(id, pivot, rotation * Vector3.forward, rotation * Vector3.right, rotation * Vector3.up, HandleUtility.GetHandleSize(pivot) * 0.1f, RectHandles.PivotHandleCap, Vector2.zero);

                if (typeBefore == EventType.MouseDown && GUIUtility.hotControl == id)
                    RectTransformSnapping.CalculatePivotSnapValues(rect, pivot, rotation);

                if (EditorGUI.EndChangeCheck())
                {
                    Vector2 offset = Quaternion.Inverse(rotation) * (newPivot - pivot);
                    offset.x /= rect.width;
                    offset.y /= rect.height;
                    Vector2 pivotCoordBefore = new Vector2(-rect.x / rect.width, -rect.y / rect.height);
                    Vector2 pivotCoordAfter = pivotCoordBefore + offset;

                    Vector2 snapSize = HandleUtility.GetHandleSize(pivot) * RectTransformSnapping.kSnapThreshold * new Vector2(1 / rect.width, 1 / rect.height);
                    pivotCoordAfter = RectTransformSnapping.SnapToGuides(pivotCoordAfter, snapSize);

                    offset = (pivotCoordAfter - pivotCoordBefore);
                    offset.x *= rect.width;
                    offset.y *= rect.height;
                    pivot += rotation * offset;
                }
            }
            ManipulationToolUtility.DetectDraggingBasedOnMouseDownUp(kChangingPivot, eventType);

            return pivot;
        }
    }
} // namespace
