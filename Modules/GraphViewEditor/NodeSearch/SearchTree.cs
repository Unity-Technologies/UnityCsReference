// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    [Serializable]
    public class SearchTreeEntry : IComparable<SearchTreeEntry>
    {
        public int level;
        public GUIContent content;

        public object userData;

        public SearchTreeEntry(GUIContent content)
        {
            this.content = content;
        }

        public string name
        {
            get { return content.text; }
        }

        public int CompareTo(SearchTreeEntry o)
        {
            return name.CompareTo(o.name);
        }
    }

    [Serializable]
    public class SearchTreeGroupEntry : SearchTreeEntry
    {
        internal int selectedIndex;
        internal Vector2 scroll;

        public SearchTreeGroupEntry(GUIContent content, int level = 0)
            : base(content)
        {
            this.content = content;
            this.level = level;
        }
    }
}
