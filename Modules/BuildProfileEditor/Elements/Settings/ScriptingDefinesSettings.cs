// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// Additional script compilation defines stored under <see cref="BuildProfile.scriptingDefines"/>.
    /// The internal flag <see cref="BuildProfile.hasScriptingDefines"/> determines settings visibility
    /// in editor.
    /// </summary>
    internal class ScriptingDefinesSettings : IBuildProfileSettingsProvider
    {
        /// <summary>
        /// Scripting define visual element handles comitting scripting define changes
        /// to a build profile. Active profiles prompts for script recompilation when
        /// navigating away from the editor window. Play mode must reflect active profiles
        /// scripting defiens.
        /// </summary>
        class ScriptingDefinesVisualElement : VisualElement
        {
            const string k_Uxml = "BuildProfile/UXML/VisualElement/ScriptingDefinesSettings.uxml";

            SerializedObject m_SerializedObject;
            BuildProfile m_Profile;
            Button recompileDefinesButton;
            Button revertDefinesButton;

            public ScriptingDefinesVisualElement(BuildProfile profile, SerializedObject serializedObject)
            {
                var uxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
                uxml.CloneTree(this);

                m_Profile = profile;
                m_SerializedObject = serializedObject;

                recompileDefinesButton = this.Q<Button>("scripting-defines-apply-button");
                revertDefinesButton = this.Q<Button>("scripting-defines-revert-button");
                var listView = this.Q<ListView>("scripting-defines-listview");
                var warningHelpbox = this.Q<HelpBox>("scripting-defines-warning-help-box");

                warningHelpbox.text = TrText.scriptingDefinesWarningHelpbox;
                revertDefinesButton.text = TrText.revert;
                recompileDefinesButton.text = TrText.apply;

                recompileDefinesButton.clicked += () => BuildProfileModuleUtil.RequestScriptCompilation(profile);
                revertDefinesButton.clicked += RevertScriptingDefines;

                var property = serializedObject.FindProperty("m_ScriptingDefines");
                listView.TrackPropertyValue(property, this.OnScriptingDefinePropertyChange);
                listView.BindProperty(property);

                if (!profile.IsActiveBuildProfileOrPlatform())
                {
                    recompileDefinesButton.Hide();
                    revertDefinesButton.Hide();
                    warningHelpbox.Hide();
                }
                else
                {
                    BuildProfileContext.instance.cachedEditorScriptingDefines = (string[])profile.scriptingDefines.Clone();
                    var targetName = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(profile.buildTarget));
                    if (string.IsNullOrEmpty(PlayerSettings.GetScriptingDefineSymbols(targetName)))
                    {
                        warningHelpbox.Hide();
                    }
                }

                this.OnScriptingDefinePropertyChange(property);

                RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            }

            void OnScriptingDefinePropertyChange(SerializedProperty property)
            {
                if (!m_Profile.IsActiveBuildProfileOrPlatform())
                    return;

                var lastCompiledDefines = BuildProfileContext.instance.cachedEditorScriptingDefines;
                if (property.arraySize != lastCompiledDefines.Length)
                {
                    recompileDefinesButton.SetEnabled(true);
                    revertDefinesButton.SetEnabled(true);
                    return;
                }

                for (int i = 0; i < property.arraySize; i++)
                {
                    if (property.GetArrayElementAtIndex(i).stringValue != lastCompiledDefines[i])
                    {
                        recompileDefinesButton.SetEnabled(true);
                        revertDefinesButton.SetEnabled(true);
                        return;
                    }
                }

                recompileDefinesButton.SetEnabled(false);
                revertDefinesButton.SetEnabled(false);
            }

            void RevertScriptingDefines()
            {
                m_Profile.scriptingDefines = BuildProfileContext.instance.cachedEditorScriptingDefines;
                m_SerializedObject.Update();
                recompileDefinesButton.SetEnabled(false);
                revertDefinesButton.SetEnabled(false);
            }

            void OnAttachToPanel(AttachToPanelEvent evt)
            {
                EditorApplication.update += EditorUpdate;
            }

            void OnDetachFromPanel(DetachFromPanelEvent evt)
            {
                EditorApplication.update -= EditorUpdate;

                if (m_Profile == null)
                    return;

                if (m_Profile != BuildProfileContext.activeProfile)
                    return;

                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                var lastCompiledDefines = BuildProfileContext.instance.cachedEditorScriptingDefines;
                if (ArrayUtility.ArrayEquals(m_Profile.scriptingDefines, lastCompiledDefines))
                {
                    return;
                }

                if (EditorUtility.DisplayDialog(TrText.scriptingDefinesModified, TrText.scriptingDefinesModifiedBody, TrText.apply, TrText.revert))
                {
                    BuildProfileModuleUtil.RequestScriptCompilation(m_Profile);
                }
                else
                {
                    m_Profile.scriptingDefines = BuildProfileContext.instance.cachedEditorScriptingDefines;
                    EditorUtility.SetDirty(m_Profile);
                }
            }

            void EditorUpdate()
            {
                bool isCompiling = EditorApplication.isCompiling || EditorApplication.isUpdating;
                bool isVirtualTexturingValid = BuildProfileModuleUtil.IsVirtualTexturingSettingsValid(m_Profile.platformGuid);

                if (!isVirtualTexturingValid || isCompiling)
                {
                    recompileDefinesButton?.SetEnabled(false);
                    revertDefinesButton?.SetEnabled(false);
                }
            }
        }

        public string GetDisplayName()
        {
            return TrText.scriptingDefines;
        }

        public string GetTooltip()
        {
            return TrText.scriptingDefinesTooltip;
        }

        public bool HasSettings(BuildProfile profile)
        {
            return profile.hasScriptingDefines;
        }

        public void OnAdd(BuildProfile profile)
        {
            profile.hasScriptingDefines = true;
            EditorUtility.SetDirty(profile);
        }

        public void OnRemove(BuildProfile profile)
        {
            profile.hasScriptingDefines = false;
            profile.scriptingDefines = Array.Empty<string>();
            EditorUtility.SetDirty(profile);

            // Script recompilation required when removing non-empty
            // scripting defines from the active build profile.
            if (profile.IsActiveBuildProfileOrPlatform()
                && BuildProfileContext.instance.cachedEditorScriptingDefines.Length != 0)
            {
                BuildProfileModuleUtil.RequestScriptCompilation(profile);
            }
        }

        public Action<BuildProfile> GetResetAction() => null;

        public VisualElement CreateInspectorGUI(BuildProfile profile, SerializedObject serializedObject)
        {
            return new ScriptingDefinesVisualElement(profile, serializedObject);
        }
    }
}
