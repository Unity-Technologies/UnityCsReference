using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR
{
    class VectorImageRenderInfoPool : LinkedPool<VectorImageRenderInfo>
    {
        public VectorImageRenderInfoPool()
            : base(() => new VectorImageRenderInfo(), vectorImageInfo => vectorImageInfo.Reset()) {}
    }

    class VectorImageRenderInfo : LinkedPoolItem<VectorImageRenderInfo>
    {
        public int useCount;
        public GradientRemap firstGradientRemap;
        public Alloc gradientSettingsAlloc;

        public void Reset()
        {
            useCount = 0;
            firstGradientRemap = null;
            gradientSettingsAlloc = new Alloc();
        }
    }

    class GradientRemapPool : LinkedPool<GradientRemap>
    {
        public GradientRemapPool()
            : base(() => new GradientRemap(), gradientRemap => gradientRemap.Reset()) {}
    }

    class GradientRemap : LinkedPoolItem<GradientRemap>
    {
        public int origIndex;
        public int destIndex;
        public RectInt location;
        public GradientRemap next; // To avoid arrays.
        public TextureId atlas;

        public void Reset()
        {
            origIndex = 0;
            destIndex = 0;
            location = new RectInt();
            atlas = TextureId.invalid;
        }
    }

    class VectorImageManager : IDisposable
    {
        public static List<VectorImageManager> instances = new List<VectorImageManager>(16);

        static ProfilerMarker s_MarkerRegister = new ProfilerMarker("UIR.VectorImageManager.Register");
        static ProfilerMarker s_MarkerUnregister = new ProfilerMarker("UIR.VectorImageManager.Unregister");

        readonly AtlasBase m_Atlas;

        Dictionary<VectorImage, VectorImageRenderInfo> m_Registered;
        VectorImageRenderInfoPool m_RenderInfoPool;
        GradientRemapPool m_GradientRemapPool;
        GradientSettingsAtlas m_GradientSettingsAtlas;
        bool m_LoggedExhaustedSettingsAtlas;

        public Texture2D atlas { get { return m_GradientSettingsAtlas?.atlas; }}

        public VectorImageManager(AtlasBase atlas)
        {
            instances.Add(this);

            m_Atlas = atlas;

            m_Registered = new Dictionary<VectorImage, VectorImageRenderInfo>(32);
            m_RenderInfoPool = new VectorImageRenderInfoPool();
            m_GradientRemapPool = new GradientRemapPool();
            m_GradientSettingsAtlas = new GradientSettingsAtlas();
        }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                m_Registered.Clear();
                m_RenderInfoPool.Clear();
                m_GradientRemapPool.Clear();
                m_GradientSettingsAtlas.Dispose();
                instances.Remove(this);
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        #region Reset Pattern

        public void Reset()
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            // Actually perform the reset here.
            m_Registered.Clear();
            m_RenderInfoPool.Clear();
            m_GradientRemapPool.Clear();
            m_GradientSettingsAtlas.Reset();
        }

        #endregion // Reset Pattern

        public void Commit()
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            m_GradientSettingsAtlas.Commit();
        }

        public GradientRemap AddUser(VectorImage vi, VisualElement context)
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return null;
            }

            if (vi == null)
                return null;

            VectorImageRenderInfo renderInfo;
            if (m_Registered.TryGetValue(vi, out renderInfo))
                ++renderInfo.useCount;
            else
                renderInfo = Register(vi, context);

            return renderInfo.firstGradientRemap;
        }

        public void RemoveUser(VectorImage vi)
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            if (vi == null)
                return;

            VectorImageRenderInfo renderInfo;
            if (m_Registered.TryGetValue(vi, out renderInfo))
            {
                --renderInfo.useCount;
                if (renderInfo.useCount == 0)
                    Unregister(vi, renderInfo);
            }
        }

        VectorImageRenderInfo Register(VectorImage vi, VisualElement context)
        {
            s_MarkerRegister.Begin();

            VectorImageRenderInfo renderInfo = m_RenderInfoPool.Get();
            renderInfo.useCount = 1;
            m_Registered[vi] = renderInfo;

            if (vi.settings?.Length > 0)
            {
                // We first attempt to allocate into the gradient settings atlas since it supports deallocation.
                int gradientCount = vi.settings.Length;
                Alloc alloc = m_GradientSettingsAtlas.Add(gradientCount);
                if (alloc.size > 0)
                {
                    // Then attempt to allocate in the texture atlas.
                    // TODO: Once the atlas actually processes returns, we should call it at some point.
                    if (m_Atlas.TryGetAtlas(context, vi.atlas, out TextureId atlasId, out RectInt uvs))
                    {
                        // Remap.
                        GradientRemap previous = null;
                        for (int i = 0; i < gradientCount; ++i)
                        {
                            // Chain.
                            GradientRemap current = m_GradientRemapPool.Get();
                            if (i > 0)
                                previous.next = current;
                            else
                                renderInfo.firstGradientRemap = current;
                            previous = current;

                            // Remap the index.
                            current.origIndex = i;
                            current.destIndex = (int)alloc.start + i;

                            // Remap the rect.
                            GradientSettings gradient = vi.settings[i];
                            RectInt location = gradient.location;
                            location.x += uvs.x;
                            location.y += uvs.y;
                            current.location = location;
                            current.atlas = atlasId;
                        }

                        // Write into the previously allocated gradient settings now that we are sure to use it.
                        m_GradientSettingsAtlas.Write(alloc, vi.settings, renderInfo.firstGradientRemap);
                    }
                    else
                    {
                        // If the texture atlas didn't fit, keep it as a standalone custom texture, only need to remap the setting indices
                        GradientRemap previous = null;
                        for (int i = 0; i < gradientCount; ++i)
                        {
                            GradientRemap current = m_GradientRemapPool.Get();
                            if (i > 0)
                                previous.next = current;
                            else
                                renderInfo.firstGradientRemap = current;
                            previous = current;

                            current.origIndex = i;
                            current.destIndex = (int)alloc.start + i;
                            current.atlas = TextureId.invalid;
                        }

                        m_GradientSettingsAtlas.Write(alloc, vi.settings, null);
                    }
                }
                else
                {
                    if (!m_LoggedExhaustedSettingsAtlas)
                    {
                        Debug.LogError("Exhausted max gradient settings (" + m_GradientSettingsAtlas.length + ") for atlas: " + m_GradientSettingsAtlas.atlas?.name);
                        m_LoggedExhaustedSettingsAtlas = true;
                    }
                }
            }

            s_MarkerRegister.End();

            return renderInfo;
        }

        void Unregister(VectorImage vi, VectorImageRenderInfo renderInfo)
        {
            s_MarkerUnregister.Begin();

            if (renderInfo.gradientSettingsAlloc.size > 0)
                m_GradientSettingsAtlas.Remove(renderInfo.gradientSettingsAlloc);

            GradientRemap remap = renderInfo.firstGradientRemap;
            while (remap != null)
            {
                GradientRemap next = remap.next;
                m_GradientRemapPool.Return(remap);
                remap = next;
            }

            m_Registered.Remove(vi);
            m_RenderInfoPool.Return(renderInfo);

            s_MarkerUnregister.End();
        }
    }
}
