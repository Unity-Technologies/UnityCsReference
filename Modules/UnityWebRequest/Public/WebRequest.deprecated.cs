// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEngine.Networking
{
    public partial class UnityWebRequest
    {
        [System.Obsolete("UnityWebRequest.isError has been renamed to isNetworkError for clarity. (UnityUpgradable) -> isNetworkError", false)]
        public bool isError
        {
            get { return isNetworkError; }
        }
    }
}
