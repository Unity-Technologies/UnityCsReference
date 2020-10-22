using System.Runtime.CompilerServices;

namespace UnityEngine.UIElements.UIR
{
    class TextureSlotManager
    {
        static TextureSlotManager()
        {
            k_SlotCount = UIRenderDevice.shaderModelIs35 ? 8 : 4;
            slotIds = new int[k_SlotCount];
            for (int i = 0; i < k_SlotCount; ++i)
                slotIds[i] = Shader.PropertyToID($"_Texture{i}");
        }

        static readonly int k_SlotCount;

        internal static readonly int[] slotIds;
        internal static readonly int textureTableId = Shader.PropertyToID("_TextureInfo");

        TextureId[] m_Textures;
        int[] m_Tickets;
        int m_CurrentTicket;
        int m_FirstUsedTicket;

        Vector4[] m_GpuTextures; // Contains IDs to be transferred to the GPU.

        public TextureSlotManager()
        {
            m_Textures = new TextureId[k_SlotCount];
            m_Tickets = new int[k_SlotCount];
            m_GpuTextures = new Vector4[k_SlotCount];

            Reset();
        }

        // This must be called before each frame starts rendering.
        public void Reset()
        {
            m_CurrentTicket = 0;
            m_FirstUsedTicket = 0;
            for (int i = 0; i < k_SlotCount; ++i)
            {
                m_Textures[i] = TextureId.invalid;
                m_Tickets[i] = -1;
                m_GpuTextures[i] = new Vector4(-1, 1f, 1f, 0);
            }
        }

        // Mark all textures slots as unused. Does not unbind any texture.
        public void StartNewBatch()
        {
            m_FirstUsedTicket = ++m_CurrentTicket;
            FreeSlots = k_SlotCount;
        }

        // Returns the slot to which the texture is currently bound to.
        public int IndexOf(TextureId id)
        {
            for (int i = 0; i < k_SlotCount; ++i)
                if (m_Textures[i].index == id.index)
                    return i;

            return -1;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void MarkUsed(int slotIndex)
        {
            int oldTicket = m_Tickets[slotIndex];
            if (oldTicket < m_FirstUsedTicket)
                --FreeSlots;
            m_Tickets[slotIndex] = ++m_CurrentTicket;
        }

        // Number of slots that are not required by the current batch.
        public int FreeSlots { get; private set; } = k_SlotCount;

        public int FindOldestSlot()
        {
            int ticket = m_Tickets[0];
            int slot = 0;
            for (int i = 1; i < k_SlotCount; ++i)
            {
                if (m_Tickets[i] < ticket)
                {
                    ticket = m_Tickets[i];
                    slot = i;
                }
            }

            return slot;
        }

        public void Bind(TextureId id, int slot, MaterialPropertyBlock mat)
        {
            Texture tex = textureRegistry.GetTexture(id);
            m_Textures[slot] = id;
            MarkUsed(slot);
            m_GpuTextures[slot] = new Vector4(id.ConvertToGpu(), 1f / tex.width, 1f / tex.height, 0);
            mat.SetTexture(slotIds[slot], tex);
            mat.SetVectorArray(textureTableId, m_GpuTextures);
        }

        // Overridable for tests
        internal TextureRegistry textureRegistry = TextureRegistry.instance;
    }
}
