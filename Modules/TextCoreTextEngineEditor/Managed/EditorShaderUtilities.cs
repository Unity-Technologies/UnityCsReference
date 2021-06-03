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
            string[] sourcePropNames = MaterialEditor.GetMaterialPropertyNames(new Object[] { source });

            for (int i = 0; i < sourcePropNames.Length; i++)
            {
                int property_ID = Shader.PropertyToID(sourcePropNames[i]);
                if (destination.HasProperty(property_ID))
                {
                    //Debug.Log(source_prop[i].name + "  Type:" + ShaderUtil.GetPropertyType(source.shader, i));
                    switch (ShaderUtil.GetPropertyType(source.shader, i))
                    {
                        case ShaderUtil.ShaderPropertyType.Color:
                            destination.SetColor(property_ID, source.GetColor(property_ID));
                            break;
                        case ShaderUtil.ShaderPropertyType.Float:
                            destination.SetFloat(property_ID, source.GetFloat(property_ID));
                            break;
                        case ShaderUtil.ShaderPropertyType.Range:
                            destination.SetFloat(property_ID, source.GetFloat(property_ID));
                            break;
                        case ShaderUtil.ShaderPropertyType.TexEnv:
                            destination.SetTexture(property_ID, source.GetTexture(property_ID));
                            break;
                        case ShaderUtil.ShaderPropertyType.Vector:
                            destination.SetVector(property_ID, source.GetVector(property_ID));
                            break;
                    }
                }
            }
        }
    }
}
