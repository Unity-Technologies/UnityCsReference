// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine
{
    /// Represents a billboard
    [NativeHeader("Runtime/Graphics/Billboard/BillboardAsset.h")]
    [NativeHeader("Runtime/Export/BillboardRenderer.bindings.h")]
    public sealed class BillboardAsset : Object
    {
        public BillboardAsset()
        {
            Internal_Create(this);
        }

        [FreeFunction(Name = "BillboardRenderer_Bindings::Internal_Create")]
        extern private static void Internal_Create([Writable] BillboardAsset obj);

        extern public float width { get; set; }
        extern public float height { get; set; }
        extern public float bottom { get; set; }

        extern public int imageCount
        {
            [NativeMethod("GetNumImages")]
            get;
        }

        extern public int vertexCount
        {
            [NativeMethod("GetNumVertices")]
            get;
        }

        extern public int indexCount
        {
            [NativeMethod("GetNumIndices")]
            get;
        }

        extern public Material material { get; set; }

        // List<T> version
        public void GetImageTexCoords(List<Vector4> imageTexCoords)
        {
            if (imageTexCoords == null)
                throw new ArgumentNullException("imageTexCoords");
            GetImageTexCoordsInternal(imageTexCoords);
        }

        // T[] version
        [NativeMethod("GetBillboardDataReadonly().GetImageTexCoords")]
        extern public Vector4[] GetImageTexCoords();

        [FreeFunction(Name = "BillboardRenderer_Bindings::GetImageTexCoordsInternal", HasExplicitThis = true)]
        extern internal void GetImageTexCoordsInternal(object list);

        // List<T> version
        public void SetImageTexCoords(List<Vector4> imageTexCoords)
        {
            if (imageTexCoords == null)
                throw new ArgumentNullException("imageTexCoords");
            SetImageTexCoordsInternalList(imageTexCoords);
        }

        // T[] version
        [FreeFunction(Name = "BillboardRenderer_Bindings::SetImageTexCoords", HasExplicitThis = true)]
        extern public void SetImageTexCoords([NotNull] Vector4[] imageTexCoords);

        [FreeFunction(Name = "BillboardRenderer_Bindings::SetImageTexCoordsInternalList", HasExplicitThis = true)]
        extern internal void SetImageTexCoordsInternalList(object list);

        // List<T> version
        public void GetVertices(List<Vector2> vertices)
        {
            if (vertices == null)
                throw new ArgumentNullException("vertices");
            GetVerticesInternal(vertices);
        }

        // T[] version
        [NativeMethod("GetBillboardDataReadonly().GetVertices")]
        extern public Vector2[] GetVertices();

        [FreeFunction(Name = "BillboardRenderer_Bindings::GetVerticesInternal", HasExplicitThis = true)]
        extern internal void GetVerticesInternal(object list);

        // List<T> version
        public void SetVertices(List<Vector2> vertices)
        {
            if (vertices == null)
                throw new ArgumentNullException("vertices");
            SetVerticesInternalList(vertices);
        }

        // T[] version
        [FreeFunction(Name = "BillboardRenderer_Bindings::SetVertices", HasExplicitThis = true)]
        extern public void SetVertices([NotNull] Vector2[] vertices);

        [FreeFunction(Name = "BillboardRenderer_Bindings::SetVerticesInternalList", HasExplicitThis = true)]
        extern internal void SetVerticesInternalList(object list);

        // List<T> version
        public void GetIndices(List<UInt16> indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");
            GetIndicesInternal(indices);
        }

        // T[] version
        [NativeMethod("GetBillboardDataReadonly().GetIndices")]
        extern public UInt16[] GetIndices();

        [FreeFunction(Name = "BillboardRenderer_Bindings::GetIndicesInternal", HasExplicitThis = true)]
        extern internal void GetIndicesInternal(object list);

        // List<T> version
        public void SetIndices(List<UInt16> indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");
            SetIndicesInternalList(indices);
        }

        // T[] version
        [FreeFunction(Name = "BillboardRenderer_Bindings::SetIndices", HasExplicitThis = true)]
        extern public void SetIndices([NotNull] UInt16[] indices);

        [FreeFunction(Name = "BillboardRenderer_Bindings::SetIndicesInternalList", HasExplicitThis = true)]
        extern internal void SetIndicesInternalList(object list);

        [FreeFunction(Name = "BillboardRenderer_Bindings::MakeMaterialProperties", HasExplicitThis = true)]
        extern internal void MakeMaterialProperties(MaterialPropertyBlock properties, Camera camera);
    }

    /// Renders a billboard.
    [NativeHeader("Runtime/Graphics/Billboard/BillboardRenderer.h")]
    public sealed class BillboardRenderer : Renderer
    {
        extern public BillboardAsset billboard { get; set; }
    }
}
