// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// Custom user data.
    /// The physics system doesn't use this data, it is entirely for custom use.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PhysicsUserData
    {
        /// <summary>
        /// A custom Unity object.
        /// </summary>
        public Object objectValue { readonly get => m_Object; set => m_Object = value; }

        /// <summary>
        /// A custom 64-bit <see cref="LowLevelPhysics2D.PhysicsMask"/>.
        /// </summary>
        public PhysicsMask physicsMaskValue { readonly get => m_PhysicsMask; set => m_PhysicsMask = value; }

        /// <summary>
        /// A custom 32-bit <see cref="System.Single"/>.
        /// </summary>
        public float floatValue { readonly get => m_Float; set => m_Float = value; }

        /// <summary>
        /// A custom 32-bit <see cref="System.Int32"/>.
        /// </summary>
        public int intValue { readonly get => m_Int; set => m_Int = value; }

        /// <summary>
        /// A custom <see cref="System.Boolean"/>.
        /// </summary>
        public bool boolValue { readonly get => m_Bool; set => m_Bool = value; }

        /// <undoc/>
        public override readonly string ToString() => $"object={objectValue}, physicsMask={physicsMaskValue}, float={floatValue}, int={intValue}, bool={boolValue}";

        #region Internal

        [SerializeField] Object m_Object;
        [SerializeField] PhysicsMask m_PhysicsMask;
        [SerializeField] float m_Float;
        [SerializeField] int m_Int;
        [SerializeField] bool m_Bool;

        #endregion
    }
}
