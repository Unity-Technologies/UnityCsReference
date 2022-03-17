// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Search
{
    interface IQuerySource
    {
        ISearchView searchView { get; }
        SearchContext context { get; }
        internal QueryBlock AddBlock(string text);
        internal QueryBlock AddBlock(QueryBlock block);
        internal QueryBlock AddProposition(in SearchProposition searchProposition);
        internal void RemoveBlock(in QueryBlock block);
        internal void BlockActivated(in QueryBlock block);
        void Apply();
        void Repaint();
    }
}
