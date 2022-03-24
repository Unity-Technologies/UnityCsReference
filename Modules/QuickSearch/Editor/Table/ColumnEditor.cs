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

        public static ColumnEditor ShowWindow(MultiColumnHeaderState.Column column, Action<MultiColumnHeaderState.Column> editCallback)
        {
            var w = GetWindowDontShow<ColumnEditor>();
            w.column = column;
            w.editCallback = editCallback;
            w.minSize = new Vector2(k_Width, k_Height);
            w.maxSize = new Vector2(k_Width * 2f, k_Height);
            if (((PropertyColumn)column).userDataObj is SearchColumn sc)
                w.titleContent = sc.content ?? w.titleContent;
            w.ShowAuxWindow();
            return w;
        }

        internal void OnGUI()
        {
            if (!(((PropertyColumn)column).userDataObj is SearchColumn sc))
                return;

            EditorGUIUtility.labelWidth = 70f;

            GUILayout.BeginVertical();

            EditorGUI.BeginChangeCheck();
            var providers = new[] { "Default" }.Concat(SearchColumnProvider.providers.Select(p => p.provider)).ToArray();
            var selectedProvider = Math.Max(0, Array.IndexOf(providers, sc.provider));
            selectedProvider = EditorGUILayout.Popup(Utils.GUIContentTemp(L10n.Tr("Format")), selectedProvider, providers.Select(ObjectNames.NicifyVariableName).ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                sc.SetProvider(selectedProvider <= 0 ? null : providers[selectedProvider]);
                editCallback?.Invoke(column);
            }

            EditorGUI.BeginChangeCheck();

            var content = column.headerContent;
            using (new EditorGUIUtility.IconSizeScope(new Vector2(16, 16)))
                content.image = EditorGUILayout.ObjectField(EditorGUIUtility.TrTextContent("Icon"), content.image, typeof(Texture), allowSceneObjects: false) as Texture;
            content.text = EditorGUILayout.TextField(EditorGUIUtility.TrTextContent("Name"), content.text);
            column.headerTextAlignment = (TextAlignment)EditorGUILayout.EnumPopup(EditorGUIUtility.TrTextContent("Alignment"), column.headerTextAlignment);
            column.canSort = EditorGUILayout.Toggle(EditorGUIUtility.TrTextContent("Sortable"), column.canSort);

            sc.path = EditorGUILayout.TextField(EditorGUIUtility.TrTextContent("Path"), sc.path);
            sc.selector = EditorGUILayout.TextField(EditorGUIUtility.TrTextContent("Selector"), sc.selector);

            if (EditorGUI.EndChangeCheck())
            {
                sc.options &= ~SearchColumnFlags.Volatile;
                editCallback?.Invoke(column);
            }
            GUILayout.EndVertical();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
                Event.current.Use();
            }
        }
    }
}
