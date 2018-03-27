// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeClass(null)]
    [NativeHeader("Runtime/BaseClasses/TagManager.h")]
    internal sealed class TagManager : ProjectSettingsBase
    {
        private TagManager() {}

        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        internal static extern int GetDefinedLayerCount();

        internal static void GetDefinedLayers(ref string[] layerNames, ref int[] layerValues)
        {
            var definedLayerCount = GetDefinedLayerCount();

            if (layerNames == null)
                layerNames = new string[definedLayerCount];
            else if (layerNames.Length != definedLayerCount)
                Array.Resize(ref layerNames, definedLayerCount);

            if (layerValues == null)
                layerValues = new int[definedLayerCount];
            else if (layerValues.Length != definedLayerCount)
                Array.Resize(ref layerValues, definedLayerCount);

            Internal_GetDefinedLayers(layerNames, layerValues);
        }

        [FreeFunction("GetTagManager().GetDefinedLayers")]
        static extern void Internal_GetDefinedLayers([Out] string[] layerNames, [Out] int[] layerValues);
    }
}
