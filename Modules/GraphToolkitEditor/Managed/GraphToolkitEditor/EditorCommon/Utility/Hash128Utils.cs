// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.InternalBridge;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    static class Hash128Utils
    {
        public static void Append(ref this Hash128 self, Hash128 h)
        {
            var (ul0, ul1) = h.ToParts();

            self.Append((int)(ul0 & uint.MaxValue));
            self.Append((int)(ul0 >> 32));
            self.Append((int)(ul1 & uint.MaxValue));
            self.Append((int)(ul1 >> 32));
        }
    }
}
