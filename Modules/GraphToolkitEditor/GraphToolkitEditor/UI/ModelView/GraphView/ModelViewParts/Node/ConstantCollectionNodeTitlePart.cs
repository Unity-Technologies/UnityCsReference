// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class ConstantCollectionNodeTitlePart : NodeTitlePart
    {
        ConstantNodeModel Constant => m_Model as ConstantNodeModel;

        protected ConstantCollectionNodeTitlePart(string name, GraphElementModel model, ChildView ownerElement, string parentClassName, int options)
            : base(name, model, ownerElement, parentClassName, options)
        {
        }

        public static ConstantCollectionNodeTitlePart Create(string name, GraphElementModel model, ChildView ownerElement, string parentClassName, int options)
        {
            return new ConstantCollectionNodeTitlePart(name, model, ownerElement, parentClassName, options);
        }

        protected override void BuildUI(VisualElement container)
        {
            base.BuildUI(container);

            (TitleLabel as Label).text = TypeHelpers.GetFriendlyName(Constant.Type).Nicify();
            SubTitle.text = "Constant";

            m_OwnerElement.EnableInClassList(emptySubtitleUssClassName, false);
        }

        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            // This part doesn't need updating.
        }
    }
}
