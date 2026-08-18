using System.Reflection;

namespace Unity.Scripting.LifecycleManagement
{
    internal sealed class LifecycleMethodRegistry
    {
        private readonly Dictionary<Type, Dictionary<Assembly, List<LifecycleMethodData>>> _lifecycleCallbacks = new();
        private readonly Dictionary<Assembly, HashSet<Type>> _assemblyToAttributeTypes = new();

        public void Register(Type lifecycleAttributeType, Assembly assembly, string methodFullName, Action callback)
        {
            if (!_lifecycleCallbacks.TryGetValue(lifecycleAttributeType, out var typeCallbacks))
            {
                typeCallbacks = new Dictionary<Assembly, List<LifecycleMethodData>>();
                _lifecycleCallbacks.Add(lifecycleAttributeType, typeCallbacks);
            }

            if (!typeCallbacks.TryGetValue(assembly, out var assemblyCallbacks))
            {
                assemblyCallbacks = new List<LifecycleMethodData>();
                typeCallbacks.Add(assembly, assemblyCallbacks);
            }

            assemblyCallbacks.Add(new LifecycleMethodData(methodFullName, callback));

            if (!_assemblyToAttributeTypes.TryGetValue(lifecycleAttributeType.Assembly, out var attributeTypes))
            {
                attributeTypes = new HashSet<Type>();
                _assemblyToAttributeTypes.Add(lifecycleAttributeType.Assembly, attributeTypes);
            }

            attributeTypes.Add(lifecycleAttributeType);
        }

        private static readonly List<LifecycleMethodData> s_NoMethods = new();

        internal List<LifecycleMethodData> Get(Type lifecycleAttributeType, IReadOnlyList<Assembly> assemblies)
        {
            // Early-out without allocating: most attribute types have no registered methods at all
            // and this runs on every scope transition (multiple times per domain reload).
            if (!_lifecycleCallbacks.TryGetValue(lifecycleAttributeType, out var typeCallbacks) || typeCallbacks.Count == 0)
            {
                return s_NoMethods;
            }

            List<LifecycleMethodData>? result = null;
            foreach (var assembly in assemblies)
            {
                if (typeCallbacks.TryGetValue(assembly, out var assemblyCallbacks))
                {
                    result ??= new List<LifecycleMethodData>();
                    result.AddRange(assemblyCallbacks);
                }
            }

            return result ?? s_NoMethods;
        }

        internal void Clear(IReadOnlyList<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                if (_assemblyToAttributeTypes.Remove(assembly, out var attributeTypes))
                {
                    foreach (var attributeType in attributeTypes)
                    {
                        _lifecycleCallbacks.Remove(attributeType);
                    }
                }

                foreach (var typeCallbacks in _lifecycleCallbacks.Values)
                {
                    typeCallbacks.Remove(assembly);
                }
            }
        }

        // Used by tests.
        internal bool ContainsAttributeType(Type attributeType)
        {
            if (_lifecycleCallbacks.ContainsKey(attributeType))
            {
                return true;
            }

            foreach (var attributeTypes in _assemblyToAttributeTypes.Values)
            {
                if (attributeTypes.Contains(attributeType))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class LifecycleMethodData
    {
        public readonly string FullName;
        public readonly Action Callback;

        public LifecycleMethodData(string fullName, Action callback)
        {
            FullName = fullName;
            Callback = callback;
        }
    }
}
