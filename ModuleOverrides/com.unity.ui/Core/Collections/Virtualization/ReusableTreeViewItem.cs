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

        Pool.ObjectPool<VisualElement> m_IndentPool = new Pool.ObjectPool<VisualElement>(
            () =>
            {
                var indentElement = new VisualElement();
                indentElement.AddToClassList(Experimental.TreeView.itemIndentUssClassName);
                return indentElement;
            });

        protected EventCallback<PointerUpEvent> m_PointerUpCallback;
        protected EventCallback<ChangeEvent<bool>> m_ToggleValueChangedCallback;

        public ReusableTreeViewItem()
            : base()
        {
            m_PointerUpCallback = OnPointerUp;
            m_ToggleValueChangedCallback = OnToggleValueChanged;
        }

        public override void Init(VisualElement item)
        {
            base.Init(item);

            m_Container = new VisualElement()
            {
                name = Experimental.TreeView.itemUssClassName,
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };
            m_Container.AddToClassList(Experimental.TreeView.itemUssClassName);

            m_IndentContainer = new VisualElement()
            {
                name = Experimental.TreeView.itemIndentsContainerUssClassName,
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };
            m_IndentContainer.AddToClassList(Experimental.TreeView.itemIndentsContainerUssClassName);
            m_Container.hierarchy.Add(m_IndentContainer);

            m_Toggle = new Toggle { name = Experimental.TreeView.itemToggleUssClassName };
            m_Toggle.userData = this;
            m_Toggle.AddToClassList(Foldout.toggleUssClassName);
            m_Toggle.visualInput.AddToClassList(Foldout.inputUssClassName);
            m_Toggle.visualInput.Q(className: Toggle.checkmarkUssClassName).AddToClassList(Foldout.checkmarkUssClassName);
            m_Container.hierarchy.Add(m_Toggle);

            m_BindableContainer = new VisualElement()
            {
                name = Experimental.TreeView.itemContentContainerUssClassName,
                style =
                {
                    flexGrow = 1
                }
            };
            m_BindableContainer.AddToClassList(Experimental.TreeView.itemContentContainerUssClassName);
            m_Container.Add(m_BindableContainer);
            m_BindableContainer.Add(item);
        }

        public override void PreAttachElement()
        {
            base.PreAttachElement();
            rootElement.AddToClassList(Experimental.TreeView.itemUssClassName);
            m_Container.RegisterCallback(m_PointerUpCallback);
            m_Toggle.RegisterValueChangedCallback(m_ToggleValueChangedCallback);
        }

        public override void DetachElement()
        {
            base.DetachElement();
            rootElement.RemoveFromClassList(Experimental.TreeView.itemUssClassName);
            m_Container.UnregisterCallback(m_PointerUpCallback);
            m_Toggle.UnregisterValueChangedCallback(m_ToggleValueChangedCallback);
        }

        public void Indent(int depth)
        {
            for (var i = 0; i < m_IndentContainer.childCount; i++)
            {
                m_IndentPool.Release(m_IndentContainer[i]);
            }

            m_IndentContainer.Clear();

            for (var i = 0; i < depth; ++i)
            {
                var indentElement = m_IndentPool.Get();
                m_IndentContainer.Add(indentElement);
            }
        }

        public void SetExpandedWithoutNotify(bool expanded)
        {
            m_Toggle.SetValueWithoutNotify(expanded);
        }

        public void SetToggleVisibility(bool visible)
        {
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
