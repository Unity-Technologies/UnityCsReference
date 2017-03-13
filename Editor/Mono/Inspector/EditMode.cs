// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    // TODO: We need to make the editMode state e.g a string so users are able to extend with new edit mode states
    // and so we still can serialize it (to survive assembly reloads)

    [InitializeOnLoad]
    public class EditMode
    {
        private const string kEditModeStringKey = "EditModeState";
        private const string kPrevToolStringKey = "EditModePrevTool";
        private const string kOwnerStringKey = "EditModeOwner";
        private static bool s_Debug = false;

        static EditMode()
        {
            ownerID = SessionState.GetInt(kOwnerStringKey, ownerID);
            editMode = (SceneViewEditMode)SessionState.GetInt(kEditModeStringKey, (int)editMode);
            toolBeforeEnteringEditMode = (Tool)SessionState.GetInt(kPrevToolStringKey, (int)toolBeforeEnteringEditMode);
            Selection.selectionChanged += OnSelectionChange;

            if (s_Debug)
                Debug.Log("EditMode static constructor: " + ownerID + " " + editMode + " " + toolBeforeEnteringEditMode);
        }

        private static GUIStyle s_ToolbarBaseStyle;
        private static GUIStyle s_EditColliderButtonStyle;
        private const float k_EditColliderbuttonWidth = 33;
        private const float k_EditColliderbuttonHeight = 23;
        private const float k_SpaceBetweenLabelAndButton = 5;

        public static OnEditModeStopFunc onEditModeEndDelegate;
        public delegate void OnEditModeStopFunc(Editor editor);

        public static OnEditModeStartFunc onEditModeStartDelegate;
        public delegate void OnEditModeStartFunc(Editor editor, SceneViewEditMode mode);

        private static Tool s_ToolBeforeEnteringEditMode = Tool.Move;
        private static int s_OwnerID;
        private static SceneViewEditMode s_EditMode;

        public enum SceneViewEditMode
        {
            None = 0,
            Collider,
            Cloth,
            ReflectionProbeBox,
            ReflectionProbeOrigin,
            LightProbeProxyVolumeBox,
            LightProbeProxyVolumeOrigin,
            LightProbeGroup,
            ParticleSystemCollisionModulePlanesMove,
            ParticleSystemCollisionModulePlanesRotate,
        }

        private static Tool toolBeforeEnteringEditMode
        {
            get { return s_ToolBeforeEnteringEditMode; }
            set
            {
                s_ToolBeforeEnteringEditMode = value;
                SessionState.SetInt(kPrevToolStringKey, (int)s_ToolBeforeEnteringEditMode);
                if (s_Debug)
                    Debug.Log("Set toolBeforeEnteringEditMode " + value);
            }
        }

        public static bool IsOwner(Editor editor)
        {
            return editor.GetInstanceID() == ownerID;
        }

        private static int ownerID
        {
            get
            {
                return s_OwnerID;
            }
            set
            {
                s_OwnerID = value;
                SessionState.SetInt(kOwnerStringKey, s_OwnerID);
                if (s_Debug)
                    Debug.Log("Set ownerID " + value);
            }
        }


        public static SceneViewEditMode editMode
        {
            get
            {
                return s_EditMode;
            }
            private set
            {
                if (s_EditMode == SceneViewEditMode.None && value != SceneViewEditMode.None)
                {
                    // We consider Tool.None to be exotic fallback state and want to always recover to something else (like move) instead
                    toolBeforeEnteringEditMode = Tools.current != Tool.None ? Tools.current : Tool.Move;
                    Tools.current = Tool.None;
                }
                else if (s_EditMode != SceneViewEditMode.None && value == SceneViewEditMode.None)
                {
                    ResetToolToPrevious();
                }
                s_EditMode = value;
                SessionState.SetInt(kEditModeStringKey, (int)s_EditMode);
                if (s_Debug)
                    Debug.Log("Set editMode " + s_EditMode);
            }
        }

        public static void ResetToolToPrevious()
        {
            if (Tools.current == Tool.None)
                Tools.current = toolBeforeEnteringEditMode;
        }

        static void EndSceneViewEditing()
        {
            ChangeEditMode(SceneViewEditMode.None, new Bounds(), null);
        }

        public static void OnSelectionChange()
        {
            QuitEditMode();
        }

        public static void QuitEditMode()
        {
            if (Tools.current == Tool.None && EditMode.editMode != EditMode.SceneViewEditMode.None)
                EditMode.ResetToolToPrevious();
            EndSceneViewEditing();
        }

        static void DetectMainToolChange()
        {
            if (Tools.current != Tool.None && editMode != SceneViewEditMode.None)
                EndSceneViewEditing();
        }

        public static void DoEditModeInspectorModeButton(SceneViewEditMode mode, string label, GUIContent icon, Bounds bounds, Editor caller)
        {
            // Editing for prefabs makes no sense with the current prefab inspector implementation
            if (EditorUtility.IsPersistent(caller.target))
                return;

            DetectMainToolChange();

            if (s_EditColliderButtonStyle == null)
            {
                s_EditColliderButtonStyle = new GUIStyle("Button");
                s_EditColliderButtonStyle.padding = new RectOffset(0, 0, 0, 0);
                s_EditColliderButtonStyle.margin = new RectOffset(0, 0, 0, 0);
            }

            Rect rect = EditorGUILayout.GetControlRect(true, k_EditColliderbuttonHeight);
            Rect buttonRect = new Rect(rect.xMin + EditorGUIUtility.labelWidth, rect.yMin, k_EditColliderbuttonWidth, k_EditColliderbuttonHeight);

            GUIContent labelContent = new GUIContent(label);
            Vector2 labelSize = GUI.skin.label.CalcSize(labelContent);

            Rect labelRect = new Rect(
                    buttonRect.xMax + k_SpaceBetweenLabelAndButton,
                    rect.yMin + (rect.height - labelSize.y) * .5f,
                    labelSize.x,
                    rect.height);

            int callerID = caller.GetInstanceID();
            bool modeEnabled = editMode == mode && ownerID == callerID;

            EditorGUI.BeginChangeCheck();

            bool toggleEnabled = GUI.Toggle(buttonRect, modeEnabled, icon, s_EditColliderButtonStyle);
            GUI.Label(labelRect, label);

            if (EditorGUI.EndChangeCheck())
            {
                ChangeEditMode(toggleEnabled ? mode : SceneViewEditMode.None, bounds, caller);
            }
        }

        public static void DoInspectorToolbar(SceneViewEditMode[] modes, GUIContent[] guiContents, Bounds bounds, Editor caller)
        {
            // Editing for prefabs makes no sense with the current prefab inspector implementation
            if (EditorUtility.IsPersistent(caller.target))
                return;

            DetectMainToolChange();

            if (s_ToolbarBaseStyle == null)
                s_ToolbarBaseStyle = "Command";

            int callerID = caller.GetInstanceID();

            int selectedIndex = ArrayUtility.IndexOf(modes, editMode);
            if (ownerID != callerID)
                selectedIndex = -1;
            EditorGUI.BeginChangeCheck();
            int newSelectedIndex = GUILayout.Toolbar(selectedIndex, guiContents, s_ToolbarBaseStyle);
            if (EditorGUI.EndChangeCheck())
            {
                // Buttons can be toggled
                SceneViewEditMode newEditMode = newSelectedIndex == selectedIndex ? SceneViewEditMode.None : modes[newSelectedIndex];
                ChangeEditMode(newEditMode, bounds, caller);
            }
        }

        public static void ChangeEditMode(SceneViewEditMode mode, Bounds bounds, Editor caller)
        {
            Editor oldOwner = InternalEditorUtility.GetObjectFromInstanceID(ownerID) as Editor;

            editMode = mode;

            ownerID = mode != SceneViewEditMode.None ? caller.GetInstanceID() : 0;

            if (onEditModeEndDelegate != null)
                onEditModeEndDelegate(oldOwner);

            if (editMode != SceneViewEditMode.None && onEditModeStartDelegate != null)
                onEditModeStartDelegate(caller, editMode);

            EditModeChanged(bounds);

            InspectorWindow.RepaintAllInspectors();
        }

        // We make sure edited object is seen by the camera. We need object bounds to do that check.
        private static void EditModeChanged(Bounds bounds)
        {
            // When entering the edit mode, check if the collider is seen by the scene view camera. If not, then we frame it.
            if (editMode != SceneViewEditMode.None && SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
                if (!SeenByCamera(SceneView.lastActiveSceneView.camera, bounds))
                    SceneView.lastActiveSceneView.Frame(bounds);

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
