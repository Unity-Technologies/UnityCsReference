// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;

namespace UnityEditor
{
    internal partial class PresetLibraryEditor<T> where T : PresetLibrary
    {
        class SettingsMenu
        {
            static PresetLibraryEditor<T> s_Owner;

            class ViewModeData
            {
                public GUIContent text;
                public int itemHeight;
                public PresetLibraryEditorState.ItemViewMode viewmode;
            }

            public static void Show(Rect activatorRect, PresetLibraryEditor<T> owner)
            {
                s_Owner = owner;

                GenericMenu menu = new GenericMenu();

                // View modes
                int minItemHeight = (int)s_Owner.minMaxPreviewHeight.x;
                int maxItemHeight = (int)s_Owner.minMaxPreviewHeight.y;
                List<ViewModeData> viewModeData;
                if (minItemHeight == maxItemHeight)
                {
                    viewModeData = new List<ViewModeData>
                    {
                        new ViewModeData {text = new GUIContent("Grid"), itemHeight = minItemHeight, viewmode = PresetLibraryEditorState.ItemViewMode.Grid},
                        new ViewModeData {text = new GUIContent("List"), itemHeight = minItemHeight, viewmode = PresetLibraryEditorState.ItemViewMode.List},
                    };
                }
                else
                {
                    viewModeData = new List<ViewModeData>
                    {
                        new ViewModeData {text = new GUIContent("Small Grid"), itemHeight = minItemHeight, viewmode = PresetLibraryEditorState.ItemViewMode.Grid},
                        new ViewModeData {text = new GUIContent("Large Grid"), itemHeight = maxItemHeight, viewmode = PresetLibraryEditorState.ItemViewMode.Grid},
                        new ViewModeData {text = new GUIContent("Small List"), itemHeight = minItemHeight, viewmode = PresetLibraryEditorState.ItemViewMode.List},
                        new ViewModeData {text = new GUIContent("Large List"), itemHeight = maxItemHeight, viewmode = PresetLibraryEditorState.ItemViewMode.List}
                    };
                }

                for (int i = 0; i < viewModeData.Count; ++i)
                {
                    bool currentSelected = s_Owner.itemViewMode == viewModeData[i].viewmode && (int)s_Owner.previewHeight == viewModeData[i].itemHeight;
                    menu.AddItem(viewModeData[i].text, currentSelected, ViewModeChange, viewModeData[i]);
                }
                menu.AddSeparator("");

                // Available libraries (show user libraries first then project libraries)
                List<string> preferencesLibs;
                List<string> projectLibs;
                PresetLibraryManager.instance.GetAvailableLibraries(s_Owner.m_SaveLoadHelper, out preferencesLibs, out projectLibs);
                preferencesLibs.Sort();
                projectLibs.Sort();

                string currentLibWithExtension = s_Owner.currentLibraryWithoutExtension + "." + s_Owner.m_SaveLoadHelper.fileExtensionWithoutDot;

                string projectFolderTag = " (Project)";
                foreach (string libPath in preferencesLibs)
                {
                    string libName = Path.GetFileNameWithoutExtension(libPath);
                    menu.AddItem(new GUIContent(libName), currentLibWithExtension == libPath, LibraryModeChange, libPath);
                }
                foreach (string libPath in projectLibs)
                {
                    string libName = Path.GetFileNameWithoutExtension(libPath);
                    menu.AddItem(new GUIContent(libName + projectFolderTag), currentLibWithExtension == libPath, LibraryModeChange, libPath);
                }
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Create New Library..."), false, CreateLibrary, 0);
                if (HasDefaultPresets())
                {
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Add Factory Presets To Current Library"), false, AddDefaultPresetsToCurrentLibrary, 0);
                }
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Reveal Current Library Location"), false, RevealCurrentLibrary, 0);
                menu.DropDown(activatorRect);
            }

            static void ViewModeChange(object userData)
            {
                ViewModeData viewModeData = (ViewModeData)userData;
                s_Owner.itemViewMode = viewModeData.viewmode;
                s_Owner.previewHeight = viewModeData.itemHeight;
            }

            static void LibraryModeChange(object userData)
            {
                string libPath = (string)userData;
                s_Owner.currentLibraryWithoutExtension = libPath;
            }

            static void CreateLibrary(object userData)
            {
                s_Owner.wantsToCreateLibrary = true;
            }

            static void RevealCurrentLibrary(object userData)
            {
                s_Owner.RevealCurrentLibrary();
            }

            static bool HasDefaultPresets()
            {
                return s_Owner.addDefaultPresets != null;
            }

            static void AddDefaultPresetsToCurrentLibrary(object userData)
            {
                if (s_Owner.addDefaultPresets != null)
                    s_Owner.addDefaultPresets(s_Owner.GetCurrentLib());

                s_Owner.SaveCurrentLib();
            }
        }
    }
} // UnityEditor
