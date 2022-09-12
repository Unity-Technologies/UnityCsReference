// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEngine.Android
{
    internal static class Common
    {
        private static AndroidJavaObject m_Activity;

        public static AndroidJavaObject GetActivity()
        {
            if (m_Activity != null)
                return m_Activity;

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                m_Activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }

            return m_Activity;
        }
    }
}
