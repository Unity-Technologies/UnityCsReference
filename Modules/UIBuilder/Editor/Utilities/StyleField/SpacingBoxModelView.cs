// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class SpacingBoxModelView : VisualElement
    {
        static readonly string k_UssPathNoExt = BuilderConstants.UtilitiesPath + "/StyleField/StyleSectionsBoxModel";
        private static readonly string k_MouseOverClassName = "mouse-over";
        static readonly string k_BoxModelClassName = "unity-box-model";
        static readonly string k_ViewClassName = k_BoxModelClassName + "__view";
        static readonly string k_ContainerClassName = k_BoxModelClassName + "__view__container";
        static readonly string k_ContainerMarginClassName = k_BoxModelClassName + "__container__margin";
        static readonly string k_ContainerPaddingClassName = k_BoxModelClassName + "__container__padding";
        static readonly string k_ContainerBorderClassName = k_BoxModelClassName + "__container__border";
        static readonly string k_ContainerContentClassName = k_BoxModelClassName + "__container__content";
        static readonly string k_TextfieldClassName = k_BoxModelClassName + "__textfield";
        static readonly string k_TextfieldContentClassName = k_TextfieldClassName + "__content-center";
        static readonly string k_TextfieldSpacerClassName = k_TextfieldClassName + "__spacer";
        static readonly string k_TextfieldCenterSpacerClassName = k_TextfieldClassName + "__center-spacer";
        static readonly string k_CheckerboardClassName = k_BoxModelClassName + "__checkerboard-background";

        private VisualElement m_Container;
        private VisualElement m_Layer1;
        private VisualElement m_Layer2;
        private BoxModelElement<string, BoxModelStyleField> m_MarginBox;
        private BoxModelElement<string, BoxModelStyleField> m_PaddingBox;
        private VisualElement m_ContentBox;
        private VisualElement m_BorderBox;

        // margin containers
        private VisualElement m_TopTextFieldMarginContainer = new VisualElement();
        private VisualElement m_BottomTextFieldMarginContainer = new VisualElement();
        private VisualElement m_LeftTextFieldMarginContainer = new VisualElement();
        private VisualElement m_RightTextFieldMarginContainer = new VisualElement();

        // padding containers
        private VisualElement m_TopTextFieldPaddingContainer = new VisualElement();
        private VisualElement m_BottomTextFieldPaddingContainer = new VisualElement();
        private VisualElement m_LeftTextFieldPaddingContainer = new VisualElement();
        private VisualElement m_RightTextFieldPaddingContainer = new VisualElement();

        private VisualElement m_ContentCenter = new VisualElement();
        private VisualElement m_CenterSpacer = new VisualElement();

        private BuilderInspector m_Inspector;

        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new SpacingBoxModelView();
        }

        public SpacingBoxModelView()
        {
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPathNoExt + (EditorGUIUtility.isProSkin ? "Dark" : "Light") + ".uss"));
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPathNoExt + ".uss"));

            AddToClassList(k_ViewClassName);
            AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);
            
            m_Container = new VisualElement();
            m_Container.AddToClassList(k_ContainerClassName);
            m_Container.AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);

            m_Layer1 = new VisualElement()
            {
                name = "BoxModelViewLayer1", classList = { BuilderConstants.InspectorCompositeStyleRowElementClassName }
            };
            m_Layer2 = new VisualElement()
            {
                name = "BoxModelViewLayer2", classList = { BuilderConstants.InspectorCompositeStyleRowElementClassName }
            };
            m_Layer2.pickingMode = PickingMode.Ignore;
            m_Layer2.StretchToParentSize();

            StyleContainers();

            m_ContentCenter.pickingMode = PickingMode.Ignore;
            m_ContentCenter.AddToClassList(k_TextfieldContentClassName);
            m_ContentCenter.AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);

            m_CenterSpacer.pickingMode = PickingMode.Ignore;
            m_CenterSpacer.AddToClassList(k_TextfieldCenterSpacerClassName);

            m_ContentCenter.Add(m_LeftTextFieldMarginContainer);
            m_ContentCenter.Add(m_LeftTextFieldPaddingContainer);
            m_ContentCenter.Add(m_CenterSpacer);
            m_ContentCenter.Add(m_RightTextFieldPaddingContainer);
            m_ContentCenter.Add(m_RightTextFieldMarginContainer);

            m_Layer2.Add(m_TopTextFieldMarginContainer);
            m_Layer2.Add(m_TopTextFieldPaddingContainer);
            m_Layer2.Add(m_ContentCenter);
            m_Layer2.Add(m_BottomTextFieldPaddingContainer);
            m_Layer2.Add(m_BottomTextFieldMarginContainer);

            m_ContentBox = new VisualElement()
            {
                classList =
                {
                    BuilderConstants.InspectorCompositeStyleRowElementClassName,
                    k_BoxModelClassName, k_ContainerContentClassName
                }
            };
            m_ContentBox.tooltip = "Size";
            m_ContentBox.RegisterCallback<MouseOverEvent, BoxType>(OnMouseOver, BoxType.Content);

            m_PaddingBox = new BoxModelElement<string, BoxModelStyleField>(BoxType.Padding, true, m_ContentBox,
                m_TopTextFieldPaddingContainer, m_BottomTextFieldPaddingContainer,
                m_LeftTextFieldPaddingContainer, m_RightTextFieldPaddingContainer);
            m_PaddingBox.tooltip = BoxType.Padding.ToString();
            m_PaddingBox.AddToClassList(k_BoxModelClassName);
            m_PaddingBox.AddToClassList(k_ContainerPaddingClassName);
            m_PaddingBox.AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);
            m_PaddingBox.RegisterCallback<MouseOverEvent, BoxType>(OnMouseOver, BoxType.Padding);

            m_BorderBox = new VisualElement();
            m_BorderBox.tooltip = BoxType.Border.ToString();
            m_BorderBox.AddToClassList(k_BoxModelClassName);
            m_BorderBox.AddToClassList(k_ContainerBorderClassName);
            m_BorderBox.AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);
            m_BorderBox.RegisterCallback<MouseOverEvent, BoxType>(OnMouseOver, BoxType.Border);
            m_BorderBox.Add(m_PaddingBox);

            m_MarginBox = new BoxModelElement<string, BoxModelStyleField>(BoxType.Margin, true, m_BorderBox,
                m_TopTextFieldMarginContainer, m_BottomTextFieldMarginContainer,
                m_LeftTextFieldMarginContainer, m_RightTextFieldMarginContainer);
            
            // checkerboard background for margin
            var checkerboardBackground = new CheckerboardBackground(); 
            checkerboardBackground.AddToClassList(k_CheckerboardClassName);
            m_MarginBox.AddBackground(checkerboardBackground);

            m_MarginBox.tooltip = BoxType.Margin.ToString();
            m_MarginBox.AddToClassList(k_BoxModelClassName);
            m_MarginBox.AddToClassList(k_ContainerMarginClassName);
            m_MarginBox.AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);
            m_MarginBox.RegisterCallback<MouseOverEvent, BoxType>(OnMouseOver, BoxType.Margin);

            m_Layer1.Add(m_MarginBox);

            m_Container.Add(m_Layer1);
            m_Container.Add(m_Layer2);
            Add(m_Container);

            RegisterCallback<MouseOutEvent>(OnMouseOut);
            RegisterCallback<GeometryChangedEvent>(OnFirstDisplay);
            
            // overlay events
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }

        private void StyleContainers()
        {
            foreach (var container in new[]
                     {
                         m_TopTextFieldMarginContainer, m_TopTextFieldPaddingContainer,
                         m_BottomTextFieldMarginContainer, m_BottomTextFieldPaddingContainer,
                         m_LeftTextFieldMarginContainer, m_LeftTextFieldPaddingContainer,
                         m_RightTextFieldMarginContainer, m_RightTextFieldPaddingContainer
                     })
            {
                container.pickingMode = PickingMode.Ignore;
                container.AddToClassList(k_TextfieldSpacerClassName);
                container.AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);
            }
        }
        
        private void OnMouseOver(MouseOverEvent evt, BoxType boxType)
        {
            m_MarginBox.EnableInClassList(k_MouseOverClassName, boxType == BoxType.Margin);
            m_BorderBox.EnableInClassList(k_MouseOverClassName, boxType == BoxType.Border);
            m_PaddingBox.EnableInClassList(k_MouseOverClassName, boxType == BoxType.Padding);
            m_ContentBox.EnableInClassList(k_MouseOverClassName, boxType == BoxType.Content);
            m_MarginBox.background.visible = boxType != BoxType.Margin;
            evt.StopPropagation();
        }
        
        private void OnMouseOut(MouseOutEvent evt)
        {
            m_MarginBox.background.visible = true;
            m_MarginBox.EnableInClassList(k_MouseOverClassName, false);
            m_BorderBox.EnableInClassList(k_MouseOverClassName, false);
            m_PaddingBox.EnableInClassList(k_MouseOverClassName, false);
            m_ContentBox.EnableInClassList(k_MouseOverClassName, false);
            evt.StopPropagation();
        }
        
        private void OnMouseEnter(MouseEnterEvent evt)
        {
            m_Inspector.highlightOverlayPainter.ClearOverlay();
            if (m_Inspector.selection.selection.Any())
                m_Inspector.highlightOverlayPainter.AddOverlay(m_Inspector.selection.selection.First());
        }
        
        private void OnMouseLeave(MouseLeaveEvent evt)
        {
            m_Inspector.highlightOverlayPainter.ClearOverlay();
        }
        
        void OnFirstDisplay(GeometryChangedEvent evt)
        {
            m_Inspector = GetFirstAncestorOfType<BuilderInspector>();
            UnregisterCallback<GeometryChangedEvent>(OnFirstDisplay);
        }
    }
}
