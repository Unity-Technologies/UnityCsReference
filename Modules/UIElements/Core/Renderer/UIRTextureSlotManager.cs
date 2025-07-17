// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

namespace UnityEngine.UIElements.UIR
{
    partial class TextureSlotManager
    {
        static TextureSlotManager()
        {
            k_MaxSlotCount = 8;
            slotIds = new int[k_MaxSlotCount];
            for (int i = 0; i < k_MaxSlotCount; ++i)
                slotIds[i] = Shader.PropertyToID($"_Texture{i}");
        }

        internal static readonly int k_MaxSlotCount;
        internal static readonly int k_SlotSize = 2; // Number of float4 per slot
        internal static int[] slotIds;
        internal static readonly int textureTableId = Shader.PropertyToID("_TextureInfo");

        TextureId[] m_Textures;
        int[] m_LastUseTime;
        int m_CurrentTime;
        int m_BatchTime;

        Vector4[] m_GpuTextures; // Contains IDs to be transferred to the GPU.

        int m_SlotCount;

        public TextureSlotManager()
        {
            m_Textures = new TextureId[k_MaxSlotCount];
            m_LastUseTime = new int[k_MaxSlotCount];
            m_GpuTextures = new Vector4[k_MaxSlotCount * k_SlotSize];
            m_SlotCount = k_MaxSlotCount;
            FreeSlots = k_MaxSlotCount;

            Reset();
        }

        // This must be called before each frame starts rendering.
        public void Reset()
        {
            m_CurrentTime = 0;
            m_BatchTime = 0;
            Unbind(0, k_MaxSlotCount);
        }

        void Unbind(int first, int count = 1)
        {
            for (int i = first; i < first + count; ++i)
            {
                m_Textures[i] = TextureId.invalid;
                m_LastUseTime[i] = -1;
                SetGpuData(i, TextureId.invalid, 1, 1, 0, 0, false);
            }
        }

        // Mark all textures slots as unused. Does not unbind any texture unless the texture slot count decreases.
        public void StartNewBatch(int slotCount)
        {
            Debug.Assert(slotCount >= 0 && slotCount <= k_MaxSlotCount, "Invalid texture slot count");

            if (slotCount < m_SlotCount)
                Unbind(slotCount, m_SlotCount - slotCount);

            m_BatchTime = ++m_CurrentTime;
            m_SlotCount = slotCount;
            FreeSlots = slotCount;
        }

        // Returns the slot to which the texture is currently bound to.
        public int IndexOf(TextureId id)
        {
            for (int i = 0; i < m_SlotCount; ++i)
                if (m_Textures[i].index == id.index)
                    return i;

            return -1;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void MarkUsed(int slotIndex)
        {
            Debug.Assert(slotIndex >= 0 && slotIndex < m_SlotCount, "Invalid texture slot");
            Debug.Assert(m_Textures[slotIndex] != TextureId.invalid, "Texture slot is not bound to a texture");

            int oldTime = m_LastUseTime[slotIndex];
            if (oldTime < m_BatchTime)
                --FreeSlots;
            m_LastUseTime[slotIndex] = ++m_CurrentTime;
        }

        // Number of slots that are not required by the current batch.
        public int FreeSlots { get; private set; }

        public int FindOldestSlot()
        {
            int oldestTime = m_LastUseTime[0];
            int slot = 0;
            for (int i = 1; i < m_SlotCount; ++i)
            {
                if (m_LastUseTime[i] < oldestTime)
                {
                    oldestTime = m_LastUseTime[i];
                    slot = i;
                }
            }

            return slot;
        }

        public void Bind(TextureId id, float sdfScale, float sharpness, bool isPremultiplied, int slot, MaterialPropertyBlock mat, CommandList commandList = null)
        {
            Texture tex = textureRegistry.GetTexture(id);
            if (tex == null) // Case 1364578: Texture may have been destroyed
                tex = Texture2D.whiteTexture;

            m_Textures[slot] = id;
            MarkUsed(slot);
            SetGpuData(slot, id, tex.width, tex.height, sdfScale, sharpness, isPremultiplied);
            if (commandList == null)
            {
                mat.SetTexture(slotIds[slot], tex);
                mat.SetVectorArray(textureTableId, m_GpuTextures);
            }
            else
            {
                int offset = slot * k_SlotSize;
                commandList.SetTexture(slotIds[slot], tex, offset, m_GpuTextures[offset], m_GpuTextures[offset+1]);
            }
        }

        public void SetGpuData(int slotIndex, TextureId id, int textureWidth, int textureHeight, float sdfScale, float sharpness, bool isPremultiplied)
        {
            int offset = slotIndex * k_SlotSize;
            float texelWidth = 1f / textureWidth;
            float texelHeight = 1f / textureHeight;
            m_GpuTextures[offset + 0] = new Vector4(id.ConvertToGpu(), texelWidth, texelHeight, sdfScale);
            m_GpuTextures[offset + 1] = new Vector4(textureWidth, textureHeight, sharpness, isPremultiplied ? 1.0f : 0.0f);
        }

        // Overridable for tests
        internal TextureRegistry textureRegistry = TextureRegistry.instance;
    }
}
