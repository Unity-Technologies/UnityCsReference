// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Search
{
    [EditorWindowTitle(title = "Edit Search Column Settings")]
    class ColumnEditor : EditorWindow
    {
        const int k_Width = 200;
        const int k_Height = 220;

        public MultiColumnHeaderState.Column column { get; private set; }
        public Action<MultiColumnHeaderState.Column> editCallback { get; private set; }

        internal static ColumnEditor ShowWindow(MultiColumnHeaderState.Column column, Action<MultiColumnHeaderState.Column> editCallback)
        {
            var w = GetWindowDontShow<ColumnEditor>();
            w.column = column;
            w.editCallback = editCallback;
            w.minSize = new Vector2(k_Width, k_Height);
            w.maxSize = new Vector2(k_Width * 2f, k_Height);
            if (column.userDataObj is SearchColumn sc)
                w.titleContent = sc.content ?? w.GetLocalizedTitleContent();
            w.ShowAuxWindow();
            return w;
        }

        internal void OnGUI()
        {
            if (!(column.userDataObj is SearchColumn sc))
                return;

            EditorGUIUtility.labelWidth = 70f;

            GUILayout.BeginVertical();

            EditorGUI.BeginChangeCheck();
            var providers = new[] { "Default" }.Concat(SearchColumnProvider.providers.Select(p => p.provider)).ToArray();
            var selectedProvider = Math.Max(0, Array.IndexOf(providers, sc.provider));
            selectedProvider = EditorGUILayout.Popup(GUIContent.Temp("Format"), selectedProvider, providers.Select(ObjectNames.NicifyVariableName).ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                sc.SetProvider(selectedProvider <= 0 ? null : providers[selectedProvider]);
                editCallback?.Invoke(column);
            }

            EditorGUI.BeginChangeCheck();

            var content = column.headerContent;
            using (new EditorGUIUtility.IconSizeScope(new Vector2(16, 16)))
                content.image = EditorGUILayout.ObjectField(new GUIContent("Icon"), content.image, typeof(Texture), allowSceneObjects: false) as Texture;
            content.text = EditorGUILayout.TextField(new GUIContent("Name"), content.text);
            column.headerTextAlignment = (TextAlignment)EditorGUILayout.EnumPopup(new GUIContent("Alignment"), column.headerTextAlignment);
            column.canSort = EditorGUILayout.Toggle(new GUIContent("Sortable"), column.canSort);

            EditorGUI.BeginDisabled(!Unsupported.IsSourceBuild());
            sc.path = EditorGUILayout.TextField(new GUIContent("Path"), sc.path);
            sc.selector = EditorGUILayout.TextField(new GUIContent("Selector"), sc.selector);
            EditorGUI.EndDisabled();

            if (EditorGUI.EndChangeCheck())
            {
                sc.options &= ~SearchColumnFlags.Volatile;
                editCallback?.Invoke(column);
            }
            GUILayout.EndVertical();
        }
    }
}
