// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using UnityEditor.Experimental.U2D;
using UnityEditor.U2D;
using UnityAssetImporter = UnityEditor.AssetImporter;
using UnityTextureImporter = UnityEditor.TextureImporter;
using System.Collections.Generic;

namespace UnityEditor.U2D.Interface
{
    internal abstract class ITextureImporter
    {
        public abstract SpriteImportMode spriteImportMode { get; }

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
        List<SpriteDataExt> m_SpritesMultiple;
        SpriteDataExt m_SpriteSingle;
        SpriteImportMode m_SpriteImportMode;

        public TextureImporter(UnityTextureImporter textureImporter)
        {
            m_AssetImporter = textureImporter;
            m_SpriteImportMode = textureImporter.textureType != TextureImporterType.Sprite ?
                SpriteImportMode.None :
                textureImporter.spriteImportMode;
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

        public override SpriteImportMode spriteImportMode
        {
            get { return m_SpriteImportMode; }
        }

        // ISpriteEditorDataProvider interface
        public float pixelsPerUnit
        {
            get { return ((UnityTextureImporter)m_AssetImporter).spritePixelsPerUnit; }
        }

        public UnityEngine.Object targetObject
        {
            get { return m_AssetImporter; }
        }

        public SpriteRect[] GetSpriteRects()
        {
            return spriteImportMode == SpriteImportMode.Multiple ? m_SpritesMultiple.Select(x => x as SpriteRect).ToArray() : new[] {m_SpriteSingle};
        }

        public void SetSpriteRects(SpriteRect[] spriteRects)
        {
            if (spriteImportMode == SpriteImportMode.Single && spriteRects.Length == 1)
            {
                m_SpriteSingle.CopyFromSpriteRect(spriteRects[0]);
            }
            else if (spriteImportMode == SpriteImportMode.Multiple)
            {
                for (int i = m_SpritesMultiple.Count - 1; i >= 0; --i)
                {
                    if (!spriteRects.Contains(m_SpritesMultiple[i]))
                        m_SpritesMultiple.RemoveAt(i);
                }
                for (int i = 0; i < spriteRects.Length; i++)
                {
                    var spriteRect = spriteRects[i];
                    var index = m_SpritesMultiple.FindIndex(x => x.spriteID == spriteRect.spriteID);
                    if (-1 == index)
                        m_SpritesMultiple.Add(new SpriteDataExt(spriteRect));
                    else
                        m_SpritesMultiple[index].CopyFromSpriteRect(spriteRects[i]);
                }
            }
        }

        public SpriteRect GetSpriteData(GUID guid)
        {
            return spriteImportMode == SpriteImportMode.Multiple ? m_SpritesMultiple.Where(x => x.spriteID == guid).FirstOrDefault() : m_SpriteSingle;
        }

        public int GetSpriteDataIndex(GUID guid)
        {
            switch (spriteImportMode)
            {
                case SpriteImportMode.Single:
                case SpriteImportMode.Polygon:
                    return 0;
                case SpriteImportMode.Multiple:
                {
                    return m_SpritesMultiple.FindIndex(x => x.spriteID == guid);
                }
                default:
                    throw new InvalidOperationException("GUID not found");
            }
        }

        public void Apply()
        {
            var so = new SerializedObject(m_AssetImporter);
            m_SpriteSingle.Apply(so);
            var spriteSheetSO = so.FindProperty("m_SpriteSheet.m_Sprites");
            GUID[] guids = new GUID[spriteSheetSO.arraySize];
            for (int i = 0; i < spriteSheetSO.arraySize; ++i)
            {
                var element = spriteSheetSO.GetArrayElementAtIndex(i);
                guids[i] = SpriteRect.GetSpriteIDFromSerializedProperty(element);
                // find the GUID in our sprite list and apply to it;
                var smd = m_SpritesMultiple.Find(x => x.spriteID == guids[i]);
                if (smd == null) // we can't find it, it is already deleted
                {
                    spriteSheetSO.DeleteArrayElementAtIndex(i);
                    --i;
                }
                else
                    smd.Apply(element);
            }

            // Add new ones
            var newSprites = m_SpritesMultiple.Where(x => !guids.Contains(x.spriteID));
            foreach (var newSprite in newSprites)
            {
                spriteSheetSO.InsertArrayElementAtIndex(spriteSheetSO.arraySize);
                var element = spriteSheetSO.GetArrayElementAtIndex(spriteSheetSO.arraySize - 1);
                newSprite.Apply(element);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public void InitSpriteEditorDataProvider()
        {
            var so = new SerializedObject(m_AssetImporter);
            var spriteSheetSO = so.FindProperty("m_SpriteSheet.m_Sprites");
            m_SpritesMultiple = new List<SpriteDataExt>();
            m_SpriteSingle = new SpriteDataExt();
            m_SpriteSingle.Load(so);

            for (int i = 0; i < spriteSheetSO.arraySize; ++i)
            {
                var data = new SpriteDataExt();
                var sp = spriteSheetSO.GetArrayElementAtIndex(i);
                data.Load(sp);
                m_SpritesMultiple.Add(data);
            }
        }

        public T GetDataProvider<T>() where T : class
        {
            if (typeof(T) == typeof(ISpriteBoneDataProvider))
            {
                return new SpriteBoneDataTransfer(this) as T;
            }
            if (typeof(T) == typeof(ISpriteMeshDataProvider))
            {
                return new SpriteMeshDataTransfer(this) as T;
            }
            if (typeof(T) == typeof(ISpriteOutlineDataProvider))
            {
                return new SpriteOutlineDataTransfer(this) as T;
            }
            if (typeof(T) == typeof(ISpritePhysicsOutlineDataProvider))
            {
                return new SpritePhysicsOutlineDataTransfer(this) as T;
            }
            if (typeof(T) == typeof(ITextureDataProvider))
            {
                return new SpriteTextureDataTransfer(this) as T;
            }
            else
                return this as T;
        }

        public bool HasDataProvider(Type type)
        {
            if (type == typeof(ISpriteBoneDataProvider) ||
                type == typeof(ISpriteMeshDataProvider) ||
                type == typeof(ISpriteOutlineDataProvider) ||
                type == typeof(ISpritePhysicsOutlineDataProvider) ||
                type == typeof(ITextureDataProvider))
            {
                return true;
            }
            else
                return type.IsAssignableFrom(GetType());
        }
    }
}
