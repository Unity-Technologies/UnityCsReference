// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    public sealed partial class Wire
    {
        internal VirtualWire m_Implementation;

        internal Wire(IPort output, IPort input, VirtualWire virtualWire)
        {
            OutputPort = output ?? throw new ArgumentNullException(nameof(output));
            InputPort = input ?? throw new ArgumentNullException(nameof(input));
            m_Implementation = virtualWire ?? throw new ArgumentNullException(nameof(virtualWire));
        }
    }
}
