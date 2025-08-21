// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
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

        /// <summary>
        /// Creates from a <see cref="Material"/>.
        /// </summary>
        public MaterialDefinition(Material m)
        {
            m_Material = m;
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

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static MaterialDefinition FromObject(object obj)
        {
            if (obj is MaterialDefinition materialDef)
                return materialDef;

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
            }
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
            return lhs.material == rhs.material;
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

        public override int GetHashCode()
        {
            var hashCode = 851985039;
            // The hash code must remain the same if the underlying object is destroyed and the handle becomes fake-null.
            // Otherwise it would suddenly become impossible to remove the entry from a dictionary.
            if (!ReferenceEquals(material, null))
                hashCode = hashCode * -1521134295 + material.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            if (material != null)
                return material.ToString();

            return "";
        }
    }
}
