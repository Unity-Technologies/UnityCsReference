// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine.Experimental.U2D;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.Experimental.U2D
{
    public interface ISpriteEditorDataProvider
    {
        SpriteImportMode spriteImportMode { get; }
        float pixelsPerUnit { get; }
        UnityObject targetObject { get; }

        SpriteRect[] GetSpriteRects();
        void SetSpriteRects(SpriteRect[] spriteRects);
        void Apply();
        void InitSpriteEditorDataProvider();

        T GetDataProvider<T>() where T : class;
        bool HasDataProvider(Type type);
    }

    public interface ISpriteBoneDataProvider
    {
        List<SpriteBone> GetBones(GUID guid);
        void SetBones(GUID guid, List<SpriteBone> bones);
    }

    public interface ISpriteOutlineDataProvider
    {
        List<Vector2[]> GetOutlines(GUID guid);
        void SetOutlines(GUID guid, List<Vector2[]> data);
        float GetTessellationDetail(GUID guid);
        void SetTessellationDetail(GUID guid, float value);
    }

    public interface ISpritePhysicsOutlineDataProvider
    {
        List<Vector2[]> GetOutlines(GUID guid);
        void SetOutlines(GUID guid, List<Vector2[]> data);
        float GetTessellationDetail(GUID guid);
        void SetTessellationDetail(GUID guid, float value);
    }

    public interface ITextureDataProvider
    {
        Texture2D texture { get; }
        void GetTextureActualWidthAndHeight(out int width , out int height);
        Texture2D GetReadableTexture2D();
    }

    internal class SpriteDataProviderBase
    {
        public SpriteDataProviderBase(TextureImporter dp)
        {
            dataProvider = dp;
        }

        protected TextureImporter dataProvider { get; private set; }
    }

    [Serializable]
    public struct Vertex2DMetaData
    {
        public Vector2 position;
        public BoneWeight boneWeight;
    }

    public interface ISpriteMeshDataProvider
    {
        Vertex2DMetaData[] GetVertices(GUID guid);
        void SetVertices(GUID guid, Vertex2DMetaData[] vertices);
        int[] GetIndices(GUID guid);
        void SetIndices(GUID guid, int[] indices);
        Vector2Int[] GetEdges(GUID guid);
        void SetEdges(GUID guid, Vector2Int[] edges);
    }

    internal class SpriteBoneDataTransfer : SpriteDataProviderBase, ISpriteBoneDataProvider
    {
        public SpriteBoneDataTransfer(TextureImporter dp) : base(dp)
        {}

        public List<SpriteBone> GetBones(GUID guid)
        {
            var index = dataProvider.GetSpriteDataIndex(guid);
            return Load(new SerializedObject(dataProvider), dataProvider.spriteImportMode, index);
        }

        public void SetBones(GUID guid, List<SpriteBone> bones)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).spriteBone = bones;
        }

        private static List<SpriteBone> Load(SerializedObject importer, SpriteImportMode mode, int index)
        {
            var sp = mode == SpriteImportMode.Multiple ?
                importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index).FindPropertyRelative("m_Bones") :
                importer.FindProperty("m_SpriteSheet.m_Bones");

            var spriteBone = new List<SpriteBone>(sp.arraySize);
            for (int i = 0; i < sp.arraySize; ++i)
            {
                var boneSO = sp.GetArrayElementAtIndex(i);
                var sb = new SpriteBone();
                sb.length = boneSO.FindPropertyRelative("length").floatValue;
                sb.position = boneSO.FindPropertyRelative("position").vector3Value;
                sb.rotation = boneSO.FindPropertyRelative("rotation").quaternionValue;
                sb.parentId = boneSO.FindPropertyRelative("parentId").intValue;
                sb.name = boneSO.FindPropertyRelative("name").stringValue;
                spriteBone.Add(sb);
            }
            return spriteBone;
        }

        public static void Apply(SerializedProperty rectSP, List<SpriteBone> spriteBone)
        {
            var sp = rectSP.FindPropertyRelative("m_Bones");
            sp.arraySize = spriteBone.Count;
            for (int i = 0; i < sp.arraySize; ++i)
            {
                var boneSO = sp.GetArrayElementAtIndex(i);
                var sb = spriteBone[i];
                boneSO.FindPropertyRelative("length").floatValue = sb.length;
                boneSO.FindPropertyRelative("position").vector3Value = sb.position;
                boneSO.FindPropertyRelative("rotation").quaternionValue = sb.rotation;
                boneSO.FindPropertyRelative("parentId").intValue = sb.parentId;
                boneSO.FindPropertyRelative("name").stringValue = sb.name;
            }
        }
    }

    internal class SpriteOutlineDataTransfer : SpriteDataProviderBase, ISpriteOutlineDataProvider
    {
        public SpriteOutlineDataTransfer(TextureImporter dp) : base(dp)
        {}

        public List<Vector2[]> GetOutlines(GUID guid)
        {
            var index = dataProvider.GetSpriteDataIndex(guid);
            return Load(new SerializedObject(dataProvider), dataProvider.spriteImportMode, index);
        }

        public void SetOutlines(GUID guid, List<Vector2[]> data)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).spriteOutline = data;
        }

        public float GetTessellationDetail(GUID guid)
        {
            return ((SpriteDataExt)dataProvider.GetSpriteData(guid)).tessellationDetail;
        }

        public void SetTessellationDetail(GUID guid, float value)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).tessellationDetail = value;
        }

        private static List<Vector2[]> Load(SerializedObject importer, SpriteImportMode mode, int index)
        {
            var outlineSP = mode == SpriteImportMode.Multiple ?
                importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index).FindPropertyRelative("m_Outline") :
                importer.FindProperty("m_SpriteSheet.m_Outline");

            var outline = new List<Vector2[]>();
            for (int j = 0; j < outlineSP.arraySize; ++j)
            {
                SerializedProperty outlinePathSP = outlineSP.GetArrayElementAtIndex(j);
                var o = new Vector2[outlinePathSP.arraySize];
                for (int k = 0; k < outlinePathSP.arraySize; ++k)
                {
                    o[k] = outlinePathSP.GetArrayElementAtIndex(k).vector2Value;
                }
                outline.Add(o);
            }
            return outline;
        }

        public static void Apply(SerializedProperty rectSP, List<Vector2[]> outline)
        {
            var outlineSP = rectSP.FindPropertyRelative("m_Outline");
            outlineSP.ClearArray();
            for (int j = 0; j < outline.Count; ++j)
            {
                outlineSP.InsertArrayElementAtIndex(j);
                var o = outline[j];
                SerializedProperty outlinePathSP = outlineSP.GetArrayElementAtIndex(j);
                outlinePathSP.ClearArray();
                for (int k = 0; k < o.Length; ++k)
                {
                    outlinePathSP.InsertArrayElementAtIndex(k);
                    outlinePathSP.GetArrayElementAtIndex(k).vector2Value = o[k];
                }
            }
        }
    }

    internal class SpriteMeshDataTransfer : SpriteDataProviderBase, ISpriteMeshDataProvider
    {
        public SpriteMeshDataTransfer(TextureImporter dp) : base(dp)
        {}

        public Vertex2DMetaData[] GetVertices(GUID guid)
        {
            var index = dataProvider.GetSpriteDataIndex(guid);
            return LoadVertex2DMetaData(new SerializedObject(dataProvider), dataProvider.spriteImportMode, index);
        }

        public void SetVertices(GUID guid, Vertex2DMetaData[] data)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).vertices = new List<Vertex2DMetaData>(data);
        }

        public int[] GetIndices(GUID guid)
        {
            var index = dataProvider.GetSpriteDataIndex(guid);
            return LoadIndices(new SerializedObject(dataProvider), dataProvider.spriteImportMode, index);
        }

        public void SetIndices(GUID guid, int[] indices)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).indices = new List<int>(indices);
        }

        public Vector2Int[] GetEdges(GUID guid)
        {
            var index = dataProvider.GetSpriteDataIndex(guid);
            return LoadEdges(new SerializedObject(dataProvider), dataProvider.spriteImportMode, index);
        }

        public void SetEdges(GUID guid, Vector2Int[] edges)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).edges = new List<Vector2Int>(edges);
        }

        private Vertex2DMetaData[] LoadVertex2DMetaData(SerializedObject importer, SpriteImportMode mode, int index)
        {
            var so = mode == SpriteImportMode.Multiple ?
                importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index) :
                importer.FindProperty("m_SpriteSheet");

            var verticesSP = so.FindPropertyRelative("m_Vertices");
            var weightsSP = so.FindPropertyRelative("m_Weights");

            var vertices = new Vertex2DMetaData[verticesSP.arraySize];
            for (int i = 0; i < verticesSP.arraySize; ++i)
            {
                var vsp = verticesSP.GetArrayElementAtIndex(i);
                var wsp = weightsSP.GetArrayElementAtIndex(i);

                vertices[i] = new Vertex2DMetaData
                {
                    position = vsp.vector2Value,
                    boneWeight = new BoneWeight
                    {
                        weight0 = wsp.FindPropertyRelative("weight[0]").floatValue,
                        weight1 = wsp.FindPropertyRelative("weight[1]").floatValue,
                        weight2 = wsp.FindPropertyRelative("weight[2]").floatValue,
                        weight3 = wsp.FindPropertyRelative("weight[3]").floatValue,
                        boneIndex0 = wsp.FindPropertyRelative("boneIndex[0]").intValue,
                        boneIndex1 = wsp.FindPropertyRelative("boneIndex[1]").intValue,
                        boneIndex2 = wsp.FindPropertyRelative("boneIndex[2]").intValue,
                        boneIndex3 = wsp.FindPropertyRelative("boneIndex[3]").intValue
                    }
                };
            }

            return vertices;
        }

        private int[] LoadIndices(SerializedObject importer, SpriteImportMode mode, int index)
        {
            var so = mode == SpriteImportMode.Multiple ?
                importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index) :
                importer.FindProperty("m_SpriteSheet");

            var indicesSP = so.FindPropertyRelative("m_Indices");

            var indices = new int[indicesSP.arraySize];
            for (int i = 0; i < indicesSP.arraySize; ++i)
            {
                indices[i] = indicesSP.GetArrayElementAtIndex(i).intValue;
            }

            return indices;
        }

        private Vector2Int[] LoadEdges(SerializedObject importer, SpriteImportMode mode, int index)
        {
            var so = mode == SpriteImportMode.Multiple ?
                importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index) :
                importer.FindProperty("m_SpriteSheet");

            var edgesSP = so.FindPropertyRelative("m_Edges");

            var edges = new Vector2Int[edgesSP.arraySize];
            for (int i = 0; i < edgesSP.arraySize; ++i)
            {
                edges[i] = edgesSP.GetArrayElementAtIndex(i).vector2IntValue;
            }

            return edges;
        }

        public static void Apply(SerializedProperty rectSP, List<Vertex2DMetaData> vertices, List<int> indices, List<Vector2Int> edges)
        {
            var verticesSP = rectSP.FindPropertyRelative("m_Vertices");
            var weightsSP = rectSP.FindPropertyRelative("m_Weights");
            var indicesSP = rectSP.FindPropertyRelative("m_Indices");
            var edgesSP = rectSP.FindPropertyRelative("m_Edges");

            verticesSP.arraySize = vertices.Count;
            weightsSP.arraySize = vertices.Count;

            for (int i = 0; i < vertices.Count; ++i)
            {
                var vsp = verticesSP.GetArrayElementAtIndex(i);
                var wsp = weightsSP.GetArrayElementAtIndex(i);

                vsp.vector2Value = vertices[i].position;
                wsp.FindPropertyRelative("weight[0]").floatValue = vertices[i].boneWeight.weight0;
                wsp.FindPropertyRelative("weight[1]").floatValue = vertices[i].boneWeight.weight1;
                wsp.FindPropertyRelative("weight[2]").floatValue = vertices[i].boneWeight.weight2;
                wsp.FindPropertyRelative("weight[3]").floatValue = vertices[i].boneWeight.weight3;
                wsp.FindPropertyRelative("boneIndex[0]").intValue = vertices[i].boneWeight.boneIndex0;
                wsp.FindPropertyRelative("boneIndex[1]").intValue = vertices[i].boneWeight.boneIndex1;
                wsp.FindPropertyRelative("boneIndex[2]").intValue = vertices[i].boneWeight.boneIndex2;
                wsp.FindPropertyRelative("boneIndex[3]").intValue = vertices[i].boneWeight.boneIndex3;
            }

            indicesSP.arraySize = indices.Count;

            for (int i = 0; i < indices.Count; ++i)
            {
                indicesSP.GetArrayElementAtIndex(i).intValue = indices[i];
            }

            edgesSP.arraySize = edges.Count;

            for (int i = 0; i < edges.Count; ++i)
            {
                edgesSP.GetArrayElementAtIndex(i).vector2IntValue = edges[i];
            }
        }
    }

    internal class SpritePhysicsOutlineDataTransfer : SpriteDataProviderBase, ISpritePhysicsOutlineDataProvider
    {
        public SpritePhysicsOutlineDataTransfer(TextureImporter dp) : base(dp)
        {}

        public List<Vector2[]> GetOutlines(GUID guid)
        {
            var index = dataProvider.GetSpriteDataIndex(guid);
            return Load(new SerializedObject(dataProvider), dataProvider.spriteImportMode, index);
        }

        public void SetOutlines(GUID guid, List<Vector2[]> data)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).spritePhysicsOutline = data;
        }

        public float GetTessellationDetail(GUID guid)
        {
            return ((SpriteDataExt)dataProvider.GetSpriteData(guid)).tessellationDetail;
        }

        public void SetTessellationDetail(GUID guid, float value)
        {
            ((SpriteDataExt)dataProvider.GetSpriteData(guid)).tessellationDetail = value;
        }

        private static List<Vector2[]> Load(SerializedObject importer, SpriteImportMode mode, int index)
        {
            var outlineSP = mode == SpriteImportMode.Multiple ?
                importer.FindProperty("m_SpriteSheet.m_Sprites").GetArrayElementAtIndex(index).FindPropertyRelative("m_PhysicsShape") :
                importer.FindProperty("m_SpriteSheet.m_PhysicsShape");

            var outline = new List<Vector2[]>();
            for (int j = 0; j < outlineSP.arraySize; ++j)
            {
                SerializedProperty outlinePathSP = outlineSP.GetArrayElementAtIndex(j);
                var o = new Vector2[outlinePathSP.arraySize];
                for (int k = 0; k < outlinePathSP.arraySize; ++k)
                {
                    o[k] = outlinePathSP.GetArrayElementAtIndex(k).vector2Value;
                }
                outline.Add(o);
            }
            return outline;
        }

        public static void Apply(SerializedProperty rectSP, List<Vector2[]> value)
        {
            var outlineSP = rectSP.FindPropertyRelative("m_PhysicsShape");
            outlineSP.ClearArray();
            for (int j = 0; j < value.Count; ++j)
            {
                outlineSP.InsertArrayElementAtIndex(j);
                var o = value[j];
                SerializedProperty outlinePathSP = outlineSP.GetArrayElementAtIndex(j);
                outlinePathSP.ClearArray();
                for (int k = 0; k < o.Length; ++k)
                {
                    outlinePathSP.InsertArrayElementAtIndex(k);
                    outlinePathSP.GetArrayElementAtIndex(k).vector2Value = o[k];
                }
            }
        }
    }

    internal class SpriteTextureDataTransfer : SpriteDataProviderBase, ITextureDataProvider
    {
        public SpriteTextureDataTransfer(TextureImporter dp) : base(dp)
        {}

        Texture2D m_ReadableTexture;
        Texture2D m_OriginalTexture;

        public Texture2D texture
        {
            get
            {
                if (m_OriginalTexture == null)
                    m_OriginalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(dataProvider.assetPath);
                return m_OriginalTexture;
            }
        }

        public void GetTextureActualWidthAndHeight(out int width, out int height)
        {
            width = height = 0;
            dataProvider.GetWidthAndHeight(ref width, ref height);
        }

        public Texture2D GetReadableTexture2D()
        {
            if (m_ReadableTexture == null)
            {
                int width = 0, height = 0;
                GetTextureActualWidthAndHeight(out width, out height);
                m_ReadableTexture = UnityEditor.SpriteUtility.CreateTemporaryDuplicate(texture, width, height);
                if (m_ReadableTexture != null)
                    m_ReadableTexture.filterMode = texture.filterMode;
            }
            return m_ReadableTexture;
        }
    }
}
