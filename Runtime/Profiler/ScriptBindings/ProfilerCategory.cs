// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Scripting;

namespace Unity.Profiling
{
    /// <summary>
    /// Defines a profiling category when you create a ProfilerMarker.
    /// </summary>
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public readonly struct ProfilerCategory
    {
        [FieldOffset(0)]
        readonly ushort m_CategoryId;
        public ProfilerCategory(string categoryName)
        {
            m_CategoryId = ProfilerUnsafeUtility.CreateCategory(categoryName, ProfilerCategoryColor.Scripts);
        }

        public ProfilerCategory(string categoryName, ProfilerCategoryColor color)
        {
            m_CategoryId = ProfilerUnsafeUtility.CreateCategory(categoryName, color);
        }

        internal ProfilerCategory(ushort category)
        {
            m_CategoryId = category;
        }

        public unsafe string Name
        {
            get
            {
                var desc = ProfilerUnsafeUtility.GetCategoryDescription(m_CategoryId);
                return ProfilerUnsafeUtility.Utf8ToString(desc.NameUtf8, desc.NameUtf8Len);
            }
        }

        public Color32 Color => ProfilerUnsafeUtility.GetCategoryDescription(m_CategoryId).Color;

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// A ProfilerMarker that belongs to the Render system.
        /// </summary>
        public static ProfilerCategory Render => new ProfilerCategory(ProfilerUnsafeUtility.CategoryRender);
        /// <summary>
        /// Default category for all ProfilerMarkers defined in scripting code.
        /// </summary>
        public static ProfilerCategory Scripts => new ProfilerCategory(ProfilerUnsafeUtility.CategoryScripts);
        /// <summary>
        /// A ProfilerMarker that belongs to the UI system.
        /// </summary>
        public static ProfilerCategory Gui => new ProfilerCategory(ProfilerUnsafeUtility.CategoryGUI);
        /// <summary>
        /// A ProfilerMarker that belongs to the Physics system.
        /// </summary>
        public static ProfilerCategory Physics => new ProfilerCategory(ProfilerUnsafeUtility.CategoryPhysics);
        /// <summary>
        /// A ProfilerMarker that belongs to the Animation system.
        /// </summary>
        public static ProfilerCategory Animation => new ProfilerCategory(ProfilerUnsafeUtility.CategoryAnimation);
        /// <summary>
        /// A ProfilerMarker that belongs to the Ai or NavMesh system.
        /// </summary>
        public static ProfilerCategory Ai => new ProfilerCategory(ProfilerUnsafeUtility.CategoryAi);
        /// <summary>
        /// A ProfilerMarker that belongs the to Audio system.
        /// </summary>
        public static ProfilerCategory Audio => new ProfilerCategory(ProfilerUnsafeUtility.CategoryAudio);
        /// <summary>
        /// A ProfilerMarker that belongs to the Video system.
        /// </summary>
        public static ProfilerCategory Video => new ProfilerCategory(ProfilerUnsafeUtility.CategoryVideo);
        /// <summary>
        /// A ProfilerMarker that belongs to the Particle system.
        /// </summary>
        public static ProfilerCategory Particles => new ProfilerCategory(ProfilerUnsafeUtility.CategoryParticles);
        /// <summary>
        /// A ProfilerMarker that belongs to the Lighting system.
        /// </summary>
        public static ProfilerCategory Lighting => new ProfilerCategory(ProfilerUnsafeUtility.CategoryLighting);
        /// <summary>
        /// A ProfilerMarker that belongs to the Networking system.
        /// </summary>
        public static ProfilerCategory Network => new ProfilerCategory(ProfilerUnsafeUtility.CategoryNetwork);
        /// <summary>
        /// A ProfilerMarker that belongs to the Loading or Streaming system.
        /// </summary>
        public static ProfilerCategory Loading => new ProfilerCategory(ProfilerUnsafeUtility.CategoryLoading);
        /// <summary>
        /// A ProfilerMarker that belongs to the VR system.
        /// </summary>
        public static ProfilerCategory Vr => new ProfilerCategory(ProfilerUnsafeUtility.CategoryVr);
        /// <summary>
        /// A ProfilerMarker that belongs to the Input system.
        /// </summary>
        public static ProfilerCategory Input => new ProfilerCategory(ProfilerUnsafeUtility.CategoryInput);
        /// <summary>
        /// Memory category.
        /// </summary>
        public static ProfilerCategory Memory => new ProfilerCategory(ProfilerUnsafeUtility.CategoryAllocation);
        /// <summary>
        /// Virtual Texturing category.
        /// </summary>
        public static ProfilerCategory VirtualTexturing => new ProfilerCategory(ProfilerUnsafeUtility.CategoryVirtualTexturing);
        /// <summary>
        /// File IO category.
        /// </summary>
        public static ProfilerCategory FileIO => new ProfilerCategory(ProfilerUnsafeUtility.CategoryFileIO);
        /// <summary>
        /// Internal category.
        /// </summary>
        public static ProfilerCategory Internal => new ProfilerCategory(ProfilerUnsafeUtility.CategoryInternal);

        internal static ProfilerCategory Any => new ProfilerCategory(ProfilerUnsafeUtility.CategoryAny);

        /// <summary>
        /// Utility operator that simplifies usage of the ProfilerCategory with ProfilerUnsafeUtility.
        /// </summary>
        /// <param name="category"></param>
        /// <returns>ProfilerCategory value as UInt16.</returns>
        public static implicit operator ushort(ProfilerCategory category)
        {
            return category.m_CategoryId;
        }
    }


    [Flags]
    public enum ProfilerCategoryFlags : ushort
    {
        None = 0,
        Builtin = 1 << 0
    }

    public enum ProfilerCategoryColor : ushort
    {
        Render = 0,
        Scripts,
        BurstJobs,
        Other,
        Physics,
        Animation,
        Audio,
        AudioJob,
        AudioUpdateJob,
        Lighting,
        GC,
        VSync,
        Memory,
        Internal,
        UI,
        Build,
        Input,
    }
}
