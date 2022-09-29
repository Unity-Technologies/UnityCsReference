// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.EditorTools
{
    public abstract class ToolAttribute : Attribute
    {
        string m_DisplayName;
        Type m_TargetContext, m_TargetType;

        public string displayName => m_DisplayName;
        public Type targetType => m_TargetType;
        public Type targetContext => m_TargetContext;

        ToolAttribute() {}

        protected ToolAttribute(string displayName, Type targetType = null, Type editorToolContext = null)
        {
            m_TargetType = targetType;
            m_DisplayName = displayName;
            if (editorToolContext != null && !typeof(EditorToolContext).IsAssignableFrom(editorToolContext))
                throw new ArgumentException($"{GetType().Name} ({displayName}): editorToolContext type argument must " +
                    $"be of type EditorToolContext", "editorToolContext");
            m_TargetContext = editorToolContext;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class EditorToolAttribute : ToolAttribute
    {
        public EditorToolAttribute(string displayName, Type componentToolTarget = null)
            : base(displayName, componentToolTarget) {}
        public EditorToolAttribute(string displayName, Type componentToolTarget, Type editorToolContext)
            : base(displayName, componentToolTarget, editorToolContext) {}
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EditorToolContextAttribute : ToolAttribute
    {
        public EditorToolContextAttribute(string displayName = "", Type targetType = null)
            : base(displayName, targetType) {}
    }
}
