// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    [EditorWindowTitle(title = "Edit Search Column Settings")]
    class ColumnEditor : EditorWindow
    {
        const int k_Width = 180;
        const int k_Height = 170;

        static ColumnEditor s_Window;

        public SearchTableViewColumn column { get; private set; }
        public Action<Column> editCallback { get; private set; }

        public static ColumnEditor ShowWindow(SearchTableViewColumn column, Action<Column> editCallback)
        {
            s_Window = GetWindowDontShow<ColumnEditor>();
            s_Window.column = column;
            s_Window.editCallback = editCallback;
            s_Window.minSize = new Vector2(k_Width, k_Height);
            s_Window.maxSize = new Vector2(k_Width * 2f, k_Height);
            var sc = column.searchColumn;
            if (sc != null)
                s_Window.titleContent = sc.content ?? s_Window.titleContent;
            s_Window.ShowAuxWindow();
            return s_Window;
        }

        static ColumnEditor()
        {
            EditorApplication.update += CloseOnLostFocus;
        }
        static void CloseOnLostFocus()
        {
            if (s_Window != null && focusedWindow && focusedWindow != s_Window)
            {
                s_Window.Close();
                s_Window = null;
            }
        }

        public void CreateGUI()
        {
            var sc = column.searchColumn;
            if (sc == null)
            {
                rootVisualElement.Add(new Label("No column is selected."));
                return;
            }

            var providers = new[] { "Default" }.Concat(SearchColumnProvider.providers.Select(p => p.provider)).ToList();
            var selectedProvider = Math.Max(0, providers.IndexOf(sc.provider));
            var formatPopup = new PopupField<string>(providers, selectedProvider, ObjectNames.NicifyVariableName, ObjectNames.NicifyVariableName)
            {
                label = L10n.Tr("Format"),
            };
            formatPopup.RegisterValueChangedCallback(evt =>
            {
                sc.SetProvider(evt.newValue);
                editCallback?.Invoke(column);
            });

            var iconField = new UIElements.ObjectField(L10n.Tr("Icon"))
            {
                objectType = typeof(Texture2D),
                value = column.icon.texture
            };
            iconField.RegisterValueChangedCallback(e =>
            {
                column.icon = Background.FromTexture2D((Texture2D)e.newValue);
                editCallback?.Invoke(column);
            });

            var nameField = new TextField(L10n.Tr("Name")) {value = column.title};
            nameField.RegisterValueChangedCallback(e =>
            {
                column.title = e.newValue;
                editCallback?.Invoke(column);
            });

            TextAlignment initialAlignment;
            switch (ItemSelectors.GetItemTextAlignment(sc))
            {
                case TextAnchor.MiddleCenter: initialAlignment = TextAlignment.Center; break;
                case TextAnchor.MiddleRight: initialAlignment = TextAlignment.Right; break;
                default: initialAlignment = TextAlignment.Left; break;
            }
            var alignmentField = new EnumField(L10n.Tr("Alignment"), TextAlignment.Left)
            {
                value = initialAlignment
            };
            alignmentField.RegisterValueChangedCallback(e =>
            {
                var newAlignment = (TextAlignment) e.newValue;
                sc.options &= ~SearchColumnFlags.TextAligmentMask;
                switch (newAlignment)
                {
                    case TextAlignment.Left: sc.options |= SearchColumnFlags.TextAlignmentLeft; break;
                    case TextAlignment.Center: sc.options |= SearchColumnFlags.TextAlignmentCenter; break;
                    case TextAlignment.Right: sc.options |= SearchColumnFlags.TextAlignmentRight; break;
                }
                editCallback?.Invoke(column);
            });

            var sortableField = new Toggle(L10n.Tr("Sortable")) {value = column.sortable};
            sortableField.RegisterValueChangedCallback(e =>
            {
                column.sortable = e.newValue;
                editCallback?.Invoke(column);
            });

            var pathField = new TextField(L10n.Tr("Path")) {value = sc.path};
            pathField.RegisterValueChangedCallback(e =>
            {
                sc.path = e.newValue;
                column.name = e.newValue;
                editCallback?.Invoke(column);
            });

            var selectorField = new TextField(L10n.Tr("Selector")) {value = sc.selector};
            selectorField.RegisterValueChangedCallback(e =>
            {
                sc.selector = e.newValue;
                editCallback?.Invoke(column);
            });

            rootVisualElement.Add(formatPopup);
            rootVisualElement.Add(iconField);
            rootVisualElement.Add(nameField);
            rootVisualElement.Add(alignmentField);
            rootVisualElement.Add(sortableField);
            rootVisualElement.Add(pathField);
            rootVisualElement.Add(selectorField);

            rootVisualElement.Query<Label>().ForEach(l => l.style.minWidth = 60f);

            rootVisualElement.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    Close();
                    e.StopImmediatePropagation();
                }

            }, invokePolicy: InvokePolicy.IncludeDisabled, useTrickleDown: TrickleDown.TrickleDown);
        }

        void OnGUI()
        {
            var ev = Event.current;
            if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape)
            {
                Close();
            }
        }
    }
}
