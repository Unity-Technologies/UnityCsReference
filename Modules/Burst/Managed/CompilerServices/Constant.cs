// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Burst.CompilerServices
{
    /// <summary>
    /// Compile-time queries intrinsics.
    /// </summary>
    public static class Constant
    {
        /// <summary>
        /// Performs a compile-time check on whether the provided argument is known to be constant by Burst.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t">The value to check whether it is constant.</param>
        /// <returns>True if Burst knows at compile-time that it is a constant, false otherwise.</returns>
        public static bool IsConstantExpression<T>(T t) where T : unmanaged => false;

        /// <summary>
        /// Performs a compile-time check on whether the provided argument is known to be constant by Burst.
        /// </summary>
        /// <param name="t">The value to check whether it is constant.</param>
        /// <returns>True if Burst knows at compile-time that it is a constant, false otherwise.</returns>
        public static unsafe bool IsConstantExpression(void* t) => false;
    }
}
