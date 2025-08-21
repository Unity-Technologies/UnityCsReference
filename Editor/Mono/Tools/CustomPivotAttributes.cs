// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.EditorTools
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class CustomPivotAttribute : Attribute
    {
        public const int defaultPriority = 1000;

        string m_DisplayName;
        Type m_TargetTool;
        Type m_TargetToolContext;
        int m_Priority = defaultPriority;
        string m_Tooltip;

        public string displayName
        {
            get => m_DisplayName;
            set => m_DisplayName = value;
        }

        public Type targetTool
        {
            get => m_TargetTool;
            set => m_TargetTool = value;
        }

        public Type targetToolContext
        {
            get => m_TargetToolContext;
            set => m_TargetToolContext = value;
        }

        public int priority
        {
            get => m_Priority;
            set => m_Priority = value;
        }

        public string tooltip
        {
            get => m_Tooltip;
            set => m_Tooltip = value;
        }

        public CustomPivotAttribute(string displayName, Type targetTool = null, Type targetToolContext = null)
        {
            m_DisplayName = displayName;
            m_TargetTool = targetTool;
            m_TargetToolContext = targetToolContext;
            m_Priority = priority;
            m_Tooltip = tooltip;
        }
    }
}
