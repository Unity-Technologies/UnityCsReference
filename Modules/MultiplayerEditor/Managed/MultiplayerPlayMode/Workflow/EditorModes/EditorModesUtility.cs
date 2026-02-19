// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Multiplayer.Internal;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class EditorModesUtility
    {
        private const string k_LayoutConfigFile = "Multiplayer/Layouts/clone_window_layout_config.json";
        private const string k_LayoutProxyDir = "Temp/Layouts";
        private const string k_LayoutProxyFile = "proxy_layout.json";
        private const string k_LayoutWindowDir = "Library/UserSettings/Layouts";
        private const string k_LayoutWindowExt = "wlt";

        private const string k_CurrentModeKey = "vpmode";
        private const string k_CurrentWindowIdKey = "vpwindow-id";
        private const string k_CurrentWindowSetKey = "vpwindow-set";

        private const string k_WindowId = "VPWindow";

        private const string k_UnknownPlayerWindowTitle = "Unknown Player";

        private static string CurrentMode
        {
            get => SessionState.GetString(k_CurrentModeKey, string.Empty);
            set
            {
                if (value == null)
                {
                    SessionState.EraseString(k_CurrentModeKey);
                }
                else
                {
                    SessionState.SetString(k_CurrentModeKey, value);
                }
            }
        }

        public static ContainerWindowProxy CurrentWindow
        {
            get
            {
                if (SessionState.GetBool(k_CurrentWindowSetKey, false))
                {
                    var value = SessionState.GetInt(k_CurrentWindowIdKey, 0);
                    return ContainerWindowProxy.FromInstanceID(EntityId.From(value));
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    SessionState.EraseInt(k_CurrentWindowIdKey);
                    SessionState.EraseBool(k_CurrentWindowSetKey);
                }
                else
                {
                    Debug.Assert(sizeof(int)==UnsafeUtility.SizeOf<EntityId>(), "EntityId is not the same size as int, update this code to use ulong");
                    SessionState.SetInt(k_CurrentWindowIdKey, (int)EntityId.ToULong(value.GetEntityId()));
                    SessionState.SetBool(k_CurrentWindowSetKey, true);
                }
            }
        }

        private static FileSystemDelegates s_FileSystemDelegates;
        private static ParsingSystemDelegates s_ParsingSystemDelegates;

        static EditorModesUtility()
        {
            Initialize(FileSystem.Delegates, ParsingSystem.Delegates);
        }

        private static void Initialize(FileSystemDelegates fileSystemDelegates,
                                       ParsingSystemDelegates parsingSystemDelegates)
        {
            s_FileSystemDelegates = fileSystemDelegates;
            s_ParsingSystemDelegates = parsingSystemDelegates;

            // Always refresh the layout proxy directory on init
            if (s_FileSystemDelegates.ExistsDirectoryFunc(k_LayoutProxyDir))
            {
                s_FileSystemDelegates.DeleteDirectoryFunc(k_LayoutProxyDir);
            }
            s_FileSystemDelegates.CreateDirectoryFunc(k_LayoutProxyDir);

            // Initialize the underlying layout flags if it hasn't been set
            var flagsFile = VirtualProjectWorkflow.WorkflowCloneContext.CloneDataFile;
            CloneDataFile.LoadFromFile(flagsFile);
        }

        internal static void SaveCurrentWindow()
        {
            if (string.IsNullOrWhiteSpace(CurrentMode))
            {
                MppmLog.Warning("No Current Mode set, no window to save.");
                return;
            }

            // Save previous layout since we are switching
            Debug.Assert(!string.IsNullOrWhiteSpace(CurrentMode), "The view is not supposed to be empty. Should be previous or default!");
            var wltFile = Path.ChangeExtension(Path.Combine(k_LayoutWindowDir, CurrentMode), k_LayoutWindowExt);
            WindowLayout.SaveWindowLayout(wltFile);
        }

        internal static LayoutFlags GetLayoutFlagsForMode(bool isPlayMode)
        {
            var flags = isPlayMode
                ? CloneDataFile.LoadFromFile(VirtualProjectWorkflow.WorkflowCloneContext.CloneDataFile).PlayModeLayoutFlags
                : CloneDataFile.LoadFromFile(VirtualProjectWorkflow.WorkflowCloneContext.CloneDataFile).EditModeLayoutFlags;
            return flags;
        }

        internal static bool SetLayoutFlagsForMode(bool isPlayMode, LayoutFlags layoutFlags)
        {
            // Update clone configuration and save to disk
            var cloneConfiguration = VirtualProjectWorkflow.WorkflowCloneContext.CloneDataFile;

            bool hasChanged;
            if (isPlayMode)
            {
                hasChanged = !cloneConfiguration.Data.PlayModeLayoutFlags.Equals(layoutFlags);
                cloneConfiguration.Data.PlayModeLayoutFlags = layoutFlags;
            }
            else
            {
                hasChanged = !cloneConfiguration.Data.EditModeLayoutFlags.Equals(layoutFlags);
                cloneConfiguration.Data.EditModeLayoutFlags = layoutFlags;
            }

            if (hasChanged)
            {
                CloneDataFile.SaveToFile(cloneConfiguration);
            }

            return hasChanged;
        }

        internal static void SwitchLayoutToMode(bool isPlayMode)
        {
            // Grab the mode's flag to switch to.
            var cloneConfiguration = VirtualProjectWorkflow.WorkflowCloneContext.CloneDataFile;
            LayoutFlags layoutFlags = isPlayMode
                    ? CloneDataFile.LoadFromFile(cloneConfiguration).PlayModeLayoutFlags
                    : CloneDataFile.LoadFromFile(cloneConfiguration).EditModeLayoutFlags;

            // Quick flag sanity checks.
            Debug.Assert(layoutFlags != LayoutFlags.None, $"Layout of {LayoutFlags.None} is not supposed to be used.");
            var viewToSwitchTo = LayoutFlagsUtil.GenerateLayoutName(layoutFlags);

            // if the open window has a layout that is the same, avoid the expansive window switch
            if (CurrentWindow != null && viewToSwitchTo.Equals(CurrentMode))
            {
                ApplyWindowTitle();
                return;
            }

            // Save and close any existing current windows before switching over.
            if (CurrentWindow != null)
            {
                CloseCurrentWindow();
            }

            // Attempt switching windows. Guard all I/O operations with Try-Catch.
            try
            {
                // If there's a cache .WlT window for this layout flag config, load it.
                var cachedWindowDir = Path.Combine(k_LayoutWindowDir, viewToSwitchTo);
                var cachedLayoutWindowPath = Path.ChangeExtension(cachedWindowDir, k_LayoutWindowExt);
                if (File.Exists(cachedLayoutWindowPath))
                {
                    CurrentWindow = WindowLayout.LoadWindowLayout(k_WindowId, cachedLayoutWindowPath);
                }

                // Else dynamically build a .WLT window representing this layout flag config.
                var proxyPath = Path.Combine(k_LayoutProxyDir, k_LayoutProxyFile);
                if (CurrentWindow == null && BuildDynamicLayoutFile(layoutFlags, proxyPath))
                {
                    CurrentWindow = WindowLayout.ShowWindowWithDynamicLayout(k_WindowId, proxyPath);
                }

                // If the window is still null, something went terribly wrong, log the error.
                if (CurrentWindow == null)
                {
                    MppmLog.Error($"Mode Switcher was unable to open window with flags {layoutFlags}");
                    return;
                }

                ApplyWindowTitle();
                CurrentMode = viewToSwitchTo;
                SaveCurrentWindow();
                InternalEditorUtility.RepaintAllViews();
            }
            catch (Exception ex)
            {
                MppmLog.Error($"Mode Switcher Encountered error when switching to: {layoutFlags} \n " +
                               $"Error:{ex.Message} \n" +
                               $"Trace:{ex.StackTrace}");
            }
        }

        private static void ApplyWindowTitle()
        {
            if (CurrentWindow == null)
            {
                return;
            }

            // Update window titles and finally our CurrentMode states
            var dataStore = SystemDataStore.GetClone();
            var hasPlayer = Filters.FindFirstPlayerWithVirtualProjectsIdentifier(dataStore.LoadAllPlayerJson(),
                VirtualProjectsEditor.CloneIdentifier, out var player);
            if (hasPlayer)
            {
                var tag = player.Tags.Count <= 0 ? string.Empty : $" [{string.Join('|', player.Tags)}]";
                var roleText = InternalUtilities.GetMultiplayerRoleDisplayText((MultiplayerRoleFlags)player.MultiplayerRole);
                CurrentWindow.title = $"{player.Name} ({roleText}){tag}";
            }
            else
            {
                CurrentWindow.title = k_UnknownPlayerWindowTitle;
            }
        }

        public static void CloseCurrentWindow()
        {
            if (CurrentWindow != null)
            {
                // Always save the current window before closing.
                SaveCurrentWindow();

                CurrentWindow.Close();
                CurrentWindow = null;
            }
        }
        private static bool BuildDynamicLayoutFile(LayoutFlags layoutFlags, string proxyPath)
        {
            // Grab our base layout config to build a dynamic file from.
            var modeAsset = EditorResources.Load<TextAsset>(k_LayoutConfigFile);
            var jsonRawIn = modeAsset.text;

            // Attempt to deserialize the base layout.
            if (!DynamicLayout.TryDeserialize(s_ParsingSystemDelegates, jsonRawIn, out DynamicLayout layout))
            {
                MppmLog.Error($"Mode Switcher failed to parse layout config: {k_LayoutConfigFile}");
                return false;
            }

            // Toggle Panel views by trimming views from the base layout with desired layout flags
            layout.TrimDynamicLayout(layoutFlags);

            // Build it back out to the desired proxyPath for Unity.WindowLayout to build WLT
            var jsonRawOut = DynamicLayout.Serialize(s_ParsingSystemDelegates, layout);
            s_FileSystemDelegates.WriteBytesFunc(proxyPath, Encoding.UTF8.GetBytes(jsonRawOut));
            return true;
        }
    }
}
