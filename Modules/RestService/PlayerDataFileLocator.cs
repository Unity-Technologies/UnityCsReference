// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
namespace UnityEditor.Experimental.RestService
{
    [Obsolete("This type is obsolete and will be deleted.", true)]
    public class PlayerDataFileLocator
    {
        public delegate bool Locator(ref string path);
        public static void Register(Locator locator)
        {
            throw new NotImplementedException();
        }

        [UnityEngine.Scripting.RequiredByNativeCode]
        static string LocatePlayerDataFile(string path)
        {
            throw new NotImplementedException();
        }
    }
}
