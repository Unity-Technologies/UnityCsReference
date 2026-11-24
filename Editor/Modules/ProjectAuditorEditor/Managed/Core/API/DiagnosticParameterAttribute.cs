// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.ProjectAuditor.Editor.Core
{
    /// <summary>
    /// Used to mark an integer field in an class that inherits from <see cref="ModuleAnalyzer"/> as being a Diagnostic Parameter.
    /// </summary>
    /// <remarks>
    /// Diagnostic Parameters are used to define threshold values against which to compare other values when an Analyzer
    /// is deciding whether or not something constitutes a reportable issue. Whilst Analyzers are free to use hard-coded
    /// constants as threshold values, Diagnostic Parameters allow you to change values in Settings > Project Auditor as
    /// a project's requirements evolve, or to set different values for different target platforms.
    ///
    /// Diagnostic Parameters and their default values are automatically registered in the <see cref="DiagnosticParams"/>
    /// object held by <see cref="ProjectAuditorSettings"/>, where their values can be customized if required. When
    /// <see cref="ProjectAuditor"/> initializes prior to running analysis, the values in the DiagnosticParams held by
    /// <see cref="AnalysisParams"/> are automatically cached back in their corresponding fields which can be used
    /// during analysis.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class DiagnosticParameterAttribute : Attribute
    {
        /// <summary>
        /// The Diagnostic Parameter's name. This name should uniquely identify this parameter within a project.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The Diagnostic Parameter's user friendly name as will be seen in Project Settings.
        /// </summary>
        public string UserFriendlyName { get; private set; }

        /// <summary>
        /// Text about this DiagnosticParameter to show as a tooltip in Project Settings.
        /// </summary>
        public string Tooltip { get; private set; }

        /// <summary>
        /// The default value for this parameter.
        /// </summary>
        public int DefaultValue { get; private set; }

        /// <summary>
        /// The minimum value for this parameter.
        /// </summary>
        public int MinValue { get; private set; }

        /// <summary>
        /// The maximum value for this parameter.
        /// </summary>
        public int MaxValue { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The Diagnotic Parameter's name</param>
        /// <param name="userFriendlyName">The Diagnostic Parameter's user friendly name for Project Settings.</param>
        /// <param name="tooltip">The Diagnotic Parameter's tooltip text for project settings.</param>
        /// <param name="defaultValue">A default value for the parameter.</param>
        public DiagnosticParameterAttribute(string name, string userFriendlyName, string tooltip, int defaultValue)
            : this(name, userFriendlyName, tooltip, defaultValue, 0)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The Diagnotic Parameter's name</param>
        /// <param name="userFriendlyName">The Diagnostic Parameter's user friendly name for Project Settings.</param>
        /// <param name="tooltip">The Diagnotic Parameter's tooltip text for project settings.</param>
        /// <param name="defaultValue">A default value for the parameter.</param>
        /// <param name="minValue">The minimum valid value this parameter may have.</param>
        /// <param name="maxValue">The maximum valid value this parameter may have.</param>
        public DiagnosticParameterAttribute(string name, string userFriendlyName, string tooltip, int defaultValue, int minValue, int maxValue = int.MaxValue)
        {
            Name = name;
            UserFriendlyName = userFriendlyName;
            Tooltip = tooltip;
            DefaultValue = defaultValue;
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }
}
