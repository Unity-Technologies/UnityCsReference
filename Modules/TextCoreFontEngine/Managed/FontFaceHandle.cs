// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEngine.TextCore.LowLevel
{
    /// <summary>
    /// Opaque handle that identifies a specific font face loaded by the <see cref="FontEngine"/>.
    /// Returned by the FontFaceHandle-based <c>LoadFontFace</c> overloads and accepted by the
    /// FontFaceHandle-based variants of subsequent FontEngine APIs to ensure that the operation
    /// targets the intended face even if another face has since become current.
    /// </summary>
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
    internal struct FontFaceHandle : IEquatable<FontFaceHandle>
    {
        internal long fontInstanceID;
        internal int faceIndex;
        internal int pointSize;

        /// <summary>
        /// Returns true if this handle refers to a loaded font face.
        /// </summary>
        public bool IsValid => fontInstanceID != 0;

        public bool Equals(FontFaceHandle other)
        {
            return fontInstanceID == other.fontInstanceID && faceIndex == other.faceIndex && pointSize == other.pointSize;
        }

        public override bool Equals(object obj) => obj is FontFaceHandle other && Equals(other);
        public override int GetHashCode() => fontInstanceID.GetHashCode() ^ (faceIndex << 16) ^ pointSize;
        public static bool operator ==(FontFaceHandle a, FontFaceHandle b) => a.Equals(b);
        public static bool operator !=(FontFaceHandle a, FontFaceHandle b) => !a.Equals(b);
    }
}
