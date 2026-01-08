// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Reflection;
using System.Collections.Generic;

namespace UnityEditor.MPE
{
    public partial class ProcessService
    {
        struct RoleProvider
        {
            public string name;
            public ProcessLevel level;
            public ProcessEvent eventType;
            public MethodInfo execute;
        };

        static List<RoleProvider> s_RoleProviders;
    }
}
