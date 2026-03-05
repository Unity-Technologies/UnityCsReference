// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class ConstantCollectionNodeView : CollapsibleInOutNodeView
    {
        ConstantNodeModel Constant => Model as ConstantNodeModel;

        public static readonly string constantCollectionName = "ge-constant-collection";

        protected override void BuildPartList()
        {

            base.BuildPartList();

            PartList.ReplacePart(titleIconContainerPartName, ConstantCollectionNodeTitlePart.Create(titleIconContainerPartName, NodeModel, this, ussClassName, NodeTitlePart.Options.Default));

            PartList.RemovePart(topPortContainerPartName);
            PartList.RemovePart(bottomPortContainerPartName);
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            ConstantCollectionNodeTitlePart titlePart = PartList.GetPart(titleIconContainerPartName) as ConstantCollectionNodeTitlePart;
            titlePart.Root.AddPackageStylesheet("TypeIcons.uss");

            // Add buttons to the title part
            foreach (var b in NodeToolbarButtons)
                titlePart.AddNodeToolbarButton(b);

            RootView.TypeHandleInfos.AddUssClasses(GraphElementHelper.iconDataTypeClassPrefix, titlePart.Icon, Constant.OutputPort.DataTypeHandle);

            var colorLine = titlePart.Root.Q<VisualElement>(name: ConstantCollectionNodeTitlePart.colorLineName);
            colorLine.AddToClassList(GraphElementHelper.colorLineDatatTypeClassPrefix + RootView.TypeHandleInfos.GetUssName(Constant.OutputPort.DataTypeHandle));

            bool overrideIcon = true;
            (Texture2D icon, Color color)? typeStyle = GraphElementModel.GraphModel.GetDataTypeStyle(Constant.Type);
            if (!typeStyle.HasValue)
            {
                typeStyle = GraphElementModel.GraphModel.GetDataTypeStyle(Constant.Type.GetCollectionElementType());
                overrideIcon = false;
            }
            if (typeStyle.HasValue)
            {
                colorLine.style.backgroundColor = typeStyle.Value.color;
                var icon = (EditableTitlePart as NodeTitlePart).Icon;
                if (icon != null)
                {
                    icon.tintColor = typeStyle.Value.color;
                    if (typeStyle.Value.icon != null && overrideIcon)
                        icon.image = typeStyle.Value.icon;
                }
            }
            AddToClassList(constantCollectionName);

        }
    }
}
