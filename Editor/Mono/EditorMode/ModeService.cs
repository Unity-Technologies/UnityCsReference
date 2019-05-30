// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.UIElements;

using JSONObject = System.Collections.IDictionary;

namespace UnityEditor
{
    internal enum ModeAction
    {
    }

    internal enum ModeCapability
    {
        Layers,
        Layouts,
        LayoutSwitching,
        Playbar
    }

    [ExcludeFromDocs]
    public static class ModeService
    {
        private struct ModeEntry
        {
            public string id;
            public string name;
            public JSONObject data;
        }

        public struct ModeChangedArgs
        {
            public int prevIndex;
            public int nextIndex;
        }

        internal const string k_DefaultModeId = "default";
        internal const string k_ModeIndexKeyName = "mode-index";
        internal const string k_ModeLayoutKeyName = "mode-layout";
        internal const string k_CapabilitiesSectionName = "capabilities";
        internal const string k_ExecuteHandlersSectionName = "execute_handlers";
        internal const string k_LayoutsSectionName = "layouts";
        internal const string k_MenusSectionName = "menus";
        internal const string k_LabelSectionName = "label";
        internal const string k_ShortcutSectionName = "shortcuts";

        public static string[] modeNames => modes.Select(m => m.name).ToArray();
        public static int modeCount => modes.Length;

        public static string currentId => currentIndex == -1 ? k_DefaultModeId : modes[currentIndex].id;
        public static int currentIndex { get; private set; }
        private static ModeEntry[] modes { get; set; } = new ModeEntry[0];

        public static event Action<ModeChangedArgs> modeChanged;

        static ModeService()
        {
            LoadModes(true);

            modeChanged += OnModeChangeMenus;
            modeChanged += OnModeChangeLayouts;
        }

        public static void ChangeModeById(string modeId)
        {
            string lcModeId = modeId.ToLowerInvariant();
            int modeIndex = Array.FindIndex(modes, m => m.id == lcModeId);
            if (modeIndex != -1)
                ChangeModeByIndex(modeIndex);
        }

        public static void Update()
        {
            // TODO: how can this not be exposed as an public API?
            EditorApplication.RequestRepaintAllViews();
        }

        internal static void ChangeModeByIndex(int modeIndex)
        {
            if (currentIndex == modeIndex)
                return;

            var prevIndex = currentIndex;
            SetModeIndex(modeIndex);
            if (prevIndex != currentIndex)
            {
                try
                {
                    modeChanged += OnModeChangeUpdate;
                    RaiseModeChanged(prevIndex, currentIndex);
                }
                catch (Exception ex)
                {
                    SetModeIndex(prevIndex);
                    Debug.LogError($"Failed to change editor mode.\r\n{ex}");
                }
                finally
                {
                    modeChanged -= OnModeChangeUpdate;
                }
            }
        }

        internal static bool HasCapability(ModeCapability capability, bool defaultValue = false)
        {
            return HasCapability(capability.ToString().ToSnakeCase(), defaultValue);
        }

        internal static bool HasCapability(string capabilityName, bool defaultValue = false)
        {
            var lcCapabilityName = capabilityName.ToLower();
            var capabilities = GetModeDataSection(currentIndex, k_CapabilitiesSectionName) as JSONObject;
            if (capabilities == null)
                return defaultValue;

            if (!capabilities.Contains(lcCapabilityName) || capabilities[lcCapabilityName].GetType() != typeof(bool))
                return defaultValue;

            return (bool)capabilities[lcCapabilityName];
        }

        internal static bool Execute(ModeAction builtinAction, params object[] args)
        {
            return Execute(builtinAction.ToString(), args);
        }

        internal static bool Execute(ModeAction builtinAction, CommandHint hint, params object[] args)
        {
            return Execute(builtinAction.ToString(), hint, args);
        }

        internal static bool Execute(string actionName, params object[] args)
        {
            return Execute(actionName, CommandHint.Undefined, args);
        }

        internal static bool Execute(string actionName, CommandHint hint, params object[] args)
        {
            // Call some command/shortcut actions to execute the current action
            actionName = actionName.ToLower();
            var executeHandlers = GetModeDataSection(currentIndex, k_ExecuteHandlersSectionName) as JSONObject;
            if (executeHandlers == null)
                return false;

            if (!executeHandlers.Contains(actionName))
                return false;

            string commandId = (string)executeHandlers[actionName];
            if (!CommandService.Exists(commandId))
                return false;

            var result = CommandService.Execute(commandId, hint, args);
            return result == null || (bool)result;
        }

        internal static bool HasSection(int modeIndex, string sectionName)
        {
            if (!IsValidIndex(modeIndex))
                return false;
            return modes[modeIndex].data.Contains(sectionName);
        }

        internal static object GetModeDataSection(int modeIndex, string sectionName)
        {
            if (!IsValidIndex(modeIndex))
                return null;

            if (!modes[modeIndex].data.Contains(sectionName))
                return null;

            return modes[modeIndex].data[sectionName];
        }

        internal static IEnumerable<T> GetModeDataSectionList<T>(int modeIndex, string sectionName)
        {
            var list = GetModeDataSection(modeIndex, sectionName) as IList<object>;
            if (list == null)
                return null;
            return list.Cast<T>();
        }

        [CommandHandler("ModeService/Refresh")]
        internal static void Refresh(CommandExecuteContext c)
        {
            LoadModes();
        }

        internal static void RaiseModeChanged(int prevIndex, int nextIndex)
        {
            modeChanged?.Invoke(new ModeChangedArgs { prevIndex = prevIndex, nextIndex = nextIndex });

            // Not required when you start the editor in the default mode.
            if (prevIndex != -1 || nextIndex != 0)
            {
                EditorUtility.Internal_UpdateAllMenus();
                ShortcutIntegration.instance.RebuildShortcuts();
            }
        }

        internal static bool IsValidModeId(string id)
        {
            return !string.IsNullOrEmpty(id) && id.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.');
        }

        internal static string GetDefaultModeLayout(string modeId = null)
        {
            var layouts = GetModeDataSection(currentIndex, k_LayoutsSectionName) as IList<object>;
            if (layouts != null && layouts.Count > 0)
            {
                var layoutPath = layouts[0] as string;
                if (layoutPath != null)
                {
                    if (File.Exists(layoutPath))
                    {
                        return layoutPath;
                    }
                    else
                    {
                        Debug.LogWarning("Default Mode Layout: " + layoutPath + " doesn't exists.");
                    }
                }
            }
            return null;
        }

        internal static bool HasStartupMode()
        {
            return Application.HasARGV("editor-mode");
        }

        private static void LoadModes(bool checkStartupMode = false)
        {
            ScanModes();
            var currentModeIndex = LoadProjectPrefModeIndex();
            if (checkStartupMode && HasStartupMode())
            {
                var requestEditorMode = Application.GetValueForARGV("editor-mode");
                var modeIndex = Array.FindIndex(modes, m => m.id == requestEditorMode);
                if (modeIndex != -1)
                {
                    currentModeIndex = modeIndex;
                    SaveProjectPrefModeIndex(currentModeIndex);
                }
            }

            SetModeIndex(currentModeIndex);
            EditorApplication.delayCall += () => RaiseModeChanged(-1, currentIndex);
        }

        private static void ScanModes()
        {
            var modesData = new Dictionary<string, object> { [k_DefaultModeId] = new Dictionary<string, object> { [k_LabelSectionName] = "Default" } };
            var modeFilePaths = AssetDatabase.FindAssets("t:DefaultAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(IsEditorModeDescriptor).OrderBy(path => - new FileInfo(path).Length);
            foreach (var modeFilePath in modeFilePaths)
            {
                var json = SJSON.Load(modeFilePath);

                foreach (var rawModeId in json.Keys)
                {
                    var modeId = ((string)rawModeId).ToLower();
                    if (IsValidModeId(modeId))
                    {
                        if (modesData.ContainsKey(modeId))
                            modesData[modeId] = JsonUtils.DeepMerge(modesData[modeId] as JSONObject, json[modeId] as JSONObject);
                        else
                            modesData[modeId] = json[modeId];
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid Mode Id: {modeId} contains non alphanumeric characters.");
                    }
                }
            }

            modes = new ModeEntry[modesData.Keys.Count];
            modes[0] = CreateEntry(k_DefaultModeId, (JSONObject)modesData[k_DefaultModeId]);
            var modeIndex = 1;
            foreach (var modeId in modesData.Keys)
            {
                if (modeId == k_DefaultModeId)
                    continue;
                modes[modeIndex] = CreateEntry(modeId, (JSONObject)modesData[modeId]);
                modeIndex++;
            }
        }

        private static ModeEntry CreateEntry(string modeId, JSONObject data)
        {
            return new ModeEntry
            {
                id = modeId,
                name = JsonUtils.JsonReadString(data, k_LabelSectionName, modeId),
                data = data
            };
        }

        private static bool IsEditorModeDescriptor(string path)
        {
            var pathLowerCased = path.ToLower();
            // Limit the authoring of editor modes to Unity packages for now.
            return pathLowerCased.StartsWith("packages/com.unity") && pathLowerCased.EndsWith(".mode");
        }

        private static void SetModeIndex(int modeIndex)
        {
            currentIndex = Math.Max(0, Math.Min(modeIndex, modeCount - 1));
        }

        private static int LoadProjectPrefModeIndex()
        {
            return EditorPrefs.GetInt(GetProjectPrefKeyName(k_ModeIndexKeyName), 0);
        }

        private static void SaveProjectPrefModeIndex(int modeIndex)
        {
            EditorPrefs.SetInt(GetProjectPrefKeyName(k_ModeIndexKeyName), modeIndex);
        }

        private static string GetProjectPrefKeyName(string prefix)
        {
            return $"{prefix}-{Application.productName}";
        }

        private static void UpdateModeMenus(int modeIndex)
        {
            var items = GetModeDataSection(modeIndex, k_MenusSectionName);
            if (items == null || (items is string && (string)items == "*"))
            {
                // Reload default menus
                Menu.ResetMenus(true);
                return;
            }

            var menus = items as IList;
            if (menus == null)
                return;

            Menu.ResetMenus(false);
            LoadMenu(menus);
        }

        private static void LoadMenu(IList menus, string prefix = "", int priority = 100)
        {
            const string k_MenuKeyName = "name";
            const string k_MenuKeyItemId = "menu_item_id";
            const string k_MenuKeyCommandId = "command_id";
            const string k_MenuKeyValidateCommandId = "validate_command_id";
            const string k_MenuKeyChildren = "children";
            const string k_MenuKeyPriority = "priority";
            const string k_MenuKeyInternal = "internal";
            const string k_MenuKeyShortcut = "shortcut";
            const string k_MenuKeyChecked = "checked";
            const string k_MenuKeyPlatform = "platform";
            const string k_MenuKeyRename = "rename";

            if (menus == null)
                return;

            foreach (var menuData in menus)
            {
                if (menuData != null)
                {
                    var menu = menuData as JSONObject;
                    if (menu == null)
                        continue;
                    var isInternal = JsonUtils.JsonReadBoolean(menu, k_MenuKeyInternal);
                    if (isInternal && !Unsupported.IsDeveloperMode())
                        continue;
                    var menuName = JsonUtils.JsonReadString(menu, k_MenuKeyName);
                    var fullMenuName = prefix + menuName;
                    var platform = JsonUtils.JsonReadString(menu, k_MenuKeyPlatform);
                    var hasExplicitPriority = menu.Contains(k_MenuKeyPriority);
                    priority = JsonUtils.JsonReadInt(menu, k_MenuKeyPriority, priority + 1);

                    // Check the menu item platform
                    if (!String.IsNullOrEmpty(platform) && !Application.platform.ToString().ToLowerInvariant().StartsWith(platform.ToLowerInvariant()))
                        continue;

                    // Check if we are a submenu
                    if (menu.Contains(k_MenuKeyChildren))
                    {
                        if (menu[k_MenuKeyChildren] is IList)
                            LoadMenu(menu[k_MenuKeyChildren] as IList, fullMenuName + "/", priority);
                        else if (menu[k_MenuKeyChildren] is string && (string)menu[k_MenuKeyChildren] == "*")
                        {
                            var whitelistedItems = Menu.ExtractSubmenus(fullMenuName);
                            var renamedTo = prefix + JsonUtils.JsonReadString(menu, k_MenuKeyRename, menuName);
                            foreach (var wi in whitelistedItems)
                                Menu.AddExistingMenuItem(wi.Replace(fullMenuName, renamedTo), wi, hasExplicitPriority ? priority : -1);
                        }
                    }
                    else
                    {
                        var commandId = JsonUtils.JsonReadString(menu, k_MenuKeyCommandId);
                        if (String.IsNullOrEmpty(commandId))
                        {
                            // We are re-using a default menu item
                            var menuItemId = JsonUtils.JsonReadString(menu, k_MenuKeyItemId, fullMenuName);
                            if (fullMenuName.Contains('/'))
                                Menu.AddExistingMenuItem(fullMenuName, menuItemId, priority);
                        }
                        else if (CommandService.Exists(commandId))
                        {
                            // Create a new menu item pointing to a command handler
                            var shortcut = JsonUtils.JsonReadString(menu, k_MenuKeyShortcut);
                            var @checked = JsonUtils.JsonReadBoolean(menu, k_MenuKeyChecked);

                            Func<bool> validateHandler = null;
                            var validateCommandId = JsonUtils.JsonReadString(menu, k_MenuKeyValidateCommandId);
                            if (!String.IsNullOrEmpty(validateCommandId))
                                validateHandler = () => (bool)CommandService.Execute(validateCommandId, CommandHint.Menu | CommandHint.Validate);

                            Menu.AddMenuItem(fullMenuName, shortcut, @checked, priority, () => CommandService.Execute(commandId, CommandHint.Menu), validateHandler);
                        }
                    }
                }
                else
                {
                    priority += 100;
                }
            }
        }

        private static string GetModeId(int modeIndex)
        {
            if (!IsValidIndex(modeIndex))
                return null;

            return modes[modeIndex].id;
        }

        private static bool IsValidIndex(int modeIndex)
        {
            return 0 <= modeIndex && modeIndex < modeCount;
        }

        private static void OnModeChangeLayouts(ModeChangedArgs args)
        {
            // Prevent double loading the default/last layout already done by the WindowLayout system.
            if (args.prevIndex == -1)
                return;

            if (!HasCapability(ModeCapability.LayoutSwitching, true))
                return;

            WindowLayout.SaveCurrentLayoutPerMode(GetModeId(args.prevIndex));


            try
            {
                // Load the last valid layout fir this mode:
                WindowLayout.LoadDefaultWindowPreferences();
            }
            catch (Exception)
            {
                // Error while loading layout. Load the default layout for current mode.
                WindowLayout.LoadDefaultLayout();
            }

            WindowLayout.ReloadWindowLayoutMenu();
        }

        private static void OnModeChangeUpdate(ModeChangedArgs args)
        {
            EditorApplication.UpdateMainWindowTitle();
            SaveProjectPrefModeIndex(args.nextIndex);
        }

        private static void OnModeChangeMenus(ModeChangedArgs args)
        {
            UpdateModeMenus(args.nextIndex);
        }

        static class JsonUtils
        {
            public static string JsonReadString(JSONObject data, string fieldName, string defaultValue = "")
            {
                if (!data.Contains(fieldName))
                    return defaultValue;
                return data[fieldName] as string;
            }

            public static int JsonReadInt(JSONObject data, string fieldName, int defaultValue = 0)
            {
                if (!data.Contains(fieldName))
                    return defaultValue;
                return (int)(double)data[fieldName];
            }

            public static bool JsonReadBoolean(JSONObject data, string fieldName, bool defaultValue = false)
            {
                if (!data.Contains(fieldName))
                    return defaultValue;
                return (bool)data[fieldName];
            }

            public static Dictionary<string, object> DeepMerge(JSONObject first, JSONObject second)
            {
                if (first == null) throw new ArgumentNullException(nameof(first));
                if (second == null) throw new ArgumentNullException(nameof(second));

                var merged = (Dictionary<string, object>)DeepClone(first);
                DeepMergeInto(merged, second);
                return merged;
            }

            private static void DeepMergeInto(JSONObject destination, JSONObject source)
            {
                if (destination == null) throw new ArgumentNullException(nameof(destination));
                if (source == null) throw new ArgumentNullException(nameof(source));

                foreach (DictionaryEntry pair in source)
                {
                    if (destination.Contains(pair.Key))
                    {
                        var value = destination[pair.Key];
                        var firstObject = value as JSONObject;
                        var secondObject = pair.Value as JSONObject;
                        if (firstObject != null && secondObject != null)
                        {
                            DeepMergeInto(firstObject, secondObject);
                            continue;
                        }

                        var firstArray = value as IList;
                        var secondArray = pair.Value as IList;
                        if (firstArray != null && secondArray != null)
                        {
                            DeepMergeArray(firstArray, secondArray);
                            continue;
                        }
                    }

                    destination[pair.Key] = DeepClone(pair.Value);
                }
            }

            private static void DeepMergeArray(IList firstArray, IList secondArray)
            {
                foreach (var secondItem in secondArray)
                {
                    bool merged = false;

                    var secondItemObject = secondItem as JSONObject;
                    if (secondItemObject != null)
                    {
                        string secondItemId = secondItemObject.Contains("id")
                            ? secondItemObject["id"] as string
                            : (secondItemObject.Contains("name") ? secondItemObject["name"] as string : null);
                        if (!String.IsNullOrEmpty(secondItemId))
                        {
                            // Find an equivalent object in the first array
                            foreach (var firstItem in firstArray)
                            {
                                var firstItemObject = firstItem as JSONObject;
                                if (firstItemObject == null)
                                    continue;

                                string firstItemId = firstItemObject.Contains("id")
                                    ? firstItemObject["id"] as string
                                    : (firstItemObject.Contains("name") ? firstItemObject["name"] as string : null);

                                if (firstItemId == secondItemId)
                                {
                                    DeepMergeInto(firstItemObject, secondItemObject);
                                    merged = true;
                                }
                            }
                        }
                    }

                    if (!merged)
                        firstArray.Add(secondItem);
                }
            }

            private static object DeepClone(object jsonValue)
            {
                if (jsonValue == null)
                    return null;

                var listValue = jsonValue as IList;
                if (listValue != null)
                {
                    var clonedList = new List<object>(listValue.Count);
                    foreach (var item in listValue)
                        clonedList.Add(DeepClone(item));
                    return clonedList;
                }

                var dictionaryValue = jsonValue as JSONObject;
                if (dictionaryValue != null)
                {
                    var clonedDict = new Dictionary<string, object>(dictionaryValue.Count);
                    foreach (DictionaryEntry kvp in dictionaryValue)
                        clonedDict.Add((string)kvp.Key, DeepClone(kvp.Value));
                    return clonedDict;
                }

                if (jsonValue is bool || jsonValue is double || jsonValue is string)
                    return jsonValue;

                throw new ArgumentException("Invalid JSON value: " + jsonValue);
            }
        }
    }
}
