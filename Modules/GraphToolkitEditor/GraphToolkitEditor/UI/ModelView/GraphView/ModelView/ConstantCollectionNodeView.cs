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
        public static readonly string constantCollectionName = "ge-constant-collection";

        const string k_TypeIconStylesheet = "TypeIcons.uss";

        ConstantNodeModel Constant => Model as ConstantNodeModel;

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
            titlePart.Root.AddPackageStylesheet(k_TypeIconStylesheet);

            // Add buttons to the title part
            foreach (var b in NodeToolbarButtons)
                titlePart.AddNodeToolbarButton(b);

            RootView.TypeHandleInfos.AddUssClasses(GraphElementHelper.iconDataTypeClassPrefix, titlePart.Icon, Constant.OutputPort.DataTypeHandle);

            NodeColorLinePart nodeColorLinePart = PartList.GetPart(topColorLineContainerPartName) as NodeColorLinePart;

            // We set the override so that the color line part doesn't update itself based on the model.
            // The color is resolved directly from a stylesheet
            nodeColorLinePart.OverrideColor();
            nodeColorLinePart.Root.AddToClassList(GraphElementHelper.colorLineDatatTypeClassPrefix + RootView.TypeHandleInfos.GetUssName(Constant.OutputPort.DataTypeHandle));

            bool overrideIcon = true;
            (Texture2D icon, Color color)? typeStyle = GraphElementModel.GraphModel.GetDataTypeStyle(Constant.Type);
            if (!typeStyle.HasValue)
            {
                typeStyle = GraphElementModel.GraphModel.GetDataTypeStyle(Constant.Type.GetCollectionElementType());
                overrideIcon = false;
            }
            if (typeStyle.HasValue)
            {
                nodeColorLinePart.OverrideColor(typeStyle.Value.color);

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
