// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    // This class does the conversion of asset guids from package to trunk, and from trunk to package.
    // Referred to as the "UI Toolkit Package asset conversion" in other files.
    // It uses the GUIDConversionTask class to help with progress reporting, and the 2 dialog classes under the
    // "UI" folder to show messages to the user and ask for actions through button clicks.
    // Inspired by the TextMeshPro conversion tool (found on TMP_PackageUtilities.cs inside the TextMesh Pro package).
    internal class GUIDConverter
    {
        private const string kMenuPath = "Window/UI Toolkit/Package Asset Converter";
        private const string kAssetGUIDConverterName = "UI Toolkit Package Asset Converter";

        private const string kAssetsFolderName = "Assets";

        [MenuItem(kMenuPath, false, 4010)]
        public static void OpenGUIDConverterDialog()
        {
            if (CheckIsEditorPlaying(true))
            {
                return;
            }

            GUIDConverterMainDialog.OpenDialog(kAssetGUIDConverterName, () => StartConversion(), () => StartConversion(true));
        }

        [Serializable]
        internal struct AssetConversionRecord
        {
            public string assetType;
            public string target;
            public string replacement;
        }

        private static GUIDConversionTask m_CurrentTask;
        private static GUIDConversionHelper m_ConversionHelper;
        private static GUIDConverterListDialog m_CurrentListDialog;

        // List containing the information to convert all UI Toolkit Package Asset types. Since there are only 3,
        // it's easier to maintain it through a list, even though they could be coming from a JSON file somewhere.
        internal static List<AssetConversionRecord> m_DefaultconversionData = new List<AssetConversionRecord>()
        {
            new AssetConversionRecord() {
                assetType = nameof(PanelSettings),
                target = "m_Script: {fileID: 11500000, guid: 782f629a2df6243629ea8fe2873666a4, type: 3}",
                replacement = "m_Script: {fileID: 19101, guid: 0000000000000000e000000000000000, type: 0}"
            },
            new AssetConversionRecord() {
                assetType = nameof(UIDocument),
                target = "m_Script: {fileID: 11500000, guid: f21c074d86024caca2a0034ce4f53f73, type: 3}",
                replacement = "m_Script: {fileID: 19102, guid: 0000000000000000e000000000000000, type: 0}"
            },
            new AssetConversionRecord() {
                assetType = nameof(PanelTextSettings),
                target = "m_Script: {fileID: 11500000, guid: 83edbd52c03fc4d319804b472b97f952, type: 3}",
                replacement = "m_Script: {fileID: 19103, guid: 0000000000000000e000000000000000, type: 0}"
            }
        };

        internal static bool StartConversion(bool invert = false, bool showUIDialogs = true)
        {
            return StartConversion(m_DefaultconversionData, new List<string>() { kAssetsFolderName }, invert, showUIDialogs);
        }

        internal static bool StartConversion(List<string> pathsToScan, bool invert = false, bool showUIDialogs = true)
        {
            return StartConversion(m_DefaultconversionData, pathsToScan, invert, showUIDialogs);
        }

        internal static bool StartConversion(List<AssetConversionRecord> conversionData, List<string> pathsToScan, bool invert = false, bool showUIDialogs = true)
        {
            if (pathsToScan == null || pathsToScan.Count == 0)
            {
                if (showUIDialogs)
                {
                    EditorUtility.DisplayDialog(kAssetGUIDConverterName,
                        "Error: No paths to scan, aborting process.", "OK", null);
                }
                return false;
            }

            if (conversionData == null || conversionData.Count == 0)
            {
                if (showUIDialogs)
                {
                    EditorUtility.DisplayDialog(kAssetGUIDConverterName,
                        "Error: conversion data not found, aborting process.", "OK", null);
                }
                return false;
            }

            if (!CheckConversionPossible(showUIDialogs))
            {
                return false;
            }

            if (m_ConversionHelper != null)
            {
                if (showUIDialogs)
                {
                    // A conversion is already in process, we need to cancel it if we want to run a new one.
                    if (!EditorUtility.DisplayDialog(kAssetGUIDConverterName,
                        "A conversion is already in progress, do you want to interrupt that and start a new one?",
                        "Yes", "No"))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                if (m_CurrentTask != null)
                {
                    m_CurrentTask.Stop("Interrupted by new conversion request");
                    m_CurrentTask = null;
                }

                if (m_CurrentListDialog != null)
                {
                    m_CurrentListDialog.CloseWindow();
                    m_CurrentListDialog = null;
                }

                m_ConversionHelper = null;
            }

            m_ConversionHelper = new GUIDConversionHelper
            {
                m_ConversionData = new List<AssetConversionRecord>(conversionData)
            };

            if (invert)
            {
                InvertConversion(m_ConversionHelper.m_ConversionData);
            }

            m_ConversionHelper.m_ProjectPath = Path.GetFullPath(kAssetsFolderName + "/..");
            string[] searchInFolders = pathsToScan.ToArray();

            // Get list of GUIDs for assets that might contain references to previous GUIDs that require updating.
            m_ConversionHelper.m_ProjectAssetsGUIDs = new List<string>(AssetDatabase.FindAssets("t:Object", searchInFolders).Distinct());

            if (m_ConversionHelper.m_ProjectAssetsGUIDs.Count == 0)
            {
                if (showUIDialogs)
                {
                    if (m_ConversionHelper.m_Inverted)
                    {
                        EditorUtility.DisplayDialog(kAssetGUIDConverterName,
                            "No UI Toolkit package asset found to revert.", "OK", null);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(kAssetGUIDConverterName,
                            "No UI Toolkit package asset found to convert.", "OK", null);
                    }
                }

                m_ConversionHelper = null;

                return false;
            }

            m_ConversionHelper.m_ShowUIDialogs = showUIDialogs;
            m_ConversionHelper.m_Inverted = invert;
            m_ConversionHelper.m_CurrentIndex = 0;
            m_ConversionHelper.m_ProjectAssetsToConvert = new List<GUIDConversionHelper.AssetInfo>();
            m_ConversionHelper.m_AssetNamesToShowInUI = new List<string>();

            m_CurrentTask =
                new GUIDConversionTask(kAssetGUIDConverterName,
                    "Scanning project assets", ScanProjectFiles, CancelConversion);
            m_CurrentTask.Start();

            return true;
        }

        private static bool CheckConversionPossible(bool showUIDialogs)
        {
            // Check Project Asset Serialization and Visible Meta Files mode.
            if (EditorSettings.serializationMode != SerializationMode.ForceText || VersionControlSettings.mode != "Visible Meta Files")
            {
                if (showUIDialogs)
                {
                    EditorUtility.DisplayDialog(kAssetGUIDConverterName,
                        "Error: Conversion cannot be executed with current version control and/or serialization mode " +
                        "settings.\nOn Project Settings, use serialization to force text, and visible meta files.",
                        "OK", null);
                }
                return false;
            }

            return true;
        }

        private static bool CheckIsEditorPlaying(bool showUIDialogs)
        {
            if (EditorApplication.isPlaying)
            {
                if (showUIDialogs)
                {
                    EditorUtility.DisplayDialog(kAssetGUIDConverterName,
                        "We recommend that you do not run the process while in Play Mode.",
                        "OK", null);
                }
                return true;
            }

            return false;
        }

        private static void ScanProjectFiles()
        {
            // The index value should always be good but making sure we don't crash and burn in case something is wrong.
            if (m_ConversionHelper.m_CurrentIndex >= m_ConversionHelper.m_ProjectAssetsGUIDs.Count)
            {
                if (m_ConversionHelper.m_ShowUIDialogs)
                {
                    EditorUtility.DisplayDialog(kAssetGUIDConverterName,
                        "Error scanning project files.", "OK", null);
                }
                m_CurrentTask.Stop("Error scanning project files", true);
                m_CurrentTask = null;
                m_ConversionHelper = null;
                return;
            }

            var currGUID = m_ConversionHelper.m_ProjectAssetsGUIDs[m_ConversionHelper.m_CurrentIndex++];

            string assetFilePath = AssetDatabase.GUIDToAssetPath(currGUID);

            // Filter out file types we have no interest in searching
            if (!ShouldIgnoreFile(assetFilePath))
            {
                string assetMetaFilePath = AssetDatabase.GetTextMetaFilePathFromAssetPath(assetFilePath);

                // Read the asset data file
                string assetDataFile = string.Empty;

                try
                {
                    assetDataFile = File.ReadAllText(Path.Combine(m_ConversionHelper.m_ProjectPath, assetFilePath));
                }
                catch
                {
                    // Continue to the next asset if we can't read the current one.
                    Debug.LogError($"{kAssetGUIDConverterName} - Unable to open file {assetFilePath}; file skipped.");
                    return;
                }

                // Read the asset meta data file
                string assetMetaFile = File.ReadAllText(Path.Combine(m_ConversionHelper.m_ProjectPath, assetMetaFilePath));

                bool hasFileChanged = false;
                bool hasMetaFileChanged = false;

                foreach (AssetConversionRecord record in m_ConversionHelper.m_ConversionData)
                {
                    if (assetDataFile.Contains(record.target))
                    {
                        hasFileChanged = true;
                        assetDataFile = assetDataFile.Replace(record.target, record.replacement);
                    }

                    // Check meta file
                    if (assetMetaFile.Contains(record.target))
                    {
                        hasMetaFileChanged = true;
                        assetMetaFile = assetMetaFile.Replace(record.target, record.replacement);
                    }
                }

                if (hasFileChanged)
                {
                    m_ConversionHelper.m_ProjectAssetsToConvert.Add(
                        new GUIDConversionHelper.AssetInfo()
                        {m_AssetFilePath = assetFilePath, m_AssetDataFile = assetDataFile, m_AssetNameForUI = assetFilePath});
                    m_ConversionHelper.m_AssetNamesToShowInUI.Add(assetFilePath);
                }

                if (hasMetaFileChanged)
                {
                    m_ConversionHelper.m_ProjectAssetsToConvert.Add(
                        new GUIDConversionHelper.AssetInfo()
                        {m_AssetFilePath = assetMetaFilePath, m_AssetDataFile = assetMetaFile, m_AssetNameForUI = assetFilePath});

                    if (!m_ConversionHelper.m_AssetNamesToShowInUI.Contains(assetFilePath))
                    {
                        m_ConversionHelper.m_AssetNamesToShowInUI.Add(assetFilePath);
                    }
                }
            }

            if (m_ConversionHelper.m_CurrentIndex == m_ConversionHelper.m_ProjectAssetsGUIDs.Count)
            {
                m_CurrentTask.Stop("Project asset scan done!");
                m_CurrentTask = null;

                if (m_ConversionHelper.m_ProjectAssetsToConvert.Count > 0)
                {
                    if (m_ConversionHelper.m_ShowUIDialogs)
                    {
                        string confirmButtonText, message;
                        if (m_ConversionHelper.m_Inverted)
                        {
                            confirmButtonText = "Revert";
                            message = "The following assets were identified for reversion. Revert assets?";
                        }
                        else
                        {
                            confirmButtonText = "Convert";
                            message = "The following assets were identified for conversion. Convert assets?";
                        }
                        m_CurrentListDialog = GUIDConverterListDialog.OpenListDialog(kAssetGUIDConverterName,
                            message, m_ConversionHelper.m_AssetNamesToShowInUI, confirmButtonText, true, OnConfirmUpdateProjectFiles,
                            () => CancelConversion());
                    }
                    else
                    {
                        OnConfirmUpdateProjectFiles();
                    }
                }
                else
                {
                    if (m_ConversionHelper.m_ShowUIDialogs)
                    {
                        EditorUtility.DisplayDialog(kAssetGUIDConverterName,
                            "No UI Toolkit package asset found to convert.", "OK", null);
                    }

                    m_CurrentTask = null;
                    m_ConversionHelper = null;
                }
            }
            else
            {
                m_CurrentTask.SetProgress((float)m_ConversionHelper.m_CurrentIndex / m_ConversionHelper.m_ProjectAssetsGUIDs.Count);
            }
        }

        private static void OnConfirmUpdateProjectFiles()
        {
            if (m_ConversionHelper == null)
            {
                // Something wrong happened midway but we can't recover, so let users restart the process if they need to.
                return;
            }

            m_CurrentListDialog = null;

            AssetDatabase.StartAssetEditing();
            m_ConversionHelper.m_AssetEditingStarted = true;
            m_ConversionHelper.m_CurrentIndex = 0;

            m_CurrentTask =
                new GUIDConversionTask(kAssetGUIDConverterName, "Saving modified assets", SaveModifiedAssets, CancelConversion);
            m_CurrentTask.Start();
        }

        private static void SaveModifiedAssets()
        {
            // The index value should always be good but making sure we don't crash and burn in case something is wrong.
            if (m_ConversionHelper.m_CurrentIndex >= m_ConversionHelper.m_ProjectAssetsToConvert.Count)
            {
                if (m_ConversionHelper.m_ShowUIDialogs)
                {
                    EditorUtility.DisplayDialog(kAssetGUIDConverterName,
                        "Error saving assets.", "OK", null);
                }
                m_CurrentTask.Stop("Error saving assets", true);
                m_CurrentTask = null;
                m_ConversionHelper = null;
                return;
            }

            var currentAsset = m_ConversionHelper.m_ProjectAssetsToConvert[m_ConversionHelper.m_CurrentIndex++];

            try
            {
                File.WriteAllText(Path.Combine(m_ConversionHelper.m_ProjectPath, currentAsset.m_AssetFilePath), currentAsset.m_AssetDataFile);
            }
            catch (Exception e)
            {
                Debug.LogError($"{ kAssetGUIDConverterName } - Unable to write file { currentAsset.m_AssetFilePath }: " + e);

                m_ConversionHelper.m_AssetNamesToShowInUI.Remove(currentAsset.m_AssetNameForUI);
                m_ConversionHelper.m_ErrorsFound = true;
            }

            if (m_ConversionHelper.m_CurrentIndex == m_ConversionHelper.m_ProjectAssetsToConvert.Count)
            {
                m_CurrentTask.Stop("Assets saved!");

                m_ConversionHelper.m_AssetEditingStarted = false;
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();

                if (m_ConversionHelper.m_ShowUIDialogs)
                {
                    string conversionDoneMessage = m_ConversionHelper.m_Inverted ?
                        "The following assets were reverted and will only function with the UI Toolkit package installed:" :
                        "The following assets were converted and will function without the UI Toolkit package installed:";

                    if (m_ConversionHelper.m_ErrorsFound)
                    {
                        conversionDoneMessage =
                            "Assets saved but some errors were found. Check the Console for details.\n\n" +
                            conversionDoneMessage;
                    }

                    GUIDConverterListDialog.OpenListDialog(kAssetGUIDConverterName, conversionDoneMessage,
                        m_ConversionHelper.m_AssetNamesToShowInUI, "OK");
                }
                m_CurrentTask = null;
                m_ConversionHelper = null;
            }
            else
            {
                m_CurrentTask.SetProgress((float)m_ConversionHelper.m_CurrentIndex / m_ConversionHelper.m_ProjectAssetsToConvert.Count);
            }
        }

        private static bool CancelConversion()
        {
            if (m_CurrentTask != null)
            {
                if (m_ConversionHelper.m_AssetEditingStarted)
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.Refresh();
                }

                m_CurrentTask.Cancel("Canceled");
                m_CurrentTask = null;
            }
            m_ConversionHelper = null;
            m_CurrentListDialog = null;

            return true;
        }

        private static void InvertConversion(List<AssetConversionRecord> conversionData)
        {
            for (int i = 0; i < conversionData.Count; i++)
            {
                var entry = conversionData[i];
                var target = entry.target;
                entry.target = entry.replacement;
                entry.replacement = target;

                conversionData[i] = entry;
            }
        }

        // Returns true if the file should be ignored. We can ignore sealed asset that do not contains other assets and
        // "external" assets that do not contain Unity references.
        private static bool ShouldIgnoreFile(string filePath)
        {
            if (AssetDatabase.IsValidFolder(filePath))
            {
                return true;
            }

            string fileExtension = Path.GetExtension(filePath);
            Type fileType = AssetDatabase.GetMainAssetTypeAtPath(filePath);

            if (m_IgnoreAssetTypes.Contains(fileType))
            {
                return true;
            }
            // Exclude FBX and other types
            if (fileType == typeof(GameObject) && (fileExtension.ToLower() == ".fbx" || fileExtension.ToLower() == ".blend" || fileExtension.ToLower() == ".json"))
            {
                return true;
            }
            return false;
        }

        internal static bool IsConversionRunning()
        {
            // Since this runs as a background task, this call helps the unit test run correctly as it waits for
            // the conversion to be done before checking its results.
            return m_ConversionHelper != null;
        }

        // Some asset types we know for a fact we can ignore.
        // NOTE: there are additional types we could ignore, but there's no dependency on their particular modules so
        // we decided to not add dependency just for this tool.
        private static HashSet<Type> m_IgnoreAssetTypes = new HashSet<Type>()
        {
            typeof(ComputeShader),
            typeof(Cubemap),
            typeof(DefaultAsset),
            typeof(Flare),
            typeof(Font),
            typeof(GUISkin),
            typeof(HumanTemplate),
            typeof(LightingDataAsset),
            typeof(Mesh),
            typeof(MonoScript),
            typeof(RenderTexture),
            typeof(Shader),
            typeof(TextAsset),
            typeof(Texture2D),
            typeof(Texture2DArray),
            typeof(Texture3D),
            typeof(UnityEditorInternal.AssemblyDefinitionAsset),
            typeof(UnityEngine.U2D.SpriteAtlas),
        };

        class GUIDConversionHelper
        {
            public bool m_ShowUIDialogs;
            public bool m_Inverted;
            public string m_ProjectPath;
            public List<AssetConversionRecord> m_ConversionData;
            public int m_CurrentIndex = -1;
            public List<string> m_ProjectAssetsGUIDs;
            public List<AssetInfo> m_ProjectAssetsToConvert;
            public List<string> m_AssetNamesToShowInUI;
            public bool m_AssetEditingStarted = false;
            public bool m_ErrorsFound = false;

            public struct AssetInfo
            {
                public string m_AssetFilePath;
                public string m_AssetDataFile;
                public string m_AssetNameForUI;
            }
        }
    }
}
