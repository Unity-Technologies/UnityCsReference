// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using Unity.ItemLibrary.Editor;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// <see cref="IItemLibraryAdapter"/> used in GraphToolsFoundation
    /// </summary>
    interface IGraphElementLibraryAdapter : IItemLibraryAdapter
    {
        /// <summary>
        /// Sets the graphview calling the searcher.
        /// </summary>
        /// <remarks>Used to instantiate an appropriate preview graphview.</remarks>
        void SetHostGraphView(GraphView graphView);
    }
}
