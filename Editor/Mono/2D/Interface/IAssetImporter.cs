// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using UnityAssetImporter = UnityEditor.AssetImporter;
using UnityTextureImporter = UnityEditor.TextureImporter;

namespace UnityEditor.U2D.Interface
{
    internal abstract class ITextureImporter
    {
        public abstract void GetWidthAndHeight(ref int width, ref int height);
        public abstract SpriteImportMode spriteImportMode { get; }
        public abstract Vector4 spriteBorder { get; }
        public abstract Vector2 spritePivot { get; }

        public abstract string assetPath { get; }

        public static bool operator==(ITextureImporter t1, ITextureImporter t2)
        {
            if (object.ReferenceEquals(t1, null))
            {
                return object.ReferenceEquals(t2, null) || t2 == null;
            }

            return t1.Equals(t2);
        }

        public static bool operator!=(ITextureImporter t1, ITextureImporter t2)
        {
            if (object.ReferenceEquals(t1, null))
            {
                return !object.ReferenceEquals(t2, null) && t2 != null;
            }

            return !t1.Equals(t2);
        }

        public override bool Equals(object other)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }

    internal class TextureImporter : ITextureImporter
    {
        protected UnityAssetImporter m_AssetImporter;

        public TextureImporter(UnityTextureImporter textureImporter)
        {
            m_AssetImporter = textureImporter;
        }

        public override string assetPath
        {
            get { return m_AssetImporter.assetPath; }
        }

        public override bool Equals(object other)
        {
            TextureImporter t = other as TextureImporter;
            if (object.ReferenceEquals(t, null))
                return m_AssetImporter == null;
            return m_AssetImporter == t.m_AssetImporter;
        }

        public override int GetHashCode()
        {
            return m_AssetImporter.GetHashCode();
        }

        public override void GetWidthAndHeight(ref int width, ref int height)
        {
            ((UnityTextureImporter)m_AssetImporter).GetWidthAndHeight(ref width, ref height);
        }

        public override SpriteImportMode spriteImportMode
        {
            get { return ((UnityTextureImporter)m_AssetImporter).spriteImportMode; }
        }

        public override Vector4 spriteBorder
        {
            get { return ((UnityTextureImporter)m_AssetImporter).spriteBorder; }
        }

        public override Vector2 spritePivot
        {
            get { return ((UnityTextureImporter)m_AssetImporter).spritePivot; }
        }
    }
}
