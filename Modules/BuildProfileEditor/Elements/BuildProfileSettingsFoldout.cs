// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

        readonly string m_Tooltip;
        Vector2 m_TooltipPosVector;

        readonly string m_DataKey;
        IBuildProfileSettingsProvider m_Provider;
        BuildProfile m_BuildProfile;
        Foldout m_Root;
        Action<BuildProfile> m_OnReset;

        public BuildProfileSettingsFoldout(
            SerializedObject serializedObject,
            BuildProfile profile,
            IBuildProfileSettingsProvider provider) : base()
        {
            m_BuildProfile = profile;
            m_Provider = provider;
            m_OnReset = provider.GetResetAction();
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
            menu.AddItem(s_RemoveContent, false, OnRemove);
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
    }
}
