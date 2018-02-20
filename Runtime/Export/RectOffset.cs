// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Offsets for rectangles, borders, etc.
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public partial class RectOffset
    {
        // Pointer to the RectOffset INSIDE a GUIStyle.
        [NonSerialized]
        [VisibleToOtherModules("UnityEngine.IMGUIModule")]
        internal IntPtr m_Ptr;

        // Pointer to the source GUIStyle so it doesn't get garbage collected.
        // If NULL, it means we own m_Ptr and need to delete it when this gets disposed
        readonly object m_SourceStyle;

        /// *listonly*
        public RectOffset()
        {
            m_Ptr = InternalCreate();
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule")]
        internal RectOffset(object sourceStyle, IntPtr source)
        {
            m_SourceStyle = sourceStyle;
            m_Ptr = source;
        }

        ~RectOffset()
        {
            if (m_SourceStyle == null)
                Dispose();
        }

        // Creates a new rectangle with offsets.
        public RectOffset(int left, int right, int top, int bottom)
        {
            m_Ptr = InternalCreate();
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
        }

        public override string ToString()
        {
            return UnityString.Format("RectOffset (l:{0} r:{1} t:{2} b:{3})", left, right, top, bottom);
        }

        private void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                InternalDestroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }
    }
}
