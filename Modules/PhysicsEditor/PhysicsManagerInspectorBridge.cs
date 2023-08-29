// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Physics.Editor.ProjectSettingsBridge")]
namespace UnityEditorInternal
{
    internal interface IPhysicsProjectSettingsECSInspectorExtension
    {
        public void SetupMainPageItems(DropdownField dropDown, HelpBox infoBox, HelpBox warningBox, SerializedObject physicsManager);

        public void SetupSettingsTab(Tab ecsTab, SerializedObject physicsManager);
    }

    internal class PhysicsManagerInspectorBridge
    {
        public static void RegisterECSInspectorExtension(IPhysicsProjectSettingsECSInspectorExtension ext)
        {
            if (PhysicsManagerInspector.EcsExtension != null)
            {
                var cExt = PhysicsManagerInspector.EcsExtension;
                throw new ArgumentException($"An ECS inspector extension instance has already been registered, current: {cExt.GetType()}");
            }

            PhysicsManagerInspector.EcsExtension = ext;
        }
    }
}
