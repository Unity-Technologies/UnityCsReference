using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal static class VisualElementUtils
    {
        private static readonly HashSet<string> s_usedNames = new HashSet<string>();

        public static string GetUniqueName(string nameBase)
        {
            string name = nameBase;
            int counter = 2;
            while (s_usedNames.Contains(name))
            {
                name = nameBase + counter;
                counter++;
            }
            s_usedNames.Add(name);
            return name;
        }
    }
}
