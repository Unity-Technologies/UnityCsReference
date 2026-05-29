// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.U2D.Profiling
{
    [Serializable]
    record SpriteAtlasProfilerCellData
    {
        public string icon = "";
        public string name;
        public int spriteCount;
        public string spriteCountString;
        public int textureCount;
        public string textureCountString;
        public float usageRatio;
        public string usageRatioString;
        public EntityId entityId;

        public SpriteAtlasProfilerCellData(SpriteAtlasNode atlasData)
        {
            name = atlasData.atlasName;
            entityId = atlasData.entityId;
            spriteCount = 0;
            usageRatio = 0f;
            HashSet<int> uniqueSpriteIDs = new HashSet<int>();
            foreach(var texture in atlasData.textures)
            {
                spriteCount += texture.sprites.Count;
                foreach (var sprite in texture.sprites)
                {
                    if (!uniqueSpriteIDs.Contains(sprite.id))
                    {
                        uniqueSpriteIDs.Add(sprite.id);
                        usageRatio += sprite.ratioSpriteInTexture;
                    }
                }
            }
            textureCount = atlasData.textures.Count;

            if (atlasData.atlasGuid == "?")
            {
                usageRatio = 0f;
                textureCount = 0;
                usageRatioString = "";
                textureCountString = "";
            }
            else
            {
                usageRatio /= textureCount;
                usageRatioString = usageRatio.ToString("0.0 %");
                textureCountString = textureCount.ToString();
            }

            spriteCountString = spriteCount.ToString();


        }

        public SpriteAtlasProfilerCellData(TextureNode textureNode)
        {
            name = textureNode.textureName;
            entityId = textureNode.entityId;
            spriteCount = textureNode.sprites.Count;
            usageRatio = 0f;
            HashSet<int> uniqueSpriteIDs = new HashSet<int>();
            foreach (var sprite in textureNode.sprites)
            {
                if (!uniqueSpriteIDs.Contains(sprite.id))
                {
                    uniqueSpriteIDs.Add(sprite.id);
                    usageRatio += sprite.ratioSpriteInTexture;
                }
            }
            spriteCountString = spriteCount.ToString();
            textureCount = 0;
            textureCountString = "";
            usageRatioString = usageRatio.ToString("0.0 %");
        }

        public SpriteAtlasProfilerCellData(SpriteNode spriteNode)
        {
            name = spriteNode.spriteAssetName;
            entityId = spriteNode.entityId;
            spriteCount = 0;
            usageRatio = spriteNode.ratioSpriteInTexture;
            spriteCountString = "";
            textureCount = 0;
            textureCountString = "";
            usageRatioString = usageRatio.ToString("0.0 %");
        }

        public static int Compare(SpriteAtlasProfilerCellData a, SpriteAtlasProfilerCellData b, string propertyToCompare)
        {
            switch (propertyToCompare)
            {
                case "name":
                case null:
                    return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
                case "spriteCountString":
                    return a.spriteCount.CompareTo(b.spriteCount);
                case "textureCountString":
                    return a.textureCount.CompareTo(b.textureCount);
                case "usageRatioString":
                    return a.usageRatio.CompareTo(b.usageRatio);
                default:
                    return 0;
            }
        }
    }
}
