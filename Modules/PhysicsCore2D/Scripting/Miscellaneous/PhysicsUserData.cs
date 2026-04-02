// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// Custom user data.
    /// The physics system doesn't use this data, it is entirely for custom use.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public struct PhysicsUserData
    {
        /// <summary>
        /// A custom Unity object.
        /// To get the <see cref="EntityId"/> of the object, use <see cref="PhysicsUserData.objectValueId"/>.
        /// </summary>
        public UnityEngine.Object objectValue { readonly get => PhysicsGlobal_GetObject(m_EntityId); set => m_EntityId = value != null ? value.GetEntityId() : EntityId.None; }

        /// <summary>
        /// The EntityId of a Unity object.
        /// This is the object referred to with <see cref="PhysicsUserData.objectValue"/>
        /// </summary>
        public readonly EntityId objectValueId => m_EntityId;

        /// <summary>
        /// A custom 64-bit <see cref="PhysicsMask"/>.
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
        /// A custom 64-bit <see cref="System.Int64"/>.
        /// </summary>
        public UInt64 int64Value { readonly get => m_Int64; set => m_Int64 = value; }

        /// <summary>
        /// A custom <see cref="System.Boolean"/>.
        /// </summary>
        public bool boolValue { readonly get => m_Bool; set => m_Bool = value; }

        /// <undoc/>
        public override readonly string ToString() => $"object={objectValue}, physicsMask={physicsMaskValue}, float={floatValue}, int={intValue}, int64={int64Value}, bool={boolValue}";

        #region Internal

        [SerializeField] internal EntityId m_EntityId;
        [SerializeField] internal PhysicsMask m_PhysicsMask;
        [SerializeField] internal float m_Float;
        [SerializeField] internal int m_Int;
        [SerializeField] internal UInt64 m_Int64;
        [SerializeField] internal bool m_Bool;

        #endregion
    }
}
