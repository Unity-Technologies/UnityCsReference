// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    internal class BuildProfilePlatformGroup : VisualElement
    {
        protected virtual string k_Uxml => "BuildProfile/UXML/BuildProfilePlatformGroup.uxml";
        protected readonly Label m_Text;
        protected readonly VisualElement m_Container;
        readonly PlatformDiscoveryWindow m_Parent;

        internal BuildProfilePlatformGroup(PlatformDiscoveryWindow parent)
        {
            m_Parent = parent;
            var uxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var stylesheet = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            styleSheets.Add(stylesheet);
            uxml.CloneTree(this);

            m_Text = this.Q<Label>("platform-group-label");
            m_Container = this.Q<VisualElement>("platform-element-container");
        }

        internal void Set(string groupingName, BuildProfileCard[] cards)
        {
            m_Text.text = groupingName;

            int i = 0;
            foreach (var card in cards)
            {
                var platformElement = new BuildProfilePlatformElement(card, element => m_Parent.OnCardSelected(element.GetBuildProfileCard(), element));
                m_Container.Insert(i, platformElement);
                i++;
            }
        }
    }
}
