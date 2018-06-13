// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Experimental.AssetImporters
{
    [CustomEditor(typeof(ScriptedImporter), true)]
    public class ScriptedImporterEditor : AssetImporterEditor
    {
        internal override string targetTitle
        {
            get { return base.targetTitle + " (" + ObjectNames.NicifyVariableName(GetType().Name) + ")"; }
        }

        public override void OnInspectorGUI()
        {
            // Loop through properties and create one field (including children) for each top level property.
            SerializedProperty property = serializedObject.GetIterator();
            bool expanded = true;
            while (property.NextVisible(expanded))
            {
                EditorGUILayout.PropertyField(property, true);
                expanded = false;
            }

            ApplyRevertGUI();
        }

        protected override bool OnApplyRevertGUI()
        {
            bool applied = base.OnApplyRevertGUI();
            if (applied)
            {
                // Displayed asset is un-loaded at this point due to reimport.
                // force rebuild to force full asset load.
                ActiveEditorTracker.sharedTracker.ForceRebuild();
            }

            return applied;
        }
    }
}
