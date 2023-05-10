// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    class ContextualizedModelViews_Internal
    {
        public readonly RootView View;
        public readonly IViewContext Context;
        public readonly Dictionary<Hash128, ChildView> ModelViews;

        public ContextualizedModelViews_Internal(RootView view, IViewContext context)
        {
            View = view;
            Context = context;
            ModelViews = new Dictionary<Hash128, ChildView>();
        }
    }
}
