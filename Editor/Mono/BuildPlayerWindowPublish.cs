// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    public partial class BuildPlayerWindow : EditorWindow
    {
        PublishStyles publishStyles = null;

        class PublishStyles
        {
            public const int kIconSize = 32;
            public const int kRowHeight = 36;
            public GUIContent xiaomiIcon = EditorGUIUtility.IconContent("BuildSettings.Xiaomi");
            public GUIContent learnAboutXiaomiInstallation = EditorGUIUtility.TextContent("Installation and Setup");
            public GUIContent publishTitle = EditorGUIUtility.TextContent("SDKs for App Stores|Integrations with 3rd party app stores");
        }

        private void AndroidPublishGUI()
        {
            if (publishStyles == null)
                publishStyles = new PublishStyles();

            GUILayout.BeginVertical();
            GUILayout.Label(publishStyles.publishTitle, styles.title);

            // Show Xiaomi UI.
            using (new EditorGUILayout.HorizontalScope(styles.box, new GUILayoutOption[] { GUILayout.Height(PublishStyles.kRowHeight) }))
            {
                GUILayout.BeginVertical();
                GUILayout.Space(3); // fix top padding for box style

                GUILayout.BeginHorizontal();
                GUILayout.Space(4); // left padding

                // icon
                GUILayout.BeginVertical();
                GUILayout.Space((PublishStyles.kRowHeight - PublishStyles.kIconSize) / 2);
                GUILayout.Label(publishStyles.xiaomiIcon, new GUILayoutOption[] { GUILayout.Width(PublishStyles.kIconSize), GUILayout.Height(PublishStyles.kIconSize) });
                GUILayout.EndVertical();

                // label
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Xiaomi Mi Game Center");
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                // link
                GUILayout.FlexibleSpace(); // right justify text
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                if (EditorGUILayout.LinkLabel(publishStyles.learnAboutXiaomiInstallation))
                    Application.OpenURL("http://unity3d.com/partners/xiaomi/guide");
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();

                GUILayout.Space(4); // right padding
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
        }
    }
}
