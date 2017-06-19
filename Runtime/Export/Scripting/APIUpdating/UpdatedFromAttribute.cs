// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Scripting.APIUpdating
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Interface)]
    public class MovedFromAttribute : Attribute
    {
        public MovedFromAttribute(string sourceNamespace) : this(sourceNamespace, false)
        {
        }

        public MovedFromAttribute(string sourceNamespace, bool isInDifferentAssembly)
        {
            Namespace = sourceNamespace;
            IsInDifferentAssembly = isInDifferentAssembly;
        }

        public string Namespace { get; private set; }
        public bool IsInDifferentAssembly {  get; private set; }
    }
}
