// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Events;
using UnityEngine.Scripting;
using UnityEngine.U2D;

namespace UnityEngine
{
    public enum SpriteDrawMode
    {
        Simple,
        Sliced,
        Tiled
    }

    public enum SpriteTileMode
    {
        Continuous,
        Adaptive
    }

    public enum SpriteMaskInteraction
    {
        None = 0,
        VisibleInsideMask = 1,
        VisibleOutsideMask = 2
    }

    internal enum SpriteChangeType
    {
        // The renderer's Sprite reference changed, or the Sprite was deleted. Unlike the public callback, also raised
        // while the renderer is disabled. Each change raises exactly one of these kinds.
        SpriteChanged,
        // The Sprite was bound to an atlas, which changes the texture and UVs it serves. Also raises the public callback.
        SpriteAtlasChanged,
        // The Sprite's mesh data was modified in place: same Sprite, new data.
        SpriteModified,
        // The Sprite's bones were modified in place.
        SpriteBonesModified
    }

    [NativeHeader("Runtime/Graphics/Mesh/SpriteRenderer.h")]
    [global::UnityEngine.NativeClass("SpriteRenderer", PersistentTypeId = 212)]
    [RequireComponent(typeof(Transform))]
    public sealed partial class SpriteRenderer : Renderer
    {
        internal SpriteRenderer(global::UnityEngine.EntityId id) : base(id) {}
        public SpriteRenderer() {}
        UnityEvent<SpriteRenderer> m_SpriteChangeEvent;
        UnityEvent<SpriteRenderer, SpriteChangeType> m_TypedSpriteChangeEvent;

        public void RegisterSpriteChangeCallback(UnityEngine.Events.UnityAction<SpriteRenderer> callback)
        {
            if (m_SpriteChangeEvent == null)
                m_SpriteChangeEvent = new UnityEvent<SpriteRenderer>();
            m_SpriteChangeEvent.AddListener(callback);
            hasSpriteChangeEvents = true;
        }

        public void UnregisterSpriteChangeCallback(UnityEngine.Events.UnityAction<SpriteRenderer> callback)
        {
            if (m_SpriteChangeEvent != null)
            {
                m_SpriteChangeEvent.RemoveListener(callback);
                if (0 == m_SpriteChangeEvent.GetCallsCount())
                    hasSpriteChangeEvents = false;
            }
        }

        // Unlike the public callback, this one also hears the Sprite being modified in place, which can happen
        // mid-edit (e.g. between SetVertexCount and SetIndices), so a listener should only mark itself dirty. It must
        // not destroy the Sprite or any renderer: native code is still using them while it notifies.
        internal void RegisterSpriteChangeCallback(UnityAction<SpriteRenderer, SpriteChangeType> callback)
        {
            if (m_TypedSpriteChangeEvent == null)
                m_TypedSpriteChangeEvent = new UnityEvent<SpriteRenderer, SpriteChangeType>();
            m_TypedSpriteChangeEvent.AddListener(callback);
            hasTypedSpriteChangeEvents = true;
        }

        internal void UnregisterSpriteChangeCallback(UnityAction<SpriteRenderer, SpriteChangeType> callback)
        {
            if (m_TypedSpriteChangeEvent != null)
            {
                m_TypedSpriteChangeEvent.RemoveListener(callback);
                if (0 == m_TypedSpriteChangeEvent.GetCallsCount())
                    hasTypedSpriteChangeEvents = false;
            }
        }

        // The typed event goes first: the public callback can swap the Sprite or destroy the renderer.
        [RequiredByNativeCode]
        void InvokeSpriteChanged(bool raisePublicCallback, bool spriteAtlasChanged)
        {
            InvokeTypedSpriteChange(spriteAtlasChanged ? SpriteChangeType.SpriteAtlasChanged : SpriteChangeType.SpriteChanged);

            if (raisePublicCallback)
            {
                try
                {
                    m_SpriteChangeEvent?.Invoke(this);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, this);
                }
            }
        }

        [RequiredByNativeCode]
        void InvokeSpriteModified() => InvokeTypedSpriteChange(SpriteChangeType.SpriteModified);

        [RequiredByNativeCode]
        void InvokeSpriteBonesModified() => InvokeTypedSpriteChange(SpriteChangeType.SpriteBonesModified);

        void InvokeTypedSpriteChange(SpriteChangeType changeType)
        {
            try
            {
                m_TypedSpriteChangeEvent?.Invoke(this, changeType);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }

        internal extern bool shouldSupportTiling
        {
            [NativeMethod("ShouldSupportTiling")]
            get;
        }

        internal extern bool hasSpriteChangeEvents
        {
            get;
            set;
        }

        internal extern bool hasTypedSpriteChangeEvents
        {
            get;
            set;
        }

        public extern Sprite sprite
        {
            get;
            set;
        }

        public extern SpriteDrawMode drawMode
        {
            get;
            set;
        }

        public extern Vector2 size
        {
            get;
            set;
        }

        public extern float adaptiveModeThreshold
        {
            get;
            set;
        }

        public extern SpriteTileMode tileMode
        {
            get;
            set;
        }

        public extern Color color
        {
            get;
            set;
        }

        public extern SpriteMaskInteraction maskInteraction
        {
            get;
            set;
        }

        public extern bool flipX
        {
            get;
            set;
        }

        public extern bool flipY
        {
            get;
            set;
        }

        public extern SpriteSortPoint spriteSortPoint
        {
            get;
            set;
        }

        public extern int boneTransformBufferIndex
        {
            [NativeMethod("GetBoneTransformIndex")]
            get;
        }

        extern public float GetBlendShapeWeight(int index);
        extern public void SetBlendShapeWeight(int index, float value);
        extern internal int GetBlendShapeChannelCount();

        extern internal bool IsSkinned();

        extern IntPtr GetCurrentMeshDataPtr();
        
        internal unsafe Mesh.MeshDataArray GetCurrentMeshData()
        {
            var ptr = GetCurrentMeshDataPtr();
            if (ptr == IntPtr.Zero)
                return new Mesh.MeshDataArray(0);
            var result = new Mesh.MeshDataArray(1);
            result.m_Ptrs[0] = ptr;
            return result;
        }

        [NativeHeader("Runtime/2D/Common/ScriptBindings/UnifiedRenderData.bindings.h")]
        [FreeFunction(Name = "UnifiedRenderData_Bindings::ExtractFromSpriteRenderer", HasExplicitThis = true)]
        extern private IntPtr Internal_GetUnifiedRenderData();

        // The renderer's OWN render data, not a copy of it: the returned handle references the
        // object the renderer holds, registered and referenced on the handle's behalf, so
        // Dispose() releases that one reference and leaves the renderer's intact.
        //
        // BUILT ON DEMAND: the renderer's normal draw path never builds or reads it, so the
        // first call (and the first call after the sprite, draw mode or size changes) bakes
        // the geometry on the main thread. A held handle is a snapshot -- a later rebuild
        // replaces the renderer's object rather than mutating the one the handle references.
        //
        // Null when the renderer has no sprite, or its geometry cannot be built yet (e.g. a
        // tiled sprite over the tiling vertex budget).
        //
        // color and maskInteraction are NOT carried: Graphics.RenderUnifiedRenderData reads both
        // from the SpriteParams it is given, so pass them there.
        internal UnifiedRenderData GetUnifiedRenderData()
        {
            IntPtr renderData = Internal_GetUnifiedRenderData();
            return IntPtr.Zero == renderData ? null : new UnifiedRenderData(renderData);
        }

        [NativeMethod(Name = "GetSpriteBounds")]
        internal extern Bounds Internal_GetSpriteBounds(SpriteDrawMode mode);
        extern internal void GetSecondaryTextureProperties([NotNull]MaterialPropertyBlock mbp);
        internal Bounds GetSpriteBounds()
        {
            return Internal_GetSpriteBounds(drawMode);
        }
    }

}
