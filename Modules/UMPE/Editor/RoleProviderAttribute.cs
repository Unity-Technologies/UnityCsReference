// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace Unity.MPE
{
    [UnityEngine.Internal.ExcludeFromDocs]
    [RequiredByNativeCode, AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class RoleProviderAttribute : Attribute
    {
        public RoleProviderAttribute(string name, ProcessEvent eventType)
        {
            this.name = name;
            this.eventType = eventType;
            this.level = ProcessLevel.UMP_UNDEFINED;
        }

        public RoleProviderAttribute(ProcessLevel level, ProcessEvent eventType)
        {
            this.name = level.ToString();
            this.level = level;
            this.eventType = eventType;
        }

        public string name;
        public ProcessEvent eventType;
        public ProcessLevel level;
    }
}
