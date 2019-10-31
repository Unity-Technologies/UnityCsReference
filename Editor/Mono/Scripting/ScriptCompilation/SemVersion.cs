// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal struct SemVersion : IComparable<SemVersion>, IComparable
    {
        public bool IsInitialized { get; }

        public int Major { get; }

        public int Minor { get; }

        public int Patch { get; }

        public string Prerelease { get; }

        public string Build { get; }

        public SemVersion(int major, int minor = 0, int patch = 0, string prerelease = "", string build = "")
        {
            Major = major;
            Minor = minor;
            Patch = patch;

            Prerelease = prerelease ?? "";
            Build = build ?? "";
            IsInitialized = true;
        }

        public static int Compare(SemVersion versionA, SemVersion versionB)
        {
            return versionA.CompareTo(versionB);
        }

        public static bool operator==(SemVersion left, SemVersion right)
        {
            return Equals(left, right);
        }

        public static bool operator!=(SemVersion left, SemVersion right)
        {
            return !Equals(left, right);
        }

        public static bool operator>(SemVersion left, SemVersion right)
        {
            return Compare(left, right) > 0;
        }

        public static bool operator>=(SemVersion left, SemVersion right)
        {
            return left == right || left > right;
        }

        public static bool operator<(SemVersion left, SemVersion right)
        {
            return Compare(left, right) < 0;
        }

        public static bool operator<=(SemVersion left, SemVersion right)
        {
            return left == right || left < right;
        }

        public int CompareTo(object obj)
        {
            return CompareTo((SemVersion)obj);
        }

        public int CompareTo(SemVersion other)
        {
            var result = Major.CompareTo(other.Major);
            if (result != 0)
            {
                return result;
            }

            result = Minor.CompareTo(other.Minor);
            if (result != 0)
            {
                return result;
            }

            result = Patch.CompareTo(other.Patch);
            if (result != 0)
            {
                return result;
            }

            result = CompareExtension(Prerelease, other.Prerelease, true);

            if (result != 0)
            {
                return result;
            }

            return 0;
        }

        private static int CompareExtension(string current, string other, bool lower = false)
        {
            var currentIsEmpty = string.IsNullOrEmpty(current);
            var otherIsEmpty = string.IsNullOrEmpty(other);
            if (currentIsEmpty && otherIsEmpty)
                return 0;

            if (currentIsEmpty)
                return lower ? 1 : -1;
            if (otherIsEmpty)
                return lower ? -1 : 1;

            var currentParts = current.Split('.');
            var otherParts = other.Split('.');

            for (var i = 0; i < Math.Min(currentParts.Length, otherParts.Length); i++)
            {
                var currentPart = currentParts[i];
                var otherPart = otherParts[i];

                int currentNumber;
                int otherNumber;
                var currentPartIsNumber = int.TryParse(currentPart, out currentNumber);
                var otherPartIsNumber = int.TryParse(otherPart, out otherNumber);
                int result;
                if (currentPartIsNumber && otherPartIsNumber)
                {
                    result = currentNumber.CompareTo(otherNumber);
                    if (result != 0)
                    {
                        return result;
                    }
                }
                else
                {
                    if (currentPartIsNumber)
                    {
                        return -1;
                    }

                    if (otherPartIsNumber)
                    {
                        return 1;
                    }

                    result = string.CompareOrdinal(currentPart, otherPart);
                    if (result != 0)
                    {
                        return result;
                    }
                }
            }
            return currentParts.Length.CompareTo(otherParts.Length);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;

            var other = (SemVersion)obj;

            return Major == other.Major &&
                Minor == other.Minor &&
                Patch == other.Patch &&
                string.Equals(Prerelease, other.Prerelease, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            var version = $"{Major}.{Minor}.{Patch}";
            if (!string.IsNullOrEmpty(Prerelease))
            {
                version += $"-{Prerelease}";
            }
            if (!string.IsNullOrEmpty(Build))
            {
                version += $"+{Build}";
            }

            return version;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = Major.GetHashCode();
                result = result * 31 + Minor.GetHashCode();
                result = result * 31 + Patch.GetHashCode();
                result = result * 31 + Prerelease.GetHashCode();
                result = result * 31 + Build.GetHashCode();
                return result;
            }
        }
    }

    internal static class SemVersionParser
    {
        private static string GetStringUntil(string value, ref int cursor, Func<char, bool> isEnd)
        {
            int length = 0;
            for (int i = cursor; i < value.Length; i++)
            {
                if (isEnd(value[i]))
                {
                    break;
                }

                length++;
            }

            int newIndex = cursor;
            cursor += length;
            return value.Substring(newIndex, length);
        }

        public static SemVersion Parse(string version, bool strict = false)
        {
            int cursor = 0;

            int major = 0;
            int minor = 0;
            int patch = 0;
            string prerelease = null;
            string build = null;

            //Doing this instead because RegEx is impressively slow
            try
            {
                major = int.Parse(GetStringUntil(version, ref cursor, x => !char.IsDigit(x)));
                if (cursor < version.Length && version[cursor] == '.')
                {
                    cursor++;
                    minor = int.Parse(GetStringUntil(version, ref cursor, x => !char.IsDigit(x)) ?? "0");
                }

                if (cursor < version.Length && version[cursor] == '.')
                {
                    cursor++;
                    patch = int.Parse(GetStringUntil(version, ref cursor, x => !char.IsDigit(x)) ?? "0");
                }

                if (cursor < version.Length && version[cursor] == '-')
                {
                    cursor++;
                    prerelease = GetStringUntil(version, ref cursor, x => x == '+');
                }

                if (cursor < version.Length && version[cursor] == '+')
                {
                    cursor++;
                    build = GetStringUntil(version, ref cursor, x => x == '\0');
                }
            }
            catch (Exception)
            {
                throw new ArgumentException($"{version} is not valid Semantic Version");
            }


            return new SemVersion(major, minor, patch, prerelease, build);
        }

        public static bool TryParse(string versionString, out SemVersion? result)
        {
            try
            {
                result = SemVersionParser.Parse(versionString);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }
    }
}
