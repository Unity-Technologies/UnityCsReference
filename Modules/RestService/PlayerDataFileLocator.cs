// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;

namespace UnityEditor.Experimental.RestService
{
    // TODO: Force file locators to register using a path prefix to do quick locator lookup
    // TODO: Move code to native to avoid calling into c# when there is no locator for a certain path prefix
    public class PlayerDataFileLocator
    {
        // return true if path was remapped
        public delegate bool Locator(ref string path);

        private static HashSet<Locator> m_Locators = new HashSet<Locator>();

        public static void Register(Locator locator)
        {
            m_Locators.Add(locator);
        }

        [UnityEngine.Scripting.RequiredByNativeCode]
        static string LocatePlayerDataFile(string path)
        {
            foreach (var locator in m_Locators)
            {
                if (locator(ref path))
                    return path;
            }
            return "Library/PlayerDataCache/" + path;
        }
    }
}
