// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Used to build UI from model. See <see cref="GraphViewFactoryExtensions"/>.
    /// </summary>
    class ElementBuilder
    {
        public RootView View { get; set; }
        public IViewContext Context { get; set; }
    }
}
