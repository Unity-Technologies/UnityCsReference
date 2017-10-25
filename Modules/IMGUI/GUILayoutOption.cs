// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    // Class internally used to pass layout options into [[GUILayout]] functions. You don't use these directly, but construct them with the layouting functions in the [[GUILayout]] class.
    public sealed class GUILayoutOption
    {
        internal enum Type
        {
            fixedWidth, fixedHeight, minWidth, maxWidth, minHeight, maxHeight, stretchWidth, stretchHeight,
            // These are just for the spacing variables
            alignStart, alignMiddle, alignEnd, alignJustify, equalSize, spacing
        }
        // *undocumented*
        internal Type type;
        // *undocumented*
        internal object value;
        // *undocumented*
        internal GUILayoutOption(Type type, object value)
        {
            this.type = type;
            this.value = value;
        }
    }
}
