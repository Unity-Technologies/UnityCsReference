// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.Search
{
    interface IBlockSource
    {
        string name { get; }
        SearchContext context { get; }
        bool formatNames { get; }

        void Apply(in SearchProposition searchProposition);
        IEnumerable<SearchProposition> FetchPropositions();

        void CloseEditor();
    }

    interface IBlockEditor
    {
        EditorWindow window { get; }
    }
}
