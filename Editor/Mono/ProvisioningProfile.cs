// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using System.IO;

namespace UnityEditorInternal
{
    internal class iOSEditorPrefKeys
    {
        public readonly static string kDefaultiOSAutomaticallySignBuild     = "DefaultiOSAutomaticallySignBuild";
        public readonly static string kDefaultiOSAutomaticSignTeamId        = "DefaultiOSAutomaticSignTeamId";
        public readonly static string kDefaultiOSProvisioningProfileUUID    = "DefaultiOSProvisioningProfileUUID";
        public readonly static string kDefaulttvOSProvisioningProfileUUID   = "DefaulttvOSProvisioningProfileUUID";
    }

    internal class ProvisioningProfile
    {
        private string m_UUID = string.Empty;

        public string UUID { get { return m_UUID; } set { m_UUID = value; } }

        // regex patterns used for finding specific parts of data from a provisioning profile
        static readonly string s_FirstLinePattern   = "<key>UUID<\\/key>";
        static readonly string s_SecondLinePattern  = "<string>((\\w*\\-?){5})";

        internal static ProvisioningProfile ParseProvisioningProfileAtPath(string pathToFile)
        {
            ProvisioningProfile profile = new ProvisioningProfile();
            parseFile(pathToFile, profile);
            return profile;
        }

        internal ProvisioningProfile() {}
        internal ProvisioningProfile(string UUID) { m_UUID = UUID; }

        private static void parseFile(string filePath, ProvisioningProfile profile)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(filePath);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                Match match = Regex.Match(line, s_FirstLinePattern);
                if (match.Success)
                {
                    // get the next line immediately and check it
                    if ((line = file.ReadLine()) != null)
                    {
                        Match secondMatch = Regex.Match(line, s_SecondLinePattern);
                        if (secondMatch.Success)
                        {
                            profile.UUID = secondMatch.Groups[1].Value;
                            break;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(profile.UUID))
                {
                    break;
                }
            }
            file.Close();
        }
    }
}
