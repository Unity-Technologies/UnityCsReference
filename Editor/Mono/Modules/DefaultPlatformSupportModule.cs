// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.DeploymentTargets;

namespace UnityEditor.Modules
{
    internal abstract class DefaultPlatformSupportModule : IPlatformSupportModule
    {
        protected ICompilationExtension compilationExtension;

        protected ITextureImportSettingsExtension textureSettingsExtension;

        public abstract string TargetName { get; }

        public abstract string JamTarget { get; }

        public virtual string ExtensionVersion { get { return null; } }

        public virtual GUIContent[] GetDisplayNames() { return null; }

        public virtual string[] NativeLibraries { get { return new string[0]; } }

        public virtual string[] AssemblyReferencesForUserScripts { get { return new string[0]; } }

        public virtual string[] AssemblyReferencesForEditorCsharpProject { get { return new string[0]; } }

        public virtual IBuildAnalyzer CreateBuildAnalyzer() { return null; }

        public abstract IBuildPostprocessor CreateBuildPostprocessor();

        public virtual IScriptingImplementations CreateScriptingImplementations() { return null; }

        public virtual ISettingEditorExtension CreateSettingsEditorExtension() { return null; }

        public virtual IPreferenceWindowExtension CreatePreferenceWindowExtension() { return null; }

        public virtual ITextureImportSettingsExtension CreateTextureImportSettingsExtension() { return textureSettingsExtension != null ? textureSettingsExtension : textureSettingsExtension = new DefaultTextureImportSettingsExtension(); }

        public virtual IBuildWindowExtension CreateBuildWindowExtension() { return null; }

        public virtual ICompilationExtension CreateCompilationExtension() { return compilationExtension != null ? compilationExtension : compilationExtension = new DefaultCompilationExtension(); }

        public virtual IPluginImporterExtension CreatePluginImporterExtension() { return null; }

        public virtual IUserAssembliesValidator CreateUserAssembliesValidatorExtension() { return null; }

        public virtual IProjectGeneratorExtension CreateProjectGeneratorExtension() { return null; }

        public virtual IDeploymentTargetsExtension CreateDeploymentTargetsExtension() { return null; }

        public virtual void RegisterAdditionalUnityExtensions() {}

        public virtual IDevice CreateDevice(string id) { throw new NotSupportedException(); }

        public virtual void OnActivate() {}

        public virtual void OnDeactivate() {}

        public virtual void OnLoad() {}

        public virtual void OnUnload() {}
    }
}
