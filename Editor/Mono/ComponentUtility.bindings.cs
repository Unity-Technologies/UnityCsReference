// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using System.Runtime.InteropServices;
using UnityObject = UnityEngine.Object;

namespace UnityEditorInternal
{
    [NativeHeader("Editor/Src/ComponentUtility.bindings.h")]
    [StaticAccessor("ComponentUtilityBindings", StaticAccessorType.DoubleColon)]
    public sealed partial class ComponentUtility
    {
        [FreeFunction("RemoveDataComponentUndoable")]
        internal static extern bool RemoveDataComponent(EntityId componentEntityId);

        // 1 enabled, 0 disabled, -1 if the component has no enabled state
        internal static extern int GetDataComponentEnabledState(EntityId componentEntityId);

        internal static extern bool SetDataComponentsEnabled(EntityId[] componentEntityIds, bool enabled);

        [FreeFunction("ResetDataComponentUndoable")]
        internal static extern bool ResetDataComponent(EntityId componentEntityId);

        [FreeFunction("CanModifyDataComponent")]
        internal static extern bool CanModifyDataComponent(EntityId componentEntityId);

        public static bool MoveComponentUp(Component component)
        {
            return MoveComponentUp_Internal(new[] { component }, false, false);
        }

        [FreeFunction]
        static extern bool MoveComponentUp_Internal(UnityObject[] context, bool validateOnly, bool showDialog);

        public static bool MoveComponentDown(Component component)
        {
            return MoveComponentDown_Internal(new UnityObject[] { component }, false, false);
        }

        [FreeFunction]
        static extern bool MoveComponentDown_Internal(UnityObject[] context, bool validateOnly, bool showDialog);

        public static bool CopyComponent(Component component)
        {
            return CopyComponent(new[] { component != null ? component.GetEntityId() : EntityId.None }, false);
        }

        internal static extern bool CopyComponent(EntityId[] componentEntityIds, bool validateOnly);

        public static bool PasteComponentValues(Component component)
        {
            return PasteComponentValues(new[] { component != null ? component.GetEntityId() : EntityId.None }, false);
        }

        internal static extern bool PasteComponentValues(EntityId[] componentEntityIds, bool validateOnly);

        //Append new Components to the GameObject
        public static extern bool PasteComponentAsNew([NotNull] GameObject go);

        internal static extern bool PasteComponentAsNew_Internal(EntityId[] componentEntityIds, bool validateOnly);

        internal static extern bool CollectConnectedComponents([NotNull] GameObject targetGameObject, Component[] components, bool copy, [NotNull][Out] List<Component> outCollectedComponents, out string outErrorMessage);

        internal static bool MoveComponentToGameObject(Component component, GameObject targetGameObject)
        {
            return MoveComponentToGameObject(component, targetGameObject, false);
        }

        [FreeFunction("MoveComponent")]
        internal static extern bool MoveComponentToGameObject([NotNull] Component component, [NotNull] GameObject targetGameObject, [DefaultValue("false")] bool validateOnly);

        internal static bool MoveComponentRelativeToComponent(Component component, Component targetComponent, bool aboveTarget)
        {
            return MoveComponentRelativeToComponent(component, targetComponent, aboveTarget, false);
        }

        [FreeFunction("MoveComponent")]
        internal static extern bool MoveComponentRelativeToComponent([NotNull] Component component, [NotNull] Component targetComponent, bool aboveTarget, [DefaultValue("false")] bool validateOnly);

        internal static bool MoveComponentsRelativeToComponents(Component[] components, Component[] targetComponents, bool aboveTarget)
        {
            return MoveComponentsRelativeToComponents(components, targetComponents, aboveTarget, false);
        }

        [FreeFunction("MoveComponents")]
        internal static extern bool MoveComponentsRelativeToComponents(Component[] components, Component[] targetComponents, bool aboveTarget, [DefaultValue("false")] bool validateOnly);

        [FreeFunction]
        internal static extern bool WarnCanAddScriptComponent([NotNull] GameObject gameObject, [NotNull] MonoScript script);

        internal static extern bool AddScriptComponents(GameObject[] gameObjects, MonoScript[] scripts, [NotNull][Out] List<Component> outAddedComponents);
    }
}
