// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace UnityEditor
{
public sealed partial class DefaultAsset : UObject
{
    internal extern  string message
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern  bool isWarning
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}


    [CustomEditor(typeof(DefaultAsset), isFallback = true)] 
    class DefaultAssetInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            var defaultAsset = (DefaultAsset)target;
            if (defaultAsset.message.Length > 0)
            {
                EditorGUILayout.HelpBox(
                    defaultAsset.message,
                    defaultAsset.isWarning ? MessageType.Warning : MessageType.Info);
            }
        }

    }
}
