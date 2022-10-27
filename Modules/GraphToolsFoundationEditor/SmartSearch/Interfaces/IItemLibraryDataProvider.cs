// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ItemLibrary.Editor;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Interface to provide custom data in a <see cref="ItemLibraryItem"/>
    /// </summary>
    interface IItemLibraryDataProvider
    {
        IItemLibraryData Data { get; }
    }
}
