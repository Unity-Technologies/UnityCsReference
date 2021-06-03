// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Toolbars
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class EditorToolbarElementAttribute : Attribute
    {
        public string id { get; }
        public Type[] targetWindows { get; }

        public EditorToolbarElementAttribute(string id, params Type[] targetWindows)
        {
            this.id = id;
            this.targetWindows = targetWindows;
        }
    }

    public interface IAccessContainerWindow
    {
        EditorWindow containerWindow { get; set; }
    }
}
