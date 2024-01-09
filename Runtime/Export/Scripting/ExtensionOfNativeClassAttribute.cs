// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using System.Reflection;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
    [RequiredByNativeCode]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    sealed class ExtensionOfNativeClassAttribute : Attribute
    {
    }
}
