// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.Experimental.U2D;
using UnityEditor.U2D;
using UnityAssetImporter = UnityEditor.AssetImporter;
using UnityTextureImporter = UnityEditor.TextureImporter;
using System.Collections.Generic;

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

    internal class TextureImporter : ITextureImporter, ISpriteEditorDataProvider
    {
        protected UnityAssetImporter m_AssetImporter;
        List<SpriteDataMultipleMode> m_SpritesMultiple;
        SpriteDataSingleMode m_SpriteSingle;
        SerializedObject m_TextureImporterSO;

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

        public void InitSpriteEditorDataProvider(SerializedObject so)
        {
            m_TextureImporterSO = so;
            var spriteSheetSO = m_TextureImporterSO.FindProperty("m_SpriteSheet.m_Sprites");
            m_SpritesMultiple = new List<SpriteDataMultipleMode>();
            m_SpriteSingle = new SpriteDataSingleMode();
            m_SpriteSingle.Load(m_TextureImporterSO);
            for (int i = 0; i < spriteSheetSO.arraySize; ++i)
            {
                var data = new SpriteDataMultipleMode();
                var sp = spriteSheetSO.GetArrayElementAtIndex(i);
                data.Load(sp);
                m_SpritesMultiple.Add(data);
            }
        }

        public int spriteDataCount
        {
            get
            {
                switch (spriteImportMode)
                {
                    case SpriteImportMode.Multiple:
                        return m_SpritesMultiple.Count;
                    case SpriteImportMode.Single:
                    case SpriteImportMode.Polygon:
                        return 1;
                }
                return 0;
            }

            set
            {
                if (spriteImportMode != SpriteImportMode.Multiple)
                {
                    Debug.LogError("SetSpriteDataSize can only be called when in SpriteImportMode.Multiple");
                    return;
                }

                while (m_SpritesMultiple.Count < value)
                    m_SpritesMultiple.Add(new SpriteDataMultipleMode());
                if (m_SpritesMultiple.Count > value)
                {
                    var diff = m_SpritesMultiple.Count - value;
                    m_SpritesMultiple.RemoveRange(m_SpritesMultiple.Count - diff, diff);
                }
            }
        }

        public UnityEngine.Object targetObject
        {
            get
            {
                return m_AssetImporter;
            }
        }

        public SpriteDataBase GetSpriteData(int i)
        {
            switch (spriteImportMode)
            {
                case SpriteImportMode.Multiple:
                    if (m_SpritesMultiple.Count > i)
                        return m_SpritesMultiple[i];
                    break;
                case SpriteImportMode.Single:
                case SpriteImportMode.Polygon:
                    return m_SpriteSingle;
            }
            return null;
        }

        public void Apply(SerializedObject so)
        {
            m_SpriteSingle.Apply(so);

            var spriteSheetSO = so.FindProperty("m_SpriteSheet.m_Sprites");
            for (int i = 0; i < m_SpritesMultiple.Count; ++i)
            {
                if (spriteSheetSO.arraySize < m_SpritesMultiple.Count)
                {
                    spriteSheetSO.InsertArrayElementAtIndex(spriteSheetSO.arraySize);
                }
                var sp = spriteSheetSO.GetArrayElementAtIndex(i);
                m_SpritesMultiple[i].Apply(sp);
            }
            while (m_SpritesMultiple.Count < spriteSheetSO.arraySize)
            {
                spriteSheetSO.DeleteArrayElementAtIndex(m_SpritesMultiple.Count);
            }
        }

        public void GetTextureActualWidthAndHeight(out int width, out int height)
        {
            width = height = 0;
            ((UnityTextureImporter)m_AssetImporter).GetWidthAndHeight(ref width, ref height);
        }
    }
}
