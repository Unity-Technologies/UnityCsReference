// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class QueryTextFieldBlock : QueryBlock
    {
        private ISearchField m_SearchField;
        internal override bool wantsEvents => true;

        internal new static readonly string ussClassName = "search-query-textfield-block";

        public QueryTextFieldBlock(IQuerySource source, ISearchField searchField)
            : base(source)
        {
            hideMenu = true;
            value = string.Empty;
            m_SearchField = searchField;
        }

        public override string ToString() => value;
        internal override IBlockEditor OpenEditor(in Rect rect) => null;

        internal ISearchField GetSearchField()
        {
            return m_SearchField;
        }

        internal override void CreateBlockElement(VisualElement container)
        {
            var textElement = m_SearchField.GetTextElement();
            if (textElement == null)
                return;

            // Calling RegisterCallback for the same callback on the same phase has no effect,
            // so no need to unregister if already registered.
            textElement.RegisterCallback<PointerDownEvent>(OnPointerDownEvent);

            container.AddToClassList(ussClassName);
            container.Add(textElement);
        }

        void OnPointerDownEvent(PointerDownEvent evt)
        {
            source.BlockActivated(this);
        }

        internal override Color GetBackgroundColor()
        {
            return Color.clear;
        }
    }
}
