// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine;

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Class)]
    class AnimationWindowControllerAttribute : Attribute
    {
        public Type componentType { get; }

        public AnimationWindowControllerAttribute(System.Type type)
        {
            if (type == null)
                Debug.LogError("Failed to load AnimationWindowControl component type");
            componentType = type;
        }
    }

}
