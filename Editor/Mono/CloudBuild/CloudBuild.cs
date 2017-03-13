// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEditor.Utils;
using UnityEditor.Web;
using UnityEditorInternal;
using UnityEditor.Connect;

namespace UnityEditor.CloudBuild
{
    [InitializeOnLoad]
    internal class CloudBuild
    {
        static CloudBuild()
        {
            JSProxyMgr.GetInstance().AddGlobalObject("unity/cloudbuild", new CloudBuild());
        }

        public Dictionary<string, Dictionary<string, string>> GetScmCandidates()
        {
            Dictionary<string, Dictionary<string, string>> candidates = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> git = DetectGit();
            if (git != null)
            {
                candidates.Add("git", git);
            }
            Dictionary<string, string> mercurial = DetectMercurial();
            if (mercurial != null)
            {
                candidates.Add("mercurial", mercurial);
            }

            Dictionary<string, string> subversion = DetectSubversion();
            if (subversion != null)
            {
                candidates.Add("subversion", subversion);
            }

            Dictionary<string, string> perforce = DetectPerforce();
            if (perforce != null)
            {
                candidates.Add("perforce", perforce);
            }
            return candidates;
        }

        private Dictionary<string, string> DetectGit()
        {
            Dictionary<string, string> gitSettings = new Dictionary<string, string>();

            string url = RunCommand("git", "config --get remote.origin.url");
            if (String.IsNullOrEmpty(url))
            {
                return null;
            }
            gitSettings.Add("url", url);
            gitSettings.Add("branch", RunCommand("git", "rev-parse --abbrev-ref HEAD"));
            gitSettings.Add("root", RemoveProjectDirectory(RunCommand("git", "rev-parse --show-toplevel")));
            return gitSettings;
        }

        private Dictionary<string, string> DetectMercurial()
        {
            Dictionary<string, string> mercurialSettings = new Dictionary<string, string>();

            string url = RunCommand("hg", "paths default");
            if (String.IsNullOrEmpty(url))
            {
                return null;
            }
            mercurialSettings.Add("url", url);
            mercurialSettings.Add("branch", RunCommand("hg", "branch"));
            mercurialSettings.Add("root", RemoveProjectDirectory(RunCommand("hg", "root")));
            return mercurialSettings;
        }

        private Dictionary<string, string> DetectSubversion()
        {
            Dictionary<string, string> subversionSettings = new Dictionary<string, string>();

            string info = RunCommand("svn", "info");
            if (info == null)
            {
                return null;
            }
            string[] lines = info.Split(Environment.NewLine.ToCharArray());
            foreach (var s in lines)
            {
                string[] parts = s.Split(new char[] {':'}, 2);
                if (parts.Length == 2)
                {
                    if (parts[0].Equals("Repository Root"))
                    {
                        subversionSettings.Add("url", parts[1].Trim());
                    }

                    if (parts[0].Equals("URL"))
                    {
                        subversionSettings.Add("branch", parts[1].Trim());
                    }

                    if (parts[0].Equals("Working Copy Root Path"))
                    {
                        subversionSettings.Add("root", RemoveProjectDirectory(parts[1].Trim()));
                    }
                }
            }
            if (!subversionSettings.ContainsKey("url"))
            {
                return null;
            }
            return subversionSettings;
        }

        private Dictionary<string, string> DetectPerforce()
        {
            Dictionary<string, string> perforceSettings = new Dictionary<string, string>();

            string url = Environment.GetEnvironmentVariable("P4PORT");
            if (String.IsNullOrEmpty(url))
            {
                return null;
            }
            perforceSettings.Add("url", url);

            string client = Environment.GetEnvironmentVariable("P4CLIENT");
            if (!String.IsNullOrEmpty(client))
            {
                perforceSettings.Add("workspace", client);
            }
            return perforceSettings;
        }

        private String RunCommand(string command, string arguments)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(command);
                startInfo.Arguments = arguments;
                Program program = new Program(startInfo);
                program.Start();
                program.WaitForExit();
                if (program.ExitCode < 0)
                {
                    return null;
                }
                var sb = new System.Text.StringBuilder();
                foreach (var s in program.GetStandardOutput())
                {
                    sb.AppendLine(s);
                }
                return sb.ToString().TrimEnd(Environment.NewLine.ToCharArray());
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Problem executing, most likely Executable not found in path on Windows systems
                return null;
            }
        }

        private String RemoveProjectDirectory(string workingDirectory)
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            // handle windows command line clients returning *nix like paths
            if (currentDirectory.StartsWith(workingDirectory.Replace('/', '\\')))
            {
                workingDirectory = workingDirectory.Replace('/', '\\');
            }
            currentDirectory = currentDirectory.Replace(workingDirectory, "");
            return currentDirectory.Trim(Path.DirectorySeparatorChar);
        }
    }
}
