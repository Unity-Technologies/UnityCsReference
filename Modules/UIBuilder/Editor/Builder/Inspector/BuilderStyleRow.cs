// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderStyleRow : BindableElement
    {
        static readonly string s_UssClassName = "unity-builder-style-row";

        VisualElement m_Container;

        public new class UxmlFactory : UxmlFactory<BuilderStyleRow, UxmlTraits> { }

        public new class UxmlTraits : BindableElement.UxmlTraits { }

        public BuilderStyleRow()
        {
            AddToClassList(s_UssClassName);

            var visualAsset = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/Inspector/BuilderStyleRow.uxml");
            visualAsset.CloneTree(this);

            m_Container = this.Q("content-container");
        }

        public override VisualElement contentContainer => m_Container == null ? this : m_Container;
    }
}
