// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    [RequireComponent(typeof(Camera))]
    [Obsolete("The Flare Layer component is deprecated now that the Built-In Render Pipeline is deprecated. To use an alternative, refer to the documentation in the component help icon. #from(6000.5)", false)]
    [HelpURL("create-lens-flare")]
    public class FlareLayer : Behaviour
    {
        internal FlareLayer() {}
    }
}
