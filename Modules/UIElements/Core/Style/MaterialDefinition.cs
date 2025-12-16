// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    enum MaterialPropertyValueType
    {
        Float,
        Vector,
        Color,
        Texture
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    struct MaterialPropertyValue : IEquatable<MaterialPropertyValue>
    {
        public string name;
        public MaterialPropertyValueType type;
        public Vector4 packedValue;
        public Texture textureValue;

        public float GetFloat() { return packedValue.x; }
        public Vector4 GetVector() { return packedValue; }
        public Color GetColor() { return new Color(packedValue.x, packedValue.y, packedValue.z, packedValue.w); }

        public void SetFloat(float v) { packedValue = new Vector4(v, 0, 0, 0); }
        public void SetVector(Vector4 v) { packedValue = v; }
        public void SetColor(Color c) { packedValue = new Vector4(c.r, c.g, c.b, c.a); }


        public override string ToString()
        {
            string result = name + "=";
            switch (type)
            {
                case MaterialPropertyValueType.Float:
                    result += GetFloat().ToString();
                    break;
                case MaterialPropertyValueType.Vector:
                    result += GetVector().ToString();
                    break;
                case MaterialPropertyValueType.Color:
                    result += GetColor().ToString();
                    break;
                case MaterialPropertyValueType.Texture:
                    result += textureValue != null ? textureValue.name : "null";
                    break;
            }
            return result;
        }

        public static bool operator ==(MaterialPropertyValue lhs, MaterialPropertyValue rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(MaterialPropertyValue lhs, MaterialPropertyValue rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override bool Equals(object obj)
        {
            if (obj is MaterialPropertyValue other)
            {
                return Equals(other);
            }
            return false;
        }

        public bool Equals(MaterialPropertyValue other)
        {
            if (other.name != name || other.type != type)
                return false;
            switch (type)
            {
                case MaterialPropertyValueType.Float:
                case MaterialPropertyValueType.Vector:
                case MaterialPropertyValueType.Color:
                    return other.packedValue == packedValue;
                case MaterialPropertyValueType.Texture:
                    return other.textureValue == textureValue;
            }
            return false;
        }

        public override int GetHashCode()
        {
            var hashCode = 1861411795;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(name);
            hashCode = hashCode * -1521134295 + type.GetHashCode();
            hashCode = hashCode * -1521134295 + packedValue.GetHashCode();
            if (textureValue != null)
                hashCode = hashCode * -1521134295 + textureValue.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>
    /// Describes a <see cref="VisualElement"/> material.
    /// </summary>
    [Serializable]
    public partial struct MaterialDefinition : IEquatable<MaterialDefinition>
    {
        [SerializeField]
        Material m_Material;

        /// <summary>
        /// The material to use to render the element.
        /// </summary>
        public Material material
        {
            get { return m_Material; }
            set { m_Material = value; }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [SerializeField]
        internal List<MaterialPropertyValue> propertyValues = null;

        /// <summary>
        /// Creates from a <see cref="Material"/>.
        /// </summary>
        public MaterialDefinition(Material m)
        {
            m_Material = m;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal MaterialDefinition(Material m, List<MaterialPropertyValue> propertyValues)
        {
            m_Material = m;
            this.propertyValues = propertyValues;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal MaterialDefinition(MaterialDefinition other)
        {
            m_Material = other.m_Material;
            if (other.propertyValues != null)
                propertyValues = new List<MaterialPropertyValue>(other.propertyValues);
            else
                propertyValues = null;
        }

        MaterialPropertyValue GetValue(string name)
        {
            if (propertyValues != null)
            {
                int propIndex = propertyValues.FindIndex(p => p.name == name);
                if (propIndex >= 0)
                    return propertyValues[propIndex];
            }
            return default;
        }

        void SetValue(MaterialPropertyValue prop)
        {
            if (propertyValues == null)
                propertyValues = new List<MaterialPropertyValue>();
            int propIndex = propertyValues.FindIndex(p => p.name == prop.name);
            if (propIndex >= 0)
                propertyValues[propIndex] = prop;
            else
                propertyValues.Add(prop);
        }

        /// <summary>
        /// Gets a float property value by name.
        /// </summary>
        /// <param name="name">The property name of the float value.</param>
        /// <returns>The float property stored for the specified name, or the empty string if none.</returns>
        public float GetFloat(string name)
        {
            return GetValue(name).GetFloat();
        }

        /// <summary>
        /// Gets a vector property value by name.
        /// </summary>
        /// <param name="name">The property name of the vector value.</param>
        /// <returns>The vector property stored for the specified name, or the zero vector if none.</returns>
        public Vector4 GetVector(string name)
        {
            return GetValue(name).GetVector();
        }

        /// <summary>
        /// Gets a color property value by name.
        /// </summary>
        /// <param name="name">The property name of the color value.</param>
        /// <returns>The color property stored for the specified name, or the clear color if none.</returns>
        public Color GetColor(string name)
        {
            return GetValue(name).GetColor();
        }

        /// <summary>
        /// Gets a texture property value by name.
        /// </summary>
        /// <param name="name">The property name of the texture value.</param>
        /// <returns>The texture property stored for the specified name, or null if none.</returns>
        public Texture GetTexture(string name)
        {
            return GetValue(name).textureValue;
        }

        /// <summary>
        /// Sets a float property value by name.
        /// </summary>
        /// <param name="name">The property name of the float value.</param>
        /// <param name="value">The float value to set.</param>
        public void SetFloat(string name, float value)
        {
            var prop = new MaterialPropertyValue
            {
                name = name,
                type = MaterialPropertyValueType.Float
            };
            prop.SetFloat(value);
            SetValue(prop);
        }

        /// <summary>
        /// Sets a vector property value by name.
        /// </summary>
        /// <param name="name">The property name of the vector value.</param>
        /// <param name="value">The vector value to set.</param>
        public void SetVector(string name, Vector4 value)
        {
            var prop = new MaterialPropertyValue
            {
                name = name,
                type = MaterialPropertyValueType.Vector
            };
            prop.SetVector(value);
            SetValue(prop);
        }

        /// <summary>
        /// Sets a color property value by name.
        /// </summary>
        /// <param name="name">The property name of the color value.</param>
        /// <param name="value">The color value to set.</param>
        public void SetColor(string name, Color value)
        {
            var prop = new MaterialPropertyValue
            {
                name = name,
                type = MaterialPropertyValueType.Color
            };
            prop.SetColor(value);
            SetValue(prop);
        }

        /// <summary>
        /// Sets a texture property value by name.
        /// </summary>
        /// <param name="name">The property name of the texture value.</param>
        /// <param name="value">The texture value to set.</param>
        public void SetTexture(string name, Texture value)
        {
            SetValue(new MaterialPropertyValue
            {
                name = name,
                type = MaterialPropertyValueType.Texture,
                textureValue = value
            });
        }

        /// <summary>
        /// Creates a material definition from a <see cref="Material"/>.
        /// </summary>
        /// <param name="m">The material to use.</param>
        /// <returns>A new material definition object.</returns>
        public static MaterialDefinition FromMaterial(Material m)
        {
            return new MaterialDefinition { material = m };
        }

        internal static MaterialDefinition FromObject(Object obj)
        {
            var material = obj as Material;
            if (material != null)
                return FromMaterial(material);

            return default;
        }

        internal static IEnumerable<Type> allowedAssetTypes
        {
            get
            {
                yield return typeof(Material);
                yield return typeof(Texture2D); // Allow Texture2D for property values
            }
        }

        internal MaterialPropertyBlock BuildPropertyBlock()
        {
            if (propertyValues == null || propertyValues.Count == 0)
                return null;

            var mpb = new MaterialPropertyBlock();
            foreach (var value in propertyValues)
            {
                switch (value.type)
                {
                    case MaterialPropertyValueType.Float:
                        mpb.SetFloat(value.name, value.GetFloat());
                        break;
                    case MaterialPropertyValueType.Vector:
                        mpb.SetVector(value.name, value.GetVector());
                        break;
                    case MaterialPropertyValueType.Color:
                        mpb.SetColor(value.name, value.GetColor());
                        break;
                    case MaterialPropertyValueType.Texture:
                        if (value.textureValue != null)
                            mpb.SetTexture(value.name, value.textureValue);
                        break;
                }
            }
            return mpb;
        }

        /// <summary>
        /// Help verify whether an asset has been assigned or not.
        /// </summary>
        /// <returns>True if no asset is assigned.</returns>
        public bool IsEmpty()
        {
            return material == null;
        }

        /// <undoc/>
        public static bool operator==(MaterialDefinition lhs, MaterialDefinition rhs)
        {
            bool sameMat = lhs.material == rhs.material;
            if (!sameMat)
                return false;

            bool lhsHasValues = lhs.propertyValues != null && lhs.propertyValues.Count > 0;
            bool rhsHasValues = rhs.propertyValues != null && rhs.propertyValues.Count > 0;

            if (lhsHasValues != rhsHasValues)
                return false;

            if (!lhsHasValues)
                return true;

            if (lhs.propertyValues.Count != rhs.propertyValues.Count)
                return false;

            for (int i = 0; i < lhs.propertyValues.Count; i++)
            {
                var l = lhs.propertyValues[i];
                var r = rhs.propertyValues[i];
                if (l != r)
                    return false;
            }

            return true;
        }

        /// <undoc/>
        public static bool operator!=(MaterialDefinition lhs, MaterialDefinition rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public static implicit operator MaterialDefinition(Material m)
        {
            return FromMaterial(m);
        }

        /// <undoc/>
        public bool Equals(MaterialDefinition other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is MaterialDefinition))
            {
                return false;
            }

            var v = (MaterialDefinition)obj;
            return v == this;
        }

        /// <undoc/>
        public override int GetHashCode()
        {
            var hashCode = 851985039;
            // The hash code must remain the same if the underlying object is destroyed and the handle becomes fake-null.
            // Otherwise it would suddenly become impossible to remove the entry from a dictionary.
            if (!ReferenceEquals(material, null))
                hashCode = hashCode * -1521134295 + material.GetHashCode();

            if (propertyValues != null)
            {
                foreach (var v in propertyValues)
                    hashCode = hashCode * -1521134295 + v.GetHashCode();
            }

            return hashCode;
        }

        /// <undoc/>
        public override string ToString()
        {
            string result = "null";
            if (material != null)
            {
                result = material.name;
                if (propertyValues != null && propertyValues.Count > 0)
                {
                    result += " { ";
                    for (int i = 0; i < propertyValues.Count; i++)
                        result += propertyValues[i].ToString() + " ";
                    result += "}";
                }
            }
            return result;
        }
    }
}
