// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Search
{
    interface IQuerySource
    {
        ISearchView searchView { get; }
        SearchContext context { get; }
        QueryBlock AddBlock(string text);
        QueryBlock AddBlock(QueryBlock block);
        QueryBlock AddProposition(in SearchProposition searchProposition);
        void RemoveBlock(in QueryBlock block);
        void BlockActivated(in QueryBlock block);
        void Apply();
        void Repaint();
    }
}
