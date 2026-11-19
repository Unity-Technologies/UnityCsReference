// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using Unity.Entities;

namespace UnityEngine
{
    /// <summary>
    /// World lifecycle entry points into Unity.Entities for engine-side code that cannot reference the
    /// package directly, notably the test framework.
    /// </summary>
    /// <remarks>
    /// <see cref="CreateTestWorld"/> returns a <see cref="GCHandle"/> that the <b>caller owns and must
    /// <see cref="GCHandle.Free"/></b>. Handing one to <see cref="DisposeWorld"/> or
    /// <see cref="PopCurrentlyActiveWorld"/> does not free the handle.
    /// </remarks>
    internal static class EntitiesWorldHooks
    {
        /// <summary>
        /// Creates a World for a test fixture and installs it as the default one, displacing any current
        /// default.
        /// </summary>
        internal static GCHandle CreateTestWorld(bool isEditMode)
        {
            return DefaultWorldInitialization.CreateTestWorld(isEditMode);
        }

        /// <summary>
        /// Unwinds <paramref name="world"/> from the currently active World stack. Does nothing if it is no
        /// longer on the stack, which happens when a play mode transition tore it down first.
        /// </summary>
        internal static void PopCurrentlyActiveWorld(GCHandle world)
        {
            DefaultWorldInitialization.PopCurrentlyActiveWorld(world);
        }

        /// <summary>
        /// Removes the World from the player loop and disposes it. Does not free <paramref name="world"/>.
        /// </summary>
        internal static void DisposeWorld(GCHandle world)
        {
            DefaultWorldInitialization.DisposeWorld(world);
        }
    }
}
