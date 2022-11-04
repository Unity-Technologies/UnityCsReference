// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Bindings;
using PassIdentifier = UnityEngine.Rendering.PassIdentifier;

namespace UnityEditor.ShaderFoundry
{
    // THIS ENUM MUST BE KEPT IN SYNC WITH THE ENUM IN PassStageType.h
    enum PassStageType : short
    {
        Invalid = -1,
        Vertex = 0,
        HullConstant,
        Hull,
        Domain,
        Geometry,
        Fragment,
        Count,
    }
}
