// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace UnityEditorInternal
{
    // TODO: We need to make the editMode state e.g a string so users are able to extend with new edit mode states
    // and so we still can serialize it (to survive assembly reloads)

    [InitializeOnLoad]
    public class EditMode
    {
        internal const int k_OwnerIdNone = 0;

        private static class Styles
        {
            public static readonly GUIStyle multiButtonStyle = "Button";
            public static readonly GUIStyle singleButtonStyle = "EditModeSingleButton";
        }

        const string kOwnerStringKey = "EditModeOwner";
        const string kEditModeStringKey = "EditModeState";

        static EditMode()
        {
            ownerID = SessionState.GetInt(kOwnerStringKey, ownerID);
            s_EditMode = (SceneViewEditMode)SessionState.GetInt(kEditModeStringKey, (int)s_EditMode);
            Selection.selectionChanged += OnSelectionChange;
            ToolManager.activeToolChanging += OnActiveToolWillChange;
        }

        private const float k_EditColliderbuttonWidth = 33;
        private const float k_EditColliderbuttonHeight = 23;
        private const float k_SpaceBetweenLabelAndButton = 5;

        // todo Obsolete, use editModeEnded
        public static OnEditModeStopFunc onEditModeEndDelegate;
        public delegate void OnEditModeStopFunc(Editor editor);

        // todo Obsolete, use editModeStarted
        public static OnEditModeStartFunc onEditModeStartDelegate;
        public delegate void OnEditModeStartFunc(Editor editor, SceneViewEditMode mode);

        internal static event Action<IToolModeOwner> editModeEnded;
        internal static event Action<IToolModeOwner, SceneViewEditMode> editModeStarted;

        private static int s_OwnerID;
        private static SceneViewEditMode s_EditMode;

        public enum SceneViewEditMode
        {
            None = 0,
            Collider,
            ClothConstraints,
            ClothSelfAndInterCollisionParticles,
            ReflectionProbeBox,
            ReflectionProbeOrigin,
            LightProbeProxyVolumeBox,
            LightProbeProxyVolumeOrigin,
            LightProbeGroup,
            JointAngularLimits,
            GridPainting,
            GridPicking,
            GridEraser,
            GridFloodFill,
            GridBox,
            GridSelect,
            GridMove,
            ParticleSystemCollisionModulePlanesMove,
            ParticleSystemCollisionModulePlanesRotate,
            LineRendererEdit,
            LineRendererCreate,
            ParticleSystemShapeModuleGizmo,
            ParticleSystemShapeModulePosition,
            ParticleSystemShapeModuleRotation,
            ParticleSystemShapeModuleScale
        }

        static EditModeTool GetTool(IToolModeOwner owner, SceneViewEditMode mode)
        {
            var editor = owner as Editor;

            if (editor == null)
                return null;

            var editorType = editor.GetType();

            return EditorToolManager.GetCustomEditorTool(x =>
            {
                var tool = x as EditModeTool;
                return tool != null
                && tool.editMode == mode
                && tool.owner == owner
                && tool.editorType == editorType
                && tool.target == editor.target;
            }, true) as EditModeTool;
        }

        public static bool IsOwner(Editor editor)
        {
            return IsOwner((IToolModeOwner)editor);
        }

        internal static bool IsOwner(IToolModeOwner owner)
        {
            return owner.GetInstanceID() == s_OwnerID;
        }

        internal static int ownerID
        {
            get { return s_OwnerID; }
            set
            {
                s_OwnerID = value;
                SessionState.SetInt(kOwnerStringKey, s_OwnerID);
            }
        }

        public static SceneViewEditMode editMode
        {
            get { return s_EditMode; }
            private set
            {
                s_EditMode = value;
                SessionState.SetInt(kEditModeStringKey, (int)s_EditMode);
            }
        }

        static void OnActiveToolWillChange()
        {
            if (ownerID != k_OwnerIdNone)
                EditModeToolStateChanged(null, SceneViewEditMode.None);
        }

        public static void OnSelectionChange()
        {
            IToolModeOwner owner = InternalEditorUtility.GetObjectFromInstanceID(ownerID) as IToolModeOwner;
            if (owner != null && owner.ModeSurvivesSelectionChange((int)s_EditMode))
                return;
            QuitEditMode();
        }

        public static void QuitEditMode()
        {
            if (ownerID != k_OwnerIdNone)
                ChangeEditMode(SceneViewEditMode.None, new Bounds(Vector3.zero, Vector3.positiveInfinity), null);
        }

        [Obsolete("Obsolete msg (UnityUpgradable) -> UnityEditor.EditorTools.ToolManager.RestorePreviousTool()")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void ResetToolToPrevious()
        {
            EditorToolManager.RestorePreviousTool();
        }

        [Obsolete("Use signature passing Func<Bounds> rather than Bounds.")]
        public static void DoEditModeInspectorModeButton(SceneViewEditMode mode, string label, GUIContent icon, Bounds bounds, Editor caller)
        {
            DoEditModeInspectorModeButton(mode, label, icon, () => bounds, (IToolModeOwner)caller);
        }

        public static void DoEditModeInspectorModeButton(SceneViewEditMode mode, string label, GUIContent icon, Func<Bounds> getBoundsOfTargets, Editor caller)
        {
            DoEditModeInspectorModeButton(mode, label, icon, getBoundsOfTargets, (IToolModeOwner)caller);
        }

        internal static void DoEditModeInspectorModeButton(SceneViewEditMode mode, string label, GUIContent icon, IToolModeOwner owner)
        {
            DoEditModeInspectorModeButton(mode, label, icon, null, owner);
        }

        private static void DoEditModeInspectorModeButton(SceneViewEditMode mode, string label, GUIContent icon, Func<Bounds> getBoundsOfTargets, IToolModeOwner owner)
        {
            Rect rect = EditorGUILayout.GetControlRect(true, k_EditColliderbuttonHeight, Styles.singleButtonStyle);
            Rect buttonRect = new Rect(rect.xMin + EditorGUIUtility.labelWidth, rect.yMin, k_EditColliderbuttonWidth, k_EditColliderbuttonHeight);

            GUIContent labelContent = new GUIContent(label);
            Vector2 labelSize = GUI.skin.label.CalcSize(labelContent);

            Rect labelRect = new Rect(
                buttonRect.xMax + k_SpaceBetweenLabelAndButton,
                rect.yMin + (rect.height - labelSize.y) * .5f,
                labelSize.x,
                rect.height);

            bool modeEnabled = IsOwner(owner);

            EditorGUI.BeginChangeCheck();

            bool toggleEnabled = false;
            using (new EditorGUI.DisabledScope(!owner.areToolModesAvailable))
            {
                toggleEnabled = GUI.Toggle(buttonRect, modeEnabled, icon, Styles.singleButtonStyle);
                GUI.Label(labelRect, label);
            }

            if (EditorGUI.EndChangeCheck())
            {
                ChangeEditMode(toggleEnabled ? mode : SceneViewEditMode.None, getBoundsOfTargets == null ? owner.GetWorldBoundsOfTargets() : getBoundsOfTargets(), owner);
            }
        }

        [Obsolete("Use signature passing Func<Bounds> rather than Bounds.")]
        public static void DoInspectorToolbar(SceneViewEditMode[] modes, GUIContent[] guiContents, Bounds bounds, Editor caller)
        {
            DoInspectorToolbar(modes, guiContents, () => bounds, (IToolModeOwner)caller);
        }

        public static void DoInspectorToolbar(SceneViewEditMode[] modes, GUIContent[] guiContents, Func<Bounds> getBoundsOfTargets, Editor caller)
        {
            DoInspectorToolbar(modes, guiContents, getBoundsOfTargets, (IToolModeOwner)caller);
        }

        internal static void DoInspectorToolbar(SceneViewEditMode[] modes, GUIContent[] guiContents, IToolModeOwner owner)
        {
            DoInspectorToolbar(modes, guiContents, null, owner);
        }

        private static void DoInspectorToolbar(SceneViewEditMode[] modes, GUIContent[] guiContents, Func<Bounds> getBoundsOfTargets, IToolModeOwner owner)
        {
            int callerID = owner.GetInstanceID();

            int selectedIndex = ArrayUtility.IndexOf(modes, editMode);
            if (ownerID != callerID)
                selectedIndex = -1;
            EditorGUI.BeginChangeCheck();
            int newSelectedIndex = selectedIndex;
            using (new EditorGUI.DisabledScope(!owner.areToolModesAvailable))
                newSelectedIndex = GUILayout.Toolbar(selectedIndex, guiContents, Styles.multiButtonStyle);
            if (EditorGUI.EndChangeCheck())
            {
                // Buttons can be toggled
                SceneViewEditMode newEditMode = newSelectedIndex == selectedIndex ? SceneViewEditMode.None : modes[newSelectedIndex];
                ChangeEditMode(newEditMode, getBoundsOfTargets == null ? owner.GetWorldBoundsOfTargets() : getBoundsOfTargets(), owner);
            }
        }

        public static void ChangeEditMode(SceneViewEditMode mode, Bounds bounds, Editor caller)
        {
            ChangeEditMode(mode, bounds, (IToolModeOwner)caller);
        }

        internal static void ChangeEditMode(SceneViewEditMode mode, IToolModeOwner owner)
        {
            ChangeEditMode(mode, owner.GetWorldBoundsOfTargets(), owner);
        }

        internal static void ChangeEditMode(SceneViewEditMode mode, Bounds bounds, IToolModeOwner owner)
        {
            if (mode == SceneViewEditMode.None)
            {
                var activeToolIsEditModeTool = (EditorToolManager.activeTool is EditModeTool || EditorToolManager.activeTool is NoneTool);

                if (s_EditMode != SceneViewEditMode.None && activeToolIsEditModeTool)
                    ToolManager.RestorePreviousPersistentTool();

                EditModeToolStateChanged(owner, mode);
                return;
            }

            var tool = GetTool(owner, mode);

            if (tool != null)
                ToolManager.SetActiveTool(tool);
            else
                // SceneViewEditModeTool doesn't exist, use old path
                ToolManager.SetActiveTool<NoneTool>();

            EditModeToolStateChanged(owner, mode);
        }

        internal static void EditModeToolStateChanged(IToolModeOwner owner, SceneViewEditMode mode)
        {
            // In cases of domain reloads the EditorTool can be deserialized prior to the target Inspector being
            // created. The EditMode tools do not expect an editModeStarted callback on reloads.
            if (owner == null && mode != SceneViewEditMode.None)
            {
                editMode = mode;
                return;
            }

            IToolModeOwner oldOwner = InternalEditorUtility.GetObjectFromInstanceID(ownerID) as IToolModeOwner;

            editMode = mode;

            if (oldOwner != null)
            {
                if (onEditModeEndDelegate != null && oldOwner is Editor)
                    onEditModeEndDelegate(oldOwner as Editor);

                if (editModeEnded != null)
                    editModeEnded(oldOwner);
            }

            ownerID = mode != SceneViewEditMode.None ? owner.GetInstanceID() : k_OwnerIdNone;

            if (editMode != SceneViewEditMode.None)
            {
                if (onEditModeStartDelegate != null && owner is Editor)
                    onEditModeStartDelegate(owner as Editor, editMode);

                if (editModeStarted != null)
                    editModeStarted(owner, editMode);
            }

            FocusEditModeToolTarget((mode != SceneViewEditMode.None && owner != null)
                ? owner.GetWorldBoundsOfTargets()
                : new Bounds(Vector3.zero, Vector3.positiveInfinity));

            InspectorWindow.RepaintAllInspectors();
        }

        // We make sure edited object is seen by the camera. We need object bounds to do that check.
        static void FocusEditModeToolTarget(Bounds bounds)
        {
            // When entering the edit mode, check if the collider is seen by the scene view camera. If not, then we frame it.
            if (editMode != SceneViewEditMode.None && SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
                if (!SeenByCamera(SceneView.lastActiveSceneView.camera, bounds))
                    SceneView.lastActiveSceneView.Frame(bounds, EditorApplication.isPlaying);

            SceneView.RepaintAll();
        }

        private static bool SeenByCamera(Camera camera, Bounds bounds)
        {
            return AnyPointSeenByCamera(camera, GetPoints(bounds));
        }

        private static Vector3[] GetPoints(Bounds bounds)
        {
            return BoundsToPoints(bounds);
        }

        private static Vector3[] BoundsToPoints(Bounds bounds)
        {
            Vector3[] result = new Vector3[8];
            result[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
            result[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            result[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            result[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
            result[4] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            result[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            result[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            result[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);
            return result;
        }

        private static bool AnyPointSeenByCamera(Camera camera, Vector3[] points)
        {
            foreach (Vector3 point in points)
                if (PointSeenByCamera(camera, point))
                    return true;

            return false;
        }

        private static bool PointSeenByCamera(Camera camera, Vector3 point)
        {
            Vector3 viewPoint = camera.WorldToViewportPoint(point);
            return viewPoint.x > 0.0f && viewPoint.x < 1.0f && viewPoint.y > 0.0f && viewPoint.y < 1.0f;
        }
    }
}
