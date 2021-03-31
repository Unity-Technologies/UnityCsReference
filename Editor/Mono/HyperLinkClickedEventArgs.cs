// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor
{
    public class HyperLinkClickedEventArgs
    {
        public Dictionary<string, string> hyperLinkData { get; private set; }

        internal HyperLinkClickedEventArgs(Dictionary<string, string> hyperLinkData)
        {
            this.hyperLinkData = hyperLinkData;
        }
    }
}
