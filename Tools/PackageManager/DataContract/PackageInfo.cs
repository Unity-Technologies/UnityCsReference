// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.DataContract
{
    public class PackageInfo
    {
        public string organisation;
        public string name;
        public PackageVersion version;
        public PackageVersion unityVersion;
        public string basePath;
        public PackageType type;
        public string description;
        public string releaseNotes;
        public bool loaded;

        Dictionary<string, PackageFileData> m_FileDict;
        public Dictionary<string, PackageFileData> files
        {
            get { return m_FileDict; }
            set { m_FileDict = value; }
        }

        public string packageName
        {
            get { return string.Format("{0}.{1}", organisation, name); }
        }

        public override string ToString()
        {
            return string.Format("{0} {1} ({2}) v{3} for Unity v{4}", organisation, name, type, version != null ? version.text : null, unityVersion != null ? basePath : null);
        }

        public override int GetHashCode()
        {
            var hash = 17;
            hash = hash * 23 + organisation.GetHashCode();
            hash = hash * 23 + name.GetHashCode();
            hash = hash * 23 + type.GetHashCode();
            hash = hash * 23 + version.GetHashCode();
            hash = hash * 23 + unityVersion.GetHashCode();
            return hash;
        }

        public override bool Equals(object other)
        {
            return this == (other as PackageInfo);
        }

        public static bool operator==(PackageInfo a, PackageInfo z)
        {
            if ((object)a == null && (object)z == null)
                return true;
            if ((object)a == null || (object)z == null)
                return false;
            return a.GetHashCode() == z.GetHashCode();
        }

        public static bool operator!=(PackageInfo a, PackageInfo z)
        {
            return !(a == z);
        }
    }


    public class PackageFileData
    {
        public PackageFileType type;
        public string url;
        public string guid;

        public PackageFileData() {}
        public PackageFileData(PackageFileType type, string url)
        {
            this.type = type;
            this.url = url;
        }

        public PackageFileData(PackageFileType type, string url, string guid) : this(type, url)
        {
            this.guid = guid;
        }
    }

    public enum PackageType
    {
        Unknown = 0,
        PlaybackEngine,
        UnityExtension,
        PackageManager
    }

    public enum PackageFileType
    {
        None,
        Package,
        Ivy,
        Dll,
        ReleaseNotes,
        DebugSymbols
    }
}
