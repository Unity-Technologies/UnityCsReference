// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.RestService
{
    internal class ScriptEditorSettings
    {
        public static string Name { get; set; }
        public static string ServerURL { get; set; }
        public static int ProcessId { get; set; }
        public static List<string> OpenDocuments { get; set; }

        static ScriptEditorSettings()
        {
            OpenDocuments = new List<string>();
            Clear();
        }

        private static string FilePath
        {
            get { return Application.dataPath + "/../Library/" + "UnityScriptEditorSettings.json"; }
        }

        private static void Clear()
        {
            Name = null;
            ServerURL = null;
            ProcessId = -1;
        }

        public static void Save()
        {
            var sb = new StringBuilder();

            sb.AppendFormat("{{\n\t\"name\" : \"{0}\",\n\t\"serverurl\" : \"{1}\",\n\t\"processid\" : {2},\n\t", Name, ServerURL, ProcessId);
            sb.AppendFormat("\"opendocuments\" : [{0}]\n}}", string.Join(",", OpenDocuments.Select(d => "\"" + d + "\"").ToArray()));
            File.WriteAllText(FilePath, sb.ToString());
        }

        public static void Load()
        {
            try
            {
                var contents = File.ReadAllText(FilePath);
                var json = new JSONParser(contents).Parse();

                Name = json.ContainsKey("name") ? json["name"].AsString() : null;
                ServerURL = json.ContainsKey("serverurl") ? json["serverurl"].AsString() : null;
                ProcessId = json.ContainsKey("processid") ? (int)json["processid"].AsFloat() : -1;
                OpenDocuments = json.ContainsKey("opendocuments") ? json["opendocuments"].AsList().Select(d => d.AsString()).ToList() : new List<String>();

                if (ProcessId >= 0)
                {
                    Process.GetProcessById(ProcessId);
                }
            }
            catch (FileNotFoundException)
            {
                Clear();
                Save();
            }
            catch (Exception e)
            {
                Logger.Log(e);
                Clear();
                Save();
            }
        }
    }
}
