using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.UIElements
{
    struct TextureId
    {
        // Default constructor inits to 0, which returns -1 through the getter (invalid)
        readonly int m_Index;

        public TextureId(int index)
        {
            m_Index = index + 1;
        }

        public int index => m_Index - 1;

        public float ConvertToGpu()
        {
            // Ids from 0 to 2048 can be perfectly represented with half.
            return index;
        }

        public static readonly TextureId invalid = new TextureId(-1);

        public override bool Equals(object obj)
        {
            if (!(obj is TextureId))
                return false;

            return (TextureId)obj == this;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Equals(TextureId other)
        {
            return m_Index == other.m_Index;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override int GetHashCode()
        {
            return m_Index.GetHashCode();
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(TextureId left, TextureId right)
        {
            return left.m_Index == right.m_Index;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(TextureId left, TextureId right)
        {
            return !(left == right);
        }
    }

    class TextureRegistry
    {
        struct TextureInfo
        {
            public Texture texture;
            public bool dynamic; // Indicates whether the texture can be updated or not, and whether there is a texture-to-id entry.
            public int refCount; // 0 means not allocated
        }

        List<TextureInfo> m_Textures = new List<TextureInfo>(128);
        Dictionary<Texture, TextureId> m_TextureToId = new Dictionary<Texture, TextureId>(128); // Not for dynamic

        Stack<TextureId> m_FreeIds = new Stack<TextureId>();

        internal const int maxTextures = 2048; // Ids from 0 to 2048 can be perfectly represented with float16.

        public static TextureRegistry instance { get; } = new TextureRegistry();

        public Texture GetTexture(TextureId id)
        {
            if (id.index < 0 || id.index >= m_Textures.Count)
            {
                Debug.LogError($"Attempted to get an invalid texture (index={id.index}).");
                return null;
            }

            TextureInfo info = m_Textures[id.index];

            if (info.refCount < 1)
            {
                Debug.LogError($"Attempted to get a texture (index={id.index}) that is not allocated.");
                return null;
            }

            return info.texture;
        }

        // A dynamic texture is a texture id for which the associated texture may vary.
        // This should typically be called by dynamic atlases once.
        public TextureId AllocAndAcquireDynamic()
        {
            // To keep things simple, dynamic should call update afterwards.
            return AllocAndAcquire(null, true);
        }

        public void UpdateDynamic(TextureId id, Texture texture)
        {
            if (id.index < 0 || id.index >= m_Textures.Count)
            {
                Debug.LogError($"Attempted to update an invalid dynamic texture (index={id.index}).");
                return;
            }

            TextureInfo info = m_Textures[id.index];

            if (!info.dynamic)
            {
                Debug.LogError($"Attempted to update a texture (index={id.index}) that is not dynamic.");
                return;
            }

            if (info.refCount < 1)
            {
                Debug.LogError($"Attempted to update a dynamic texture (index={id.index}) that is not allocated.");
                return;
            }

            info.texture = texture;
            m_Textures[id.index] = info;
        }

        TextureId AllocAndAcquire(Texture texture, bool dynamic)
        {
            TextureId id;
            TextureInfo info = new TextureInfo
            {
                texture = texture,
                dynamic = dynamic,
                refCount = 1
            };

            if (m_FreeIds.Count > 0)
            {
                id = m_FreeIds.Pop();
                m_Textures[id.index] = info;
            }
            else
            {
                if (m_Textures.Count == maxTextures)
                {
                    Debug.LogError($"Failed to allocate a {nameof(TextureId)} because the limit of {maxTextures} textures is reached.");
                    return TextureId.invalid;
                }
                id = new TextureId(m_Textures.Count);
                m_Textures.Add(info);
            }

            if (!dynamic)
                m_TextureToId[texture] = id;

            return id;
        }

        // This method must not be used for dynamic textures
        public TextureId Acquire(Texture tex)
        {
            if (m_TextureToId.TryGetValue(tex, out TextureId id))
            {
                TextureInfo info = m_Textures[id.index];
                Debug.Assert(info.refCount > 0); // Otherwise, we should not have been able to get the id.
                Debug.Assert(!info.dynamic); // It should not be possible to map a dynamic texture to an id.
                ++info.refCount;
                m_Textures[id.index] = info;
                return id;
            }

            return AllocAndAcquire(tex, false);
        }

        // Increases refcount
        public void Acquire(TextureId id)
        {
            if (id.index < 0 || id.index >= m_Textures.Count)
            {
                Debug.LogError($"Attempted to acquire an invalid texture (index={id.index}).");
                return;
            }

            TextureInfo info = m_Textures[id.index];

            if (info.refCount < 1)
            {
                Debug.LogError($"Attempted to acquire a texture (index={id.index}) that is not allocated.");
                return;
            }

            ++info.refCount;
            m_Textures[id.index] = info;
        }

        // Decreases refcount and deallocates when it reaches 0.
        public void Release(TextureId id)
        {
            if (id.index < 0 || id.index >= m_Textures.Count)
            {
                Debug.LogError($"Attempted to release an invalid texture (index={id.index}).");
                return;
            }

            TextureInfo info = m_Textures[id.index];

            if (info.refCount < 1)
            {
                Debug.LogError($"Attempted to release a texture (index={id.index}) that is not allocated.");
                return;
            }

            --info.refCount;
            if (info.refCount == 0)
            {
                if (!info.dynamic)
                    m_TextureToId.Remove(info.texture);

                info.texture = null;
                info.dynamic = false;
                m_FreeIds.Push(id);
            }

            m_Textures[id.index] = info;
        }

        public TextureId TextureToId(Texture texture)
        {
            if (m_TextureToId.TryGetValue(texture, out TextureId id))
                return id;
            return TextureId.invalid;
        }

        #region Debugging

        public struct Statistics
        {
            public int freeIdsCount;
            public int createdIdsCount;
            public int allocatedIdsTotalCount;
            public int allocatedIdsDynamicCount;
            public int allocatedIdsStaticCount;
            public int availableIdsCount;
        }

        public Statistics GatherStatistics()
        {
            var s = new Statistics();
            s.freeIdsCount = m_FreeIds.Count;
            s.createdIdsCount = m_Textures.Count;
            s.allocatedIdsTotalCount = m_Textures.Count - m_FreeIds.Count;
            s.allocatedIdsDynamicCount = s.allocatedIdsTotalCount - m_TextureToId.Count;
            s.allocatedIdsStaticCount = s.allocatedIdsTotalCount - s.allocatedIdsDynamicCount;
            s.availableIdsCount = maxTextures - s.allocatedIdsTotalCount;
            return s;
        }

        #endregion // Debugging
    }
}
