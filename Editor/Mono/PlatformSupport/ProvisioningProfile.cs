// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace UnityEditor.PlatformSupport
{
    internal class iOSEditorPrefKeys
    {
        public readonly static string kDefaultiOSAutomaticallySignBuild     = "DefaultiOSAutomaticallySignBuild";
        public readonly static string kDefaultiOSAutomaticSignTeamId        = "DefaultiOSAutomaticSignTeamId";

        public readonly static string kDefaultiOSProvisioningProfileUUID    = "DefaultiOSProvisioningProfileUUID";
        public readonly static string kDefaulttvOSProvisioningProfileUUID   = "DefaulttvOSProvisioningProfileUUID";
        public readonly static string kDefaultiOSProvisioningProfileType    = "kDefaultiOSProvisioningProfileType";
        public readonly static string kDefaulttvOSProvisioningProfileType   = "kDefaulttvOSProvisioningProfileType";
    }

    internal class ProvisioningProfile
    {
        private string m_UUID = string.Empty;
        private ProvisioningProfileType m_Type = ProvisioningProfileType.Development;

        public string UUID { get { return m_UUID; } set { m_UUID = value; } }
        public ProvisioningProfileType type { get { return m_Type; } set { m_Type = value; } }

        // regex patterns used for finding specific parts of data from a provisioning profile
        static readonly string s_PatternUUID   = "<key>UUID<\\/key>[\n\t]*<string>((\\w*\\-?){5})";
        private static readonly string s_PatternDeveloperCertificates =
            "<key>DeveloperCertificates<\\/key>[\n\t]*<array>[\n\t]*<data>([\\w\\/+=]+)<\\/data>";
        static readonly string s_DistributionPattern = "iPhone Distribution: ";

        internal static ProvisioningProfile ParseProvisioningProfileAtPath(string pathToFile)
        {
            ProvisioningProfile profile = new ProvisioningProfile();
            parseFile(pathToFile, profile);
            return profile;
        }

        private ProvisioningProfile() {}

        public ProvisioningProfile(string UUID, ProvisioningProfileType type)
        {
            m_UUID = UUID;
            m_Type = type;
        }

        internal static void parseFile(string filePath, ProvisioningProfile profile)
        {
            var provisioningFileContents = File.ReadAllText(filePath);
            Match matchUUID = Regex.Match(provisioningFileContents, s_PatternUUID, RegexOptions.Singleline);
            if (matchUUID.Success)
            {
                profile.UUID = matchUUID.Groups[1].Value;
            }

            Match matchCertificate = Regex.Match(provisioningFileContents, s_PatternDeveloperCertificates,
                RegexOptions.Singleline);
            if (matchCertificate.Success)
            {
                string value = matchCertificate.Groups[1].Value;
                string decodedCertificate = Encoding.UTF8.GetString(Convert.FromBase64String(value));

                if (decodedCertificate.Contains(s_DistributionPattern))
                {
                    profile.type = ProvisioningProfileType.Distribution;
                }
                else
                {
                    profile.type = ProvisioningProfileType.Development;
                }
            }
        }

        internal static readonly string[] DefaultProvisioningProfileSearchPaths = new string[]
        {
            "{Home}/Library/MobileDevice/Provisioning Profiles",
        };

        internal static ProvisioningProfile FindLocalProfileByUUID(string UUID, string[] searchPaths = null)
        {
            var localProfilePath = LoadLocalProfiles(searchPaths).FirstOrDefault(p => p.Contains(UUID));

            if (localProfilePath != null && File.Exists(localProfilePath))
            {
                return ParseProvisioningProfileAtPath(localProfilePath);
            }
            return null;
        }

        internal static List<string> LoadLocalProfiles(string[] searchPaths = null)
        {
            if (searchPaths == null)
                searchPaths = DefaultProvisioningProfileSearchPaths;

            List<string> localProfiles = new List<string>();
            foreach (var path in searchPaths)
            {
                var profilesFolder =
                    path.Replace("{Home}", Environment.GetFolderPath(Environment.SpecialFolder.Personal));

                if (!Directory.Exists(profilesFolder))
                    continue;

                foreach (var file in Directory.GetFiles(profilesFolder))
                {
                    if (Path.GetExtension(file) == ".mobileprovision")
                    {
                        localProfiles.Add(file);
                    }
                }
            }
            return localProfiles;
        }
    }
}
