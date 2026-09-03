// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: BuildSettingsWindow not yet converted
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// Build profile editor settings section foldout main component.
    /// </summary>
    internal class BuildProfileSettingsFoldout : VisualElement
    {
        const string k_Uxml = "BuildProfile/UXML/BuildProfileSettingsFoldout.uxml";
        const string k_FoldoutRoot = "bp-settings-foldout";
        const string k_FoldoutOptions = "bp-settings-foldout-options";
        static readonly GUIContent s_ResetContent = EditorGUIUtility.TrTextContent("Reset");
        static readonly GUIContent s_RemoveContent = EditorGUIUtility.TrTextContent("Remove");
        static readonly GUIContent s_Copy = EditorGUIUtility.TrTextContent("Copy");
        static readonly GUIContent s_Paste = EditorGUIUtility.TrTextContent("Paste");

        readonly string m_Tooltip;
        Vector2 m_TooltipPosVector;

        readonly string m_DataKey;
        IBuildProfileSettingsProvider m_Provider;
        BuildProfile m_BuildProfile;
        Foldout m_Root;
        Action<BuildProfile> m_OnReset;
        Action<BuildProfile> m_OnCopy;
        Action<BuildProfile> m_OnPaste;

        public BuildProfileSettingsFoldout(
            SerializedObject serializedObject,
            BuildProfile profile,
            IBuildProfileSettingsProvider provider) : base()
        {
            m_BuildProfile = profile;
            m_Provider = provider;
            m_OnReset = provider.GetResetAction();
            m_OnCopy = provider.OnCopy();
            m_OnPaste = provider.OnPaste();
            m_DataKey = "bp-settings-foldout-" + provider.GetType().Name;

            var uxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var uss = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            styleSheets.Add(uss);
            uxml.CloneTree(this);

            m_Root = this.Q<Foldout>(k_FoldoutRoot);
            m_Root.text = provider.GetDisplayName();
            m_Root.Add(provider.CreateInspectorGUI(profile, serializedObject));
            m_Root.value = EditorPrefs.GetBool(m_DataKey, false);
            m_Root.RegisterValueChangedCallback(evt =>
            {
                EditorPrefs.SetBool(m_DataKey, evt.newValue);
            });

            m_Tooltip = provider.GetTooltip();
            if (!string.IsNullOrEmpty(m_Tooltip))
            {
                RegisterTooltipCallbacks();
            }

            var options = this.Q<Button>(k_FoldoutOptions);
            options.clicked += OptionsClicked;

            var foldoutHeader = m_Root.Q<Toggle>();
            foldoutHeader.RegisterCallback<ContextClickEvent>(_ => FoldoutContextMenu());
        }

        /// <summary>
        /// Default foldout tooltip renders above inspector window,
        /// repositions tooltip rec to label position.
        /// </summary>
        void RegisterTooltipCallbacks()
        {
            var foldoutLabel = this.Q<Label>();
            foldoutLabel.RegisterCallback<MouseOverEvent>(evt =>
            {
                m_TooltipPosVector = evt.mousePosition;
            });
            foldoutLabel.RegisterCallback<TooltipEvent>(evt =>
            {
                evt.tooltip = m_Tooltip;
                evt.rect = new Rect(m_TooltipPosVector.x, m_TooltipPosVector.y, evt.rect.width, evt.rect.height);
                evt.StopPropagation();
            });
        }

        void OptionsClicked()
        {
            var menu = new GenericMenu();
            if (m_OnReset != null)
                menu.AddItem(s_ResetContent, false, OnReset);
            if (!m_Provider.GetIsRequired())
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    menu.AddDisabledItem(s_RemoveContent);
                else
                    menu.AddItem(s_RemoveContent, false, OnRemove);
            }
            menu.ShowAsContext();
        }

        void OnReset()
        {
            if (!EditorUtility.DisplayDialog(
                    TrText.resetSettings,
                    TrText.resetMessage,
                    TrText.reset, TrText.cancelButtonText))
            {
                return;
            }

            m_OnReset?.Invoke(m_BuildProfile);
            BuildProfileModuleUtil.UpdateActiveEditors(m_BuildProfile);
        }

        void OnRemove()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!EditorUtility.DisplayDialog(
                    TrText.removeSettings,
                    TrText.removeMessage,
                    TrText.remove, TrText.cancelButtonText))
            {
                return;
            }

            m_Provider.OnRemove(m_BuildProfile);
            BuildProfileModuleUtil.UpdateActiveEditors(m_BuildProfile);
        }

        void FoldoutContextMenu()
        {
            var menu = new GenericMenu();

            if(m_OnCopy is not null)
                menu.AddItem(s_Copy, false, OnCopy);
            if(m_OnPaste is not null)
                menu.AddItem(s_Paste, false, OnPaste);

            menu.ShowAsContext();
        }

        void OnCopy()
        {
            m_OnCopy?.Invoke(m_BuildProfile);
        }

        void OnPaste()
        {
            m_OnPaste?.Invoke(m_BuildProfile);
            BuildProfileModuleUtil.UpdateActiveEditors(m_BuildProfile);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
