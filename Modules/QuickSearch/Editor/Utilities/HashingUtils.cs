// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEditor.Search
{
    static class HashingUtils
    {
        /// <summary>
        /// Calculates a stable hashcode for a string.
        /// Taken from https://github.com/microsoft/referencesource/blob/main/mscorlib/system/string.cs
        /// </summary>
        /// <param name="value">The string to compute the hashcode from.</param>
        /// <returns>A stable hashcode.</returns>
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

        /// <summary>
        /// Calculates a stable hashcode for an IStringView that is identical to the stable hashcode of a string with the same content.
        /// Taken from https://github.com/microsoft/referencesource/blob/main/mscorlib/system/string.cs
        /// </summary>
        /// <param name="sv">The <see cref="IStringView"/> to compute the hashcode from.</param>
        /// <returns>A stable hashcode.</returns>
        public static int GetHashCode(IStringView sv)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < sv.length && sv[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ sv[i];
                    if (i == sv.length - 1 || sv[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ sv[i + 1];
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

        public static ulong GetHashCode64(this string strText)
        {
            if (string.IsNullOrEmpty(strText))
                return 0;
            // Using http://www.isthe.com/chongo/tech/comp/fnv/index.html#FNV-1a
            // with basis and prime:
            const ulong offsetBasis = 14695981039346656037;
            const ulong prime = 1099511628211;

            ulong result = offsetBasis;

            foreach (var c in strText)
            {
                result = prime * (result ^ (byte)(c & 255));
                result = prime * (result ^ (byte)(c >> 8));
            }

            return result;
        }
    }
}
