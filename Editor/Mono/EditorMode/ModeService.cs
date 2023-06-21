// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_MODE_SERVICE

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.MPE;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.UIElements;
using UnityEditor.ShortcutManagement;

using JSONObject = System.Collections.IDictionary;
using UnityEngine.Scripting;

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
        LayoutWindowMenu,
        HostViewGenericMenu,
        Playbar,
        GameViewToolbar,
        StatusBarExtraFeatures,
        SafeMode,
        Remember,
        AllowAssetCreation,
        StaticTabs,
        SearchPopupButton
    }

    [Serializable]
    internal class ModeDescriptor : ScriptableObject
    {
        public const string LabelKey = "label";
        public const string MenusKey = "menus";
        public const string LayoutKey = "layout";
        public const string LayoutsKey = "layouts";
        public const string ShortcutsKey = "shortcuts";
        public const string CapabilitiesKey = "capabilities";
        public const string ExecuteHandlersKey = "execute_handlers";

        [SerializeField] public string path;
    }

    [UsedImplicitly, ExcludeFromPreset, ScriptedImporter(version: 1, ext: "mode")]
    class ModeDescriptorImporter : ScriptedImporter
    {
        internal static bool allowExplicitModeRefresh { get; set; }
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var modeDescriptor = ScriptableObject.CreateInstance<ModeDescriptor>();
            modeDescriptor.path = ctx.assetPath;
            modeDescriptor.hideFlags = HideFlags.NotEditable;
            ctx.AddObjectToAsset("mode", modeDescriptor);
            ctx.SetMainObject(modeDescriptor);

            if (!allowExplicitModeRefresh)
                return;

            EditorApplication.update -= DelayLoadMode;
            EditorApplication.update += DelayLoadMode;
        }

        public static void DelayLoadMode()
        {
            EditorApplication.update -= DelayLoadMode;
            ModeService.Refresh(null);
        }
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

        internal const string k_ModePathsCache = "mode-paths-cache";
        internal const string k_DefaultModeId = "default";
        internal const string k_ModeCurrentIdKeyName = "mode-current-id";
        internal const string k_MenuKeyChecked = "checked";
        internal const string k_MenuKeyChildren = "children";
        internal const string k_MenuKeyCommandId = "command_id";
        internal const string k_MenuKeyExclude = "exclude";
        internal const string k_MenuKeyInternal = "internal";
        internal const string k_MenuKeyItemId = "menu_item_id";
        internal const string k_MenuKeyName = "name";
        internal const string k_MenuKeyNotExclude = "include";
        internal const string k_MenuKeyOriginalFullName = "original_full_name";
        internal const string k_MenuKeyOriginalName = "original_name";
        internal const string k_MenuKeyPlatform = "platform";
        internal const string k_MenuKeyPriority = "priority";
        internal const string k_MenuKeySecondaryPriority = "secondary_priority";
        internal const string k_MenuKeyShortcut = "shortcut";
        internal const string k_MenuKeyValidateCommandId = "validate_command_id";

        public static string[] modeNames => modes.Select(m => m.name).ToArray();
        public static int modeCount => modes.Length;

        public static string currentId => currentIndex == -1 || modes.Length == 0 ? k_DefaultModeId : modes[currentIndex].id;
        public static int currentIndex { get; private set; }
        private static ModeEntry[] modes { get; set; } = new ModeEntry[0];
        internal static bool hasSwitchableModes { get; private set; }
        private static bool initialModeChanged
        {
            get => SessionState.GetBool(nameof(initialModeChanged), false);
            set => SessionState.SetBool(nameof(initialModeChanged), value);
        }

        public static event Action<ModeChangedArgs> modeChanged;

        static ModeService()
        {
            Log("Initialize");

            LoadModes(true);

            modeChanged += OnModeChangeMenus;
            modeChanged += OnModeChangeLayouts;
            UnityEditor.PackageManager.Events.registeredPackages += OnRegisteredPackages;

            ModeDescriptorImporter.allowExplicitModeRefresh = true;
        }

        private static bool IsMainWindowLoaded()
        {
            return WindowLayout.FindMainWindow();
        }

        internal static int GetModeIndexById(string modeId)
        {
            string lcModeId = modeId.ToLowerInvariant();
            int modeIndex = Array.FindIndex(modes, m => m.id == lcModeId);
            return modeIndex;
        }

        [RequiredByNativeCode]
        public static void ChangeModeById(string modeId)
        {
            int modeIndex = GetModeIndexById(modeId);
            if (modeIndex != -1)
                ChangeModeByIndex(modeIndex);
        }

        public static void Update()
        {
            Log("Update");
            EditorApplication.RequestRepaintAllViews();
        }

        internal static void ChangeModeByIndex(int modeIndex)
        {
            if (currentIndex == modeIndex)
                return;

            Log($"ChangeMode({GetModeId(modeIndex)})");

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
            return HasCapability(currentIndex, capability, defaultValue);
        }

        internal static bool HasCapability(string capabilityName, bool defaultValue = false)
        {
            return HasCapability(currentIndex, capabilityName, defaultValue);
        }

        internal static bool HasCapability(int modeIndex, ModeCapability capability, bool defaultValue = false)
        {
            return HasCapability(modeIndex, capability.ToString().ToSnakeCase(), defaultValue);
        }

        internal static bool HasCapability(int modeIndex, string capabilityName, bool defaultValue = false)
        {
            var lcCapabilityName = capabilityName.ToLower();
            var capabilities = GetModeDataSection(modeIndex, ModeDescriptor.CapabilitiesKey) as JSONObject;
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
            var executeHandlers = GetModeDataSection(currentIndex, ModeDescriptor.ExecuteHandlersKey) as JSONObject;
            if (!HasExecuteHandler(actionName, executeHandlers))
                return false;

            var result = CommandService.Execute((string)executeHandlers[actionName], hint, args);
            return result == null || (bool)result;
        }

        internal static bool HasExecuteHandler(string actionName)
        {
            actionName = actionName.ToLower();
            var executeHandlers = GetModeDataSection(currentIndex, ModeDescriptor.ExecuteHandlersKey) as JSONObject;
            return HasExecuteHandler(actionName, executeHandlers);
        }

        static bool HasExecuteHandler(string loweredActionName, JSONObject executeHandlers)
        {
            if (executeHandlers == null)
                return false;

            if (!executeHandlers.Contains(loweredActionName))
                return false;

            string commandId = (string)executeHandlers[loweredActionName];
            if (!CommandService.Exists(commandId))
                return false;

            return true;
        }

        internal static bool HasSection(int modeIndex, string sectionName)
        {
            if (!IsValidIndex(modeIndex))
                return false;
            return modes[modeIndex].data.Contains(sectionName);
        }

        internal static object GetModeDataSection(string sectionName)
        {
            return GetModeDataSection(currentIndex, sectionName);
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
            Log($"RaiseModeChanged({GetModeId(prevIndex) ?? k_DefaultModeId}, {GetModeId(nextIndex)})");

            modeChanged?.Invoke(new ModeChangedArgs { prevIndex = prevIndex, nextIndex = nextIndex });

            initialModeChanged = true;
        }

        internal static bool IsValidModeId(string id)
        {
            return !string.IsNullOrEmpty(id) && id.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.');
        }

        internal static string GetDefaultModeLayout(string modeId = null)
        {
            var layouts = GetModeDataSection(currentIndex, ModeDescriptor.LayoutsKey) as IList<object>;
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

        private static void OnRegisteredPackages(PackageManager.PackageRegistrationEventArgs evt)
        {
            SessionState.EraseString(k_ModePathsCache);
            LoadModes();
        }

        private static void LoadModes(bool checkStartupMode = false)
        {
            Log("LoadModes");

            ScanModes();
            var currentModeIndex = LoadProjectPrefModeIndex();
            if (checkStartupMode && HasStartupMode())
            {
                var requestEditorMode = Application.GetValueForARGV("editor-mode");
                var modeIndex = GetModeIndexById(requestEditorMode);
                if (modeIndex != -1)
                {
                    currentModeIndex = modeIndex;
                    Console.WriteLine($"[MODES] Loading editor mode {modeNames[currentModeIndex]} ({currentModeIndex}) from command line.");
                }
            }

            SetModeIndex(currentModeIndex);
        }

        internal static void InitializeCurrentMode()
        {
            if (initialModeChanged)
                return;
            Log("InitializeCurrentMode");
            RaiseModeChanged(-1, currentIndex);
        }

        private static void FillModeData(string path, Dictionary<string, object> modesData)
        {
            try
            {
                var json = SJSON.Load(path);
                foreach (var rawModeId in json.Keys)
                {
                    var modeId = ((string)rawModeId).ToLower();
                    if (IsValidModeId(modeId))
                    {
                        object modeData = null;
                        if (modesData.TryGetValue(modeId, out modeData))
                            modesData[modeId] = JsonUtils.DeepMerge(modeData as JSONObject, json[modeId] as JSONObject);
                        else
                            modesData[modeId] = json[modeId];
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid Mode Id: {modeId} contains non alphanumeric characters.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModeService] Error while parsing mode file {path}.\n{ex}");
            }
        }

        internal static void ScanModes()
        {
            var modesData = new Dictionary<string, object>
            {
                [k_DefaultModeId] = new Dictionary<string, object>
                {
                    [ModeDescriptor.LabelKey] = "Default"
                }
            };

            var builtinModeFile = Path.Combine(EditorApplication.applicationContentsPath, "Resources/default.mode");
            FillModeData(builtinModeFile, modesData);

            var modeFilePathCache = SessionState.GetString(k_ModePathsCache, "");
            
            if (modeFilePathCache == "")
            {
                var modeDescriptors = AssetDatabase.EnumerateAllAssets(new SearchFilter
                {
                    searchArea = SearchFilter.SearchArea.InPackagesOnly,
                    classNames = new[] { nameof(ModeDescriptor) },
                    showAllHits = true
                });

                while (modeDescriptors.MoveNext())
                {
                    var md = modeDescriptors.Current.pptrValue as ModeDescriptor;
                    if (md == null)
                        continue;
                    FillModeData(md.path, modesData);
                    modeFilePathCache += md.path + ";";
                }
                SessionState.SetString(k_ModePathsCache, modeFilePathCache);
            }
            else
            {
                var paths = modeFilePathCache.Split(";");
                foreach(var path in paths)
                {
                    if (!File.Exists(path))
                        continue;
                    FillModeData(path, modesData);
                }
            }
            
            modes = new ModeEntry[modesData.Keys.Count];
            modes[0] = CreateEntry(k_DefaultModeId, (JSONObject)modesData[k_DefaultModeId]);
            var modeIndex = 1;
            hasSwitchableModes = false;
            foreach (var modeId in modesData.Keys)
            {
                if (modeId == k_DefaultModeId)
                    continue;
                var modeFields = (JSONObject)modesData[modeId];
                modes[modeIndex] = CreateEntry(modeId, modeFields);
                hasSwitchableModes |= !JsonUtils.JsonReadBoolean(modeFields, "builtin");
                modeIndex++;
            }

            Array.Sort(modes, (m1, m2) =>
            {
                if (m1.id == "default")
                    return -1;
                if (m2.id == "default")
                    return 1;
                return m1.id.CompareTo(m2.id);
            });
        }

        private static ModeEntry CreateEntry(string modeId, JSONObject data)
        {
            return new ModeEntry
            {
                id = modeId.ToLowerInvariant(),
                name = JsonUtils.JsonReadString(data, ModeDescriptor.LabelKey, modeId),
                data = data
            };
        }

        private static void SetModeIndex(int modeIndex)
        {
            currentIndex = Math.Max(0, Math.Min(modeIndex, modeCount - 1));

            var capabilities = GetModeDataSection(currentIndex, ModeDescriptor.CapabilitiesKey) as IDictionary;
            if (capabilities != null)
            {
                foreach (var cap in capabilities.Keys)
                {
                    var capName = Convert.ToString(cap);
                    if (String.IsNullOrEmpty(capName))
                        continue;
                    var state = capabilities[capName];
                    if (state is Boolean)
                        SessionState.SetBool(capName, (Boolean)state);
                }
            }
        }

        private static int LoadProjectPrefModeIndex()
        {
            var modePreyKeyName = GetProjectPrefKeyName(k_ModeCurrentIdKeyName);
            var loadModeId = EditorPrefs.GetString(modePreyKeyName, "default");
            var loadModeIndex = GetModeIndexById(loadModeId);
            if (loadModeIndex == -1)
                return 0; // Fallback to default mode index
            Console.WriteLine($"[MODES] Loading mode {modeNames[loadModeIndex]} ({loadModeIndex}) for {modePreyKeyName}");
            return loadModeIndex;
        }

        private static void SaveProjectPrefModeIndex(int modeIndex)
        {
            if (!HasCapability(ModeCapability.Remember, true))
                return;
            var modePreyKeyName = GetProjectPrefKeyName(k_ModeCurrentIdKeyName);
            Console.WriteLine($"[MODES] Saving user mode to {modeNames[modeIndex]} ({modeIndex}) for {modePreyKeyName}");
            EditorPrefs.SetString(modePreyKeyName, modes[modeIndex].id);
        }

        private static string GetProjectPrefKeyName(string prefix)
        {
            var key = $"{prefix}-{Application.productName}";
            if (!string.IsNullOrEmpty(ProcessService.roleName))
            {
                key += "-" + ProcessService.roleName;
            }
            return key;
        }

        public static bool HasContextMenu(string menuId)
        {
            return GetContextMenu(menuId) != null;
        }

        public static void PopupContextMenu(string menuId)
        {
            var menuDescriptor = GetContextMenu(menuId);
            if (menuDescriptor == null)
                return;

            var menu = DropdownUtility.CreateDropdown(true);
            BuildContextMenu(menuDescriptor, menuId, menu);
            DropdownUtility.ShowDropdown(menu, Event.current.mousePosition);
        }

        static private IList GetContextMenu(string menuId)
        {
            var section = GetModeDataSection("context_menus");
            var contextMenus = section as Dictionary<string, object>;
            if (contextMenus == null)
                return null;

            if (menuId.EndsWith("/"))
                menuId = menuId.Remove(menuId.Length - 1);
            if (!contextMenus.ContainsKey(menuId))
                return null;
            return contextMenus[menuId] as IList;
        }

        internal static bool HasDynamicLayout(int modeIndex = -1)
        {
            var layoutData = GetModeDataSection(modeIndex == -1 ? currentIndex : modeIndex, ModeDescriptor.LayoutKey) as JSONObject;
            return layoutData != null;
        }

        internal static JSONObject GetDynamicLayout(int modeIndex = -1)
        {
            return GetModeDataSection(modeIndex == -1 ? currentIndex : modeIndex, ModeDescriptor.LayoutKey) as JSONObject;
        }

        private static void BuildContextMenu(IList menus, string menuId, IGenericMenu contextMenu, string prefix = "")
        {
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
                    var menuItemId = menuId + fullMenuName;
                    var platform = JsonUtils.JsonReadString(menu, k_MenuKeyPlatform);

                    // Check the menu item platform
                    if (!string.IsNullOrEmpty(platform) && !Application.platform.ToString().ToLowerInvariant().StartsWith(platform.ToLowerInvariant()))
                        continue;

                    // Check if we are a submenu
                    if (menu.Contains(k_MenuKeyChildren))
                    {
                        if (menu[k_MenuKeyChildren] is IList childrenItems)
                            BuildContextMenu(childrenItems, menuId, contextMenu, fullMenuName + "/");
                        else if (menu[k_MenuKeyChildren] is string msw && msw == "*")
                        {
                            var whitelistedItems = Menu.ExtractSubmenus(menuItemId);
                            foreach (var wi in whitelistedItems)
                            {
                                var @checked = Menu.GetChecked(wi);
                                if (!Menu.GetEnabled(wi))
                                    contextMenu.AddDisabledItem(wi.Replace(menuId, ""), @checked);
                                else
                                    contextMenu.AddItem(wi.Replace(menuId, ""), @checked, () => EditorApplication.ExecuteMenuItem(wi));
                            }
                        }
                    }
                    else
                    {
                        var commandId = JsonUtils.JsonReadString(menu, k_MenuKeyCommandId);
                        if (string.IsNullOrEmpty(commandId))
                        {
                            // We are re-using a default menu item
                            menuItemId = JsonUtils.JsonReadString(menu, k_MenuKeyItemId, menuItemId);
                            if (EditorApplication.ValidateMenuItem(menuItemId))
                                contextMenu.AddItem(fullMenuName, false, () => EditorApplication.ExecuteMenuItem(menuItemId));
                            else
                                contextMenu.AddDisabledItem(fullMenuName, false);
                        }
                        else if (CommandService.Exists(commandId))
                        {
                            // Create a new menu item pointing to a command handler
                            contextMenu.AddItem(fullMenuName, false, () => CommandService.Execute(commandId, CommandHint.Menu));
                        }
                    }
                }
                else
                {
                    contextMenu.AddSeparator(prefix);
                }
            }
        }

        [RequiredByNativeCode]
        internal static string GetCurrentModeId()
        {
            return currentId;
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
            if (args.prevIndex == -1 || !IsMainWindowLoaded())
                return;

            Log("OnModeChangeLayouts");

            if (HasCapability(ModeCapability.LayoutSwitching, true))
            {
                var dynamicLayout = GetDynamicLayout(args.prevIndex);
                if (dynamicLayout == null || Convert.ToBoolean(dynamicLayout["restore_saved_layout"]))
                {
                    WindowLayout.SaveCurrentLayoutPerMode(GetModeId(args.prevIndex));
                }

                try
                {
                    if (args.nextIndex != 0 || HasCapability(args.prevIndex, ModeCapability.LayoutSwitching, true))
                    {
                        // Load the last valid layout for this mode
                        WindowLayout.LoadCurrentModeLayout(keepMainWindow: true);
                    }
                }
                catch (Exception)
                {
                    // Error while loading layout. Load the default layout for current mode.
                    WindowLayout.LoadDefaultLayout();
                }
            }

            WindowLayout.UpdateWindowLayoutMenu();
        }

        private static void OnModeChangeUpdate(ModeChangedArgs args)
        {
            Log("OnModeChangeUpdate");

            EditorApplication.UpdateMainWindowTitle();
            SaveProjectPrefModeIndex(args.nextIndex);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static void OnModeChangeMenus(ModeChangedArgs args)
        {
            if (args.prevIndex == -1 || !IsMainWindowLoaded())
                return;

            EditorUtility.RebuildAllMenus();

            ShortcutIntegration.instance.RebuildShortcuts();
        }

        private static void Log(string actionName)
        {
            var debugMsg = $"[MODES] ModeService[{GetModeId(currentIndex) ?? "none"}].{actionName}";
            Console.WriteLine(debugMsg);
        }

        internal static class JsonUtils
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

                                if (String.Equals(firstItemId, secondItemId, StringComparison.OrdinalIgnoreCase))
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
