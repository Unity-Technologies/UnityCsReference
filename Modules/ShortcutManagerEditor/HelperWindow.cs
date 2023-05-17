// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShortcutManagement
{
    internal class HelperWindow : EditorWindow
    {
        internal const int kHelperBarMinWidth = 300;

        // HelperWindow is not ready for use yet
        //[MenuItem("Window/General/Helper _%H")]
        static void ShowWindow() => GetWindow<HelperWindow>().Show();

        static HelperWindow() => EditorApplication.globalEventHandler += Init;

        static Dictionary<KeyCode, GUIContent> keyIcons;

        static void Init()
        {
            EditorApplication.globalEventHandler += UpdateContext;
            ContextManager.onTagChange += UpdateContext;
            Selection.selectionChanged += UpdateContext;
            focusedWindowChanged += UpdateContext;

            EditorApplication.globalEventHandler -= Init;
        }

        static bool HandleKey(Event evt)
        {
            if (evt == null) return false;

            var keyCode = evt.keyCode;
            if (keyCode == KeyCode.None)
            {
                Enum.TryParse(typeof(KeyCode), Event.current?.character.ToString() ?? string.Empty, true, out var charCode);
                keyCode = (KeyCode)(charCode ?? KeyCode.None);
            }

            if (keyCode == KeyCode.None)
            {
                keyCodes.Clear();
                return true;
            }
            else
            {
                switch (evt.type)
                {
                    case EventType.MouseDown:
                    case EventType.KeyDown:
                    case EventType.TouchDown:
                        keyCodes.Add(keyCode);
                        filterAction = EditorGUI.actionKey;
                        filterShift = evt.shift;
                        filterAlt = evt.alt;
                        break;
                    case EventType.MouseUp:
                    case EventType.KeyUp:
                    case EventType.TouchUp:
                        keyCodes.Remove(keyCode);
                        filterAction = false;
                        filterShift = false;
                        filterAlt = false;
                        break;
                    default:
                        keyCodes.Clear();
                        break;
                }
            }

            bool changed = keyHash != keyCodes.GetHashCode();
            keyHash = keyCode.GetHashCode();
            return changed;
        }

        static void UpdateContext()
        {
            // We don't consider contextual menu a window
            if (focusedWindow?.GetType() == typeof(EditorMenuExtensions.ContextMenu))
                return;

            HandleKey(Event.current);

            shortcuts.Clear();
            ShortcutIntegration.instance.directory.FindPotentialShortcutEntries(ShortcutIntegration.instance.contextManager, shortcuts);
            shortcuts = shortcuts.Where(s => s.combinations.Count > 0).OrderBy(s => s.priority).ThenBy(s => Path.GetFileName(s.displayName)).ToList();

            if (filterAction) shortcuts = shortcuts.Where(s => s.combinations.Any(c => c.action)).ToList();
            if (filterShift) shortcuts = shortcuts.Where(s => s.combinations.Any(c => c.shift)).ToList();
            if (filterAlt) shortcuts = shortcuts.Where(s => s.combinations.Any(c => c.alt)).ToList();

            if (HasOpenInstances<HelperWindow>())
            {
                if (activeContexts != null) activeContexts.value = string.Join(", ", ShortcutIntegration.instance.contextManager.GetActiveContexts().Select(t => t.Name));
                if (activeTags != null) activeTags.value = string.Join(", ", ShortcutIntegration.instance.contextManager.GetActiveTags());
                if (input != null) input.value = string.Join(" + ", keyCodes);

                if (shortcutList != null)
                {
                    shortcutList.itemsSource = shortcuts;
                    shortcutList.Rebuild();
                }
            }

            if (EditorPrefs.GetBool("EnableHelperBar", true)) AppStatusBar.StatusChanged();
        }

        static TextField activeContexts;
        static TextField activeTags;

        static HashSet<KeyCode> keyCodes = new HashSet<KeyCode>();
        static TextField input;

        static ListView shortcutList;
        static List<ShortcutEntry> shortcuts = new List<ShortcutEntry>();

        static int keyHash;

        static bool filterAction = false;
        static bool filterShift = false;
        static bool filterAlt = false;

        private void CreateGUI()
        {
            var container = new VisualElement();

            activeContexts = new TextField("Contexts");
            activeTags = new TextField("Tags");
            input = new TextField("Input");
            shortcutList = new ListView(shortcuts, 30, () =>
            {
                var container = new VisualElement();
                container.style.backgroundColor = new Color(0, 0, 0, 0.5f);
                container.style.borderBottomLeftRadius = container.style.borderBottomRightRadius = container.style.borderTopLeftRadius = container.style.borderTopRightRadius = 3;
                container.style.marginBottom = container.style.marginLeft = container.style.marginRight = container.style.marginTop = 2;
                container.style.flexDirection = FlexDirection.Row;

                var combination = new Label();
                combination.style.fontSize = 20;
                combination.style.minWidth = 150;
                combination.style.marginRight = 15;
                container.Add(combination);

                var infoContainer = new VisualElement();
                container.Add(infoContainer);

                var name = new Label();
                name.style.fontSize = 12;
                infoContainer.Add(name);

                var context = new Label();
                context.style.fontSize = 8;
                infoContainer.Add(context);

                return container;
            }, (e, i) =>
            {
                if (i >= shortcuts.Count) return;

                var combination = e.Children().First() as Label;
                var infoContainer = e.Children().Skip(1).First();
                var name = infoContainer.Children().First() as Label;
                var context = infoContainer.Children().Skip(1).First() as Label;

                combination.text = shortcuts[i].combinations.First().ToString();
                name.text = Path.GetFileName(shortcuts[i].displayName);
                context.text = shortcuts[i].context.Name + " " + shortcuts[i].tag;
            });

            container.Add(activeContexts);
            container.Add(activeTags);
            container.Add(input);
            container.Add(shortcutList);
            rootVisualElement.Add(container);
        }

        const int k_HorizontalEntryPadding = 4;
        const int space = 2;

        internal static void StatusBarShortcuts()
        {
            if (keyIcons == null)
            {
                keyIcons = new Dictionary<KeyCode, GUIContent>();
                keyIcons[KeyCode.Mouse0] = new GUIContent(EditorGUIUtility.LoadIconRequired($"Icons/HelperBar/{KeyCode.Mouse0}.png"));
                keyIcons[KeyCode.Mouse1] = new GUIContent(EditorGUIUtility.LoadIconRequired($"Icons/HelperBar/{KeyCode.Mouse1}.png"));
                keyIcons[KeyCode.Mouse2] = new GUIContent(EditorGUIUtility.LoadIconRequired($"Icons/HelperBar/{KeyCode.Mouse2}.png"));
                keyIcons[KeyCode.Mouse3] = new GUIContent(EditorGUIUtility.LoadIconRequired($"Icons/HelperBar/{KeyCode.Mouse3}.png"));
                keyIcons[KeyCode.Mouse4] = new GUIContent(EditorGUIUtility.LoadIconRequired($"Icons/HelperBar/{KeyCode.Mouse4}.png"));
            }

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.MaxWidth(8192), GUILayout.MinWidth(kHelperBarMinWidth));
            var r = EditorGUILayout.GetControlRect(false);
            r.y -= space;
            r.height += 1;
            foreach (var shortcut in shortcuts)
            {
                var name = Path.GetFileName(shortcut.displayName);
                var nameContent = new GUIContent(name);

                var combination = shortcut.combinations.First();
                var combinationText = combination.ToString();

                keyIcons.TryGetValue(combination.keyCode, out var icon);
                int iconWidth = 0;
                if (icon != null)
                {
                    iconWidth = (int)r.height;
                    combinationText = combinationText.Substring(0, combinationText.LastIndexOf("+") + 1);
                }
                var combinationContent = new GUIContent(combinationText);

                var nameWidth = EditorStyles.label.CalcSize(nameContent).x;
                var combinationWidth = EditorStyles.boldLabel.CalcSize(combinationContent).x;
                var width = k_HorizontalEntryPadding + combinationWidth + iconWidth + space + nameWidth + k_HorizontalEntryPadding + 1;

                if (r.width < width) break;

                var shortcutBox = new Rect(r.x, r.y, width, r.height);
                GUI.Box(shortcutBox, GUIContent.none, EditorStyles.miniButton);
                GUI.Label(new Rect(r.x + k_HorizontalEntryPadding, r.y - 1, combinationWidth, r.height), combinationContent, EditorStyles.boldLabel);
                if (iconWidth > 0) GUI.Box(new Rect(r.x + k_HorizontalEntryPadding + combinationWidth, r.y + 1, iconWidth, r.height), icon, GUIStyle.none);
                GUI.Label(new Rect(r.x + k_HorizontalEntryPadding + combinationWidth + iconWidth + space, r.y - 1, nameWidth, r.height), nameContent, EditorStyles.label);
                r.xMin += width + space * 3;

                var evt = Event.current;
                if (evt.type == EventType.MouseDown && shortcutBox.Contains(evt.mousePosition))
                {
                    if (evt.button == 0)
                    {
                        ShortcutManagerWindow shortcutManager;
                        if (HasOpenInstances<ShortcutManagerWindow>())
                        {
                            shortcutManager = GetWindow<ShortcutManagerWindow>();
                        }
                        else
                        {
                            shortcutManager = CreateInstance<ShortcutManagerWindow>();
                            shortcutManager.ShowUtility();
                        }
                        shortcutManager.rootVisualElement.Q<ToolbarPopupSearchField>().value = shortcut.displayName;
                        GUIUtility.ExitGUI();
                    }
                    else if (evt.button == 1)
                    {
                        GenericMenu contextMenu = new GenericMenu();
                        // HelperWindow is not ready for use yet
                        //contextMenu.AddItem(new GUIContent("Show Helper Window"), false, () => ShowWindow());
                        contextMenu.AddItem(new GUIContent("Disable Helper Bar"), false, () => EditorPrefs.SetBool("EnableHelperBar", false));
                        contextMenu.ShowAsContext();
                    }
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
