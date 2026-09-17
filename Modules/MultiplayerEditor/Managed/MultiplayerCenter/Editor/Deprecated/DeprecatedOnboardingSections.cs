// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Multiplayer.Center.Common;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    [Obsolete("To be removed when IOnboardingSection is removed.")]
    class DeprecatedOnboardingSections : VisualElement
    {
        IOnboardingSection m_Section;

        public DeprecatedOnboardingSections(IOnboardingSection section)
        {
            m_Section = section;
            styleSheets.Add(EditorGUIUtility.LoadRequired("Multiplayer/MultiplayerCenter/UI/DeprecatedMultiplayerCenterWindow.uss") as StyleSheet);
            AddToClassList(StyleClasses.CategorySection);
            RegisterCallback<AttachToPanelEvent>(AttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(DetachFromPanel);
        }

        void AttachToPanel(AttachToPanelEvent evt)
        {
            m_Section.Load();
            Add(m_Section.Root);
        }

        void DetachFromPanel(DetachFromPanelEvent evt)
        {
            Remove(m_Section.Root);
            m_Section.Unload();
        }
    }
}
