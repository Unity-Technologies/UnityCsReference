// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Project-specific settings.
    /// </summary>
    /// <remarks>
    /// The settings in this class include the global <see cref="Unity.ProjectAuditor.Editor.DiagnosticParams"/> and a structure containing a list of <see cref="Unity.ProjectAuditor.Editor.Rule"/> instances.
    /// These can be viewed and edited in the Settings > Project Auditor window in the Editor and are saved in ProjectSettings/ProjectAuditorSettings.asset, but
    /// they are not directly exposed to scripts in the package API.
    /// </remarks>
    [FilePath("ProjectSettings/ProjectAuditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class ProjectAuditorSettings : ScriptableSingleton<ProjectAuditorSettings>, ISerializationCallbackReceiver
    {
        // The SeverityRules object which defines which issues should be ignored or given increased severity when viewing reports.
        [SerializeField] internal SeverityRules Rules;

        // The DiagnosticParams object which defines the customizable thresholds for reporting certain diagnostics.
        [SerializeField] internal DiagnosticParams DiagnosticParams;

        // Default constructor.
        internal ProjectAuditorSettings()
        {
            Rules = new SeverityRules();
            DiagnosticParams = new DiagnosticParams();
            DiagnosticParams.RegisterParameters();
        }

        private void OnEnable()
        {
            DiagnosticParams.OnBeforeSerialize();
            hideFlags = HideFlags.HideAndDontSave & ~HideFlags.NotEditable;
        }

        void OnDisable()
        {
            Save();
        }

        /// <summary>
        /// Save the Project Auditor Settings file.
        /// </summary>
        public void Save()
        {
            DiagnosticParams.OnBeforeSerialize();
            Save(true);
        }

        /// <summary>
        /// Pre-serialize callback.
        /// </summary>
        public void OnBeforeSerialize()
        {
            Rules = null;
        }

        /// <summary>
        /// Post-serialize callback.
        /// </summary>
        public void OnAfterDeserialize()
        {
            Rules = new SeverityRules();
        }

        internal SerializedObject GetSerializedObject()
        {
            return new SerializedObject(this);
        }
    }
}
