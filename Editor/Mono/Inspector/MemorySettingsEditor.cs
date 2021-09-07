// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditorInternal;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor.Callbacks;

namespace UnityEditor
{
    [CustomEditor(typeof(MemorySettings))]
    [ExcludeFromPreset]
    internal class MemorySettingsEditor : Editor
    {
        class Content
        {
            public static readonly GUIContent kGeneralSettingsWarning = EditorGUIUtility.TrTextContent("Changing Memory setup values can cause severe performance degradation.");
            public static readonly GUIContent kEditorSettingsWarning = EditorGUIUtility.TrTextContent("Changing the memory setup for editor will update the ProjectSettings/boot.config file. This file is loaded at Editor startup, so will take effect at next startup.");

            public static readonly GUIContent kMainAllocatorsTitle = EditorGUIUtility.TrTextContent("Main Allocators");

            public static readonly GUIContent kMainAllocatorTitle = EditorGUIUtility.TrTextContent("Main Allocator");
            public static readonly GUIContent kMainAllocatorBlockSize = EditorGUIUtility.TrTextContent("Main Thread Block Size", "Block size used by main thread allocator");
            public static readonly GUIContent kThreadAllocatorBlockSize = EditorGUIUtility.TrTextContent("Shared Thread Block Size", "Block size used by shared thread allocator");

            public static readonly GUIContent kGfxAllocatorTitle = EditorGUIUtility.TrTextContent("Gfx Allocator");
            public static readonly GUIContent kMainGfxBlockSize = EditorGUIUtility.TrTextContent("Main Thread Block Size", "Block size used by main thread for gfx allocations");
            public static readonly GUIContent kThreadGfxBlockSize = EditorGUIUtility.TrTextContent("Shared Thread Block Size", "Block size used by shared threads for gfx allocations");

            public static readonly GUIContent kExtraAllocatorTitle = EditorGUIUtility.TrTextContent("Other Allocators");
            public static readonly GUIContent kCacheBlockSize = EditorGUIUtility.TrTextContent("File Cache Block Size", "Block size used by file cache allocator. Setting this value to 0 will cause the file cache allocations to be passed to the main allocator");
            public static readonly GUIContent kTypetreeBlockSize = EditorGUIUtility.TrTextContent("Type Tree Block Size", "Block size used by the tree allocator. Setting this value to 0 will cause the type tree allocations to be passed to the main allocator");

            public static readonly GUIContent kTempAllocatorTitle_Player = EditorGUIUtility.TrTextContent("Fast Per Thread Temporary Allocators", "Block size can grow to twice the initial size");
            public static readonly GUIContent kTempAllocatorTitle_Editor = EditorGUIUtility.TrTextContent("Fast Per Thread Temporary Allocators", "Block size can grow to 8 times the initial size");
            public static readonly GUIContent kTempAllocatorSizeMain = EditorGUIUtility.TrTextContent("Main Thread Block Size", "Initial size for main thread temp allocator");
            public static readonly GUIContent kTempAllocatorSizeJobWorker = EditorGUIUtility.TrTextContent("Job Worker Block Size", "Block size for worker job temp allocators");
            public static readonly GUIContent kTempAllocatorSizeBackgroundWorker = EditorGUIUtility.TrTextContent("Background Job Worker Block Size", "Block size for worker job temp allocators");
            public static readonly GUIContent kTempAllocatorSizePreloadManager = EditorGUIUtility.TrTextContent("Preload Block Size", "Block size for worker job temp allocators");
            public static readonly GUIContent kTempAllocatorSizeAudioWorker = EditorGUIUtility.TrTextContent("Audio Worker Block Size", "Block size for worker job temp allocators");
            public static readonly GUIContent kTempAllocatorSizeCloudWorker = EditorGUIUtility.TrTextContent("Cloud Worker Block Size", "Block size for worker job temp allocators");
            public static readonly GUIContent kTempAllocatorSizeGfx = EditorGUIUtility.TrTextContent("Gfx Thread Block Size", "Block size for worker job temp allocators");
            public static readonly GUIContent kTempAllocatorSizeGIBakingWorker = EditorGUIUtility.TrTextContent("GI Baking Block Size", "Block size for GI baking workers temp allocators");
            public static readonly GUIContent kTempAllocatorSizeNavMeshWorker = EditorGUIUtility.TrTextContent("NavMesh Worker Block Size", "Block size for worker job temp allocators");

            // TODO: guard input parameters
            public static readonly GUIContent kJobTempAllocatorTitle = EditorGUIUtility.TrTextContent("Fast Thread Shared Temporary Allocators");
            public static readonly GUIContent kJobTempAllocatorBlockSize = EditorGUIUtility.TrTextContent("Job Allocator Block Size", "Block size for worker job temp allocators. Can grow to 64 blocks");
            public static readonly GUIContent kBackgroundJobTempAllocatorBlockSize = EditorGUIUtility.TrTextContent("Background Job Allocator Block Size", "Block size for background worker job temp allocators. Can grow to 64 blocks");
            public static readonly GUIContent kJobTempAllocatorReducedBlockSize = EditorGUIUtility.TrTextContent("Job Allocator Block Sizes on low memory platform", "Block sizes for job and background if platform has less than 2GB memory");

            public static readonly GUIContent kBucketAllocatorTitle = EditorGUIUtility.TrTextContent("Shared Bucket Allocator");
            public static readonly GUIContent kBucketAllocatorGranularity = EditorGUIUtility.TrTextContent("Bucket Allocator Granularity", "Bucket allocator bucket granularity");
            public static readonly GUIContent kBucketAllocatorBucketsCount = EditorGUIUtility.TrTextContent("Bucket Allocator BucketCount", "Number of bucket size increments of bucket granularity");
            public static readonly GUIContent kBucketAllocatorBlockSize = EditorGUIUtility.TrTextContent("Bucket Allocator Block Size", "Bucket allocator block size");
            public static readonly GUIContent kBucketAllocatorBlockCount = EditorGUIUtility.TrTextContent("Bucket Allocator Block Count", "Bucket allocator block count");

            public static readonly GUIContent kProfilerAllocatorTitle = EditorGUIUtility.TrTextContent("Profiler Allocators");
            public static readonly GUIContent kProfilerBlockSize = EditorGUIUtility.TrTextContent("Profiler Block Size", "Block size used by main profiler allocations");
            public static readonly GUIContent kProfilerEditorBlockSize = EditorGUIUtility.TrTextContent("Editor Profiler Block Size", "Editor only: Block size used by editor specific profiler allocations");

            public static readonly GUIContent kProfilerBucketAllocatorTitle = EditorGUIUtility.TrTextContent("Shared Profiler Bucket Allocator");
            public static readonly GUIContent kProfilerBucketAllocatorGranularity = EditorGUIUtility.TrTextContent("Bucket Allocator Granularity", "Bucket allocator bucket granularity");
            public static readonly GUIContent kProfilerBucketAllocatorBucketsCount = EditorGUIUtility.TrTextContent("Bucket Allocator BucketCount", "Number of bucket size increments of bucket granularity");
            public static readonly GUIContent kProfilerBucketAllocatorBlockSize = EditorGUIUtility.TrTextContent("Bucket Allocator Block Size", "Bucket allocator block size");
            public static readonly GUIContent kProfilerBucketAllocatorBlockCount = EditorGUIUtility.TrTextContent("Bucket Allocator Block Count", "Bucket allocator block count");

            public static readonly GUIContent kEditorLabel = EditorGUIUtility.TrTextContent("Editor", "Editor settings");
            public static readonly GUIContent kPlayerLabel = EditorGUIUtility.TrTextContent("Players", "player settings");
        }

        class Styles
        {
            public static readonly GUIStyle lockButton = "IN LockButton";
            public static readonly GUIStyle titleGroupHeader = new GUIStyle(EditorStyles.toolbar) { margin = new RectOffset() };
            public static readonly GUIStyle settingsFramebox = new GUIStyle(EditorStyles.frameBox) { padding = new RectOffset(1, 1, 1, 0) };

            public static readonly string warningDialogTitle = L10n.Tr("Edit memory settings");
            public static readonly string warningDialogText = L10n.Tr("Changing default memory setting can have severe negative impact on performance. Are you sure you want to continue?");
            public static readonly string okDialogButton = L10n.Tr("Ok");
            public static readonly string cancelDialogButton = L10n.Tr("Cancel");
        }

        const string kWarningDialogSessionKey = "MemorySettingsWarning";

        SerializedProperty m_PlatformMemorySettingsProperty;
        SerializedProperty m_EditorMemorySettingsProperty;
        SerializedProperty m_DefaultMemorySettingsProperty;

        static Styles s_Styles;
        static SettingsProvider s_SettingsProvider;

        int m_SelectedPlatform = 0;
        BuildPlatform[] m_ValidPlatforms;
        const int kMaxGroupCount = 10;
        bool[] m_ShowSettingsUI = new bool[kMaxGroupCount];
        AnimatedValues.AnimBool[] m_SettingsAnimator = new AnimatedValues.AnimBool[kMaxGroupCount];

        Dictionary<BuildTargetGroup, SerializedProperty> m_MemorySettingsDictionary;

        public void OnEnable()
        {
            m_ValidPlatforms = BuildPlatforms.instance.GetValidPlatforms(true).ToArray();

            m_EditorMemorySettingsProperty = serializedObject.FindProperty("m_EditorMemorySettings");
            m_PlatformMemorySettingsProperty = serializedObject.FindProperty("m_PlatformMemorySettings");
            m_DefaultMemorySettingsProperty = serializedObject.FindProperty("m_DefaultMemorySettings");

            m_MemorySettingsDictionary = new Dictionary<BuildTargetGroup, SerializedProperty>();

            foreach (SerializedProperty prop in m_PlatformMemorySettingsProperty)
            {
                m_MemorySettingsDictionary.Add((BuildTargetGroup)prop.FindPropertyRelative("first").intValue, prop.FindPropertyRelative("second"));
            }

            for (var i = 0; i < m_SettingsAnimator.Length; i++)
                m_SettingsAnimator[i] = new AnimatedValues.AnimBool(m_ShowSettingsUI[i], RepaintSettingsEditorWindow);
        }

        bool m_EditorSelected = true;
        static GUIStyle s_TabOnlyOne;
        static GUIStyle s_TabFirst;
        static GUIStyle s_TabMiddle;
        static GUIStyle s_TabLast;

        static Rect GetTabRect(Rect rect, int tabIndex, int tabCount, out GUIStyle tabStyle)
        {
            if (s_TabOnlyOne == null)
            {
                s_TabOnlyOne = "Tab onlyOne";
                s_TabFirst = "Tab first";
                s_TabMiddle = "Tab middle";
                s_TabLast = "Tab last";
            }

            tabStyle = s_TabMiddle;

            if (tabCount == 1)
            {
                tabStyle = s_TabOnlyOne;
            }
            else if (tabIndex == 0)
            {
                tabStyle = s_TabFirst;
            }
            else if (tabIndex == (tabCount - 1))
            {
                tabStyle = s_TabLast;
            }

            float tabWidth = rect.width / tabCount;
            int left = Mathf.RoundToInt(tabIndex * tabWidth);
            int right = Mathf.RoundToInt((tabIndex + 1) * tabWidth);
            return new Rect(rect.x + left, rect.y, right - left, EditorGUI.kTabButtonHeight);
        }

        void RepaintSettingsEditorWindow()
        {
            // Invoking a Repaint on an Editor instantiated via AssetSettingsProvider does not currently work due to a bug. So instead we store a reference to the settings provider and repaint it directly.
            s_SettingsProvider?.Repaint();
        }

        private bool BeginGroup(int index, GUIContent title)
        {
            Debug.Assert(kMaxGroupCount > index, "Max group count in MemorySettings is too low");

            var indentLevel = EditorGUI.indentLevel;
            EditorGUILayout.BeginVertical(GUILayout.Height(20));

            EditorGUILayout.BeginHorizontal((indentLevel == 0) ? Styles.titleGroupHeader : GUIStyle.none);
            Rect r = GUILayoutUtility.GetRect(title, EditorStyles.inspectorTitlebarText);
            r = EditorGUI.IndentedRect(r);
            EditorGUI.indentLevel = 0;
            m_ShowSettingsUI[index] = EditorGUI.FoldoutTitlebar(r, title, m_ShowSettingsUI[index], true, EditorStyles.inspectorTitlebarFlat, EditorStyles.inspectorTitlebarText);
            EditorGUI.indentLevel = indentLevel;
            EditorGUILayout.EndHorizontal();

            m_SettingsAnimator[index].target = m_ShowSettingsUI[index];

            var visible = EditorGUILayout.BeginFadeGroup(m_SettingsAnimator[index].faded);
            EditorGUI.indentLevel++;
            EditorGUILayout.Space();
            return visible;
        }

        private void EndGroup()
        {
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.EndFadeGroup();
            EditorGUILayout.EndVertical();
        }

        enum SizeEnum
        {
            B,
            KB,
            MB,
        }

        private void OptionalVariableField(SerializedProperty settings, string variablename, GUIContent label, bool useBytes = true)
        {
            const int k_FieldSpacing = 2;
            if (s_Styles == null)
                s_Styles = new Styles();

            var prop = settings.FindPropertyRelative(variablename);
            var defaultValueProp = m_DefaultMemorySettingsProperty.FindPropertyRelative(variablename);
            var overrideText = new GUIContent(string.Empty, "Override Default");

            var toggleSize = EditorStyles.toggle.CalcSize(overrideText);
            var enumValue = SizeEnum.MB;
            var sizeEnumWidth = EditorStyles.popup.CalcSize(GUIContent.Temp(enumValue.ToString())).x;
            var minWidth = EditorGUI.indent + EditorGUIUtility.labelWidth + EditorGUI.kSpacing + toggleSize.x + EditorGUI.kSpacing + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing + sizeEnumWidth;
            var rect = GUILayoutUtility.GetRect(minWidth, EditorGUIUtility.singleLineHeight + k_FieldSpacing);
            rect.height -= k_FieldSpacing;
            rect = EditorGUI.IndentedRect(rect);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var labelRect = rect;
            labelRect.width = EditorGUIUtility.labelWidth;
            GUI.Label(labelRect, label);

            var toggleRect = rect;
            toggleRect.xMin = labelRect.xMax + EditorGUI.kSpacing;
            toggleRect.size = toggleSize;
            var useDefault = prop.intValue < 0;
            var newuseDefault = GUI.Toggle(toggleRect, useDefault, overrideText, Styles.lockButton);

            var fieldRect = rect;
            fieldRect.xMin = toggleRect.xMax + EditorGUI.kSpacing;
            fieldRect.xMax = Mathf.Max(fieldRect.xMax - sizeEnumWidth - EditorGUI.kSpacing, fieldRect.xMin + EditorGUIUtility.fieldWidth);
            var defaultValue = defaultValueProp.intValue;

            var sizeEnumRect = rect;
            sizeEnumRect.xMin = fieldRect.xMax + EditorGUI.kSpacing;
            sizeEnumRect.width = sizeEnumWidth;

            if (newuseDefault != useDefault)
            {
                if (!newuseDefault)
                {
                    var result = EditorUtility.DisplayDialog(Styles.warningDialogTitle, Styles.warningDialogText, Styles.okDialogButton, Styles.cancelDialogButton, DialogOptOutDecisionType.ForThisSession, kWarningDialogSessionKey);
                    if (!result)
                        newuseDefault = true;
                }

                prop.intValue = newuseDefault ? -1 : defaultValue;
            }

            if (newuseDefault)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    int displayValue = defaultValue;
                    if (useBytes)
                    {
                        enumValue = SizeEnum.B;
                        if ((defaultValue % (1024 * 1024)) == 0)
                        {
                            enumValue = SizeEnum.MB;
                            displayValue /= 1024 * 1024;
                        }
                        else if ((defaultValue % 1024) == 0)
                        {
                            enumValue = SizeEnum.KB;
                            displayValue /= 1024;
                        }
                    }
                    EditorGUI.IntField(fieldRect, displayValue);
                    if (useBytes)
                        EditorGUI.EnumPopup(sizeEnumRect, enumValue);
                }
            }
            else
            {
                int factor = 1;
                enumValue = SizeEnum.B;
                int oldIntValue = prop.intValue;
                if (useBytes)
                {
                    if ((oldIntValue % (1024 * 1024)) == 0)
                    {
                        factor = 1024 * 1024;
                        enumValue = SizeEnum.MB;
                    }
                    else if ((oldIntValue % 1024) == 0)
                    {
                        factor = 1024;
                        enumValue = SizeEnum.KB;
                    }
                }
                var newIntValue = factor * EditorGUI.DelayedIntField(fieldRect, oldIntValue / factor);
                if (useBytes)
                {
                    SizeEnum newEnumValue = (SizeEnum)EditorGUI.EnumPopup(sizeEnumRect, enumValue);

                    if (newEnumValue != enumValue)
                    {
                        if (newEnumValue == SizeEnum.MB)
                        {
                            if (enumValue == SizeEnum.KB)
                                newIntValue *= 1024;
                            else
                                newIntValue *= 1024 * 1024;
                        }
                        if (newEnumValue == SizeEnum.KB)
                        {
                            if (enumValue == SizeEnum.MB)
                                newIntValue /= 1024;
                            else
                                newIntValue *= 1024;
                        }
                        if (newEnumValue == SizeEnum.B)
                        {
                            if (enumValue == SizeEnum.MB)
                                newIntValue /= 1024 * 1024;
                            else
                                newIntValue /= 1024;
                        }
                    }
                }
                prop.intValue = newIntValue;
            }

            EditorGUI.indentLevel = indent;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.HelpBox(Content.kGeneralSettingsWarning.text, MessageType.Warning, true);

            Rect r = EditorGUILayout.BeginVertical(Styles.settingsFramebox);
            GUIStyle buttonStyle = null;

            Rect buttonRect = GetTabRect(r, 0, 2, out buttonStyle);
            if (GUI.Toggle(buttonRect, m_EditorSelected, Content.kEditorLabel, buttonStyle))
                m_EditorSelected = true;

            buttonRect = GetTabRect(r, 1, 2, out buttonStyle);
            if (GUI.Toggle(buttonRect, !m_EditorSelected, Content.kPlayerLabel, buttonStyle))
                m_EditorSelected = false;

            GUILayoutUtility.GetRect(10, EditorGUI.kTabButtonHeight);

            EditorGUI.EndChangeCheck();

            SerializedProperty currentSettings;
            if (m_EditorSelected)
            {
                EditorGUI.BeginChangeCheck();
                GUILayout.Label("Settings for Editor");
                EditorGUILayout.HelpBox(Content.kEditorSettingsWarning.text, MessageType.Warning, true);
                EditorGUILayout.Space();
                currentSettings = m_EditorMemorySettingsProperty;
                MemorySettingsUtils.InitializeDefaultsForPlatform(-1);
            }
            else
            {
                GUILayout.Label("Settings for Players");
                m_SelectedPlatform = EditorGUILayout.BeginPlatformGrouping(m_ValidPlatforms, null, Styles.settingsFramebox);
                GUILayout.Label(string.Format(L10n.Tr("Settings for {0}"), m_ValidPlatforms[m_SelectedPlatform].title.text));
                if (!m_MemorySettingsDictionary.TryGetValue(m_ValidPlatforms[m_SelectedPlatform].targetGroup, out currentSettings))
                {
                    MemorySettingsUtils.SetPlatformDefaultValues((int)m_ValidPlatforms[m_SelectedPlatform].targetGroup);
                    serializedObject.Update();
                    OnEnable();
                    m_MemorySettingsDictionary.TryGetValue(m_ValidPlatforms[m_SelectedPlatform].targetGroup, out currentSettings);
                }
                MemorySettingsUtils.InitializeDefaultsForPlatform((int)m_ValidPlatforms[m_SelectedPlatform].targetGroup);
            }

            if (BeginGroup(0, Content.kMainAllocatorsTitle))
            {
                if (BeginGroup(1, Content.kMainAllocatorTitle))
                {
                    OptionalVariableField(currentSettings, "m_MainAllocatorBlockSize", Content.kMainAllocatorBlockSize);
                    OptionalVariableField(currentSettings, "m_ThreadAllocatorBlockSize", Content.kThreadAllocatorBlockSize);
                }
                EndGroup();
                if (BeginGroup(2, Content.kGfxAllocatorTitle))
                {
                    OptionalVariableField(currentSettings, "m_MainGfxBlockSize", Content.kMainGfxBlockSize);
                    OptionalVariableField(currentSettings, "m_ThreadGfxBlockSize", Content.kThreadGfxBlockSize);
                }
                EndGroup();
                if (BeginGroup(3, Content.kExtraAllocatorTitle))
                {
                    OptionalVariableField(currentSettings, "m_CacheBlockSize", Content.kCacheBlockSize);
                    OptionalVariableField(currentSettings, "m_TypetreeBlockSize", Content.kTypetreeBlockSize);
                }
                EndGroup();
                if (BeginGroup(4, Content.kBucketAllocatorTitle))
                {
                    OptionalVariableField(currentSettings, "m_BucketAllocatorGranularity", Content.kBucketAllocatorGranularity);
                    OptionalVariableField(currentSettings, "m_BucketAllocatorBucketsCount", Content.kBucketAllocatorBucketsCount, false);
                    OptionalVariableField(currentSettings, "m_BucketAllocatorBlockSize", Content.kBucketAllocatorBlockSize);
                    OptionalVariableField(currentSettings, "m_BucketAllocatorBlockCount", Content.kBucketAllocatorBlockCount, false);
                }
                EndGroup();
            }
            EndGroup();

            if (BeginGroup(5, m_EditorSelected ? Content.kTempAllocatorTitle_Editor : Content.kTempAllocatorTitle_Player))
            {
                OptionalVariableField(currentSettings, "m_TempAllocatorSizeMain", Content.kTempAllocatorSizeMain);
                OptionalVariableField(currentSettings, "m_TempAllocatorSizeJobWorker", Content.kTempAllocatorSizeJobWorker);
                OptionalVariableField(currentSettings, "m_TempAllocatorSizeBackgroundWorker", Content.kTempAllocatorSizeBackgroundWorker);
                OptionalVariableField(currentSettings, "m_TempAllocatorSizePreloadManager", Content.kTempAllocatorSizePreloadManager);
                OptionalVariableField(currentSettings, "m_TempAllocatorSizeAudioWorker", Content.kTempAllocatorSizeAudioWorker);
                OptionalVariableField(currentSettings, "m_TempAllocatorSizeCloudWorker", Content.kTempAllocatorSizeCloudWorker);
                OptionalVariableField(currentSettings, "m_TempAllocatorSizeGfx", Content.kTempAllocatorSizeGfx);
                OptionalVariableField(currentSettings, "m_TempAllocatorSizeGIBakingWorker", Content.kTempAllocatorSizeGIBakingWorker);
                OptionalVariableField(currentSettings, "m_TempAllocatorSizeNavMeshWorker", Content.kTempAllocatorSizeNavMeshWorker);
            }
            EndGroup();
            if (BeginGroup(6, Content.kJobTempAllocatorTitle))
            {
                OptionalVariableField(currentSettings, "m_JobTempAllocatorBlockSize", Content.kJobTempAllocatorBlockSize);
                OptionalVariableField(currentSettings, "m_BackgroundJobTempAllocatorBlockSize", Content.kBackgroundJobTempAllocatorBlockSize);
                OptionalVariableField(currentSettings, "m_JobTempAllocatorReducedBlockSize", Content.kJobTempAllocatorReducedBlockSize);
            }
            EndGroup();

            if (BeginGroup(7, Content.kProfilerAllocatorTitle))
            {
                OptionalVariableField(currentSettings, "m_ProfilerBlockSize", Content.kProfilerBlockSize);
                if (m_EditorSelected)
                    OptionalVariableField(currentSettings, "m_ProfilerEditorBlockSize", Content.kProfilerEditorBlockSize);

                if (BeginGroup(8, Content.kProfilerBucketAllocatorTitle))
                {
                    OptionalVariableField(currentSettings, "m_ProfilerBucketAllocatorGranularity", Content.kProfilerBucketAllocatorGranularity);
                    OptionalVariableField(currentSettings, "m_ProfilerBucketAllocatorBucketsCount", Content.kProfilerBucketAllocatorBucketsCount, false);
                    OptionalVariableField(currentSettings, "m_ProfilerBucketAllocatorBlockSize", Content.kProfilerBucketAllocatorBlockSize);
                    OptionalVariableField(currentSettings, "m_ProfilerBucketAllocatorBlockCount", Content.kProfilerBucketAllocatorBlockCount, false);
                }
                EndGroup();
            }
            EndGroup();

            if (m_EditorSelected)
            {
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    MemorySettingsUtils.WriteEditorMemorySettings();
                }
            }
            else
            {
                EditorGUILayout.EndPlatformGrouping();
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.EndVertical();
        }

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(
                "Project/Memory Settings", "ProjectSettings/MemorySettings.asset",
                SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Content>());
            s_SettingsProvider = provider;
            return provider;
        }
    }
}
