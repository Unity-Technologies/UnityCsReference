// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEngine.Playables
{
    internal partial struct PlayableOutput
    {
        const int m_NullVersion = Int32.MaxValue;
        static readonly PlayableOutput m_NullPlayableOutput = new PlayableOutput { m_Version = m_NullVersion };

        internal static PlayableOutput Null { get { return m_NullPlayableOutput; } }
    }
}
