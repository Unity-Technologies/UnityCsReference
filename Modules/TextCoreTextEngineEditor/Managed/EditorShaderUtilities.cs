// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;


namespace UnityEditor.TextCore.Text
{
    internal static class EditorShaderUtilities
    {
        /// <summary>
        /// Copy Shader properties from source to destination material.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        internal static void CopyMaterialProperties(Material source, Material destination)
        {
            destination.CopyPropertiesFromMaterial(source);
        }
    }
}
