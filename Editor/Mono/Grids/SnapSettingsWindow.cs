// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    sealed class SnapSettingsWindow : PopupWindowContent
    {
        static class Contents
        {
            public static readonly GUIContent snapSettings = EditorGUIUtility.TrTextContent("Snap Settings");
            public static readonly GUIContent moveValue = EditorGUIUtility.TrTextContent("Move", "Snap value for the Move tool");
            public static readonly GUIContent moveX = EditorGUIUtility.TrTextContent("X", "X snap value");
            public static readonly GUIContent moveY = EditorGUIUtility.TrTextContent("Y", "Y snap value");
            public static readonly GUIContent moveZ = EditorGUIUtility.TrTextContent("Z", "Z snap value");
            public static readonly GUIContent rotateValue = EditorGUIUtility.TrTextContent("Rotate", "Snap value for the Rotate tool");
            public static readonly GUIContent scaleValue = EditorGUIUtility.TrTextContent("Scale", "Snap value for the Scale tool");
            public static readonly GUIContent pushToGrid = EditorGUIUtility.TrIconContent("SceneViewPushToGrid", "Snaps selected object to the grid");
            public static readonly GUIContent pushX = EditorGUIUtility.TrIconContent("SceneViewPushToGrid", "Snaps selected object to the grid on the X axis");
            public static readonly GUIContent pushY = EditorGUIUtility.TrIconContent("SceneViewPushToGrid", "Snaps selected object to the grid on the Y axis");
            public static readonly GUIContent pushZ = EditorGUIUtility.TrIconContent("SceneViewPushToGrid", "Snaps selected object to the grid on the Z axis");
            public static readonly GUIContent reset = EditorGUIUtility.TrTextContent("Reset");
            public static readonly GUIContent preferGrid = EditorGUIUtility.TrTextContent("Prefer Grid", "When moving a handle along a cardinal direction, handles will snap to the nearest grid point instead of increments from the handle origin.");
        }

        static class Styles
        {
            public static readonly GUIStyle separator = "sv_iconselector_sep";
            public static readonly GUIStyle header = EditorStyles.boldLabel;
            public static readonly GUIStyle button = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(2, 2, 2, 2),
            };
        }

        const float k_LabelWidth = 80;
        const float k_WindowWidth = 200 + k_LabelWidth;
        readonly float k_WindowHeight = EditorGUIUtility.singleLineHeight * 10 + 5;
        readonly float k_PushToGridIconWidth = EditorGUIUtility.singleLineHeight;
        readonly float k_SettingsIconSize = EditorGUIUtility.singleLineHeight;

        static bool snapValueLinked
        {
            get { return EditorPrefs.GetBool("SnapSettingsWindow.snapValueLinked", true); }
            set { EditorPrefs.SetBool("SnapSettingsWindow.snapValueLinked", value); }
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(k_WindowWidth, k_WindowHeight);
        }

        public override void OnGUI(Rect rect)
        {
            DrawTitleSettingsButton(rect);
            Draw();

            // Use mouse move so we get hover state correctly in the menu item rows
            if (Event.current.type == EventType.MouseMove)
                Event.current.Use();

            // Escape closes the window
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        void Draw()
        {
            EditorGUIUtility.labelWidth = k_LabelWidth;

            GUILayout.Label(Contents.snapSettings, Styles.header);

            EditorGUI.BeginChangeCheck();

            DrawMoveValuesFields();

            DoSeparator();

            EditorSnapSettings.rotate = EditorGUILayout.FloatField(Contents.rotateValue, EditorSnapSettings.rotate);

            EditorSnapSettings.scale = EditorGUILayout.FloatField(Contents.scaleValue, EditorSnapSettings.scale);

            if (EditorGUI.EndChangeCheck())
                EditorSnapSettings.Save();

            EditorGUIUtility.labelWidth = 0;
        }

        void DoSeparator()
        {
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            GUILayout.Label(GUIContent.none, Styles.separator);
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
        }

        void DrawMoveValuesFields()
        {
            EditorSnapSettings.preferGrid = EditorGUILayout.Toggle(Contents.preferGrid, EditorSnapSettings.preferGrid);

            var v = EditorSnapSettings.move;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var linked = snapValueLinked;
                var newValue = DoFloatFieldWithLink(Contents.moveValue, v.x, ref linked);
                if (EditorGUI.EndChangeCheck())
                {
                    snapValueLinked = linked;
                    EditorSnapSettings.move = new Vector3(newValue, newValue, newValue);
                }

                if (GUILayout.Button(Contents.pushToGrid, Styles.button, GUILayout.Width(k_PushToGridIconWidth)))
                {
                    SnapSelectionToGrid();
                }
            }

            ++EditorGUI.indentLevel;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(snapValueLinked))
                {
                    var value = EditorSnapSettings.move;
                    EditorGUI.BeginChangeCheck();
                    using (new EditorGUI.DisabledScope(snapValueLinked))
                    {
                        var newValue = EditorGUILayout.FloatField(Contents.moveX, value.x);

                        if (EditorGUI.EndChangeCheck())
                        {
                            EditorSnapSettings.move = new Vector3(newValue, value.y, value.z);
                        }
                    }
                }

                if (GUILayout.Button(Contents.pushX, Styles.button, GUILayout.Width(k_PushToGridIconWidth)))
                {
                    SnapSelectionToGrid(SnapAxis.X);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(snapValueLinked))
                {
                    var value = EditorSnapSettings.move;
                    EditorGUI.BeginChangeCheck();
                    var newValue = EditorGUILayout.FloatField(Contents.moveY, value.y);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorSnapSettings.move = new Vector3(value.x, newValue, value.z);
                    }
                }

                if (GUILayout.Button(Contents.pushY, Styles.button, GUILayout.Width(k_PushToGridIconWidth)))
                    SnapSelectionToGrid(SnapAxis.Y);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(snapValueLinked))
                {
                    var value = EditorSnapSettings.move;
                    EditorGUI.BeginChangeCheck();
                    var newValue = EditorGUILayout.FloatField(Contents.moveZ, value.z);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorSnapSettings.move = new Vector3(value.x, value.y, newValue);
                    }
                }

                if (GUILayout.Button(Contents.pushZ, Styles.button, GUILayout.Width(k_PushToGridIconWidth)))
                    SnapSelectionToGrid(SnapAxis.Z);
            }
            --EditorGUI.indentLevel;
        }

        public static bool IsMoveSnapValueMixed()
        {
            if (snapValueLinked)
                return false;

            return EditorSnapSettings.move.x != EditorSnapSettings.move.y || EditorSnapSettings.move.y != EditorSnapSettings.move.z;
        }

        float DoFloatFieldWithLink(GUIContent content, float value, ref bool linkToggle)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(content);

            float result;
            using (new EditorGUI.DisabledScope(!linkToggle))
            {
                EditorGUI.showMixedValue = IsMoveSnapValueMixed();
                result = EditorGUILayout.FloatField(value);
                EditorGUI.showMixedValue = false;
            }

            linkToggle = EditorGUILayout.Toggle(linkToggle, GUILayout.Width(15));
            EditorGUILayout.EndHorizontal();

            return result;
        }

        void DrawTitleSettingsButton(Rect rect)
        {
            var settingsRect = rect;
            settingsRect.x = settingsRect.xMax - k_SettingsIconSize;
            settingsRect.y = 0;
            settingsRect.width = settingsRect.height = k_SettingsIconSize;

            if (GUI.Button(settingsRect, EditorGUI.GUIContents.titleSettingsIcon, EditorStyles.iconButton))
                ShowContextMenu();
        }

        void ShowContextMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(Contents.reset, false, EditorSnapSettings.ResetSnapSettings);
            menu.ShowAsContext();
        }

        internal static void SnapSelectionToGrid(SnapAxis axis = SnapAxis.All)
        {
            var selections = Selection.transforms;
            if (selections != null && selections.Length > 0)
            {
                Undo.RecordObjects(selections, L10n.Tr("Snap to Grid"));
                Handles.SnapToGrid(selections, axis);
            }
        }
    }
}
