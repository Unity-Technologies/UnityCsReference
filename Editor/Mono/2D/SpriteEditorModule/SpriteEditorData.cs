// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.U2D;
using UnityEngine.Experimental.U2D;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.U2D
{
    internal class SpriteDataExt : SpriteRect
    {
        public float tessellationDetail = 0;

        // The following lists are to be left un-initialized.
        // If they never loaded or assign explicitly, we avoid writing empty list to metadata.
        public List<Vector2[]> spriteOutline;
        public List<Vertex2DMetaData> vertices;
        public List<int> indices;
        public List<Vector2Int> edges;
        public List<Vector2[]> spritePhysicsOutline;
        public List<SpriteBone> spriteBone;

        internal SpriteDataExt(SerializedObject so)
        {
            var ti = so.targetObject as TextureImporter;
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(ti.assetPath);
            name = texture.name;
            alignment = (SpriteAlignment)so.FindProperty("m_Alignment").intValue;
            border = ti.spriteBorder;
            pivot = SpriteEditorUtility.GetPivotValue(alignment, ti.spritePivot);
            tessellationDetail = so.FindProperty("m_SpriteTessellationDetail").floatValue;

            int width = 0, height = 0;
            ti.GetWidthAndHeight(ref width, ref height);
            rect = new Rect(0, 0, width, height);

            var guidSP = so.FindProperty("m_SpriteSheet.m_SpriteID");
            spriteID = new GUID(guidSP.stringValue);
        }

        internal SpriteDataExt(SerializedProperty sp)
        {
            rect = sp.FindPropertyRelative("m_Rect").rectValue;
            border = sp.FindPropertyRelative("m_Border").vector4Value;
            name = sp.FindPropertyRelative("m_Name").stringValue;
            alignment = (SpriteAlignment)sp.FindPropertyRelative("m_Alignment").intValue;
            pivot = SpriteEditorUtility.GetPivotValue(alignment, sp.FindPropertyRelative("m_Pivot").vector2Value);
            tessellationDetail = sp.FindPropertyRelative("m_TessellationDetail").floatValue;
            spriteID = new GUID(sp.FindPropertyRelative("m_SpriteID").stringValue);
        }

        internal SpriteDataExt(SpriteRect sr)
        {
            originalName = sr.originalName;
            name = sr.name;
            border = sr.border;
            tessellationDetail = 0;
            rect = sr.rect;
            spriteID = sr.spriteID;
            alignment = sr.alignment;
            pivot = sr.pivot;
            spriteOutline = new List<Vector2[]>();
            vertices = new List<Vertex2DMetaData>();
            indices = new List<int>();
            edges = new List<Vector2Int>();
            spritePhysicsOutline = new List<Vector2[]>();
            spriteBone = new List<SpriteBone>();
        }

        public void Apply(SerializedObject so)
        {
            so.FindProperty("m_Alignment").intValue = (int)alignment;
            so.FindProperty("m_SpriteBorder").vector4Value = border;
            so.FindProperty("m_SpritePivot").vector2Value = pivot;
            so.FindProperty("m_SpriteTessellationDetail").floatValue = tessellationDetail;
            so.FindProperty("m_SpriteSheet.m_SpriteID").stringValue = spriteID.ToString();

            var sp = so.FindProperty("m_SpriteSheet");
            if (spriteBone != null)
                SpriteBoneDataTransfer.Apply(sp, spriteBone);
            if (spriteOutline != null)
                SpriteOutlineDataTransfer.Apply(sp, spriteOutline);
            if (spritePhysicsOutline != null)
                SpritePhysicsOutlineDataTransfer.Apply(sp, spritePhysicsOutline);
            if (vertices != null)
                SpriteMeshDataTransfer.Apply(sp, vertices, indices, edges);
        }

        public void Apply(SerializedProperty sp)
        {
            sp.FindPropertyRelative("m_Rect").rectValue = rect;
            sp.FindPropertyRelative("m_Name").stringValue = name;
            sp.FindPropertyRelative("m_Border").vector4Value = border;
            sp.FindPropertyRelative("m_Alignment").intValue = (int)alignment;
            sp.FindPropertyRelative("m_Pivot").vector2Value = pivot;
            sp.FindPropertyRelative("m_TessellationDetail").floatValue = tessellationDetail;
            sp.FindPropertyRelative("m_SpriteID").stringValue = spriteID.ToString();

            if (spriteBone != null)
                SpriteBoneDataTransfer.Apply(sp, spriteBone);
            if (spriteOutline != null)
                SpriteOutlineDataTransfer.Apply(sp, spriteOutline);
            if (spritePhysicsOutline != null)
                SpritePhysicsOutlineDataTransfer.Apply(sp, spritePhysicsOutline);
            if (vertices != null)
                SpriteMeshDataTransfer.Apply(sp, vertices, indices, edges);
        }

        public void CopyFromSpriteRect(SpriteRect spriteRect)
        {
            alignment = spriteRect.alignment;
            border = spriteRect.border;
            name = spriteRect.name;
            pivot = spriteRect.pivot;
            rect = spriteRect.rect;
            spriteID = spriteRect.spriteID;
        }
    }
}
