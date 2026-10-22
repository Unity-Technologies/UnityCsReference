// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    internal delegate GCHandle CreateTestWorldDelegate(bool isEditMode);
    internal delegate void PopCurrentlyActiveWorldDelegate(GCHandle world);
    internal delegate void DisposeWorldDelegate(GCHandle world);

    /// <summary>
    /// World lifecycle entry points into Unity.Entities for engine-side code that cannot reference the
    /// package directly, notably the test framework. Unity.Entities installs the delegates from an
    /// <see cref="OnCodeLoadedAttribute"/> handler; every member here no-ops when the package is absent.
    /// </summary>
    /// <remarks>
    /// <see cref="CreateTestWorld"/> returns a <see cref="GCHandle"/> that the <b>caller owns and must
    /// <see cref="GCHandle.Free"/></b>. Handing one to <see cref="DisposeWorld"/> or
    /// <see cref="PopCurrentlyActiveWorld"/> does not free the handle.
    /// </remarks>
    // partial: the AutoStaticsCleanupOnCodeReload source generator emits the other half.
    internal static partial class EntitiesWorldHooks
    {
        [AutoStaticsCleanupOnCodeReload]
        internal static CreateTestWorldDelegate CreateTestWorldFn;
        [AutoStaticsCleanupOnCodeReload]
        internal static PopCurrentlyActiveWorldDelegate PopCurrentlyActiveWorldFn;
        [AutoStaticsCleanupOnCodeReload]
        internal static DisposeWorldDelegate DisposeWorldFn;

        /// <summary>
        /// Creates a World for a test fixture and installs it as the default one, displacing any current
        /// default. Returns null when Unity.Entities is not present.
        /// </summary>
        internal static GCHandle? CreateTestWorld(bool isEditMode)
        {
            return CreateTestWorldFn?.Invoke(isEditMode);
        }

        /// <summary>
        /// Unwinds <paramref name="world"/> from the currently active World stack. Does nothing if it is no
        /// longer on the stack, which happens when a play mode transition tore it down first.
        /// </summary>
        internal static void PopCurrentlyActiveWorld(GCHandle world)
        {
            PopCurrentlyActiveWorldFn?.Invoke(world);
        }

        /// <summary>
        /// Removes the World from the player loop and disposes it. Does not free <paramref name="world"/>.
        /// </summary>
        internal static void DisposeWorld(GCHandle world)
        {
            DisposeWorldFn?.Invoke(world);
        }
    }
}
