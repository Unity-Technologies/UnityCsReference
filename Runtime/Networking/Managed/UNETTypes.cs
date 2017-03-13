// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;

namespace UnityEngine.Networking
{
    namespace Types
    {
        [DefaultValue(NetworkAccessLevel.Invalid)]
        public enum NetworkAccessLevel : ulong { Invalid = 0, User = 1 << 0, Owner = 1 << 1, Admin = 1 << 2 }

        [DefaultValue(AppID.Invalid)]
        public enum AppID : ulong { Invalid = ulong.MaxValue }

        [DefaultValue(SourceID.Invalid)]
        public enum SourceID : ulong { Invalid = ulong.MaxValue }

        [DefaultValue(NetworkID.Invalid)]
        public enum NetworkID : ulong { Invalid = ulong.MaxValue }

        [DefaultValue(NodeID.Invalid)]
        public enum NodeID : ushort { Invalid = 0 }

        [DefaultValue(HostPriority.Invalid)]
        public enum HostPriority : int { Invalid = int.MaxValue }

        public class NetworkAccessToken
        {
            private const int NETWORK_ACCESS_TOKEN_SIZE = 64;

            public NetworkAccessToken()
            {
                array = new byte[NETWORK_ACCESS_TOKEN_SIZE];
            }

            public NetworkAccessToken(byte[] array)
            {
                this.array = array;
            }

            public NetworkAccessToken(string strArray)
            {
                try
                {
                    array = Convert.FromBase64String(strArray);
                }
                catch (Exception)
                {
                    array = new byte[NETWORK_ACCESS_TOKEN_SIZE];
                }
            }

            public string GetByteString()
            {
                return Convert.ToBase64String(array);
            }

            public bool IsValid()
            {
                // This contains a valid token if we have an array, it's size is correct, and it is not filled with 0
                if (array == null || array.Length != NETWORK_ACCESS_TOKEN_SIZE)
                    return false;

                // Verify the array is not zeroed
                bool bFoundNonZero = false;
                foreach (byte element in array)
                {
                    if (element != 0)
                    {
                        bFoundNonZero = true;
                        break;
                    }
                }

                return bFoundNonZero;
            }

            // Numeric types default 0 which is important for non set tokens
            public byte[] array;
        }
    }

    // FIXME: Yank these when we can properly enumerate them from other sources like Cloud code
    public partial class Utility
    {
        [Obsolete("This property is unused and should not be referenced in code.", true)]
        public static bool useRandomSourceID { get { return false; } set {} }

        private static Dictionary<Types.NetworkID, Types.NetworkAccessToken> s_dictTokens = new Dictionary<Types.NetworkID, Types.NetworkAccessToken>();

        private Utility() {}

        public static Types.SourceID GetSourceID()
        {
            return (Types.SourceID)(SystemInfo.deviceUniqueIdentifier).GetHashCode();
        }


        [Obsolete("This function is unused and should not be referenced in code. Please sign in and setup your project in the editor instead.", true)]
        public static void SetAppID(Types.AppID newAppID)
        {
        }

        [Obsolete("This function is unused and should not be referenced in code. Please sign in and setup your project in the editor instead.", true)]
        public static Types.AppID GetAppID()
        {
            return Types.AppID.Invalid;
        }

        public static void SetAccessTokenForNetwork(Types.NetworkID netId, Types.NetworkAccessToken accessToken)
        {
            // If we're updating an existing token we need to remove the stale one first
            if (s_dictTokens.ContainsKey(netId))
                s_dictTokens.Remove(netId);

            s_dictTokens.Add(netId, accessToken);
        }

        public static Types.NetworkAccessToken GetAccessTokenForNetwork(Types.NetworkID netId)
        {
            Types.NetworkAccessToken ret;
            if (s_dictTokens.TryGetValue(netId, out ret) == false)
            {
                ret = new Types.NetworkAccessToken();
            }
            return ret;
        }
    }
}
