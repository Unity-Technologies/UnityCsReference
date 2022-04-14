// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    [FoundryAPI]
    internal interface ITemplateLinker
    {
        public abstract void Link(ShaderBuilder builder, ShaderContainer shaderContainer, TemplateInstance templateInstance);
    }
}
