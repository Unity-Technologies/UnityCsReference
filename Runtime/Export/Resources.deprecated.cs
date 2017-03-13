// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngineInternal;

namespace UnityEngine
{
    partial class Resources
    {
        // Returns a resource at an asset path (Editor Only).
        [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
        [Obsolete("Use AssetDatabase.LoadAssetAtPath instead (UnityUpgradable) -> * [UnityEditor] UnityEditor.AssetDatabase.LoadAssetAtPath(*)", true)]
        public static Object LoadAssetAtPath(string assetPath, Type type) { return null; }

        [Obsolete("Use AssetDatabase.LoadAssetAtPath<T>() instead (UnityUpgradable) -> * [UnityEditor] UnityEditor.AssetDatabase.LoadAssetAtPath<T>(*)", true)]
        public static T LoadAssetAtPath<T>(string assetPath) where T : Object { return null; }
    }
}

