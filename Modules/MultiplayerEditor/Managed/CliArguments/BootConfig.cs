// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace Unity.DedicatedServer
{
    // Allow multiples when read
    // Adding keys replaces the last known key with first value
    // Order lines read is maintained for ease in testing
    class BootConfig
    {
        private string _bootConfigFullPath;
        private Dictionary<string, int> _data;
        private List<string> _lines;

        private string BootConfigFullPath(BuildTarget target, string pathToBuiltProject)
        {
            if (target == BuildTarget.StandaloneOSX)
                return Path.Combine(pathToBuiltProject, "Data", "boot.config");
            else
            {
                var projectDir = Path.GetDirectoryName(pathToBuiltProject);
                var projectName = Path.GetFileNameWithoutExtension(pathToBuiltProject);
                return Path.Combine(projectDir, $"{projectName}_Data", "boot.config");
            }
        }

        public BootConfig(BuildTarget target, string pathToBuiltProject)
        {
            _bootConfigFullPath = BootConfigFullPath(target, pathToBuiltProject);
            _data = new Dictionary<string, int>();
            _lines = new List<string>();
        }

        private void Reset()
        {
            _data.Clear();
            _lines.Clear();
        }

        public bool Read()
        {
            try
            {
                using (StreamReader sr = new StreamReader(_bootConfigFullPath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        _lines.Add(line);
                        string[] parts = line.Split('=');
                        if (parts.Length > 2)
                        {
                            Reset();
                            return false;
                        }
                        var key = parts[0];
                        _data[key] = _lines.Count - 1;
                    }
                }
#pragma warning disable 168
            }
            catch (FileNotFoundException e)
            {
                Reset();
                return false;
            }
#pragma warning restore

            return true;
        }

        public void Add(string key, string value)
        {
            if (_data.ContainsKey(key))
            {
                var line = _data[key];
                _lines[line] = $"{key}={value}";
            }
            else
            {
                _lines.Add($"{key}={value}");
            }
        }

        public bool Write()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var line in _lines)
                sb.AppendLine(line);
            File.WriteAllText(_bootConfigFullPath, sb.ToString());
            return true;
        }
    }
}
