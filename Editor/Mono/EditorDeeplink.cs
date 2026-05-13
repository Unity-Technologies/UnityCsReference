// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class DeeplinkHandlerAttribute : Attribute
    {
        public string HandlerNamespace { get; }
        public DeeplinkHandlerAttribute(string value) => HandlerNamespace = value.ToLowerInvariant();
    }

    internal static class EditorDeeplink
    {
        [RequiredByNativeCode]
        public static void Internal_OpenDeeplinkInEditor(string openInEditorUrl)
        {
            if (!IsValidOpenInEditorUrl(openInEditorUrl,  out var uri))
            {
                Debug.LogWarning(string.Format(L10n.Tr("Invalid URL \"{0}\". Ignoring deeplink operation."), openInEditorUrl));
                return;
            }

            if (!TryGetUrlNamespaceTarget(openInEditorUrl, out var urlNamespaceTarget))
            {
                Debug.LogWarning(string.Format(L10n.Tr("Invalid URL \"{0}\", missing namespace target. Ignoring deeplink operation."), openInEditorUrl));
                return;
            }

            var deeplinkHandler = FindDeeplinkHandler(urlNamespaceTarget);
            if (deeplinkHandler == null)
            {
                Debug.LogWarning(string.Format(L10n.Tr("Missing handler for URL \"{0}\". Ignoring deeplink operation."), openInEditorUrl));
                return;
            }

            var handlerReflectedType = deeplinkHandler.ReflectedType;
            var handlerReflectedAssembly = handlerReflectedType.Assembly;
            UnityEditor.PackageManager.PackageInfo methodPackageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(handlerReflectedAssembly);
            // If the assembly of the handler is in a package from a Unity trusted registry
            if (methodPackageInfo != null && methodPackageInfo.registry.isDefault)
            {
                deeplinkHandler.Invoke(null, new object[] { uri });
            }
            else
            {
                // Otherwise prompt user for manual confirmation
                if (InternalEditorUtility.isHumanControllingUs)
                {
                    var dialogText = new StringBuilder(string.Format(L10n.Tr("An URL targeting the \"{0}\" namespace has been received:\n"), urlNamespaceTarget));
                    dialogText.AppendLine(openInEditorUrl);

                    bool openURL = EditorUtility.DisplayDialog(L10n.Tr("Open URL in Editor"),
                        dialogText.ToString(),
                        L10n.Tr("Open"), L10n.Tr("Dismiss"));

                    if (openURL)
                    {
                        deeplinkHandler.Invoke(null, new object[] { uri });
                    }
                }
                else
                {
                    Debug.LogWarning(string.Format(L10n.Tr("Unvalidated URL \"{0}\" requires manual user confirmation before opening. Ignoring deeplink operation."), openInEditorUrl));
                }
            }
        }

        static MethodInfo FindDeeplinkHandler(string handlerNamespace)
        {
            foreach (var methodInfo in EditorAssemblies.GetAllMethodsWithAttribute<DeeplinkHandlerAttribute>(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (methodInfo.GetParameters().Length != 1)
                {
                    Debug.LogWarning(string.Format(L10n.Tr("Missing System.Uri parameter in method '{0} {1}'."), methodInfo.DeclaringType.FullName, methodInfo.Name));
                    continue;
                }
                if (methodInfo.GetParameters()[0].ParameterType != typeof(Uri))
                {
                    Debug.LogWarning(string.Format(L10n.Tr("Wrong parameter type in method '{0} {1}'. Expecting System.Uri parameter."), methodInfo.DeclaringType.FullName, methodInfo.Name));
                    continue;
                }
                var deeplinkHandlerAttribute = methodInfo.GetCustomAttribute<DeeplinkHandlerAttribute>();
                if(!deeplinkHandlerAttribute.HandlerNamespace.ToLowerInvariant().Equals(handlerNamespace)) continue;

                var handlerAssemblyQualifiedName = methodInfo.ReflectedType.AssemblyQualifiedName;
                // If the handler's assembly qualified name is a parent or a match for the requested namespace
                if (handlerAssemblyQualifiedName == null || !handlerAssemblyQualifiedName.ToLowerInvariant()
                        .StartsWith(deeplinkHandlerAttribute.HandlerNamespace)) continue;

                return methodInfo;
            }
            return null;
        }

        static bool IsValidOpenInEditorUrl(string url, out Uri uri)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return false;
            return uri.Host.ToLowerInvariant().Equals("editor") && uri.Scheme.ToLowerInvariant().Equals("com.unity.editor");
        }

        // Extract the handler.namespace from openInEditorUrl, which is formatted like this: com.unity.editor://editor/handler.namespace/
        static bool TryGetUrlNamespaceTarget(string url, out string urlNamespaceTarget)
        {
            urlNamespaceTarget= string.Empty;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            if (uri.Segments.Length < 2) return false;
            urlNamespaceTarget = uri.Segments[1].ToLowerInvariant();
            if(urlNamespaceTarget.EndsWith("/"))
                urlNamespaceTarget = urlNamespaceTarget.Remove(urlNamespaceTarget.Length - 1);
            return urlNamespaceTarget.Length > 0;
        }
    }
}
