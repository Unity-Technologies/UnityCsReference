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
        /// Material used to display SDF glyphs in the Character and Glyph tables.
        /// </summary>
        internal static Material internalSDFMaterial
        {
            get
            {
                if (s_InternalSDFMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/TextCore/Distance Field SSD");

                    if (shader != null)
                        s_InternalSDFMaterial = new Material(shader);
                }

                return s_InternalSDFMaterial;
            }
        }
        static Material s_InternalSDFMaterial;

        /// <summary>
        /// Material used to display Bitmap glyphs in the Character and Glyph tables.
        /// </summary>
        internal static Material internalBitmapMaterial
        {
            get
            {
                if (s_InternalBitmapMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/Internal-GUITextureClipText");

                    if (shader != null)
                        s_InternalBitmapMaterial = new Material(shader);
                }

                return s_InternalBitmapMaterial;
            }
        }
        static Material s_InternalBitmapMaterial;

        /// <summary>
        /// Material used to display color glyphs in the Character and Glyph tables.
        /// </summary>
        internal static Material internalColorBitmapMaterial
        {
            get
            {
                if (s_InternalColorBitmapMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/Internal-GUITextureClip");

                    if (shader != null)
                        s_InternalColorBitmapMaterial = new Material(shader);
                }

                return s_InternalColorBitmapMaterial;
            }
        }
        static Material s_InternalColorBitmapMaterial;

        /// <summary>
        /// Copy Shader properties from source to destination material.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        internal static void CopyMaterialProperties(Material source, Material destination)
        {
            destination.CopyMatchingPropertiesFromMaterial(source);
        }
    }
}
