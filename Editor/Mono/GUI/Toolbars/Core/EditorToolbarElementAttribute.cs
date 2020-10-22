// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Toolbars
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    class EditorToolbarElementAttribute : Attribute
    {
        public string id { get; }
        public Type[] targetContexts { get; }

        public EditorToolbarElementAttribute(string id, params Type[] targetContexts)
        {
            this.id = id;
            this.targetContexts = targetContexts;
        }
    }

    interface IEditorToolbarContext
    {
        object context { get; set; }
    }
}
