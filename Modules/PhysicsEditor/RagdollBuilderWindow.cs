// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: _3DPhysics not yet converted
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System;

namespace UnityEditor
{
    class RagdollBuilderWindow : EditorWindow
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        internal RagdollBuilderWindow() { }
        #pragma warning restore UAL0015

        private RagdollBuilder ragdollBuilder;
        private Vector2 scrollPosition;

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
            if (ragdollBuilder != null)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                EditorGUILayout.LabelField("Ragdoll Builder", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(ragdollBuilder.helpString, MessageType.Info);
                GUILayout.Space(10);

                ragdollBuilder.OnGUI();

                GUILayout.Space(10);

                EditorGUILayout.EndScrollView();

                GUILayout.BeginHorizontal();
                GUI.enabled = ragdollBuilder.isValid;
                if (GUILayout.Button("Create"))
                {
                    ragdollBuilder.OnWizardCreate();
                    Close();
                    return;
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
                    return;
                }
                GUILayout.EndHorizontal();

           		// Call OnWizardUpdate whenever the GUI is updated or modified
                ragdollBuilder.OnWizardUpdate();
            }
		}
    }
}

#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
