using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class PositionAnchors : VisualElement
    {
        static readonly string k_UssClassName = "unity-position-anchors";
        static readonly string k_UssDefaultClassName = "default";
        static readonly string k_UssPathNoExt = BuilderConstants.UtilitiesPath + "/StyleField/PositionSection";
        static readonly string k_UssAnchoredClassName = "anchored";
        static readonly string k_UssHoverClassName = "hover";
        static readonly string k_UssActiveClassName = "active";
        static readonly string k_OuterContainerName = "outer-container";
        static readonly string k_ContainerName = "container";
        static readonly string k_SquareHoverRectName = "square-hover-rect";
        static readonly string k_SquareName = "square";
        static readonly string k_SquareExpandTooltip = "Expand to all anchors";
        static readonly string k_SquareDetachTooltip = "Detach anchors";

        VisualElement m_OuterContainer;
        VisualElement m_Container;
        
        PositionAnchorPoint m_TopPoint;
        PositionAnchorPoint m_BottomPoint;
        PositionAnchorPoint m_LeftPoint;
        PositionAnchorPoint m_RightPoint;
        VisualElement m_Square;
        VisualElement m_SquareHoverRect;

        public PositionAnchorPoint topPoint => m_TopPoint;
        public PositionAnchorPoint bottomPoint => m_BottomPoint;
        public PositionAnchorPoint leftPoint => m_LeftPoint;
        public PositionAnchorPoint rightPoint => m_RightPoint;
        public VisualElement square => m_Square;

        [UsedImplicitly]
        protected new class UxmlFactory : UxmlFactory<PositionAnchors, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="PositionAnchors"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a PositionAnchors element that you can
        /// use in a UXML asset.
        /// </remarks>
        protected new class UxmlTraits : VisualElement.UxmlTraits
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                focusable.defaultValue = true;
            }
        }
        
        public PositionAnchors()
        {
            AddToClassList(k_UssClassName);
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPathNoExt + (EditorGUIUtility.isProSkin ? "Dark" : "Light") + ".uss"));
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPathNoExt + ".uss"));

            m_OuterContainer = new VisualElement() { name = k_OuterContainerName };
            
            m_Container = new VisualElement
            {
                name = k_ContainerName,
                pickingMode = PickingMode.Ignore
            };

            m_SquareHoverRect = new VisualElement
            {
                name = k_SquareHoverRectName,
                pickingMode = PickingMode.Ignore
            };
            
            m_Square = new VisualElement() { name = k_SquareName, tooltip = k_SquareExpandTooltip};
            RegisterSquareHoverInteraction();
            m_Square.AddManipulator(new Clickable(OnSquareClicked));
            m_SquareHoverRect.Add(m_Square);

            m_TopPoint = AddPoint(0.5f, 0, PositionProperty.Top);
            m_RightPoint = AddPoint(1, 0.5f, PositionProperty.Right);
            m_BottomPoint = AddPoint(0.5f, 1, PositionProperty.Bottom);
            m_LeftPoint = AddPoint(0, 0.5f, PositionProperty.Left);
            
            m_OuterContainer.Add(m_SquareHoverRect);
            m_OuterContainer.Add(m_Container);
            Add(m_OuterContainer);

            tabIndex = 0;
            focusable = true;
            
            RegisterCallback<ClickEvent>(e =>
            {
                pseudoStates |= (PseudoStates.Focus);
            });
        }

        void RegisterSquareHoverInteraction()
        {
            m_SquareHoverRect.RegisterCallback<MouseEnterEvent>(e => { 
                m_Container.SendToBack();
            });
            m_SquareHoverRect.RegisterCallback<MouseLeaveEvent>(e =>
            {
                m_SquareHoverRect.SendToBack(); 
            });
        }

        void OnSquareClicked(EventBase obj)
        {
            var allSelected = m_TopPoint.value && m_LeftPoint.value && m_RightPoint.value && m_BottomPoint.value;
            
            m_TopPoint.value = !allSelected;
            m_LeftPoint.value = !allSelected;
            m_RightPoint.value = !allSelected;
            m_BottomPoint.value = !allSelected;
        }
        
        void UpdateDefaultAnchors()
        {
            bool defaultTop = !m_BottomPoint.value && !m_TopPoint.value;
            bool defaultLeft = !m_RightPoint.value && !m_LeftPoint.value;

            m_TopPoint.EnableInClassList(k_UssDefaultClassName, defaultTop);
            m_LeftPoint.EnableInClassList(k_UssDefaultClassName, defaultLeft);
        }
        
        void UpdateAnchors(PositionProperty positionProperty, bool selected)
        {
            m_Container.EnableInClassList(k_UssAnchoredClassName + "-" + positionProperty.ToString().ToLower(), selected);
            m_SquareHoverRect.EnableInClassList(positionProperty.ToString().ToLower(), selected);
            
            // update square tooltip
            var allSelected = m_TopPoint.value && m_LeftPoint.value && m_RightPoint.value && m_BottomPoint.value;
            m_Square.tooltip = allSelected ? k_SquareDetachTooltip : k_SquareExpandTooltip;

            UpdateDefaultAnchors();
        }

        PositionAnchorPoint AddPoint(float x, float y, PositionProperty positionProperty)
        {
            var point = new PositionAnchorPoint(positionProperty)
            {
                x = x,
                y = y
            };
            
            point.pointSelected += UpdateAnchors;

            point.pointHovered += (clicked, enable) =>
            {
                var className = clicked ? k_UssActiveClassName : k_UssHoverClassName;
                m_Container.EnableInClassList(className + "-" + positionProperty.ToString().ToLower(), enable);
            };

            m_OuterContainer.Add(point);
            return point;
        }
    }
}
