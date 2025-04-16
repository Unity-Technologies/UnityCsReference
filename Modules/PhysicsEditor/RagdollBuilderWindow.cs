// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;

namespace UnityEditor
{
    class RagdollBuilderWindow : EditorWindow
    {
        private RagdollBuilder ragdollBuilder;

        [MenuItem("GameObject/3D Object/Ragdoll...", false, 2000)]
        static void CreateWindow()
        {
            var window = GetWindow<RagdollBuilderWindow>(true, "Ragdoll Builder");
            window.ShowUtility();
        }

        private void OnEnable()
        {
            ragdollBuilder = new RagdollBuilder();
        }

        private void OnDisable()
        {
            ragdollBuilder = null;
        }

        protected virtual void OnWizardUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Ragdoll Builder", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(ragdollBuilder.helpString, MessageType.Info);
            GUILayout.Space(10);

            if (ragdollBuilder != null)
            {
                ragdollBuilder.OnGUI();

                GUILayout.Space(10);

                GUILayout.BeginHorizontal();
                GUI.enabled = ragdollBuilder.isValid;
                if (GUILayout.Button("Create"))
                {
                    ragdollBuilder.OnWizardCreate();
                    Close();
                }
                GUI.enabled = true;

                GUI.enabled = ragdollBuilder.hasAnyBonesAssigned;
                if (GUILayout.Button("Clear"))
                {
                    ragdollBuilder.Clear();
                }
                GUI.enabled = true;

                if (GUILayout.Button("Cancel"))
                {
                    Close();
                }
                GUILayout.EndHorizontal();

           		// Call OnWizardUpdate whenever the GUI is updated or modified
                ragdollBuilder.OnWizardUpdate();
            }
		}
    }
}

