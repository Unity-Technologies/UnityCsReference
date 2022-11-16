// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using ATarget = System.AttributeTargets;

namespace UnityEditor.ShaderKeywordFilter
{
    public enum FilterAction
    {
        Select,         // Only selected keywords will remain in the tuple
        Remove,         // Remove specific keywords from the tuple
        SelectOrRemove  // Select if the condition is evaluated true, remove otherwise
    };

    /* FilterAttribute and the derived attributes below enable users to control multi_compile keywords based  
     * on settings data. The attribute marks a data field with condition and filter action. If the comparison
     * between condition and field data passes, the attribute filter action is applied to the keywords 
     * passed in as attribute arguments. Any keyword tuple (i.e. keywords from a single multi_compile) that
     * has one of these keywords is filtered using this action. Filter actions are evaluated in the order of
     * appearance in the data type tree. Overriding previous actions only happens if explicitly requested
     * by the attribute.
     */
    [System.AttributeUsage(ATarget.Field, AllowMultiple = true)]
    public class FilterAttribute : System.Attribute
    {
        public enum Precedence
        {
            Normal,         // Any previous filter rule will take precedence
            Override        // This filter rule will be taken over any previous ones
        };

        public enum EvaluationMode
        {
            Normal,         // This filter rule applies if condition value matches with the target field data
            Negated         // This filter rule applies if condition value does NOT match with the target field data
        };

        public FilterAttribute(FilterAction action, Precedence precedence, EvaluationMode evaluationMode, object condition, string fileName, int lineNumber, params string[] keywordNames)
        {
            m_Action = action;
            m_Precedence = precedence;
            m_EvaluationMode = evaluationMode;
            m_Condition = condition;
            m_Names = keywordNames;
            m_FileName = fileName;
            m_LineNumber = lineNumber;

            if (m_DoDebugLogging)
            {
                // TODO - Need to add a setting around the logging/analytics/stats reporting.
                //        Should have have levels or modes to allow the user to validate their rules.
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null,
                    "{0} attribute declared at {1}:{2} for keyword(s) '{3}' predicated on the value '{4}'",
                    GetFilterActionName(m_Action), m_FileName, m_LineNumber, String.Join(", ", m_Names), m_Condition);
            }
        }

        internal string GetFilterActionName(FilterAction action)
        {
            switch (action)
            {
                case FilterAction.Select:
                    return "Select";
                case FilterAction.Remove:
                    return "Remove";
                case FilterAction.SelectOrRemove:
                    return "SelectOrRemove";
                default:
                    return "Unknown";
            }
        }

        internal void EnableLogging()
        {
            m_DoDebugLogging = true;
        }

        internal void DisableLogging()
        {
            m_DoDebugLogging = false;
        }

        internal string GetFormattedResolutionMessageActive(object data)
        {
            var s = String.Format("Applying {0} attribute declared at {1}:{2} because condition was '{3}'",
                GetFilterActionName(m_Action), m_FileName, m_LineNumber, data);

            if (m_DoDebugLogging)
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, s);

            return s;
        }

        internal string GetFormattedResolutionMessageInactive(object data)
        {
            var s = String.Format("Skipping {0} attribute declared at {1}:{2} because condition was '{3}'",
                GetFilterActionName(m_Action), m_FileName, m_LineNumber, data);

            if (m_DoDebugLogging)
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, s);

            return s;
        }

        internal string GetFormattedResolutionMessageOverride(object data, FilterRule originalRule)
        {
            var s = String.Format("Overriding {0} attribute for keyword '{1}' with {2} attribute declared at {3}:{4}",
                GetFilterActionName(originalRule.action), originalRule.keywordName, GetFilterActionName(m_Action),
                m_FileName, m_LineNumber);

            if (m_DoDebugLogging)
                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, s);

            return s;
        }

        internal Precedence RulePrecedence
        {
            get => m_Precedence;
        }

        internal string[] KeywordNames
        {
            get => m_Names;
        }

        internal FilterAction Action
        {
            get => m_Action;
        }

        // Based on input data evaluate the correct filter action (either Select or Remove).
        // Returns false if the filter is inactive and therefore to be ignored.
        internal bool GetActiveFilterAction(object data, out FilterAction action)
        {
            bool match = m_Condition.Equals(data);

            if (m_EvaluationMode == EvaluationMode.Negated)
                match = !match;

            if (m_Action == FilterAction.SelectOrRemove)
            {
                action = match ? FilterAction.Select : FilterAction.Remove;
                return true; // SelectOrRemove always returns an active filter action
            }

            action = m_Action;
            return match;
        }

        static private bool m_DoDebugLogging = false;
        private FilterAction m_Action;
        private Precedence m_Precedence;
        private EvaluationMode m_EvaluationMode;
        private string[] m_Names;
        private object m_Condition;
        // These two record the location of this particular filter attribute declaration
        private string m_FileName;
        private int m_LineNumber;
    }

    public class SelectIfAttribute : FilterAttribute
    {
        public SelectIfAttribute(object condition, bool overridePriority = false, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, params string[] keywordNames)
            : base(FilterAction.Select, overridePriority ? Precedence.Override : Precedence.Normal, EvaluationMode.Normal, condition, Path.GetFileName(filePath), lineNumber, keywordNames)
        {
        }
    }

    public class RemoveIfAttribute : FilterAttribute
    {
        public RemoveIfAttribute(object condition, bool overridePriority = false, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, params string[] keywordNames)
            : base(FilterAction.Remove, overridePriority ? Precedence.Override : Precedence.Normal, EvaluationMode.Normal, condition, Path.GetFileName(filePath), lineNumber, keywordNames)
        {
        }
    }

    public class SelectOrRemoveAttribute : FilterAttribute
    {
        public SelectOrRemoveAttribute(object condition, bool overridePriority = false, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, params string[] keywordNames)
            : base(FilterAction.SelectOrRemove, overridePriority ? Precedence.Override : Precedence.Normal, EvaluationMode.Normal, condition, Path.GetFileName(filePath), lineNumber, keywordNames)
        {
        }
    }

    public class SelectIfNotAttribute : FilterAttribute
    {
        public SelectIfNotAttribute(object condition, bool overridePriority = false, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, params string[] keywordNames)
            : base(FilterAction.Select, overridePriority ? Precedence.Override : Precedence.Normal, EvaluationMode.Negated, condition, Path.GetFileName(filePath), lineNumber, keywordNames)
        {
        }
    }

    public class RemoveIfNotAttribute : FilterAttribute
    {
        public RemoveIfNotAttribute(object condition, bool overridePriority = false, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, params string[] keywordNames)
            : base(FilterAction.Remove, overridePriority ? Precedence.Override : Precedence.Normal, EvaluationMode.Negated, condition, Path.GetFileName(filePath), lineNumber, keywordNames)
        {
        }
    }

    public class RemoveOrSelectAttribute : FilterAttribute
    {
        public RemoveOrSelectAttribute(object condition, bool overridePriority = false, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0, params string[] keywordNames)
            : base(FilterAction.SelectOrRemove, overridePriority ? Precedence.Override : Precedence.Normal, EvaluationMode.Negated, condition, Path.GetFileName(filePath), lineNumber, keywordNames)
        {
        }
    }
}
