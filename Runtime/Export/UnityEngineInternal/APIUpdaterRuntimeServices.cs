// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEngineInternal
{
    public sealed class APIUpdaterRuntimeServices
    {

        [Obsolete(@"AddComponent(string) has been deprecated. Use GameObject.AddComponent<T>() / GameObject.AddComponent(Type) instead.
API Updater could not automatically update the original call to AddComponent(string name), because it was unable to resolve the type specified in parameter 'name'.
Instead, this call has been replaced with a call to APIUpdaterRuntimeServices.AddComponent() so you can try to test your game in the editor.
In order to be able to build the game, replace this call (APIUpdaterRuntimeServices.AddComponent()) with a call to GameObject.AddComponent<T>() / GameObject.AddComponent(Type).")]
        public static Component AddComponent(GameObject go, string sourceInfo, string name)
        {
            Debug.LogWarningFormat("Performing a potentially slow search for component {0}.", name);

            var type = ResolveType(name, Assembly.GetCallingAssembly(), sourceInfo);
            return type == null
                ? null
                : go.AddComponent(type);
        }

        private static Type ResolveType(string name, Assembly callingAssembly, string sourceInfo)
        {
            var foundOnUnityEngine = ComponentsFromUnityEngine.FirstOrDefault(t => (t.Name == name || t.FullName == name) && !IsMarkedAsObsolete(t));
            if (foundOnUnityEngine != null)
            {
                Debug.LogWarningFormat("[{1}] Component type '{0}' found in UnityEngine, consider replacing with go.AddComponent<{0}>()", name, sourceInfo);
                return foundOnUnityEngine;
            }

            var candidateType = callingAssembly.GetType(name);
            if (candidateType != null)
            {
                Debug.LogWarningFormat("[{1}] Component type '{0}' found on caller assembly, consider replacing with go.AddComponent<{0}>()", name, sourceInfo);
                return candidateType;
            }

            candidateType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).SingleOrDefault(t => (t.Name == name || t.FullName == name) && typeof(Component).IsAssignableFrom(t));
            if (candidateType != null)
            {
                Debug.LogWarningFormat("[{2}] Component type '{0}' found on assembly {1}, consider replacing with go.AddComponent<{0}>()", name, new AssemblyName(candidateType.Assembly.FullName).Name, sourceInfo);
                return candidateType;
            }

            Debug.LogErrorFormat("[{1}] Component Type '{0}' not found.", name, sourceInfo);
            return null;
        }

        private static bool IsMarkedAsObsolete(Type t)
        {
            return t.GetCustomAttributes(typeof(ObsoleteAttribute), false).Any();
        }

        static APIUpdaterRuntimeServices()
        {
            var componentType = typeof(Component);
            ComponentsFromUnityEngine =  componentType.Assembly.GetTypes().Where(componentType.IsAssignableFrom).ToList();
        }

        private static IList<Type> ComponentsFromUnityEngine;
    }
}
