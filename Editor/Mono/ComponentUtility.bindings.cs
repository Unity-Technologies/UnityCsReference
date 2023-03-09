// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityObject = UnityEngine.Object;

namespace UnityEditorInternal
{
    [NativeHeader("Editor/Src/ComponentUtility.bindings.h")]
    [StaticAccessor("ComponentUtilityBindings", StaticAccessorType.DoubleColon)]
    public sealed partial class ComponentUtility
    {
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
            return CopyComponent(new UnityObject[] { component }, false);
        }

        [FreeFunction]
        static extern bool CopyComponent(UnityObject[] context, bool validateOnly);

        public static bool PasteComponentValues(Component component)
        {
            return PasteComponentValues(new UnityObject[] { component }, false);
        }

        [FreeFunction]
        static extern bool PasteComponentValues(UnityObject[] context, bool validateOnly);

        //Append new Components to the GameObject
        public static extern bool PasteComponentAsNew([NotNull] GameObject go);

        internal static extern bool CollectConnectedComponents([NotNull] GameObject targetGameObject, Component[] components, bool copy, [NotNull] List<Component> outCollectedComponents, out string outErrorMessage);

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

        internal static extern bool CopyComponentToGameObject([NotNull] Component component, [NotNull] GameObject targetGameObject, bool validateOnly, [NotNull] List<Component> outNewComponents);
        internal static extern bool CopyComponentToGameObjects([NotNull] Component component, GameObject[] targetGameObjects, bool validateOnly, [NotNull] List<Component> outNewComponents);
        internal static extern bool CopyComponentRelativeToComponent([NotNull] Component component, [NotNull] Component targetComponent, bool aboveTarget, bool validateOnly, [NotNull] List<Component> outNewComponents);
        internal static extern bool CopyComponentRelativeToComponents([NotNull] Component component, Component[] targetComponents, bool aboveTarget, bool validateOnly, [NotNull] List<Component> outNewComponents);
        internal static extern bool CopyComponentsRelativeToComponents(Component[] components, Component[] targetComponents, bool aboveTarget, bool validateOnly, List<Component> outNewComponents);

        [FreeFunction]
        internal static extern bool WarnCanAddScriptComponent([NotNull] GameObject gameObject, [NotNull] MonoScript script);
    }
}
