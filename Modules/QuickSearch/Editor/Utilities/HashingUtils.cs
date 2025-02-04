// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEditor.Search
{
    static class HashingUtils
    {
        public static int GetHashCode(string value)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < value.Length && value[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ value[i];
                    if (i == value.Length - 1 || value[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ value[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        public static int GetHashCode(int value)
        {
            return value;
        }

        public static int GetHashCode(long value)
        {
            return (unchecked((int)((long)value)) ^ (int)(value >> 32));
        }
    }
}
