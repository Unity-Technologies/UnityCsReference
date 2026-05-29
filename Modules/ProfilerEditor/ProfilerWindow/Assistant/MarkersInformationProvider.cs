// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;

namespace UnityEditor.Profiling
{
    /// <summary>
    /// Provides marker information for profiler and assistant packages
    /// </summary>
    internal static partial class MarkersInformationProvider
    {
        private static readonly Dictionary<string, string> k_MarkersInfo = new Dictionary<string, string>
        {
            { "WaitForJobGroupID", "CPU thread is waiting for a specific Job System job group to complete. High values indicate either job dependency chains that are too long, insufficient parallelization, or jobs that take too long to execute. Check job scheduling and consider breaking down complex jobs into smaller parallel tasks." },
            { "TimeUpdate.WaitForLastPresentationAndUpdateTime", "Main thread is waiting for GPU presentation or VSync. If accompanied by a related sample 'Gfx.WaitForGfxCommandsFromMainThread' on 'Render Thread', this indicates VSync limiting. Otherwise, check GPU-bound operations like complex shaders, high resolution rendering, or excessive draw calls." },
            { "Gfx.WaitForPresent", "CPU waiting for GPU to finish presenting the current frame. This is a GPU bottleneck indicator. Investigate: overdraw, shader complexity, texture resolution, post-processing effects, or insufficient GPU memory bandwidth." },
            { "WaitForTargetFPS", "Application is artificially limited by Application.targetFrameRate or VSync. This is intentional frame rate capping. If unwanted, disable VSync (QualitySettings.vSyncCount = 0) and set Application.targetFrameRate = -1." },
            { "Mono.JIT", "Time spent in Just-In-Time compilation of C# code. High values on first execution are normal, but repeated JIT during gameplay indicates missing AOT compilation or dynamic code generation. Consider IL2CPP builds for production." },
            { "EditorLoop", "Unity Editor overhead - not present in builds. Check child samples, if any child sample exists, then this is an Editor profiling capture and further sample details should be investigated. Otherwise ignore for runtime performance analysis. Consider using Development builds for more accurate performance data." },
            { "EnterPlayMode", "Entering PlayMode in the Unity Editor. Does code state reset and scene reset to the state similar to how the scene would be loaded in the Player." },
            { "Application.Reload", "Corresponds to the assemblies reload activity in Unity Editor followed by an asset refresh. If called under 'EnterPlayMode' sample check whether or not 'Domain Reload Disabled' setting is enabled. For the fast iteration we recommend to disable Domain Reload when entering Play Mode." },
            { "Physics.SyncTransforms", "Synchronizing Transform changes with Physics system. Caused by: moving static Colliders, excessive Rigidbody.MovePosition calls, or Transform modifications on physics objects. Keep static colliders truly static and use Rigidbody methods for physics movement." },
            { "Physics.Simulate", "Core physics simulation step. High cost caused by: too many active Rigidbodies, complex Mesh Colliders, high solver iteration counts, or small Fixed Timestep. Optimize by using primitive colliders, reducing active physics objects, or adjusting physics settings." },
            { "GC.Alloc", "Managed memory allocation that will trigger garbage collection. Critical performance marker - each allocation creates GC pressure. Common sources: string concatenation, LINQ, boxing, temporary arrays, or Unity API calls that return new objects. Minimize allocations in Update loops." },
            { "MonoUnityDomainUnload", "Managed domain unloading during scene transitions or application shutdown. Check related samples on 'Domain Unloader' and 'Finalizer' threads for blocking operations. High values may indicate objects not being properly disposed or excessive finalizers running." },
            { "Initialize Graphics", "Graphics system initialization - typically occurs once at startup. In runtime, may indicate graphics device reset or context recreation, which suggests graphics driver issues or memory pressure." },
            { "Camera.Render", "Complete camera rendering pipeline including culling, rendering, and post-processing. High values indicate: too many objects in view, complex shaders, excessive draw calls, or expensive post-processing. Check Camera rendering statistics and optimize visible geometry." },
            { "RenderCameraStack", "Having multiple camera stacks may impact performance." },
            { "Culling", "Frustum and occlusion culling to determine visible objects. CPU-bound operation that scales with total object count. Optimize by: reducing total GameObjects, using LOD groups, implementing custom culling, or spatial partitioning techniques." },
            { "Render.Mesh", "GPU mesh rendering commands. High values with low GPU usage indicates CPU-bound draw call submission. Optimize with: batching, GPU instancing, mesh combining, or reducing material variants to minimize state changes." },
            { "Shadows.Render", "Shadow map rendering for shadow-casting lights. Expensive operation scaling with light count and shadow resolution. Optimize by: reducing shadow distance, lowering shadow resolution, using shadow cascades efficiently, or limiting shadow-casting objects." },
            { "UI.Layout", "Canvas layout recalculation for UI elements. Triggered by changing RectTransform properties. High values indicate frequent UI updates. Optimize by: minimizing dynamic UI changes, using Canvas groups, splitting UI across multiple Canvas, or caching layout calculations." },
            { "UI.Canvas.BuildBatch", "Building UI rendering batches and geometry. Occurs when Canvas elements change. Reduce by: minimizing UI animator, avoiding frequent text changes, using object pooling for UI elements, or separating static and dynamic UI elements." },
            { "Animation.Update", "Animator component updates including state machine evaluation and blend tree calculations. Scales with active Animators and animation complexity. Optimize by: culling distant animators, reducing animation layers, or using simpler animation techniques." },
            { "ParticleSystem.Update", "Particle system simulation including emission, physics, and collision calculations. CPU-intensive for complex particle effects. Optimize by: reducing particle counts, simplifying collision detection, using GPU particles, or culling distant effects." },
            { "Loading.ReadObject", "Asset loading and deserialization from disk or memory. May indicate streaming issues or synchronous asset loading during gameplay. Use async loading methods and preload critical assets to avoid runtime hitches." },
            { "ScriptableRenderPipeline.Render", "Custom render pipeline execution (URP/HDRP). Encompasses entire rendering process including culling, lighting, and post-processing. Investigate specific render passes and features enabled in the pipeline asset." },
            { "DelayedCallManager.Update", "Processing delayed callbacks and coroutines. High values indicate excessive use of Invoke(), InvokeRepeating(), or complex coroutine chains. Consider using Update-based timers or job system for better performance." },
            { "PlayerLoop", "Complete Unity player loop including all Update calls, physics, and rendering. This is the total frame time. Investigate child samples to identify specific bottlenecks within the frame." },
            { "ScriptRunBehaviour.Update", "All MonoBehaviour Update() method calls combined. High values indicate expensive Update logic. Optimize by: reducing Update frequency, caching expensive calculations, using object pooling, or moving logic to FixedUpdate/LateUpdate as appropriate." },
            { "BehaviourUpdate", "Execution of MonoBehaviour lifecycle methods (Update, FixedUpdate, LateUpdate). Break down by specific scripts to identify performance hotspots. Consider alternatives like job system or ECS for performance-critical code." },
            { "PreLateUpdate.ScriptRunBehaviourLateUpdate", "All LateUpdate() calls after regular Update. Used for camera following and final position adjustments. Expensive LateUpdate calls can delay rendering start and impact frame rate." },
            { "PostLateUpdate.FinishFrameRendering", "Final frame presentation and cleanup. Includes GPU synchronization and buffer swaps. Extended times may indicate GPU driver overhead or graphics API inefficiencies." }
        };

        /// <summary>
        /// Retrieves information about a specific profiler marker.
        /// </summary>
        public static string GetMarkerInfo(string markerName)
        {
            var info = GetMarkerInfoRaw(markerName);
            if (info != null)
                return info;

            // ProfilingScope in SRP and URP/HDRP generates "inline" (main thread)
            // markers with "Inl_" prefix, so we check for those as well if the exact marker name is not found.
            return GetMarkerInfoRaw("Inl_" + markerName);
        }

        static string GetMarkerInfoRaw(string markerName)
        {
            if (k_MarkersInfo.TryGetValue(markerName, out var info))
                return info;
            if (k_GeneratedMarkersInfo.TryGetValue(markerName, out info))
                return info;

            return null;
        }
    }
}
