// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


namespace UnityEngine
{
public sealed partial class Security
{
    [System.Obsolete ("Security.PrefetchSocketPolicy is no longer supported, since the Unity Web Player is no longer supported by Unity.", true)]
[uei.ExcludeFromDocs]
public static bool PrefetchSocketPolicy (string ip, int atPort) {
    int timeout = 3000;
    return PrefetchSocketPolicy ( ip, atPort, timeout );
}

[System.Obsolete ("Security.PrefetchSocketPolicy is no longer supported, since the Unity Web Player is no longer supported by Unity.", true)]
public static bool PrefetchSocketPolicy(string ip, int atPort, [uei.DefaultValue("3000")]  int timeout )
        {
            return false;
        }

    
    
}

}
