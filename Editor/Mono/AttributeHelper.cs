// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEditor
{
    internal class AttributeHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct MonoGizmoMethod
        {
            public MethodInfo drawGizmo;
            public Type       drawnType;
            public int        options;
        }

        [RequiredByNativeCode]
        static MonoGizmoMethod[] ExtractGizmos(Assembly assembly)
        {
            var commands = new List<MonoGizmoMethod>();

            Type[] types = AssemblyHelper.GetTypesFromAssembly(assembly);
            foreach (Type type in types)
            {
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                // Iterate all methods
                // - Find static gizmo commands with attributes
                // - Add them to the commands list
                for (int i =  0; i < methods.GetLength(0); i++)
                {
                    MethodInfo mi = methods[i];

                    object[] attrs = mi.GetCustomAttributes(typeof(DrawGizmo), false);
                    foreach (DrawGizmo gizmoAttr in  attrs)
                    {
                        ParameterInfo[] parameters = mi.GetParameters();
                        if (parameters.Length != 2)
                        {
                            Debug.LogWarning(String.Format("Method {0}.{1} is marked with the DrawGizmo attribute but does not take parameters (ComponentType, GizmoType) so will be ignored.", mi.DeclaringType.FullName, mi.Name));
                            continue;
                        }
                        else if (mi.DeclaringType != null && mi.DeclaringType.IsGenericTypeDefinition)
                        {
                            Debug.LogWarning(String.Format("Method {0}.{1} is marked with the DrawGizmo attribute but is defined on a generic type definition, so will be ignored.", mi.DeclaringType.FullName, mi.Name));
                            continue;
                        }

                        MonoGizmoMethod item = new MonoGizmoMethod();

                        if (gizmoAttr.drawnType == null)
                            item.drawnType = parameters[0].ParameterType;
                        else if (parameters[0].ParameterType.IsAssignableFrom(gizmoAttr.drawnType))
                            item.drawnType = gizmoAttr.drawnType;
                        else
                        {
                            Debug.LogWarning(String.Format("Method {0}.{1} is marked with the DrawGizmo attribute but the component type it applies to could not be determined.", mi.DeclaringType.FullName, mi.Name));
                            continue;
                        }

                        if (parameters[1].ParameterType != typeof(GizmoType) && parameters[1].ParameterType != typeof(int))
                        {
                            Debug.LogWarning(String.Format("Method {0}.{1} is marked with the DrawGizmo attribute but does not take a second parameter of type GizmoType so will be ignored.", mi.DeclaringType.FullName, mi.Name));
                            continue;
                        }

                        item.drawGizmo = mi;
                        item.options = (int)gizmoAttr.drawOptions;

                        commands.Add(item);
                    }
                }
            }

            return commands.ToArray();
        }

        [RequiredByNativeCode]
        static string GetComponentMenuName(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(AddComponentMenu), false);
            if (attrs.Length > 0)
            {
                var menu = (AddComponentMenu)attrs[0];
                return menu.componentMenu;
            }
            return null;
        }

        [RequiredByNativeCode]
        static int GetComponentMenuOrdering(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(AddComponentMenu), false);
            if (attrs.Length > 0)
            {
                var menu = (AddComponentMenu)attrs[0];
                return menu.componentOrder;
            }
            return 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MonoCreateAssetItem
        {
            public string menuItem;
            public string fileName;
            public int    order;
            public Type   type;
        }

        [RequiredByNativeCode]
        static MonoCreateAssetItem[] ExtractCreateAssetMenuItems(Assembly assembly)
        {
            List<MonoCreateAssetItem> result = new List<MonoCreateAssetItem>();

            Type[] types = AssemblyHelper.GetTypesFromAssembly(assembly);
            foreach (Type type in types)
            {
                var attr = (CreateAssetMenuAttribute)Attribute.GetCustomAttribute(type, typeof(CreateAssetMenuAttribute));
                if (attr == null)
                    continue;

                if (!type.IsSubclassOf(typeof(ScriptableObject)))
                {
                    Debug.LogWarningFormat("CreateAssetMenu attribute on {0} will be ignored as {0} is not derived from ScriptableObject.", type.FullName);
                    continue;
                }

                string menuItemName = (string.IsNullOrEmpty(attr.menuName)) ? ObjectNames.NicifyVariableName(type.Name) : attr.menuName;
                string fileName = (string.IsNullOrEmpty(attr.fileName)) ? ("New " + ObjectNames.NicifyVariableName(type.Name) + ".asset") : attr.fileName;
                if (!System.IO.Path.HasExtension(fileName))
                    fileName = fileName + ".asset";

                result.Add(new MonoCreateAssetItem
                {
                    menuItem = menuItemName,
                    fileName = fileName,
                    order = attr.order,
                    type = type
                });
            }

            return result.ToArray();
        }

        internal static ArrayList FindEditorClassesWithAttribute(Type attribType)
        {
            var attributedTypes = new ArrayList();
            foreach (var loadedType in EditorAssemblies.loadedTypes)
            {
                if (loadedType.GetCustomAttributes(attribType, false).Length != 0)
                    attributedTypes.Add(loadedType);
            }
            return attributedTypes;
        }

        static internal object InvokeMemberIfAvailable(object target, string methodName, object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                return method.Invoke(target, args);
            }
            else
            {
                return null;
            }
        }

        internal static bool GameObjectContainsAttribute(GameObject go, Type attributeType)
        {
            var behaviours = go.GetComponents(typeof(Component));
            for (var index = 0; index < behaviours.Length; index++)
            {
                var behaviour = behaviours[index];
                if (behaviour == null)
                    continue;

                var behaviourType = behaviour.GetType();
                if (behaviourType.GetCustomAttributes(attributeType, true).Length > 0)
                    return true;
            }
            return false;
        }

        internal static IEnumerable<T> CallMethodsWithAttribute<T>(Type attributeType, params object[] arguments)
        {
            foreach (var assembly in EditorAssemblies.loadedAssemblies)
            {
                foreach (var type in AssemblyHelper.GetTypesFromAssembly(assembly))
                {
                    foreach (var method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
                    {
                        if (method.GetCustomAttributes(attributeType, false).Length > 0)
                        {
                            yield return (T)method.Invoke(null, arguments);
                        }
                    }
                }
            }
        }
    }
}
