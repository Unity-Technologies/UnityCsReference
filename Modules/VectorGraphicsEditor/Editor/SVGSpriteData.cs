// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEditor;

namespace Unity.VectorGraphics.Editor
{
    [Serializable]
    internal class SVGSpriteData
    {
        public float TessellationDetail = 0.0f;

        public string SpriteName = null;
        public Vector2 SpritePivot = Vector2.zero;
        public SpriteAlignment SpriteAlignment = SpriteAlignment.Center;
        public Vector4 SpriteBorder = Vector4.zero;
        public Rect SpriteRect = Rect.zero;
        public string SpriteID = null;

        private GUID m_GUID;

        public GUID SpriteGUID
        {
            get
            {
                if (m_GUID.Empty())
                    ValidateGUID();
                return m_GUID;
            }
            set
            {
                m_GUID = value;
                if (m_GUID.Empty())
                    SpriteID = null;
                else
                    SpriteID = m_GUID.ToString();

                ValidateGUID();
            }
        }

        void ValidateGUID()
        {
            if (!string.IsNullOrEmpty(SpriteID))
            {
                m_GUID = new GUID(SpriteID);
                if (!m_GUID.Empty())
                    return;
            }

            if (m_GUID.Empty())
                m_GUID = GUID.Generate();

            SpriteID = m_GUID.ToString();
        }

        public List<OutlineData> PhysicsOutlines = new List<OutlineData>();

        private SpriteAlignment m_PrevAlignment;
        private Vector2 m_PrevPivot;

        public void Load(SerializedObject so)
        {
            var importer = so.targetObject as SVGImporter;
            var sprite = SVGImporter.GetImportedSprite(importer.assetPath);
            if (sprite == null)
                return;

            SpriteName = sprite.name;

            int targetWidth = 0;
            int targetHeight = 0;
            importer.TextureSizeForSpriteEditor(sprite, out targetWidth, out targetHeight);
            SpriteRect = new Rect(0, 0, targetWidth, targetHeight);
            var textureSize = new Vector2(targetWidth, targetHeight);

            var baseSP = so.FindProperty("m_SpriteData");
            SpriteBorder = baseSP.FindPropertyRelative("SpriteBorder").vector4Value;
            SpritePivot = sprite.pivot / textureSize;

            var guidSP = baseSP.FindPropertyRelative("SpriteID");
            SpriteID = guidSP.stringValue;

            ValidateGUID();

            SpriteAlignment = SpriteAlignment.Center;
            if (Enum.IsDefined(typeof(SpriteAlignment), (int)importer.Alignment))
                SpriteAlignment = (SpriteAlignment)importer.Alignment;
            else if (importer.Alignment == VectorUtils.Alignment.SVGOrigin)
                SpriteAlignment = SpriteAlignment.Custom;
            m_PrevAlignment = SpriteAlignment;
            m_PrevPivot = SpritePivot;
        }

        public void Apply(SerializedObject so)
        {
            if (SpriteAlignment != m_PrevAlignment || SpritePivot != m_PrevPivot)
            {
                // Only apply the alignment if it changed, otherwise we may override the special "SVG Origin" value
                var alignSP = so.FindProperty("SpriteAlignment");
                alignSP.intValue = (int)SpriteAlignment;

                var pivotSP = so.FindProperty("SpritePivot");
                pivotSP.vector2Value = SpritePivot;
            }

            var baseSP = so.FindProperty("m_SpriteData");
            var borderSP = baseSP.FindPropertyRelative("SpriteBorder");
            borderSP.vector4Value = SpriteBorder;
        }
    }

    [Serializable]
    internal struct OutlineData
    {
        public Vector2[] Vertices;
    }
}
