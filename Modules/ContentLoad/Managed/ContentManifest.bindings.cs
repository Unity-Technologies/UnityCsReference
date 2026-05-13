// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Loading
{
    [NativeHeader("Modules/ContentLoad/Public/ContentManifest.h")]
    [UsedByNativeCode]
    internal sealed class ContentManifest : IDisposable
    {
        public static bool LoadFromFile(string path, out ContentManifest outCatalog)
        {
            outCatalog = null;
            IntPtr catalogPtr = LoadContentManifest(path);

            if (catalogPtr == IntPtr.Zero)
                return false;

            outCatalog = new ContentManifest(catalogPtr);
            return true;
        }

        public extern string BuildName
        {
            [NativeMethod("GetBuildName", IsThreadSafe = true)]
            get;
        }

        public extern bool BuiltWithTypeTrees
        {
            [NativeMethod("GetBuiltWithTypeTrees", IsThreadSafe = true)]
            get;
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        /////////////////////////////////////////////////////////////

        private IntPtr m_Ptr;
        private ContentManifest(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr LoadContentManifest(string filename);

        [NativeMethod(IsThreadSafe = true)]
        static extern void Internal_Destroy(IntPtr ptr);

        ~ContentManifest()
        {
            Destroy();
        }

        void Destroy()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(ContentManifest catalog) => catalog != null ? catalog.m_Ptr : IntPtr.Zero;
        }
    }
}
