// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.Sprites
{
    // TightRotateEnabledSpritePackerPolicy will tightly pack non-rectangle Sprites unless their packing tag contains "[RECT]" with rotation and flipping for optimal packing.
    internal class TightRotateEnabledSpritePackerPolicy : DefaultPackerPolicy
    {
        protected override string TagPrefix { get { return "[RECT]"; } }
        protected override bool AllowTightWhenTagged { get { return false; } }
        protected override bool AllowRotationFlipping { get { return true; } }
    }
}
