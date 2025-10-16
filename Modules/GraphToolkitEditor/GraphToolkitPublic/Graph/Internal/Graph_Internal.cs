// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor.Implementation;

namespace Unity.GraphToolkit.Editor
{
    public partial class Graph
    {
        internal GraphModelImp m_Implementation;

        internal void SetImplementation(GraphModelImp implementation)
        {
            m_Implementation = implementation;
        }

        internal void CheckImplementation()
        {
            if (m_Implementation == null)
            {
                throw new InvalidOperationException("Only Graph instances returned by either GraphDatabase.LoadGraph or GraphDatabase.CreateGraph are valid.");
            }
        }
    }
}
