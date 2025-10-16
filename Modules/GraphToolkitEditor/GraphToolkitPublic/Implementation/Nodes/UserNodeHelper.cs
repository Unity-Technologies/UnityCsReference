// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor.Implementation
{
    static class UserNodeHelper
    {
        public static Type GetNodeImpType(Type nodeType)
        {
            if (typeof(ContextNode).IsAssignableFrom(nodeType))
            {
                return typeof(UserContextNodeModelImp);
            }
            if (typeof(BlockNode).IsAssignableFrom(nodeType))
            {
                return typeof(UserBlockNodeModelImp);
            }
            return typeof(UserNodeModelImp);
        }
    }
}
