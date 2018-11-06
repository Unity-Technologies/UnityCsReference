// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngineInternal;
using UnityEngine;
using uei = UnityEngine.Internal;

namespace UnityEditor
{
    [NativeClass(null)]
    [RequiredByNativeCode]
    [NativeType(Header = "Runtime/Mono/MonoBehaviour.h")]
    public abstract class ScriptGeneratorAsset : ScriptableObject
    {
        // Returns the ScriptGeneratorAsset object containing specified MonoBehaviour
        public static ScriptGeneratorAsset FromMonoBehaviour(MonoBehaviour behaviour)
        {
            return ScriptGeneratorAsset.Internal_FromMonoBehaviour(behaviour) as ScriptGeneratorAsset;
        }

        public static void SetGeneratorAsset(MonoBehaviour behaviour, ScriptGeneratorAsset asset)
        {
            ScriptGeneratorAsset.Internal_SetGeneratorAsset(behaviour, asset);
        }

        public abstract string label { get; }

        [RequiredByNativeCode]
        private string GetLabel()
        {
            return label;
        }

        public abstract MonoScript defaultScript { get; }

        [RequiredByNativeCode]
        private MonoScript GetDefaultScript()
        {
            return defaultScript;
        }

        // Returns the ScriptGeneratorAsset object used by the given scripted object
        [FreeFunction("MonoBehaviour::Internal_FromMonoBehaviour")]
        internal static extern UnityEngine.Object Internal_FromMonoBehaviour(MonoBehaviour behaviour);

        // Sets the ScriptGenerator asset in the MonoBehaviour
        [FreeFunction("MonoBehaviour::Internal_SetGeneratorAsset")]
        internal static extern void Internal_SetGeneratorAsset([NotNull] MonoBehaviour behaviour, UnityEngine.Object asset);
    }
}
