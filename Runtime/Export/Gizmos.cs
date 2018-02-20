// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Internal;

namespace UnityEngine
{
    public sealed partial class Gizmos
    {
        public static void DrawRay(Ray r)
        {
            DrawLine(r.origin, r.origin + r.direction);
        }

        public static void DrawRay(Vector3 from, Vector3 direction)
        {
            DrawLine(from, from + direction);
        }

        [ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Vector3 position , Quaternion rotation)
        {
            Vector3 scale = Vector3.one;
            DrawMesh(mesh, position, rotation, scale);
        }

        [ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, Vector3 position)
        {
            Vector3 scale = Vector3.one;
            Quaternion rotation = Quaternion.identity;
            DrawMesh(mesh, position, rotation, scale);
        }

        [ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh)
        {
            Vector3 scale = Vector3.one;
            Quaternion rotation = Quaternion.identity;
            Vector3 position = Vector3.zero;
            DrawMesh(mesh, position, rotation, scale);
        }

        public static void DrawMesh(Mesh mesh, [DefaultValue("Vector3.zero")]  Vector3 position , [DefaultValue("Quaternion.identity")]  Quaternion rotation , [DefaultValue("Vector3.one")]  Vector3 scale)
        {
            DrawMesh(mesh, -1, position, rotation, scale);
        }

        [ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, int submeshIndex, Vector3 position , Quaternion rotation)
        {
            Vector3 scale = Vector3.one;
            DrawMesh(mesh, submeshIndex, position, rotation, scale);
        }

        [ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, int submeshIndex, Vector3 position)
        {
            Vector3 scale = Vector3.one;
            Quaternion rotation = Quaternion.identity;
            DrawMesh(mesh, submeshIndex, position, rotation, scale);
        }

        [ExcludeFromDocs]
        public static void DrawMesh(Mesh mesh, int submeshIndex)
        {
            Vector3 scale = Vector3.one;
            Quaternion rotation = Quaternion.identity;
            Vector3 position = Vector3.zero;
            DrawMesh(mesh, submeshIndex, position, rotation, scale);
        }

        [ExcludeFromDocs]
        public static void DrawWireMesh(Mesh mesh, Vector3 position , Quaternion rotation)
        {
            Vector3 scale = Vector3.one;
            DrawWireMesh(mesh, position, rotation, scale);
        }

        [ExcludeFromDocs]
        public static void DrawWireMesh(Mesh mesh, Vector3 position)
        {
            Vector3 scale = Vector3.one;
            Quaternion rotation = Quaternion.identity;
            DrawWireMesh(mesh, position, rotation, scale);
        }

        [ExcludeFromDocs]
        public static void DrawWireMesh(Mesh mesh)
        {
            Vector3 scale = Vector3.one;
            Quaternion rotation = Quaternion.identity;
            Vector3 position = Vector3.zero;
            DrawWireMesh(mesh, position, rotation, scale);
        }

        public static void DrawWireMesh(Mesh mesh, [DefaultValue("Vector3.zero")]  Vector3 position , [DefaultValue("Quaternion.identity")]  Quaternion rotation , [DefaultValue("Vector3.one")]  Vector3 scale)
        {
            DrawWireMesh(mesh, -1, position, rotation, scale);
        }

        [ExcludeFromDocs]
        public static void DrawWireMesh(Mesh mesh, int submeshIndex, Vector3 position , Quaternion rotation)
        {
            Vector3 scale = Vector3.one;
            DrawWireMesh(mesh, submeshIndex, position, rotation, scale);
        }

        [ExcludeFromDocs]
        public static void DrawWireMesh(Mesh mesh, int submeshIndex, Vector3 position)
        {
            Vector3 scale = Vector3.one;
            Quaternion rotation = Quaternion.identity;
            DrawWireMesh(mesh, submeshIndex, position, rotation, scale);
        }

        [ExcludeFromDocs]
        public static void DrawWireMesh(Mesh mesh, int submeshIndex)
        {
            Vector3 scale = Vector3.one;
            Quaternion rotation = Quaternion.identity;
            Vector3 position = Vector3.zero;
            DrawWireMesh(mesh, submeshIndex, position, rotation, scale);
        }

        [ExcludeFromDocs]
        public static void DrawIcon(Vector3 center, string name)
        {
            bool allowScaling = true;
            DrawIcon(center, name, allowScaling);
        }

        [ExcludeFromDocs]
        public static void DrawGUITexture(Rect screenRect, Texture texture)
        {
            Material mat = null;
            DrawGUITexture(screenRect, texture, mat);
        }

        public static void DrawGUITexture(Rect screenRect, Texture texture, [DefaultValue("null")] Material mat)
        {
            DrawGUITexture(screenRect, texture, 0, 0, 0, 0, mat);
        }

        [ExcludeFromDocs]
        public static void DrawGUITexture(Rect screenRect, Texture texture, int leftBorder, int rightBorder, int topBorder, int bottomBorder)
        {
            Material mat = null;
            DrawGUITexture(screenRect, texture, leftBorder, rightBorder, topBorder, bottomBorder, mat);
        }
    }
}
