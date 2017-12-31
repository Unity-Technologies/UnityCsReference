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
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed partial class RequiredSignatureAttribute : Attribute {}

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

            foreach (var mi in EditorAssemblies.GetAllMethodsWithAttribute<DrawGizmo>(BindingFlags.Static).Where(m => m.DeclaringType.Assembly == assembly))
            {
                var attrs = mi.GetCustomAttributes(typeof(DrawGizmo), false).Cast<DrawGizmo>();
                foreach (var gizmoAttr in attrs)
                {
                    var parameters = mi.GetParameters();
                    if (parameters.Length != 2)
                    {
                        Debug.LogWarningFormat(
                            "Method {0}.{1} is marked with the DrawGizmo attribute but does not take parameters (ComponentType, GizmoType) so will be ignored.",
                            mi.DeclaringType.FullName, mi.Name
                            );
                        continue;
                    }
                    if (mi.DeclaringType != null && mi.DeclaringType.IsGenericTypeDefinition)
                    {
                        Debug.LogWarningFormat(
                            "Method {0}.{1} is marked with the DrawGizmo attribute but is defined on a generic type definition, so will be ignored.",
                            mi.DeclaringType.FullName, mi.Name
                            );
                        continue;
                    }

                    var item = new MonoGizmoMethod();

                    if (gizmoAttr.drawnType == null)
                        item.drawnType = parameters[0].ParameterType;
                    else if (parameters[0].ParameterType.IsAssignableFrom(gizmoAttr.drawnType))
                        item.drawnType = gizmoAttr.drawnType;
                    else
                    {
                        Debug.LogWarningFormat(
                            "Method {0}.{1} is marked with the DrawGizmo attribute but the component type it applies to could not be determined.",
                            mi.DeclaringType.FullName, mi.Name
                            );
                        continue;
                    }

                    if (parameters[1].ParameterType != typeof(GizmoType) &&
                        parameters[1].ParameterType != typeof(int))
                    {
                        Debug.LogWarningFormat(
                            "Method {0}.{1} is marked with the DrawGizmo attribute but does not take a second parameter of type GizmoType so will be ignored.",
                            mi.DeclaringType.FullName, mi.Name
                            );
                        continue;
                    }

                    item.drawGizmo = mi;
                    item.options = (int)gizmoAttr.drawOptions;

                    commands.Add(item);
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
            var result = new List<MonoCreateAssetItem>();

            foreach (var type in EditorAssemblies.GetAllTypesWithAttribute<CreateAssetMenuAttribute>())
            {
                var attr = type.GetCustomAttributes(typeof(CreateAssetMenuAttribute), false).FirstOrDefault() as CreateAssetMenuAttribute;
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

                var item = new MonoCreateAssetItem
                {
                    menuItem = menuItemName,
                    fileName = fileName,
                    order = attr.order,
                    type = type
                };
                result.Add(item);
            }

            return result.ToArray();
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

        internal static bool GameObjectContainsAttribute<T>(GameObject go) where T : Attribute
        {
            var behaviours = go.GetComponents(typeof(Component));
            for (var index = 0; index < behaviours.Length; index++)
            {
                var behaviour = behaviours[index];
                if (behaviour == null)
                    continue;

                var behaviourType = behaviour.GetType();
                if (behaviourType.GetCustomAttributes(typeof(T), true).Length > 0)
                    return true;
            }
            return false;
        }

        internal static IEnumerable<TReturnValue> CallMethodsWithAttribute<TReturnValue, TAttr>(params object[] arguments)
            where TAttr : Attribute
        {
            foreach (var method in EditorAssemblies.GetAllMethodsWithAttribute<TAttr>(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                yield return (TReturnValue)method.Invoke(null, arguments);
        }

        private static bool AreSignaturesMatching(MethodInfo left, MethodInfo right)
        {
            if (left.IsStatic != right.IsStatic)
                return false;
            if (left.ReturnType != right.ReturnType)
                return false;


            ParameterInfo[] leftParams = left.GetParameters();
            ParameterInfo[] rightParams = right.GetParameters();
            if (leftParams.Length != rightParams.Length)
                return false;
            for (int i = 0; i < leftParams.Length; i++)
            {
                if (leftParams[i].ParameterType != rightParams[i].ParameterType)
                    return false;
            }

            return true;
        }

        internal static string MethodToString(MethodInfo method)
        {
            return string.Format("{0}{1}", method.IsStatic ? "static " : "", method);
        }

        internal static bool MethodMatchesAnyRequiredSignatureOfAttribute(MethodInfo method, Type attributeType)
        {
            List<MethodInfo> validSignatures = new List<MethodInfo>();
            foreach (var signature in attributeType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var requiredSignatureAttributes = signature.GetCustomAttributes(typeof(RequiredSignatureAttribute), false);
                if (requiredSignatureAttributes.Length > 0)
                {
                    if (AreSignaturesMatching(method, signature))
                    {
                        return true;
                    }
                    validSignatures.Add(signature);
                }
            }
            if (validSignatures.Count == 0)
                Debug.LogError(MethodToString(method) + " has an invalid attribute : " + attributeType + ". " + attributeType +  " must have at least one required signature declaration");
            else if (validSignatures.Count == 1)
                Debug.LogError(MethodToString(method) + " does not match " + attributeType + " expected signature.\n Use " + MethodToString(validSignatures[0]));
            else
                Debug.LogError(MethodToString(method) + " does not match any of " + attributeType + " expected signatures.\n Valid signatures are: " + string.Join(" , ", validSignatures.Select((a) => MethodToString(a)).ToArray()));
            return false;
        }

        internal struct MethodWithAttribute
        {
            public MethodInfo info;
            public Attribute attribute;
        }

        internal class MethodInfoSorter
        {
            internal MethodInfoSorter(List<MethodWithAttribute> methodsWithAttributes)
            {
                this.methodsWithAttributes = methodsWithAttributes;
            }

            public IEnumerable<MethodInfo> FilterAndSortOnAttribute<T>(Func<T, bool> filter, Func<T, IComparable> sorter) where T : Attribute
            {
                return methodsWithAttributes.Where(a => filter((T)a.attribute)).OrderBy(c => sorter((T)c.attribute)).Select(o => o.info);
            }

            public IEnumerable<MethodWithAttribute> methodsWithAttributes { get; }
        }

        static Dictionary<Type, MethodInfoSorter> s_DecoratedMethodsByAttrTypeCache = new Dictionary<Type, MethodInfoSorter>();
        private const BindingFlags kAllStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal static MethodInfoSorter GetMethodsWithAttribute<T>(BindingFlags bindingFlags = kAllStatic) where T : Attribute
        {
            MethodInfoSorter result;
            if (!s_DecoratedMethodsByAttrTypeCache.TryGetValue(typeof(T), out result))
            {
                var tmp = new List<MethodWithAttribute>();
                foreach (var method in EditorAssemblies.GetAllMethodsWithAttribute<T>(bindingFlags))
                {
                    if (method.IsGenericMethod)
                    {
                        Debug.LogErrorFormat(
                            "{0} is a generic method. {1} cannot be applied to it.", MethodToString(method), typeof(T)
                            );
                    }
                    else
                    {
                        foreach (var attr in method.GetCustomAttributes(typeof(T), false))
                        {
                            if (MethodMatchesAnyRequiredSignatureOfAttribute(method, typeof(T)))
                            {
                                var methodWithAttribute = new MethodWithAttribute { info = method, attribute = (T)attr };
                                tmp.Add(methodWithAttribute);
                            }
                        }
                    }
                }

                result = new MethodInfoSorter(tmp);
                s_DecoratedMethodsByAttrTypeCache[typeof(T)] = result;
            }
            return result;
        }
    }
}
