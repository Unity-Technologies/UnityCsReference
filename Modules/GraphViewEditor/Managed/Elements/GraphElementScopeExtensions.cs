// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Experimental.GraphView
{
    public static class GraphElementScopeExtensions
    {
        static readonly PropertyName containingScopePropertyKey = "containingScope";

        public static Scope GetContainingScope(this GraphElement element)
        {
            if (element == null)
                return null;

            return element.GetProperty(containingScopePropertyKey) as Scope;
        }

        internal static void SetContainingScope(this GraphElement element, Scope scope)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.SetProperty(containingScopePropertyKey, scope);
        }
    }
}
