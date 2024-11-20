// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using UnityEditor.EditorTools;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    static class ShortcutHelperBarUtility
    {
        const string k_IconsParentPath = "Icons/ShortcutHelperBar/";

        static bool s_FilterControl;
        static bool s_FilterAction;
        static bool s_FilterShift;
        static bool s_FilterAlt;
        public static bool filterControl => s_FilterControl;
        public static bool filterAction => s_FilterAction;
        public static bool filterShift => s_FilterShift;
        public static bool filterAlt => s_FilterAlt;

        static Texture2D[] s_MouseIcons = new Texture2D[3];
        static Texture2D[] s_MouseDragIcons = new Texture2D[3];
        static string[] s_ExtraMouseButtons = new string[4];

        static string m_ControlModifierLabel;
        static string m_ActionModifierLabel;
        static string m_ShiftModifierLabel;
        static string m_AltModifierLabel;
        public static string controlModifierLabel => m_ControlModifierLabel;
        public static string actionModifierLabel => m_ActionModifierLabel;
        public static string shiftModifierLabel => m_ShiftModifierLabel;
        public static string altModifierLabel => m_AltModifierLabel;

        static GUIContent[] m_MouseButtonContent = new GUIContent[3];
        static GUIContent[] m_MouseDragContent = new GUIContent[3];
        static GUIContent[] m_ExtraMouseButtonContent = new GUIContent[4];

        static (ClutchShortcutContext context, KeyCode keyCode) s_ClutchShortcutContext;

        static List<ShortcutEntry> s_Shortcuts = new();

        static SortedDictionary<ShortcutModifiers, List<ShortcutEntry>> s_GroupedShortcuts = new(new SortShortcutModifierHelper());

        public static ReadOnlyDictionary<ShortcutModifiers, List<ShortcutEntry>> groupedShortcuts => new (s_GroupedShortcuts);

        static readonly EventType[] k_EventFilter = new EventType[]
        {
            EventType.KeyDown,
            EventType.KeyUp,
            EventType.MouseDown,
            EventType.MouseUp,
            EventType.ScrollWheel,
            EventType.TouchDown,
            EventType.TouchUp
        };

        static HashSet<IShortcutUpdate> s_Clients = new();
        public interface IShortcutUpdate
        {
            void OnShortcutUpdate();
        }

        class SortShortcutModifierHelper : IComparer<ShortcutModifiers>
        {
            static bool IsPowerOfTwo(ShortcutModifiers value)
            {
                return (value & (value - 1)) == 0;
            }

            // This compare function is sorting the shortcuts by the number of modifiers and by the type of modifiers that they have.
            // We show the shortcuts that have single modifiers first, followed by the ones that have two modifiers, etc.
            // And then we order the groups of shortcuts by the type of their modifiers; to show the modifiers in the order of their flag bit
            // (None = 0, Alt = 1, Action = 2, Shift = 4, Control = 8).
            int IComparer<ShortcutModifiers>.Compare(ShortcutModifiers firstValue, ShortcutModifiers secondValue)
            {
                var result = firstValue.CompareTo(secondValue);
                if (result != 0)
                {
                    var currentModifiers = GetFilterCondition();
                    if (firstValue.Equals(currentModifiers))
                        return -1;

                    if (secondValue.Equals(currentModifiers))
                        return 1;

                    var isFirstPowerOfTwo = IsPowerOfTwo(firstValue);
                    var isSecondPowerOfTwo = IsPowerOfTwo(secondValue);

                    if (isFirstPowerOfTwo != isSecondPowerOfTwo)
                        return isFirstPowerOfTwo ? -1 : 1;

                    return result;
                }

                return result;
            }
        }

        static ShortcutHelperBarUtility()
        {
            // Load mouse icons.
            s_MouseIcons[0] =
                EditorGUIUtility.LoadIconRequired(string.Format("{0}Mouse{1}.png", k_IconsParentPath, "Right"));
            s_MouseIcons[1] =
                EditorGUIUtility.LoadIconRequired(string.Format("{0}Mouse{1}.png", k_IconsParentPath, "Left"));
            s_MouseIcons[2] =
                EditorGUIUtility.LoadIconRequired(string.Format("{0}Mouse{1}.png", k_IconsParentPath, "Middle"));

            // Load mouse drag icons.
            s_MouseDragIcons[0] =
                EditorGUIUtility.LoadIconRequired(string.Format("{0}Mouse{1}-Drag.png", k_IconsParentPath, "Right"));
            s_MouseDragIcons[1] =
                EditorGUIUtility.LoadIconRequired(string.Format("{0}Mouse{1}-Drag.png", k_IconsParentPath, "Left"));
            s_MouseDragIcons[2] =
                EditorGUIUtility.LoadIconRequired(string.Format("{0}Mouse{1}-Drag.png", k_IconsParentPath, "Middle"));

            // Initialize Mouse3 to Mouse6 text content.
            s_ExtraMouseButtons[0] = "M3";
            s_ExtraMouseButtons[1] = "M4";
            s_ExtraMouseButtons[2] = "M5";
            s_ExtraMouseButtons[3] = "M6";

            m_ControlModifierLabel = CreateModifierLabel(ShortcutModifiers.Control);
            m_ActionModifierLabel = CreateModifierLabel(ShortcutModifiers.Action);
            m_ShiftModifierLabel = CreateModifierLabel(ShortcutModifiers.Shift);
            m_AltModifierLabel = CreateModifierLabel(ShortcutModifiers.Alt);

            m_MouseButtonContent[0] = new GUIContent(s_MouseIcons[0]);
            m_MouseButtonContent[1] = new GUIContent(s_MouseIcons[1]);
            m_MouseButtonContent[2] = new GUIContent(s_MouseIcons[2]);

            m_MouseDragContent[0] = new GUIContent(s_MouseDragIcons[0]);
            m_MouseDragContent[1] = new GUIContent(s_MouseDragIcons[1]);
            m_MouseDragContent[2] = new GUIContent(s_MouseDragIcons[2]);

            m_ExtraMouseButtonContent[0] = new GUIContent(s_ExtraMouseButtons[0]);
            m_ExtraMouseButtonContent[1] = new GUIContent(s_ExtraMouseButtons[1]);
            m_ExtraMouseButtonContent[2] = new GUIContent(s_ExtraMouseButtons[2]);
            m_ExtraMouseButtonContent[3] = new GUIContent(s_ExtraMouseButtons[3]);
        }

        public static void OnClientEnable(IShortcutUpdate client)
        {
            // Register to the callbacks when we're adding the first client.
            if (s_Clients.Count == 0)
            {
                GUIUtility.beforeEventProcessed += HandleKey;
                // We need to delay updating the shortcuts to prevent the UI from refreshing before the click event is handled
                // (e.g., when opening the shortcut manager by clicking on a shortcut).
                // This update should also occur after the Clutch contexts are set.
                EditorApplication.shortcutHelperBarEventHandler += UpdateShortcuts;
                Selection.selectionChanged += UpdateShortcuts;
                SceneView.lastActiveSceneViewChanged += SceneViewChanged;
                EditorToolManager.activeToolChanged += ActiveToolChanged;

                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.modeChanged2D += UpdateShortcuts2DModeChanged;
            }

            s_Clients.Add(client);
        }

        public static void OnClientDisable(IShortcutUpdate client)
        {
            // Unregister the callbacks when there are no more clients.
            s_Clients.Remove(client);
            ClearIfNoClients();
        }

        public static void RemoveAppStatusBarClient()
        {
            s_Clients.RemoveWhere(c => c is ShortcutHelperBar);
            ClearIfNoClients();
        }

        static void UpdateClients()
        {
            foreach (var client in s_Clients)
                client.OnShortcutUpdate();
        }

        static void ClearIfNoClients()
        {
            if (s_Clients.Count == 0)
            {
                GUIUtility.beforeEventProcessed -= HandleKey;
                EditorApplication.shortcutHelperBarEventHandler -= UpdateShortcuts;
                Selection.selectionChanged -= UpdateShortcuts;
                SceneView.lastActiveSceneViewChanged -= SceneViewChanged;
                EditorToolManager.activeToolChanged -= ActiveToolChanged;

                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.modeChanged2D -= UpdateShortcuts2DModeChanged;

                Reset();
            }
        }

        public static void Reset()
        {
            s_Shortcuts.Clear();
            s_GroupedShortcuts.Clear();

            s_FilterControl = false;
            s_FilterAction = false;
            s_FilterShift = false;
            s_FilterAlt = false;

            ResetClutchShortcutContext();
        }

        static void ActiveToolChanged(EditorTool oldTool, EditorTool newTool)
        {
            UpdateShortcuts();
        }

        static void SceneViewChanged(SceneView oldScene, SceneView newScene)
        {
            if (oldScene != null)
                oldScene.modeChanged2D -= UpdateShortcuts2DModeChanged;

            if (newScene != null)
                newScene.modeChanged2D += UpdateShortcuts2DModeChanged;
        }

        static void UpdateShortcuts2DModeChanged(bool mode)
        {
            UpdateShortcuts();
        }

        static bool s_UpdateShortcuts;
        static EventType s_PreviousEventType;
        static KeyCode s_PreviousKeyCode;
        static EventModifiers s_PreviousModifiers;

        static void HandleKey(EventType type, KeyCode keyCode, EventModifiers modifiers)
        {
            var isCorrectInputEventType = false;
            foreach (var item in k_EventFilter)
            {
                if (item == type)
                {
                    isCorrectInputEventType = true;
                    break;
                }
            }

            // On Mac, pressing only a modifier key triggers a Repaint event.
            // Therefore, we need to retain the event if the modifier key has changed.
            if (modifiers == s_PreviousModifiers)
            {
                if (keyCode == KeyCode.None)
                    return;

                if (!isCorrectInputEventType)
                    return;
            }

            s_UpdateShortcuts = true;
            s_PreviousEventType = type;
            s_PreviousKeyCode = keyCode;
            s_PreviousModifiers = modifiers;

            UpdateModifierFilters(modifiers);

            // This case applies to Mac when modifier keys pressed alone don't trigger input events.
            // In this situation, the globalEventHandler is not invoked.
            // Since we don't handle mouse clicks, we can update the shortcuts immediately.
            if (!isCorrectInputEventType)
                UpdateShortcuts();
        }

        static void UpdateShortcuts()
        {
            if (!s_UpdateShortcuts)
                return;

            s_UpdateShortcuts = false;

            if (s_PreviousKeyCode == s_ClutchShortcutContext.keyCode && (s_PreviousEventType == EventType.MouseUp || s_PreviousEventType == EventType.TouchUp))
                ResetClutchShortcutContext();
            else if (s_PreviousEventType == EventType.MouseDown || s_PreviousEventType == EventType.TouchDown)
                GetClutchShortcutContext(s_PreviousKeyCode);

            UpdateContext();
        }

        static void ResetClutchShortcutContext()
        {
            s_ClutchShortcutContext.context = null;
            s_ClutchShortcutContext.keyCode = KeyCode.None;
        }

        static void GetClutchShortcutContext(KeyCode keyCode)
        {
            var trigger = ShortcutIntegration.instance.trigger;
            if (trigger.m_ClutchActivatedContexts == null || trigger.m_ClutchActivatedContexts.Count != 1)
                return;

            foreach (var kvp in trigger.m_ClutchActivatedContexts)
            {
                s_ClutchShortcutContext.context = kvp.Value;
                s_ClutchShortcutContext.keyCode = keyCode;
                break;
            }
        }

        static void UpdateModifierFilters(EventModifiers modifiers)
        {
            s_FilterControl = (modifiers & EventModifiers.Control) != 0;
            s_FilterAction = EditorGUI.actionKey;
            s_FilterShift = (modifiers & EventModifiers.Shift) != 0;
            s_FilterAlt = (modifiers & EventModifiers.Alt) != 0;
        }

        static void UpdateContext()
        {
            var shortcuts = new List<ShortcutEntry>();
            var groupedShortcuts = new SortedDictionary<ShortcutModifiers, List<ShortcutEntry>>(new SortShortcutModifierHelper());

            // Get all the potential shortcuts.
            var contextManager = ShortcutIntegration.instance.contextManager;
            ShortcutIntegration.instance.directory.FindPotentialShortcutEntries(contextManager, shortcuts, true);

            for (int i = shortcuts.Count - 1; i >= 0; i--)
            {
                var shortcut = shortcuts[i];
                if (shortcut.combinations.Count == 0)
                {
                    shortcuts.RemoveAt(i);
                    continue;
                }

                if ((s_ClutchShortcutContext.context != null && shortcut.context != s_ClutchShortcutContext.context.GetType()) // Get the shortcuts with an activated clutch context.
                    || (s_ClutchShortcutContext.context == null && !(shortcut.combinations[0].keyCode >= KeyCode.Mouse0 && shortcut.combinations[0].keyCode <= KeyCode.Mouse6))) // Get all the mouse shortcuts for the active contexts.
                {
                    shortcuts.RemoveAt(i);
                    continue;
                }

                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    if ((s_FilterControl && !shortcut.combinations[0].control) // Get all the shortcuts that have a control modifier.
                        || (s_FilterAction && !shortcut.combinations[0].action)) // Get all the shortcuts that have a command modifier.
                    {
                        shortcuts.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    // Get all the shortcuts that have a control modifier.
                    // Control and Action filter booleans should always have the same value.
                    if (s_FilterControl && s_FilterAction
                        && !shortcut.combinations[0].control && !shortcut.combinations[0].action)
                    {
                        shortcuts.RemoveAt(i);
                        continue;
                    }
                }

                if ((s_FilterShift && !shortcut.combinations[0].shift) // Get all the shortcuts that have a shift modifier.
                    || (s_FilterAlt && !shortcut.combinations[0].alt)) // Get all the shortcuts that have an alt modifier.
                {
                    shortcuts.RemoveAt(i);
                    continue;
                }
            }

            shortcuts.Sort(CompareShortcutEntries);

            // Group the shortcuts by modifiers.
            foreach (var shortcut in shortcuts)
            {
                var modifiers = shortcut.combinations[0].modifiers;
                if (Application.platform != RuntimePlatform.OSXEditor)
                {
                    if (modifiers.HasFlag(ShortcutModifiers.Control))
                        modifiers = (modifiers & ~ShortcutModifiers.Control) | ShortcutModifiers.Action;
                }

                if (groupedShortcuts.TryGetValue(modifiers, out List<ShortcutEntry> group))
                {
                    group.Add(shortcut);
                }
                else
                {
                    groupedShortcuts[modifiers] = new List<ShortcutEntry>();
                    groupedShortcuts[modifiers].Add(shortcut);
                }
            }

            if (ShortcutsHaveChanges(shortcuts, groupedShortcuts))
            {
                s_Shortcuts = shortcuts;
                s_GroupedShortcuts = groupedShortcuts;
                UpdateClients();
            }
        }

        static bool ShortcutsHaveChanges(List<ShortcutEntry> shortcuts, SortedDictionary<ShortcutModifiers, List<ShortcutEntry>> groupedShortcuts)
        {
            if (shortcuts.Count != s_Shortcuts.Count)
                return true;

            for (int i = 0; i < shortcuts.Count; i++)
            {
                if (shortcuts[i] != s_Shortcuts[i])
                    return true;
            }

            if (groupedShortcuts.Count != s_GroupedShortcuts.Count)
                return true;

            foreach (var key in groupedShortcuts.Keys)
            {
                if (!s_GroupedShortcuts.ContainsKey(key))
                    return true;

                if (groupedShortcuts[key].Count != s_GroupedShortcuts[key].Count)
                    return true;

                for (int i = 0; i < groupedShortcuts[key].Count; i++)
                {
                    if (groupedShortcuts[key][i] != s_GroupedShortcuts[key][i])
                        return true;
                }
            }

            return false;
        }

        public static ShortcutModifiers GetFilterCondition()
        {
            var condition = ShortcutModifiers.None;

            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                if (s_FilterControl)
                    condition |= ShortcutModifiers.Control;

                if (s_FilterAction)
                    condition |= ShortcutModifiers.Action;
            }
            else
            {
                if (s_FilterAction && s_FilterControl)
                    condition |= ShortcutModifiers.Action;
            }

            if (s_FilterShift)
                condition |= ShortcutModifiers.Shift;

            if (s_FilterAlt)
                condition |= ShortcutModifiers.Alt;

            return condition;
        }

        static int CompareShortcutEntries(ShortcutEntry firstValue, ShortcutEntry secondValue)
        {
            var returnValue = firstValue.combinations[0].keyCode
                    .CompareTo(secondValue.combinations[0].keyCode);

            if (returnValue == 0)
            {
                returnValue = firstValue.priority.CompareTo(secondValue.priority);

                if (returnValue == 0)
                    returnValue = Path.GetFileName(firstValue.displayName)
                        .CompareTo(Path.GetFileName(secondValue.displayName));
            }

            return returnValue;
        }

        static string CreateModifierLabel(ShortcutModifiers modifier)
        {
            var builder = new StringBuilder();
            KeyCombination.VisualizeModifiers(modifier, builder);
            var label = builder.ToString();
            if (label.EndsWith('+'))
                label = label.Remove(label.Length - 1);

            return label;
        }

        public static GUIContent GetMouseContentForShortcut(ShortcutEntry shortcut)
        {
            var combination = shortcut.combinations[0];
            var keyCode = combination.keyCode;
            var isMouseButton = keyCode >= KeyCode.Mouse0 && keyCode <= KeyCode.Mouse2;
            var isMouseExtraButton = keyCode >= KeyCode.Mouse3 && keyCode <= KeyCode.Mouse6;

            GUIContent content = null;
            if ((shortcut.type == ShortcutType.Action || shortcut.type == ShortcutType.Menu) && isMouseButton)
                content = m_MouseButtonContent[keyCode - KeyCode.Mouse0];
            else if (shortcut.type == ShortcutType.Clutch && isMouseButton)
                content = m_MouseDragContent[keyCode - KeyCode.Mouse0];
            else if (isMouseExtraButton)
                content = m_ExtraMouseButtonContent[keyCode - KeyCode.Mouse3];

            return content;
        }
    }
}
