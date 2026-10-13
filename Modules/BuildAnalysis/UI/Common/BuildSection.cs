// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    // Addressables' SummaryTabEmbedded.uxml uses this tag and its attributes.
    // To rename one, keep the old name with [MovedFrom] or [UxmlAttribute("new-name", "old-name")].
    [UxmlElement]
    internal partial class BuildSection : VisualElement
    {
        public static readonly string ussClassName = "section";
        public static readonly string foldoutUssClassName = "section-foldout";
        public static readonly string contentUssClassName = "section-content";

        private const string k_UssPath = "BuildAnalysis/StyleSheets/Section.uss";

        private readonly Foldout m_Foldout;
        private readonly VisualElement m_Content;

        public Foldout foldout => m_Foldout;

        [UxmlAttribute]
        public string title
        {
            get => m_Foldout.text;
            set => m_Foldout.text = value;
        }

        public override VisualElement contentContainer => m_Content;

        public BuildSection() : this(string.Empty)
        {
        }

        public BuildSection(string title, bool padContent = true)
        {
            var styleSheet = EditorGUIUtility.LoadRequired(k_UssPath) as StyleSheet;
            styleSheets.Add(styleSheet);
            AddToClassList(ussClassName);

            m_Foldout = new Foldout { text = title, value = true };
            m_Foldout.AddToClassList(foldoutUssClassName);
            hierarchy.Add(m_Foldout);

            if (padContent)
            {
                m_Content = new VisualElement();
                m_Content.AddToClassList(contentUssClassName);
                m_Foldout.Add(m_Content);
            }
            else
            {
                m_Content = m_Foldout.contentContainer;
            }
        }
    }
}
