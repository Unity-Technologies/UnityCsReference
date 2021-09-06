// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal struct UnityVersion : IVersion<UnityVersion>
    {
        // ReleaseType enum must stay in sync with the native enum ReleaseType in UnityVersion.h
        public enum UnityReleaseType
        {
            kAlphaRelease = 0,
            kBetaRelease,
            kPublicRelease,
            kChinaPublicRelease,
            kPatchRelease,
            kExperimentalRelease,
            kNumUnityReleaseTypes
        }

        public bool IsInitialized { get; }

        public int Major { get; }

        public int Minor { get; }

        public int Revision { get; }

        public UnityReleaseType ReleaseType { get; }

        public int IncrementalVersion { get; }

        // Suffix following the parse-able part of a string UnityVersion - ignored for any functional purpose. E.g. "2018.4.3f1-prefab-asset-editing"
        public string Suffix { get; }

        public UnityVersion(int major, int minor = 0, int revision = 0, UnityReleaseType releaseType = UnityReleaseType.kPublicRelease, int incrementalVersion = 0, string suffix = "")
        {
            Major = major;
            Minor = minor;
            Revision = revision;
            ReleaseType = releaseType;
            IncrementalVersion = incrementalVersion;
            Suffix = suffix;
            IsInitialized = true;
        }

        public static int Compare(UnityVersion versionA, UnityVersion versionB)
        {
            return versionA.CompareTo(versionB);
        }

        public static bool operator==(UnityVersion left, UnityVersion right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(UnityVersion left, UnityVersion right)
        {
            return !(left == right);
        }

        public static bool operator>(UnityVersion left, UnityVersion right)
        {
            return Compare(left, right) > 0;
        }

        public static bool operator>=(UnityVersion left, UnityVersion right)
        {
            return left == right || left > right;
        }

        public static bool operator<(UnityVersion left, UnityVersion right)
        {
            return Compare(left, right) < 0;
        }

        public static bool operator<=(UnityVersion left, UnityVersion right)
        {
            return left == right || left < right;
        }

        public int CompareTo(object obj)
        {
            return CompareTo((UnityVersion)obj);
        }

        public int CompareTo(UnityVersion other)
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

            result = Revision.CompareTo(other.Revision);
            if (result != 0)
            {
                return result;
            }

            result = CompareReleaseType(ReleaseType, other.ReleaseType);
            if (result != 0)
            {
                return result;
            }

            result = IncrementalVersion.CompareTo(other.IncrementalVersion);
            if (result != 0)
            {
                return result;
            }

            // We do not compare the Suffix

            return 0;
        }

        private static int CompareReleaseType(UnityReleaseType current, UnityReleaseType other)
        {
            var rt1 = current != UnityReleaseType.kChinaPublicRelease ? current : UnityReleaseType.kPublicRelease;
            var rt2 = other != UnityReleaseType.kChinaPublicRelease ? other : UnityReleaseType.kPublicRelease;
            return (int)rt1 - (int)rt2;
        }

        public bool Equals(UnityVersion other)
        {
            // We do not compare the suffix
            return Major == other.Major &&
                Minor == other.Minor &&
                Revision == other.Revision &&
                CompareReleaseType(ReleaseType, other.ReleaseType) == 0 &&
                IncrementalVersion == other.IncrementalVersion;
        }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(null, other))
                return false;

            return other.GetType() == GetType() && Equals((UnityVersion)other);
        }

        public override string ToString()
        {
            var version = $"{Major}.{Minor}.{Revision}";
            if (ReleaseType != UnityReleaseType.kPublicRelease || IncrementalVersion != 0)
            {
                version += $"{UnityVersionTypeTraits.k_ValidReleaseTypeSymbols[(int)ReleaseType]}";

                if (IncrementalVersion != 0)
                {
                    version += $"{IncrementalVersion}";
                }
            }
            return version;
        }

        public string ToStringFull()
        {
            var version = ToString();
            if (!string.IsNullOrEmpty(Suffix))
            {
                version += $"{Suffix}";
            }
            return version;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = Major.GetHashCode();
                result = result * 31 + Minor.GetHashCode();
                result = result * 31 + Revision.GetHashCode();
                result = result * 31 + GetReleaseTypeHashCode(ReleaseType);
                result = result * 31 + IncrementalVersion.GetHashCode();
                // We do not compare the suffix, so we do not hash it either
                return result;
            }
        }

        private static int GetReleaseTypeHashCode(UnityReleaseType releaseType)
        {
            var rt = releaseType != UnityReleaseType.kChinaPublicRelease ? releaseType : UnityReleaseType.kPublicRelease;
            return rt.GetHashCode();
        }

        public UnityVersion Parse(string version, bool strict = false)
        {
            return UnityVersionParser.Parse(version, strict);
        }

        public static UnityVersionTypeTraits VersionTypeTraits { get; } = new UnityVersionTypeTraits();
        public IVersionTypeTraits GetVersionTypeTraits()
        {
            return VersionTypeTraits;
        }
    }

    internal class UnityVersionTypeTraits : IVersionTypeTraits
    {
        public bool IsAllowedFirstCharacter(char c, bool strict = false)
        {
            return VersionTypeTraitsUtils.IsCharDigit(c);
        }

        public bool IsAllowedLastCharacter(char c, bool strict = false)
        {
            return strict ? (VersionTypeTraitsUtils.IsCharDigit(c) || VersionTypeTraitsUtils.IsCharLetter(c)) : IsAllowedCharacter(c);
        }

        public bool IsAllowedCharacter(char c)
        {
            return VersionTypeTraitsUtils.IsCharDigit(c) || VersionTypeTraitsUtils.IsCharLetter(c)
                || c == '.' || c == '-' || c == '/';
        }

        public static readonly char[] k_ValidReleaseTypeSymbols = new[]
        {
            'a', // UnityReleaseType.kAlphaRelease
            'b', // UnityReleaseType.kBetaRelease
            'f', // UnityReleaseType.kPublicRelease
            'c', // China release, treat as 'f'
            'p', // UnityReleaseType.kPatchRelease
            'x', // UnityReleaseType.kExperimentalRelease
        };

        static UnityVersionTypeTraits()
        {
            UnityEngine.Assertions.Assert.IsTrue(k_ValidReleaseTypeSymbols.Length == (int)UnityVersion.UnityReleaseType.kNumUnityReleaseTypes);
        }

        public static bool IsAllowedUnityReleaseTypeIdentifier(char c)
        {
            for (int i = 0; i < k_ValidReleaseTypeSymbols.Length; i++)
                if (k_ValidReleaseTypeSymbols[i] == c)
                    return true;

            return false;
        }

        public static UnityVersion.UnityReleaseType ParseUnityReleaseType(char releaseType)
        {
            if (TryParseUnityReleaseType(releaseType, out var result))
            {
                return result;
            }
            throw new ArgumentException($"UnityVersionTypeTraits does not recognize release type symbol '{releaseType}'.");
        }

        public static bool TryParseUnityReleaseType(char releaseType, out UnityVersion.UnityReleaseType result)
        {
            for (int i = (int)UnityVersion.UnityReleaseType.kAlphaRelease; i < (int)UnityVersion.UnityReleaseType.kNumUnityReleaseTypes; ++i)
            {
                if (releaseType == k_ValidReleaseTypeSymbols[i])
                {
                    result = (UnityVersion.UnityReleaseType)i;
                    return true;
                }
            }
            result = UnityVersion.UnityReleaseType.kNumUnityReleaseTypes;
            return false;
        }
    }

    internal static class UnityVersionParser
    {
        private static bool TryReadVersionNumber(string version, ref int cursor, ref int versionComponent)
        {
            string nextVersionComponent = VersionUtils.ConsumeVersionComponentFromString(version, ref cursor, x => !char.IsDigit(x));
            if (!String.IsNullOrEmpty(nextVersionComponent))
            {
                return int.TryParse(nextVersionComponent, out versionComponent);
            }
            return false;
        }

        private static bool TrySkipVersionSeparator(string version, ref int cursor)
        {
            if (cursor < version.Length && version[cursor] == '.')
            {
                cursor++;
                return cursor < version.Length; // . is not allowed to be the last symbol
            }
            return false;
        }

        private static bool TryReadVersionReleaseType(string version, ref int cursor, ref UnityVersion.UnityReleaseType releaseType)
        {
            if (cursor < version.Length && UnityVersionTypeTraits.IsAllowedUnityReleaseTypeIdentifier(version[cursor]))
            {
                if (UnityVersionTypeTraits.TryParseUnityReleaseType(version[cursor], out releaseType))
                {
                    cursor++;
                    return true;
                }
            }
            return false;
        }

        public static UnityVersion Parse(string version, bool strict = false)
        {
            if (TryParse(version, out var result, strict) && result.HasValue)
            {
                return result.Value;
            }

            throw new ArgumentException($"'{version}' is not a valid Unity Version");
        }

        public static bool TryParse(string version, out UnityVersion? result, bool strict = false)
        {
            int cursor = 0;

            int major = 0;
            int minor = 0;
            int revision = 0;
            // missing release type identifier means 'f' - public release, so that is the default as well
            UnityVersion.UnityReleaseType releaseType = UnityVersion.UnityReleaseType.kPublicRelease;
            int incrementalVersion = 0;
            string suffix = "";
            bool isValid = true;

            // Doing this instead because RegEx is impressively slow
            try
            {
                isValid = cursor < version.Length && TryReadVersionNumber(version, ref cursor, ref major);
                isValid = isValid && (cursor == version.Length || TrySkipVersionSeparator(version, ref cursor));
                isValid = isValid && (cursor == version.Length || TryReadVersionNumber(version, ref cursor, ref minor));
                isValid = isValid && (cursor == version.Length || TrySkipVersionSeparator(version, ref cursor));
                isValid = isValid && (cursor == version.Length || TryReadVersionNumber(version, ref cursor, ref revision));
                isValid = isValid && (cursor == version.Length || TryReadVersionReleaseType(version, ref cursor, ref releaseType));
                isValid = isValid && (cursor == version.Length ||
                    // experimental 'x' releases can have any characters following the 'x', so we don't read incrementalVersion but save the rest into the suffix
                    releaseType == UnityVersion.UnityReleaseType.kExperimentalRelease ||
                    TryReadVersionNumber(version, ref cursor, ref incrementalVersion));
                // if there's anything left, read the rest as suffix if experimental release or allowSuffix parse request
                if (isValid && cursor < version.Length)
                {
                    if (releaseType == UnityVersion.UnityReleaseType.kExperimentalRelease || !strict)
                        suffix = version.Substring(cursor);
                    else
                        isValid = false;
                }
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
            if (!isValid)
            {
                result = null;
                return false;
            }

            result = new UnityVersion(major, minor, revision, releaseType, incrementalVersion, suffix);
            return true;
        }
    }
}
