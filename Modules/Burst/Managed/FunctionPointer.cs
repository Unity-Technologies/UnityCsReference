// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unity.Burst
{
    /// <summary>
    /// Base interface for a function pointer.
    /// </summary>
    public interface IFunctionPointer
    {
        /// <summary>
        /// Converts a pointer to a function pointer.
        /// </summary>
        /// <param name="ptr">The native pointer.</param>
        /// <returns>An instance of this interface.</returns>
        [Obsolete("This method will be removed in a future version of Burst")]
        IFunctionPointer FromIntPtr(IntPtr ptr);
    }

    /// <summary>
    /// A function pointer that can be used from a Burst Job or from regular C#.
    /// It needs to be compiled through <see cref="BurstCompiler.CompileFunctionPointer{T}"/>
    /// </summary>
    /// <typeparam name="T">Type of the delegate of this function pointer</typeparam>
    public readonly struct FunctionPointer<T> : IFunctionPointer
    {
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
        private readonly IntPtr _ptr;

        /// <summary>
        /// Creates a new instance of this function pointer with the following native pointer.
        /// </summary>
        /// <param name="ptr"></param>
        public FunctionPointer(IntPtr ptr)
        {
            _ptr = ptr;
        }

        /// <summary>
        /// Gets the underlying pointer.
        /// </summary>
        public IntPtr Value => _ptr;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckIsCreated()
        {
            if (!IsCreated)
            {
                throw new NullReferenceException("Object reference not set to an instance of an object");
            }
        }

        /// <summary>
        /// Gets the delegate associated to this function pointer in order to call the function pointer.
        /// This delegate can be called from a Burst Job or from regular C#.
        /// If calling from regular C#, it is recommended to cache the returned delegate of this property
        /// instead of using this property every time you need to call the delegate.
        /// </summary>
        public T Invoke
        {
            get
            {
                CheckIsCreated();
                return Marshal.GetDelegateForFunctionPointer<T>(_ptr);
            }
        }

        /// <summary>
        /// Whether the function pointer is valid.
        /// </summary>
        public bool IsCreated => _ptr != IntPtr.Zero;

		/// <summary>
        /// Converts a pointer to a function pointer.
        /// </summary>
        /// <param name="ptr">The native pointer.</param>
        /// <returns>An instance of this interface.</returns>
        IFunctionPointer IFunctionPointer.FromIntPtr(IntPtr ptr) => new FunctionPointer<T>(ptr);
    }
}
