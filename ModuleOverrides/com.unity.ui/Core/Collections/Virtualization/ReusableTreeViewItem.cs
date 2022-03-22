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
        VisualElement m_IndentContainer;
        VisualElement m_BindableContainer;

        public override VisualElement rootElement => m_Container ?? bindableElement;
        public event Action<PointerUpEvent> onPointerUp;
        public event Action<ChangeEvent<bool>> onToggleValueChanged;

        static Pool.ObjectPool<VisualElement> s_IndentPool = new Pool.ObjectPool<VisualElement>(
            () =>
            {
                var indentElement = new VisualElement();
                indentElement.AddToClassList(BaseTreeView.itemIndentUssClassName);
                return indentElement;
            });

        EventCallback<PointerUpEvent> m_PointerUpCallback;
        EventCallback<ChangeEvent<bool>> m_ToggleValueChangedCallback;

        public ReusableTreeViewItem()
        {
            m_PointerUpCallback = OnPointerUp;
            m_ToggleValueChangedCallback = OnToggleValueChanged;
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

            m_IndentContainer = new VisualElement()
            {
                name = BaseTreeView.itemIndentsContainerUssClassName,
                style = { flexDirection = FlexDirection.Row },
            };
            m_IndentContainer.AddToClassList(BaseTreeView.itemIndentsContainerUssClassName);
            m_Container.hierarchy.Add(m_IndentContainer);

            m_Toggle = new Toggle { name = BaseTreeView.itemToggleUssClassName };
            m_Toggle.userData = this;
            m_Toggle.AddToClassList(Foldout.toggleUssClassName);
            m_Toggle.visualInput.AddToClassList(Foldout.inputUssClassName);
            m_Toggle.visualInput.Q(className: Toggle.checkmarkUssClassName).AddToClassList(Foldout.checkmarkUssClassName);
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
            m_Toggle?.RegisterValueChangedCallback(m_ToggleValueChangedCallback);
        }

        public override void DetachElement()
        {
            base.DetachElement();
            rootElement.RemoveFromClassList(BaseTreeView.itemUssClassName);
            m_Container?.UnregisterCallback(m_PointerUpCallback);
            m_Toggle?.UnregisterValueChangedCallback(m_ToggleValueChangedCallback);
        }

        public void Indent(int depth)
        {
            if (m_IndentContainer == null)
                return;

            for (var i = 0; i < m_IndentContainer.childCount; i++)
            {
                s_IndentPool.Release(m_IndentContainer[i]);
            }

            m_IndentContainer.Clear();

            for (var i = 0; i < depth; ++i)
            {
                var indentElement = s_IndentPool.Get();
                m_IndentContainer.Add(indentElement);
            }
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
