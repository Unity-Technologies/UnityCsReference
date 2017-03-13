// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // match MonoMaterialProperty layout!
    [StructLayout(LayoutKind.Sequential)]
    public sealed class MaterialProperty
    {
        public enum PropType
        {
            Color,
            Vector,
            Float,
            Range,
            Texture,
        }

        [Obsolete("Use UnityEngine.Rendering.TextureDimension instead", false)]
        public enum TexDim
        {
            Unknown = -1,
            None = 0,
            Tex2D = 2,
            Tex3D = 3,
            Cube = 4,
            Any = 6,
        }

        [Flags]
        public enum PropFlags
        {
            None = 0,
            HideInInspector = (1 << 0),
            PerRendererData = (1 << 1),
            NoScaleOffset = (1 << 2),
            Normal = (1 << 3),
            HDR = (1 << 4),
        }
        public delegate bool ApplyPropertyCallback(MaterialProperty prop, int changeMask, object previousValue);

        private Object[] m_Targets;
        private ApplyPropertyCallback m_ApplyPropertyCallback;
        private string m_Name;
        private string m_DisplayName;
        private System.Object m_Value;
        private Vector4 m_TextureScaleAndOffset;
        private Vector2 m_RangeLimits;
        private PropType m_Type;
        private PropFlags m_Flags;
        private UnityEngine.Rendering.TextureDimension m_TextureDimension;
        private int m_MixedValueMask;


        public Object[] targets { get { return m_Targets; } }
        public PropType type { get { return m_Type; } }
        public string name { get { return m_Name; } }
        public string displayName { get { return m_DisplayName; } }
        public PropFlags flags { get { return m_Flags; } }
        public UnityEngine.Rendering.TextureDimension textureDimension { get { return m_TextureDimension; } }
        public Vector2 rangeLimits { get { return m_RangeLimits; } }
        public bool hasMixedValue { get { return (m_MixedValueMask & 1) != 0; } }
        public ApplyPropertyCallback applyPropertyCallback { get { return m_ApplyPropertyCallback; }  set { m_ApplyPropertyCallback = value; } }

        // Textures have 5 different mixed values for texture + UV scale/offset
        internal int mixedValueMask { get { return m_MixedValueMask; } }

        public void ReadFromMaterialPropertyBlock(MaterialPropertyBlock block)
        {
            ShaderUtil.ApplyMaterialPropertyBlockToMaterialProperty(block, this);
        }

        public void WriteToMaterialPropertyBlock(MaterialPropertyBlock materialblock, int changedPropertyMask)
        {
            ShaderUtil.ApplyMaterialPropertyToMaterialPropertyBlock(this, changedPropertyMask, materialblock);
        }

        public Color colorValue
        {
            get
            {
                if (m_Type == PropType.Color)
                    return (Color)m_Value;
                return Color.black;
            }
            set
            {
                if (m_Type != PropType.Color)
                    return;
                if (!hasMixedValue && value == (Color)m_Value)
                    return;

                ApplyProperty(value);
            }
        }

        public Vector4 vectorValue
        {
            get
            {
                if (m_Type == PropType.Vector)
                    return (Vector4)m_Value;
                return Vector4.zero;
            }
            set
            {
                if (m_Type != PropType.Vector)
                    return;
                if (!hasMixedValue && value == (Vector4)m_Value)
                    return;

                ApplyProperty(value);
            }
        }

        internal static bool IsTextureOffsetAndScaleChangedMask(int changedMask)
        {
            changedMask >>= 1;
            return changedMask != 0;
        }

        public float floatValue
        {
            get
            {
                if (m_Type == PropType.Float || m_Type == PropType.Range)
                    return (float)m_Value;
                return 0.0f;
            }
            set
            {
                if (m_Type != PropType.Float && m_Type != PropType.Range)
                    return;
                if (!hasMixedValue && value == (float)m_Value)
                    return;

                ApplyProperty(value);
            }
        }

        public Texture textureValue
        {
            get
            {
                if (m_Type == PropType.Texture)
                    return (Texture)m_Value;
                return null;
            }
            set
            {
                if (m_Type != PropType.Texture)
                    return;
                if (!hasMixedValue && value == (Texture)m_Value)
                    return;

                m_MixedValueMask &= ~1;
                object previousValue = m_Value;
                m_Value = value;

                ApplyProperty(previousValue, 1);
            }
        }

        public Vector4 textureScaleAndOffset
        {
            get
            {
                if (m_Type == PropType.Texture)
                    return m_TextureScaleAndOffset;
                return Vector4.zero;
            }
            set
            {
                if (m_Type != PropType.Texture)
                    return;
                if (!hasMixedValue && value == m_TextureScaleAndOffset)
                    return;

                m_MixedValueMask &= 1;
                int changedMask = 0;
                for (int c = 1; c < 5; c++)
                    changedMask |= 1 << c;

                object previousValue = m_TextureScaleAndOffset;
                m_TextureScaleAndOffset = value;
                ApplyProperty(previousValue, changedMask);
            }
        }

        private void ApplyProperty(object newValue)
        {
            m_MixedValueMask = 0;
            object previousValue = m_Value;
            m_Value = newValue;
            ApplyProperty(previousValue, 1);
        }

        private void ApplyProperty(object previousValue, int changedPropertyMask)
        {
            if (targets == null || targets.Length == 0)
                throw new ArgumentException("No material targets provided");

            Object[] mats = targets;
            string targetTitle;
            if (mats.Length == 1)
                targetTitle = mats[0].name;
            else
                targetTitle = mats.Length + " " + ObjectNames.NicifyVariableName(ObjectNames.GetClassName(mats[0])) + "s";

            //@TODO: Maybe all this logic should be moved to C++
            // reduces api surface...
            bool didApply = false;
            if (m_ApplyPropertyCallback != null)
                didApply = m_ApplyPropertyCallback(this, changedPropertyMask, previousValue);

            if (!didApply)
                ShaderUtil.ApplyProperty(this, changedPropertyMask, "Modify " + displayName + " of " + targetTitle);
        }
    }
} // namespace UnityEngine.Rendering
