// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.SceneTemplate
{
    internal class ZebraList : VisualElement
    {
        public const string kListView = "zebra-list-view";
        public const string kHeaderItem = "zebra-list-view-header-item";
        public const string kItemOdd = "zebra-list-view-item-odd";

        public ListView listView { get; private set; }
        public VisualElement header { get; private set; }
        public Func<VisualElement> headerCreator;

        Action<VisualElement, int> m_BindItem;

        public ZebraList(IList itemsSource, int itemHeight, Func<VisualElement> makeHeader, Func<VisualElement> makeItem, Action<VisualElement, int> bindItem)
        {
            m_BindItem = bindItem;
            listView = new ListView(itemsSource, itemHeight, makeItem, BindZebraItem);
            listView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            AddToClassList(kListView);
            listView.style.flexGrow = 1;
            headerCreator = makeHeader;

            if (headerCreator != null)
            {
                header = headerCreator();
                header.AddToClassList(kHeaderItem);
                header.style.height = itemHeight;
                Add(header);
            }

            Add(listView);
        }

        private void BindZebraItem(VisualElement element, int index)
        {
            m_BindItem(element, index);
        }
    }
}
