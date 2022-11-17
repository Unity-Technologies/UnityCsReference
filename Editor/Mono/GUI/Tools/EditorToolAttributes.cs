// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.EditorTools
{
    public abstract class ToolAttribute : Attribute
    {
        public const int defaultPriority = 1000;

        string m_DisplayName;
        Type m_TargetContext, m_TargetType;
        Type m_VariantGroup;
        int m_ToolPriority = defaultPriority;
        int m_VariantPriority = defaultPriority;

        public string displayName
        {
            get => m_DisplayName;
            set => m_DisplayName = value;
        }

        public Type targetType
        {
            get => m_TargetType;
            set => m_TargetType = value;
        }

        public Type targetContext
        {
            get => m_TargetContext;
            set => m_TargetContext = value;
        }

        public int toolPriority
        {
            get => m_ToolPriority;
            set => m_ToolPriority = value;
        }

        public Type variantGroup
        {
            get => m_VariantGroup;
            set => m_VariantGroup = value;
        }

        public int variantPriority
        {
            get => m_VariantPriority;
            set => m_VariantPriority = value;
        }

        ToolAttribute() {}

        protected ToolAttribute(string displayName, Type targetType = null, Type editorToolContext = null)
        {
            m_TargetType = targetType;
            m_DisplayName = displayName;
            if (editorToolContext != null && !typeof(EditorToolContext).IsAssignableFrom(editorToolContext))
                throw new ArgumentException($"{GetType().Name} ({displayName}): editorToolContext type argument must " +
                    $"be of type EditorToolContext", "editorToolContext");
            m_TargetContext = editorToolContext;
            m_VariantGroup = variantGroup;
            m_ToolPriority = toolPriority;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class EditorToolAttribute : ToolAttribute
    {
        public EditorToolAttribute(string displayName, Type componentToolTarget = null)
            : base(displayName, componentToolTarget) {}

        public EditorToolAttribute(string displayName, Type componentToolTarget, Type editorToolContext)
            : base(displayName, componentToolTarget, editorToolContext) {}

        public EditorToolAttribute(
            string displayName,
            Type componentToolTarget,
            Type editorToolContext,
            int toolPriority,
            Type variantGroup)
            : base(displayName, componentToolTarget, editorToolContext)
        {
            this.toolPriority = toolPriority;
            this.variantGroup = variantGroup;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EditorToolContextAttribute : ToolAttribute
    {
        public EditorToolContextAttribute(string displayName = "", Type targetType = null)
            : base(displayName, targetType) {}
    }
}
