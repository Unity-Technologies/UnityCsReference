// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.Experimental.U2D;
using UnityEditor.U2D;
using System.Collections.Generic;

namespace UnityEditor
{
    public sealed partial class TextureImporter : AssetImporter, ISpriteEditorDataProvider
    {
        List<SpriteDataExt> m_SpritesMultiple;
        SpriteDataExt m_SpriteSingle;

        float ISpriteEditorDataProvider.pixelsPerUnit
        {
            get { return spritePixelsPerUnit; }
        }

        UnityEngine.Object ISpriteEditorDataProvider.targetObject
        {
            get { return this; }
        }

        SpriteRect[] ISpriteEditorDataProvider.GetSpriteRects()
        {
            return spriteImportMode == SpriteImportMode.Multiple ? m_SpritesMultiple.Select(x => new SpriteDataExt(x) as SpriteRect).ToArray() : new[] { new SpriteDataExt(m_SpriteSingle) };
        }

        void ISpriteEditorDataProvider.SetSpriteRects(SpriteRect[] spriteRects)
        {
            if (spriteImportMode != SpriteImportMode.Multiple && spriteImportMode != SpriteImportMode.None && spriteRects.Length == 1)
            {
                m_SpriteSingle.CopyFromSpriteRect(spriteRects[0]);
            }
            else if (spriteImportMode == SpriteImportMode.Multiple)
            {
                for (int i = m_SpritesMultiple.Count - 1; i >= 0; --i)
                {
                    var spriteID = m_SpritesMultiple[i].spriteID;
                    if (spriteRects.FirstOrDefault(x => x.spriteID == spriteID) == null)
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

        internal SpriteRect GetSpriteData(GUID guid)
        {
            return spriteImportMode == SpriteImportMode.Multiple ? m_SpritesMultiple.FirstOrDefault(x => x.spriteID == guid) : m_SpriteSingle;
        }

        internal int GetSpriteDataIndex(GUID guid)
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
                    throw new InvalidOperationException(string.Format("Sprite with GUID {0} not found", guid));
            }
        }

        void ISpriteEditorDataProvider.Apply()
        {
            var so = new SerializedObject(this);
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

        void ISpriteEditorDataProvider.InitSpriteEditorDataProvider()
        {
            var so = new SerializedObject(this);
            var spriteSheetSO = so.FindProperty("m_SpriteSheet.m_Sprites");
            m_SpritesMultiple = new List<SpriteDataExt>();
            m_SpriteSingle = new SpriteDataExt(so);

            for (int i = 0; i < spriteSheetSO.arraySize; ++i)
            {
                var sp = spriteSheetSO.GetArrayElementAtIndex(i);
                var data = new SpriteDataExt(sp);
                m_SpritesMultiple.Add(data);
            }
        }

        T ISpriteEditorDataProvider.GetDataProvider<T>()
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

        bool ISpriteEditorDataProvider.HasDataProvider(Type type)
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
