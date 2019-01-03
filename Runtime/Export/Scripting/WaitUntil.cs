// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public sealed class WaitUntil : CustomYieldInstruction
    {
        Func<bool> m_Predicate;

        public override bool keepWaiting { get { return !m_Predicate(); } }

        public WaitUntil(Func<bool> predicate) { m_Predicate = predicate; }
    }
}
