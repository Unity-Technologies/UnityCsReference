// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEditor.ShortcutManagement
{
    class Discovery : IDiscovery
    {
        public IEnumerable<ShortcutEntry> GetAllShortcuts()
        {
            var staticMethodsBindings = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var methods = EditorAssemblies.GetAllMethodsWithAttribute<ShortcutBaseAttribute>(staticMethodsBindings);

            var results = new List<ShortcutEntry>(methods.Count());
            foreach (var methodInfo in methods)
            {
                var attributes = (ShortcutBaseAttribute[])methodInfo.GetCustomAttributes(typeof(ShortcutBaseAttribute), true);
                foreach (var attribute in attributes)
                {
                    var shortcutEntry = attribute.CreateShortcutEntry(methodInfo);
                    results.Add(shortcutEntry);
                }
            }

            return results;
        }
    }
}
