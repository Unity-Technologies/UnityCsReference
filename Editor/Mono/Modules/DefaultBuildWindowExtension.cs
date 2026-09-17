// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor.Modules
{
    internal class DefaultBuildWindowExtension : IBuildWindowExtension
    {
        internal class Styles
        {
            public GUIContent runLastBuild = L10n.TextContent("Run Last Build", "Run the most recent build", null, null);
            public static readonly GUIContent patch = L10n.TextContent("Patch", "Compiles only the scripts and patches the previous build with the updated code.", null, null);
            public static readonly GUIContent patchAndRun = L10n.TextContent("Patch And Run", "Compiles only the scripts, patches the previous build with the updated code, then runs the build.", null, null);
        }

        [NoAutoStaticsCleanup] // lazy GUIContent Styles cache; content survives reload, re-inits on first access
        static private Styles m_Styles = null;

        internal Styles styles
        {
            get
            {
                if (m_Styles == null)
                    m_Styles = new Styles();
                return m_Styles;
            }
        }

        public virtual void ShowPlatformBuildOptions() {}
        public virtual void ShowPlatformBuildWarnings() {}
        public virtual void ShowInternalPlatformBuildOptions() {}
        public virtual bool EnabledBuildButton() { return true; }
        public virtual bool EnabledBuildAndRunButton() { return true; }
        public virtual void GetBuildButtonTitles(out GUIContent buildButtonTitle, out GUIContent buildAndRunButtonTitle)
        {
            buildButtonTitle = null;
            buildAndRunButtonTitle = null;
        }

        public virtual bool AskForBuildLocation()
        {
            return true;
        }

        public virtual bool ShouldDrawScriptDebuggingCheckbox() { return true; }
        public virtual bool ShouldDrawProfilerCheckbox() { return true; }
        public virtual bool ShouldDrawDevelopmentPlayerCheckbox() { return true; }
        public virtual bool ShouldDrawExplicitNullCheckbox() { return false; }
        public virtual bool ShouldDrawExplicitDivideByZeroCheckbox() { return false; }
        public virtual bool ShouldDrawExplicitArrayBoundsCheckbox() { return false; }
        public virtual bool ShouldDrawForceOptimizeScriptsCheckbox() { return false; }
        public virtual bool ShouldDrawWaitForManagedDebugger() { return false; }
        public virtual bool ShouldDrawManagedDebuggerFixedPort() { return false; }
        public virtual bool ShouldDisableManagedDebuggerCheckboxes() { return false; }
        public virtual bool ShouldDrawRunLastBuildButton() { return false; }
        public virtual void DoRunLastBuildButtonGui()
        {
            GUI.enabled = EnabledRunLastBuildButton();
            if (GUILayout.Button(styles.runLastBuild, GUILayout.Width(110)))
            {
                DoRunLastBuild();
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;
        }

        protected virtual bool EnabledRunLastBuildButton() { return false; }
        protected virtual void DoRunLastBuild() {}
    }
}
