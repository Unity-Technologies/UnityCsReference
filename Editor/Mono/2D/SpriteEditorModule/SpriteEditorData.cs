// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.U2D;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.U2D
{
    internal class SpriteDataSingleMode : SpriteDataBase
    {
        public void Apply(SerializedObject so)
        {
            so.FindProperty("m_Alignment").intValue = (int)alignment;
            so.FindProperty("m_SpriteBorder").vector4Value = border;
            so.FindProperty("m_SpritePivot").vector2Value = pivot;
            so.FindProperty("m_SpriteTessellationDetail").floatValue = tessellationDetail;

            SerializedProperty outlineSP = so.FindProperty("m_SpriteSheet.m_Outline");

            if (outline != null)
                ApplyOutlineChanges(outlineSP, outline);
            else
                outlineSP.ClearArray();

            SerializedProperty physicsShapeSP = so.FindProperty("m_SpriteSheet.m_PhysicsShape");

            if (physicsShape != null)
                ApplyOutlineChanges(physicsShapeSP, physicsShape);
            else
                physicsShapeSP.ClearArray();
        }

        public void Load(SerializedObject so)
        {
            var ti = so.targetObject as TextureImporter;
            name = ti.name;
            alignment = (SpriteAlignment)so.FindProperty("m_Alignment").intValue;
            border = ti.spriteBorder;
            pivot = SpriteEditorUtility.GetPivotValue(alignment, ti.spritePivot);
            tessellationDetail = so.FindProperty("m_SpriteTessellationDetail").floatValue;
            SerializedProperty outlineSP = so.FindProperty("m_SpriteSheet.m_Outline");
            outline = AcquireOutline(outlineSP);
            SerializedProperty physicsShapeSP = so.FindProperty("m_SpriteSheet.m_PhysicsShape");
            physicsShape = AcquireOutline(physicsShapeSP);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ti.assetPath);
            rect = new Rect(0, 0, texture.width, texture.height);
        }

        static protected List<Vector2[]> AcquireOutline(SerializedProperty outlineSP)
        {
            var outline = new List<Vector2[]>();
            for (int j = 0; j < outlineSP.arraySize; ++j)
            {
                SerializedProperty outlinePathSO = outlineSP.GetArrayElementAtIndex(j);
                var o = new Vector2[outlinePathSO.arraySize];
                for (int k = 0; k < outlinePathSO.arraySize; ++k)
                {
                    o[k] = outlinePathSO.GetArrayElementAtIndex(k).vector2Value;
                }
                outline.Add(o);
            }

            return outline;
        }

        static protected void ApplyOutlineChanges(SerializedProperty outlineSP, List<Vector2[]> outline)
        {
            outlineSP.ClearArray();
            for (int j = 0; j < outline.Count; ++j)
            {
                outlineSP.InsertArrayElementAtIndex(j);
                var o = outline[j];
                SerializedProperty outlinePathSO = outlineSP.GetArrayElementAtIndex(j);
                outlinePathSO.ClearArray();
                for (int k = 0; k < o.Length; ++k)
                {
                    outlinePathSO.InsertArrayElementAtIndex(k);
                    outlinePathSO.GetArrayElementAtIndex(k).vector2Value = o[k];
                }
            }
        }

        public override SpriteAlignment alignment
        {
            get; set;
        }

        public override  Vector4 border { get; set; }

        public override  string name { get; set; }

        public override List<Vector2[]> outline { get; set; }

        public override List<Vector2[]> physicsShape { get; set; }

        public override Vector2 pivot { get; set; }

        public override Rect rect { get; set; }

        public override float tessellationDetail { get; set; }
    }

    internal class SpriteDataMultipleMode : SpriteDataSingleMode
    {
        public void Load(SerializedProperty sp)
        {
            rect = sp.FindPropertyRelative("m_Rect").rectValue;
            border = sp.FindPropertyRelative("m_Border").vector4Value;
            name = sp.FindPropertyRelative("m_Name").stringValue;
            alignment = (SpriteAlignment)sp.FindPropertyRelative("m_Alignment").intValue;
            pivot = SpriteEditorUtility.GetPivotValue(alignment, sp.FindPropertyRelative("m_Pivot").vector2Value);
            tessellationDetail = sp.FindPropertyRelative("m_TessellationDetail").floatValue;
            SerializedProperty outlineSP = sp.FindPropertyRelative("m_Outline");
            outline = AcquireOutline(outlineSP);
            outlineSP = sp.FindPropertyRelative("m_PhysicsShape");
            physicsShape = AcquireOutline(outlineSP);
        }

        public void Apply(SerializedProperty sp)
        {
            sp.FindPropertyRelative("m_Rect").rectValue = rect;
            sp.FindPropertyRelative("m_Border").vector4Value = border;
            sp.FindPropertyRelative("m_Name").stringValue = name;
            sp.FindPropertyRelative("m_Alignment").intValue = (int)alignment;
            sp.FindPropertyRelative("m_Pivot").vector2Value = pivot;
            sp.FindPropertyRelative("m_TessellationDetail").floatValue = tessellationDetail;

            SerializedProperty outlineSP = sp.FindPropertyRelative("m_Outline");
            outlineSP.ClearArray();
            if (outline != null)
                ApplyOutlineChanges(outlineSP, outline);

            SerializedProperty physicsShapeSP = sp.FindPropertyRelative("m_PhysicsShape");
            physicsShapeSP.ClearArray();
            if (physicsShape != null)
                ApplyOutlineChanges(physicsShapeSP, physicsShape);
        }
    }
}
