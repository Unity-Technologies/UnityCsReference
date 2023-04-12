// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.UI.Builder
{
    class BorderBoxModelView : VisualElement
    {
        static readonly string k_UssPathNoExt = BuilderConstants.UtilitiesPath + "/StyleField/StyleSectionsBoxModel";
        static readonly string k_BoxModelClassName = "unity-box-model";
        static readonly string k_ViewClassName = k_BoxModelClassName + "__view";
        static readonly string k_ContainerClassName = k_BoxModelClassName + "__view__container";
        static readonly string k_ContainerColorClassName = k_BoxModelClassName + "__container__color";
        static readonly string k_ContainerBorderWidthClassName = k_BoxModelClassName + "__container__border-width";
        static readonly string k_ContainerBorderRadiusClassName = k_BoxModelClassName + "__container__border-radius";
        static readonly string k_ContainerContentClassName = k_BoxModelClassName + "__container__content";
        static readonly string k_TextfieldClassName = k_BoxModelClassName + "__textfield";
        static readonly string k_TextfieldContentClassName = k_TextfieldClassName + "__content-center";
        static readonly string k_TextfieldContentTopClassName = k_TextfieldClassName + "__content-top";
        static readonly string k_TextfieldContentBottomClassName = k_TextfieldClassName + "__content-bottom";
        static readonly string k_TextfieldSpacerClassName = k_TextfieldClassName + "__spacer";
        static readonly string k_TextfieldCenterSpacerClassName = k_TextfieldClassName + "__center-spacer";
        static readonly string k_CheckerboardClassName = k_BoxModelClassName + "__checkerboard-background";
        static readonly string k_BorderElementClassName = k_BoxModelClassName + "__border-element";
        static readonly string k_BorderElementVerticalClassName = k_BorderElementClassName + "__vertical";
        static readonly string k_BorderElementHorizontalClassName = k_BorderElementClassName + "__horizontal";
        static readonly string k_BordeLayer2ClassName = k_BoxModelClassName + "__border-layer-2";

        private VisualElement m_Container;
        private VisualElement m_Layer1;
        private VisualElement m_Layer2;
        private BoxModelElement<Color, ColorField> m_ColorBox;
        private BoxModelElement<string, BoxModelStyleField> m_BorderWidthBox;
        private BoxModelElement<string, BoxModelStyleField> m_BorderRadiusBox;
        private VisualElement m_ContentBox;
        
        // color field containers
        private VisualElement m_TopColorFieldContainer = new VisualElement();
        private VisualElement m_BottomColorFieldContainer = new VisualElement();
        private VisualElement m_LeftColorFieldContainer = new VisualElement();
        private VisualElement m_RightColorFieldContainer = new VisualElement();

        // border fields (width and radius) containers
        private VisualElement m_TopBorderWidthFieldContainer = new VisualElement();
        private VisualElement m_BottomBorderWidthFieldContainer = new VisualElement();
        private VisualElement m_LeftBorderWidthFieldContainer = new VisualElement();
        private VisualElement m_RightBorderWidthFieldContainer = new VisualElement();

        private VisualElement m_TopBorderRadiusFieldContainer = new VisualElement();
        private VisualElement m_BottomBorderRadiusFieldContainer = new VisualElement();
        private VisualElement m_LeftBorderRadiusFieldContainer = new VisualElement();
        private VisualElement m_RightBorderRadiusFieldContainer = new VisualElement();

        private VisualElement m_ContentCenter = new VisualElement();
        private VisualElement m_ContentTop = new VisualElement();
        private VisualElement m_ContentBottom = new VisualElement();
        private VisualElement m_CenterSpacer = new VisualElement();
        
        [UsedImplicitly]
        public new class UxmlFactory : UxmlFactory<BorderBoxModelView, UxmlTraits> { }

        public BorderBoxModelView()
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
                name = "BoxModelViewLayer2", classList =
                {
                    BuilderConstants.InspectorCompositeStyleRowElementClassName,
                    k_BordeLayer2ClassName
                }
            };
            m_Layer2.pickingMode = PickingMode.Ignore;
            m_Layer2.StretchToParentSize();

            StyleContainers();

            m_ContentCenter.pickingMode = PickingMode.Ignore;
            m_ContentCenter.AddToClassList(k_TextfieldContentClassName);
            m_ContentCenter.AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);

            m_ContentTop.pickingMode = PickingMode.Ignore;
            m_ContentTop.AddToClassList(k_TextfieldContentTopClassName);
            m_ContentTop.AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);

            m_ContentBottom.pickingMode = PickingMode.Ignore;
            m_ContentBottom.AddToClassList(k_TextfieldContentBottomClassName);
            m_ContentBottom.AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);

            m_CenterSpacer.pickingMode = PickingMode.Ignore;
            m_CenterSpacer.AddToClassList(k_TextfieldCenterSpacerClassName);

            m_ContentCenter.Add(m_LeftColorFieldContainer);
            m_ContentCenter.Add(m_LeftBorderWidthFieldContainer);
            m_ContentCenter.Add(m_CenterSpacer);
            m_ContentCenter.Add(m_RightBorderWidthFieldContainer);
            m_ContentCenter.Add(m_RightColorFieldContainer);

            m_ContentTop.Add(m_TopBorderRadiusFieldContainer);
            m_ContentTop.Add(m_TopBorderWidthFieldContainer);
            m_ContentTop.Add(m_RightBorderRadiusFieldContainer);

            m_ContentBottom.Add(m_LeftBorderRadiusFieldContainer);
            m_ContentBottom.Add(m_BottomBorderWidthFieldContainer);
            m_ContentBottom.Add(m_BottomBorderRadiusFieldContainer);

            m_Layer2.Add(m_TopColorFieldContainer);
            m_Layer2.Add(m_ContentTop);
            m_Layer2.Add(m_ContentCenter);
            m_Layer2.Add(m_ContentBottom);
            m_Layer2.Add(m_BottomColorFieldContainer);

            m_ContentBox = new VisualElement()
            {
                classList =
                {
                    BuilderConstants.InspectorCompositeStyleRowElementClassName,
                    k_BoxModelClassName, k_ContainerContentClassName
                }
            };
            
            m_BorderWidthBox = new BoxModelElement<string, BoxModelStyleField>(BoxType.BorderWidth, true, m_ContentBox,
                m_TopBorderWidthFieldContainer, m_BottomBorderWidthFieldContainer,
                m_LeftBorderWidthFieldContainer, m_RightBorderWidthFieldContainer);
            m_BorderWidthBox.AddToClassList(k_BoxModelClassName);
            m_BorderWidthBox.AddToClassList(k_ContainerBorderWidthClassName);
            m_BorderWidthBox.AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);

            m_BorderRadiusBox = new BoxModelElement<string, BoxModelStyleField>(BoxType.BorderRadius, true, new VisualElement(),
                m_TopBorderRadiusFieldContainer, m_BottomBorderRadiusFieldContainer,
                m_LeftBorderRadiusFieldContainer, m_RightBorderRadiusFieldContainer);
            m_BorderRadiusBox.AddToClassList(k_BoxModelClassName);
            m_BorderRadiusBox.AddToClassList(k_ContainerBorderRadiusClassName);
            m_BorderRadiusBox.AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);
            m_BorderWidthBox.Add(m_BorderRadiusBox);
            
            // visual elements used for border style
            var borderElement1 = new VisualElement() { classList = { k_BorderElementClassName, k_BorderElementVerticalClassName } };
            var borderElement2 = new VisualElement() { classList = { k_BorderElementClassName, k_BorderElementHorizontalClassName } };
            m_BorderWidthBox.Add(borderElement1);
            m_BorderWidthBox.Add(borderElement2);

            m_ColorBox = new BoxModelElement<Color, ColorField>(BoxType.BorderColor, true, m_BorderWidthBox,
                m_TopColorFieldContainer, m_BottomColorFieldContainer,
                m_LeftColorFieldContainer, m_RightColorFieldContainer);
            
            // checkerboard background for color box
            var checkerboardBackground = new CheckerboardBackground(); 
            checkerboardBackground.AddToClassList(k_CheckerboardClassName);
            m_ColorBox.AddBackground(checkerboardBackground);

            m_ColorBox.AddToClassList(k_BoxModelClassName);
            m_ColorBox.AddToClassList(k_ContainerColorClassName);
            m_ColorBox.AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);

            m_Layer1.Add(m_ColorBox);

            m_Container.Add(m_Layer1);
            m_Container.Add(m_Layer2);
            Add(m_Container);
        }

        private void StyleContainers()
        {
            foreach (var container in new[]
                     {
                         m_TopColorFieldContainer, m_TopBorderWidthFieldContainer,
                         m_BottomColorFieldContainer, m_BottomBorderWidthFieldContainer,
                         m_LeftColorFieldContainer, m_LeftBorderWidthFieldContainer,
                         m_RightColorFieldContainer, m_RightBorderWidthFieldContainer,
                         m_TopBorderRadiusFieldContainer, m_BottomBorderRadiusFieldContainer,
                         m_LeftBorderRadiusFieldContainer, m_RightBorderRadiusFieldContainer
                     })
            {
                container.pickingMode = PickingMode.Ignore;
                container.AddToClassList(k_TextfieldSpacerClassName);
                container.AddToClassList(BuilderConstants.InspectorCompositeStyleRowElementClassName);
            }
        }
    }
}
