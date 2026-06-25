using System;
using System.Runtime.CompilerServices;

namespace Unity.Scripting
{
    /// <summary>
    /// Internal ScriptingCore Profiling API similar to Unity's Profiling API.
    /// </summary>
    internal static class Profiling
    {
        // Must match the C++ ProfilerCallbacks struct in ScriptingCoreInitialization.h
        public readonly unsafe struct ProfilerCallbacks
        {
            public ProfilerCallbacks(
                delegate* unmanaged[Cdecl]<int, char*, IntPtr> profiler_marker_create,
                delegate* unmanaged[Cdecl]<int, char*, IntPtr> profiler_marker_get_static,
                delegate* unmanaged[Cdecl]<IntPtr, int, char*, void> profiler_marker_begin_with_string,
                delegate* unmanaged[Cdecl]<IntPtr, void> profiler_marker_begin,
                delegate* unmanaged[Cdecl]<IntPtr, void> profiler_marker_end,
                delegate* unmanaged[Cdecl]<int, void> profiler_domain_reload_phase = null)
            {
                this.profiler_marker_create = profiler_marker_create;
                this.profiler_marker_get_static = profiler_marker_get_static;
                this.profiler_marker_begin_with_string = profiler_marker_begin_with_string;
                this.profiler_marker_begin = profiler_marker_begin;
                this.profiler_marker_end = profiler_marker_end;
                this.profiler_domain_reload_phase = profiler_domain_reload_phase;
            }

            public readonly delegate* unmanaged[Cdecl]<int, char*, IntPtr> profiler_marker_create;
            public readonly delegate* unmanaged[Cdecl]<int, char*, IntPtr> profiler_marker_get_static;
            public readonly delegate* unmanaged[Cdecl]<IntPtr, int, char*, void> profiler_marker_begin_with_string;
            public readonly delegate* unmanaged[Cdecl]<IntPtr, void> profiler_marker_begin;
            public readonly delegate* unmanaged[Cdecl]<IntPtr, void> profiler_marker_end;
            public readonly delegate* unmanaged[Cdecl]<int, void> profiler_domain_reload_phase;
        }

        /// <summary>
        /// Initializes profiling by installing callbacks.
        /// </summary>
        public static void Initialize(ProfilerCallbacks callbacks) => profilerCallbacks = callbacks;

        /// <summary>
        /// Copy of the native function pointers that call native profiler api.
        /// </summary>
        private static ProfilerCallbacks profilerCallbacks;

        internal enum DomainReloadPhase
        {
            StartPhase1 = 1,
            EndPhase1 = 2,
            StartPhase2 = 3,
            EndPhase2 = 4,
        }

        public readonly struct DomainReloadPhaseScope : IDisposable
        {
            private readonly DomainReloadPhase _endPhase;

            internal DomainReloadPhaseScope(DomainReloadPhase startPhase, DomainReloadPhase endPhase)
            {
                _endPhase = endPhase;
                EmitPhase(startPhase);
            }

            public void Dispose() => EmitPhase(_endPhase);
        }

        public static DomainReloadPhaseScope DomainReloadPhase1() =>
            new(DomainReloadPhase.StartPhase1, DomainReloadPhase.EndPhase1);

        public static DomainReloadPhaseScope DomainReloadPhase2() =>
            new(DomainReloadPhase.StartPhase2, DomainReloadPhase.EndPhase2);

        private static unsafe void EmitPhase(DomainReloadPhase phase)
        {
            if (profilerCallbacks.profiler_domain_reload_phase != null)
                profilerCallbacks.profiler_domain_reload_phase((int)phase);
        }

        public static unsafe ProfilerMarker GetStaticMarker(string name)
        {
            if (profilerCallbacks.profiler_marker_get_static == null)
                return default;

            fixed (char* p = name)
            {
                IntPtr ptr = profilerCallbacks.profiler_marker_get_static(name.Length, p);
                return new ProfilerMarker(ptr);
            }
        }

        /// <summary>
        /// Struct that defines a code instrumentation scope.
        /// </summary>
        /// <example>
        /// <code>
        /// var marker = Profiling.CreateProfilerMarker("MyCode");
        /// using (marker.Auto())
        /// {
        ///     // Code to profile
        /// }
        /// </code>
        /// </example>
        public readonly unsafe struct ProfilerMarker
        {
            internal readonly IntPtr ptr;

            internal ProfilerMarker(IntPtr existingPtr)
            {
                ptr = existingPtr;
            }

            public ProfilerMarker(string name)
            {
                if (profilerCallbacks.profiler_marker_create == null)
                {
                    ptr = IntPtr.Zero;
                    return;
                }

                fixed (char* p = name)
                    ptr = profilerCallbacks.profiler_marker_create(name.Length, p);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
            public void Begin()
            {
                if (ptr == IntPtr.Zero)
                    return;
                profilerCallbacks.profiler_marker_begin(ptr);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
            public void Begin(string param)
            {
                if (ptr == IntPtr.Zero)
                    return;
                fixed (char* p = param)
                    profilerCallbacks.profiler_marker_begin_with_string(ptr, param.Length, p);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
            public void End()
            {
                if (ptr == IntPtr.Zero)
                    return;
                profilerCallbacks.profiler_marker_end(ptr);
            }

            public readonly struct ProfilerAuto : IDisposable
            {
                private readonly IntPtr ptr;

                [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
                internal ProfilerAuto(IntPtr markerPtr)
                {
                    ptr = markerPtr;
                    if (ptr != IntPtr.Zero)
                        profilerCallbacks.profiler_marker_begin(ptr);
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
                internal ProfilerAuto(IntPtr markerPtr, string param)
                {
                    ptr = markerPtr;
                    if (ptr != IntPtr.Zero)
                    {
                        fixed (char* paramPtr = param)
                            profilerCallbacks.profiler_marker_begin_with_string(ptr, param.Length, paramPtr);
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
                public void Dispose()
                {
                    if (ptr != IntPtr.Zero)
                        profilerCallbacks.profiler_marker_end(ptr);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
            public ProfilerAuto Auto()
            {
                return new ProfilerAuto(ptr);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining | (MethodImplOptions)512)]
            public ProfilerAuto Auto(string param)
            {
                return new ProfilerAuto(ptr, param);
            }
        }
    }
}
