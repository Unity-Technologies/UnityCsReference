// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderPropertyPathInfoView : VisualElement
    {
        static readonly string s_UssClassName = "unity-builder-inspector__varinfo-view";
        static readonly string s_ValueLabelUssName = "unity-builder-inspector__varinfo-view__data-label";
        static readonly string s_UnresolvedValueLabelUssClassName = s_ValueLabelUssName + "--unresolved";
        static readonly string s_EmptyText = "None";

        Label m_NameLabel;
        Label m_DataTypeLabel;
        Label m_ValueLabel;

        public string propertyPath
        {
            get => m_NameLabel.text;
            set => m_NameLabel.text = value;
        }

        public string dataType
        {
            get => m_DataTypeLabel.text;
            set => m_DataTypeLabel.text = value;
        }

        public string value
        {
            get => m_ValueLabel.text;
            set => m_ValueLabel.text = value;
        }

        public BuilderPropertyPathInfoView()
        {
            AddToClassList(s_UssClassName);

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Inspector/PropertyPathInfoDetailsView.uxml");
            template.CloneTree(this);
            m_NameLabel = this.Q<Label>("name-label");
            m_DataTypeLabel = this.Q<Label>("data-type-label");
            m_ValueLabel = this.Q<Label>("value-label");

            ClearUI();
        }

        void ClearUI()
        {
            propertyPath = s_EmptyText;
            dataType = s_EmptyText;
            value = s_EmptyText;
        }

        public void SetInfo(object target, PropertyPathInfo path, IProperty property)
        {
            ClearUI();

            if (string.IsNullOrEmpty(path.propertyPath.ToString()))
                return;

            var name = path.propertyPath.ToString();
            bool inArray = false;

            if (name.Contains("[0]"))
            {
                name = name.Replace("[0]", "[index]");
                inArray = true;
            }
            propertyPath = name;
            dataType = path.type.GetDisplayFullName();
            if (property != null && !inArray && PropertyContainer.TryGetValue(target, path.propertyPath, out object v))
            {
                if (v != null)
                {
                    if (v is string vStr)
                    {
                        value = "\"" + vStr + "\"";
                    }
                    else
                    {
                        value = v.ToString();
                    }
                }
                else
                {
                    value = "null";
                }
                m_ValueLabel.RemoveFromClassList(s_UnresolvedValueLabelUssClassName);
            }
            else
            {
                value = BuilderConstants.UnresolvedValue;
                m_ValueLabel.AddToClassList(s_UnresolvedValueLabelUssClassName);
            }
        }
    }
}
