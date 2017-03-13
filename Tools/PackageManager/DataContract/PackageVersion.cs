// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;

namespace Unity.DataContract
{
    public class PackageVersion : IComparable
    {
        public int major { get; private set; }
        public int minor { get; private set; }
        public int micro { get; private set; }
        public string special { get; private set; }
        public string text { get; private set; }

        public int parts { get; private set; }

        // major.minor.micro.special (or any non-numeric between micro and special)
        // major and minor are required; micro and special are not
        static readonly string kVersionMatch = @"(?<major>\d+)\.(?<minor>\d+)(\.(?<micro>\d+))?(\.?(?<special>.+))?";

        public PackageVersion(string version)
        {
            if (null == version)
                return;

            var match = Regex.Match(version, kVersionMatch);
            if (!match.Success)
                throw new ArgumentException("Invalid version: " + version);

            major = int.Parse(match.Groups["major"].Value);
            minor = int.Parse(match.Groups["minor"].Value);
            micro = 0;
            special = string.Empty;
            parts = 2;
            if (match.Groups["micro"].Success)
            {
                micro = int.Parse(match.Groups["micro"].Value);
                parts = 3;
            }

            if (match.Groups["special"].Success)
            {
                special = match.Groups["special"].Value;
                parts = 4;
                if (!ValidateSpecial())
                    throw new ArgumentException("Invalid version: " + version);
            }

            text = version;
        }

        public override string ToString()
        {
            return text;
        }

        public int CompareTo(object obj)
        {
            var other = obj as PackageVersion;
            if (this > other)
                return 1;
            if (this == other)
                return 0;
            return -1;
        }

        public override int GetHashCode()
        {
            if (text == null)
                return 0;

            return text.GetHashCode();
        }

        public bool IsCompatibleWith(PackageVersion other)
        {
            if (other == null)
                return false;

            if (this == other)
                return true;

            if ((parts == 2 && other.parts > 2) &&
                major == other.major &&
                minor == other.minor
                )
                return true;

            if ((parts == 3 && other.parts >= 3) &&
                major == other.major &&
                minor == other.minor &&
                micro == other.micro)
                return true;
            return false;
        }

        public override bool Equals(object obj)
        {
            var other = obj as PackageVersion;
            return this == other;
        }

        public static bool operator==(PackageVersion a, PackageVersion z)
        {
            if ((object)a == null && (object)z == null)
                return true;
            if ((object)a == null || (object)z == null)
                return false;
            return a.major == z.major && a.minor == z.minor && a.micro == z.micro && a.special == z.special;
        }

        public static bool operator!=(PackageVersion a, PackageVersion z)
        {
            return !(a == z);
        }

        public static bool operator>(PackageVersion a, PackageVersion z)
        {
            if ((object)a == null && (object)z == null)
                return false;
            if ((object)a == null)
                return false;
            if ((object)z == null)
                return true;

            if (a == z)
                return false;

            if (a.major != z.major)
                return a.major > z.major;

            if (a.minor != z.minor)
                return a.minor > z.minor;

            if (a.micro != z.micro)
                return a.micro > z.micro;

            if (a.parts != z.parts)
            {
                if (a.parts == 4)
                    return char.IsDigit(a.special[0]);
                return !char.IsDigit(z.special[0]);
            }

            for (int i = 0, j = 0; i < a.special.Length && j < z.special.Length;)
            {
                while (i < a.special.Length && j < z.special.Length && !char.IsDigit(a.special[i]) && !char.IsDigit(z.special[j]))
                {
                    if (a.special[i] != z.special[j])
                        return a.special[i] > z.special[j];
                    i++;
                    j++;
                }

                if (i < a.special.Length && j < z.special.Length && (!char.IsDigit(a.special[i]) || !char.IsDigit(z.special[j])))
                    return char.IsDigit(a.special[i]);

                var nonDigitPosA = FindFirstNonDigit(a.special.Substring(i));
                var nonDigitPosZ = FindFirstNonDigit(z.special.Substring(j));

                var numberA = -1;
                if (nonDigitPosA > -1)
                {
                    numberA = int.Parse(a.special.Substring(i, nonDigitPosA));
                    i += nonDigitPosA;
                }
                else
                {
                    int.TryParse(a.special.Substring(i), out numberA);
                    i = a.special.Length;
                }

                var numberZ = -1;
                if (nonDigitPosZ > -1)
                {
                    numberZ = int.Parse(z.special.Substring(j, nonDigitPosZ));
                    j += nonDigitPosZ;
                }
                else
                {
                    int.TryParse(z.special.Substring(j), out numberZ);
                    j = z.special.Length;
                }

                if (numberA != numberZ)
                    return numberA > numberZ;
            }

            return a.special.Length < z.special.Length;
        }

        /// <summary>
        /// Validates the special part of a version for 0-numbered parts (i.e., 5.1.2.0alpha1 is invalid, 5.1.2.alpha0 is invalid, 5.1.0.alpha1 is valid, 5.1.2.alpha1 is valid).
        /// The fourth part of a version represents either the state (alpha, beta, etc) or a build number (> 0).
        /// </summary>
        /// <returns><c>true</c>, if special was validated, <c>false</c> otherwise.</returns>
        bool ValidateSpecial()
        {
            for (int pos = 0, next = 0; pos < special.Length && (next = FindFirstDigit(special.Substring(pos))) >= 0;)
            {
                pos += next;

                var index = FindFirstNonDigit(special.Substring(pos));

                if (index < 0)
                    index = special.Length - pos;

                var val = int.Parse(special.Substring(pos, index));
                if (val == 0)
                    return false;
                pos += index;
            }
            return true;
        }

        static int FindFirstNonDigit(string str)
        {
            for (var i = 0; i < str.Length; i++)
            {
                if (!char.IsDigit(str[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        static int FindFirstDigit(string str)
        {
            for (var i = 0; i < str.Length; i++)
            {
                if (char.IsDigit(str[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public static bool operator<(PackageVersion a, PackageVersion z)
        {
            return (a != z) && !(a > z);
        }

        public static bool operator>=(PackageVersion a, PackageVersion z)
        {
            return a == z || a > z;
        }

        public static bool operator<=(PackageVersion a, PackageVersion z)
        {
            return a == z || a < z;
        }

        public static implicit operator string(PackageVersion version)
        {
            if (version == null)
                return null;
            return version.ToString();
        }
    }
}
