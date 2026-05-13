using System.Collections;
using System.Reflection;

namespace Unity.Scripting.LifecycleManagement
{
    internal sealed class ReadOnlyAssemblyList : IReadOnlyList<Assembly>
    {
        private readonly IReadOnlyList<Assembly> _assemblies;
        private readonly Dictionary<string, Assembly> _assemblyLookup;

        public ReadOnlyAssemblyList(IReadOnlyList<Assembly> assemblies)
        {
            _assemblies = assemblies;

            _assemblyLookup = new Dictionary<string, Assembly>();
            foreach (var assembly in assemblies)
            {
                _assemblyLookup[assembly.GetName().Name] = assembly;
            }
        }

        public Assembly this[int index] => _assemblies[index];

        public int Count => _assemblies.Count;

        public IEnumerator<Assembly> GetEnumerator() => _assemblies.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _assemblies.GetEnumerator();

        public bool Contains(string assemblyName) => _assemblyLookup.ContainsKey(assemblyName);

        public bool Contains(AssemblyName assemblyName) => Contains(assemblyName.Name);

        public bool TryGetAssembly(string assemblyName, out Assembly assembly)
        {
            return _assemblyLookup.TryGetValue(assemblyName, out assembly);
        }

        public override string ToString() => $"[{string.Join(", ", _assemblyLookup.Keys)}]";
    }
}
