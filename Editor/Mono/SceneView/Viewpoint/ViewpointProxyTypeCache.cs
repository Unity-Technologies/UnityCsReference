// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    class ViewpointProxyTypeCache
    {
        static ViewpointProxyTypeCache[] s_Cache;

        Type m_ViewpointType;
        Type m_TranslatorType;

        internal Type viewpointType { get => m_ViewpointType; }
        internal Type translatorType { get => m_TranslatorType; }

        internal static ViewpointProxyTypeCache[] caches
        {
            get
            {
                if (s_Cache == null)
                    s_Cache = GetProxiesForViewpoints();
                return s_Cache;
            }
        }

        ViewpointProxyTypeCache(Type targetType, Type translatorType)
        {
            m_ViewpointType = targetType;
            m_TranslatorType = translatorType;
        }

        internal static IEnumerable<ViewpointProxyTypeCache> GetSupportedCameraComponents()
        {
            foreach (var entry in s_Cache)
            {
                bool found = false;
                foreach (Type interfaceType in entry.translatorType.GetInterfaces())
                {
                    if (interfaceType == typeof(ICameraLensData))
                        found = true;
                }

                if (!found)
                    continue;

                yield return entry;
            }
        }

        internal static Type GetTranslatorTypeForType(Type type)
        {
            foreach (var entry in caches)
            {
                if (entry.viewpointType == type)
                    return entry.translatorType;
            }
            return null;
        }

        internal static Texture2D GetIcon(IViewpoint viewpoint)
        {
            Texture2D icon = EditorGUIUtility.GetIconForObject(viewpoint.TargetObject);

            // This way works better to get the icon for the Camera component.
            if (icon == null)
                icon = EditorGUIUtility.FindTexture(viewpoint.TargetObject.GetType());

            return icon;
        }

        static ViewpointProxyTypeCache[] GetProxiesForViewpoints()
        {
            (Type, Type) k_NullData = (null, null);

            var types = TypeCache.GetTypesDerivedFrom<IViewpoint>();

            List<Type> filteredTypes = new List<Type>();
            foreach (var type in types)
            {
                if (!type.IsAbstract && !type.IsSubclassOf(typeof(MonoBehaviour)))
                    filteredTypes.Add(type);
            }

            List<ViewpointProxyTypeCache> viewpoints = new List<ViewpointProxyTypeCache>();

            foreach (Type type in filteredTypes)
            {
                (Type, Type) targetAndTranslatorTypes = GetGenericViewpointType(type);
                if (targetAndTranslatorTypes == k_NullData)
                    continue;

                viewpoints.Add(new ViewpointProxyTypeCache(targetAndTranslatorTypes.Item2, type));
            }
            return viewpoints.ToArray();
        }

        static (Type, Type) GetGenericViewpointType(Type type)
        {
            Type baseT = type.BaseType;

            while (baseT != null)
            {
                if (baseT.IsGenericType)
                {
                    if (baseT.GetGenericTypeDefinition() == typeof(Viewpoint<>))
                        return (baseT, baseT.GetGenericArguments()[0]);
                }
                baseT = baseT.BaseType;
            }
            return (null, null);
        }
    }
}
