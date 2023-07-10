// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    class ReusableTreeViewItem : ReusableCollectionItem
    {
        Toggle m_Toggle;
        VisualElement m_Container;
        VisualElement m_IndentElement;
        VisualElement m_BindableContainer;
        VisualElement m_Checkmark;

        public override VisualElement rootElement => m_Container ?? bindableElement;
        public event Action<PointerUpEvent> onPointerUp;
        public event Action<ChangeEvent<bool>> onToggleValueChanged;

        int m_Depth;
        float m_IndentWidth;

        // Internal for tests.
        internal float indentWidth => m_IndentWidth;

        EventCallback<PointerUpEvent> m_PointerUpCallback;
        EventCallback<ChangeEvent<bool>> m_ToggleValueChangedCallback;
        EventCallback<GeometryChangedEvent> m_ToggleGeometryChangedCallback;

        public ReusableTreeViewItem()
        {
            m_PointerUpCallback = OnPointerUp;
            m_ToggleValueChangedCallback = OnToggleValueChanged;
            m_ToggleGeometryChangedCallback = OnToggleGeometryChanged;
        }

        public override void Init(VisualElement item)
        {
            base.Init(item);

            var container = new VisualElement() { name = BaseTreeView.itemUssClassName };
            container.AddToClassList(BaseTreeView.itemUssClassName);

            InitExpandHierarchy(container, item);
        }

        protected void InitExpandHierarchy(VisualElement root, VisualElement item)
        {
            m_Container = root;
            m_Container.style.flexDirection = FlexDirection.Row;

            m_IndentElement = new VisualElement()
            {
                name = BaseTreeView.itemIndentUssClassName,
                style = { flexDirection = FlexDirection.Row },
            };
            m_Container.hierarchy.Add(m_IndentElement);

            m_Toggle = new Toggle
            {
                name = BaseTreeView.itemToggleUssClassName,
                userData = this
            };
            m_Toggle.AddToClassList(Foldout.toggleUssClassName);
            m_Toggle.AddToClassList(BaseTreeView.itemToggleUssClassName);
            m_Toggle.visualInput.AddToClassList(Foldout.inputUssClassName);
            m_Checkmark = m_Toggle.visualInput.Q(className: Toggle.checkmarkUssClassName);
            m_Checkmark.AddToClassList(Foldout.checkmarkUssClassName);
            m_Container.hierarchy.Add(m_Toggle);

            m_BindableContainer = new VisualElement()
            {
                name = BaseTreeView.itemContentContainerUssClassName,
                style = { flexGrow = 1 },
            };

            m_BindableContainer.AddToClassList(BaseTreeView.itemContentContainerUssClassName);
            m_Container.Add(m_BindableContainer);
            m_BindableContainer.Add(item);
        }

        public override void PreAttachElement()
        {
            base.PreAttachElement();
            rootElement.AddToClassList(BaseTreeView.itemUssClassName);
            m_Container?.RegisterCallback(m_PointerUpCallback);
            m_Toggle?.visualInput.Q(className: Toggle.checkmarkUssClassName).RegisterCallback(m_ToggleGeometryChangedCallback);
            m_Toggle?.RegisterValueChangedCallback(m_ToggleValueChangedCallback);
        }

        public override void DetachElement()
        {
            base.DetachElement();
            rootElement.RemoveFromClassList(BaseTreeView.itemUssClassName);
            m_Container?.UnregisterCallback(m_PointerUpCallback);
            m_Toggle?.visualInput.Q(className: Toggle.checkmarkUssClassName).UnregisterCallback(m_ToggleGeometryChangedCallback);
            m_Toggle?.UnregisterValueChangedCallback(m_ToggleValueChangedCallback);
        }

        public void Indent(int depth)
        {
            if (m_IndentElement == null)
                return;

            m_Depth = depth;
            UpdateIndentLayout();
        }

        public void SetExpandedWithoutNotify(bool expanded)
        {
            m_Toggle?.SetValueWithoutNotify(expanded);
        }

        public void SetToggleVisibility(bool visible)
        {
            if (m_Toggle != null)
                m_Toggle.visible = visible;
        }

        void OnToggleGeometryChanged(GeometryChangedEvent evt)
        {
            var width = m_Checkmark.resolvedStyle.width + m_Checkmark.resolvedStyle.marginLeft + m_Checkmark.resolvedStyle.marginRight;
            if (Math.Abs(width - m_IndentWidth) < float.Epsilon)
                return;

            m_IndentWidth = width;
            UpdateIndentLayout();
        }

        void UpdateIndentLayout()
        {
            m_IndentElement.style.width = m_IndentWidth * m_Depth;
            m_IndentElement.EnableInClassList(BaseTreeView.itemIndentUssClassName, m_Depth > 0);
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            onPointerUp?.Invoke(evt);
        }

        void OnToggleValueChanged(ChangeEvent<bool> evt)
        {
            onToggleValueChanged?.Invoke(evt);
        }
    }
}
