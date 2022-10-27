// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    class BlackboardCreationContext : IViewContext
    {
        public static readonly BlackboardCreationContext VariableCreationContext = new BlackboardCreationContext();
        public static readonly BlackboardCreationContext VariablePropertyCreationContext = new BlackboardCreationContext();
        public bool Equals(IViewContext other)
        {
            return ReferenceEquals(this, other);
        }
    }
}
