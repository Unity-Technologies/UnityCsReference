// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Entities
{
    /// <summary>
    /// An interface for shared component types to inherit from. Whenever
    /// a IRefCounted shared component is added to a world, its Retain() method will be invoked. Similarly,
    /// when removed from a world, its Release() method will be invoked. This interface can be used to safely manage
    /// the lifetime of a shared component whose instance data is shared between multiple worlds.
    /// </summary>
    public interface IRefCounted
    {
        /// <summary>
        /// Delegate method used for invoking Retain() and Release() member functions from Burst compiled code.
        /// </summary>
        public delegate void RefCountDelegate(IntPtr _this);

        /// <summary>
        /// Called when a world has a new instance of a IRefCounted type added to it.
        /// </summary>
        void Retain();

        /// <summary>
        /// Called when a world has the last instance of a IRefCounted type removed from it.
        /// </summary>
        void Release();
    }
}
