// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Modules
{
    internal class DefaultBuildWindowExtension : IBuildWindowExtension
    {
        public virtual void ShowPlatformBuildOptions() {}
        public virtual void ShowInternalPlatformBuildOptions() {}
        public virtual bool EnabledBuildButton() { return true; }
        public virtual bool EnabledBuildAndRunButton() { return true; }
        public virtual bool ShouldDrawScriptDebuggingCheckbox() { return true; }
        public virtual bool ShouldDrawProfilerCheckbox() { return true; }
        public virtual bool ShouldDrawDevelopmentPlayerCheckbox() { return true; }
        public virtual bool ShouldDrawExplicitNullCheckbox() { return false; }
        public virtual bool ShouldDrawExplicitDivideByZeroCheckbox() { return false; }
        public virtual bool ShouldDrawForceOptimizeScriptsCheckbox() { return false; }
    }
}
