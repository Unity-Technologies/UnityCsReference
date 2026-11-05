// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    // Addressables' SummaryTabEmbedded.uxml uses this tag and its attributes.
    // To rename one, keep the old name with [MovedFrom] or [UxmlAttribute("new-name", "old-name")].
    [UxmlElement]
    internal sealed partial class StatCard : VisualElement
    {
        // Build Analysis' own icons, themed by StatCard.uss. None leaves the icon to the caller's USS.
        public enum Icon
        {
            None,
            BuildSize,
            Duration,
            Assets,
            Scenes,
            RootAsset,
        }

        public static readonly string ussClassName = "stat-card-element";

        private const string k_UxmlPath = "BuildAnalysis/UXML/StatCard.uxml";

        private readonly Label m_Value;
        private readonly Label m_Label;
        private Icon m_IconKind;

        // CloneTree adds StatCard.uxml's <Style> to this element.
        public StatCard()
        {
            var template = EditorGUIUtility.LoadRequired(k_UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);
            AddToClassList(ussClassName);

            m_Value = this.Q<Label>("value");
            m_Label = this.Q<Label>("label");
        }

        public StatCard(string name, string label) : this()
        {
            this.name = name;
            this.label = label;
        }

        [UxmlAttribute]
        public string label
        {
            get => m_Label.text;
            set => m_Label.text = value ?? string.Empty;
        }

        [UxmlAttribute]
        public string value
        {
            get => m_Value.text;
            set => m_Value.text = value ?? string.Empty;
        }

        [UxmlAttribute("icon")]
        public Icon iconKind
        {
            get => m_IconKind;
            set
            {
                RemoveFromClassList(IconUssClassName(m_IconKind));
                m_IconKind = value;
                AddToClassList(IconUssClassName(value));
            }
        }

        private static string IconUssClassName(Icon kind)
        {
            return kind switch
            {
                Icon.BuildSize => "stat-card--build-size",
                Icon.Duration => "stat-card--duration",
                Icon.Assets => "stat-card--assets",
                Icon.Scenes => "stat-card--scenes",
                Icon.RootAsset => "stat-card--root-asset",
                _ => "stat-card--no-icon",
            };
        }
    }
}
