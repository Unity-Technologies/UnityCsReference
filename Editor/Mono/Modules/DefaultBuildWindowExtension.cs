// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Modules
{
    internal class DefaultBuildWindowExtension : IBuildWindowExtension
    {
        internal class Styles
        {
            public GUIContent buildScriptsOnly = EditorGUIUtility.TrTextContent("Scripts Only Build", "Scripts Only Build re-compiles only scripts in the current Project, skipping processing other assets. When you Build, it will create a new Player build based on a previous successful build.");
            public GUIContent runLastBuild = EditorGUIUtility.TrTextContent("Run Last Build", "Run the most recent build");
            public static readonly GUIContent patch = EditorGUIUtility.TrTextContent("Patch", "Compiles only the scripts and patches the previous build with the updated code.");
            public static readonly GUIContent patchAndRun = EditorGUIUtility.TrTextContent("Patch And Run", "Compiles only the scripts, patches the previous build with the updated code, then runs the build.");
        }

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

        public virtual void DoScriptsOnlyGUI()
        {
            EditorUserBuildSettings.buildScriptsOnly = EditorGUILayout.Toggle(styles.buildScriptsOnly, EditorUserBuildSettings.buildScriptsOnly);
        }

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
