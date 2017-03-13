// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using UnityEditor.Web;

namespace UnityEditor
{
    /// <summary>
    /// The interface of this class is mirrored into the browser's JavaScript world as
    /// "window.context".
    /// </summary>
    /// <remarks>
    /// The WebView will expose the methods of this class as functions on the JavaScript
    /// object created by AssetStoreWindow.SetContextObject().  Return values and arguments
    /// of these methods can be any primitive type, arrays of any other valid type, and
    /// classes with public fields of any other valid type.  In the last case, objects of
    /// these classes will be translate to and from JavaScript objects with C# fields mapping
    /// to JavaScript properties.
    /// </remarks>
    [InitializeOnLoad]
    internal partial class AssetStoreContext
    {
        static AssetStoreContext()
        {
            AssetStoreContext.GetInstance();
        }

        public static AssetStoreContext GetInstance()
        {
            if (s_Instance == null)
            {
                s_Instance = new AssetStoreContext();
                JSProxyMgr.GetInstance().AddGlobalObject("AssetStoreContext", s_Instance);
            }

            return s_Instance;
        }

        public string GetInitialOpenURL()
        {
            if (initialOpenURL != null)
            {
                string tmp = initialOpenURL;
                initialOpenURL = null;
                return tmp;
            }
            else
            {
                return "";
            }
        }

        public string GetAuthToken()
        {
            return UnityEditorInternal.InternalEditorUtility.GetAuthToken();
        }

        public int[] GetLicenseFlags()
        {
            return UnityEditorInternal.InternalEditorUtility.GetLicenseFlags();
        }

        public string GetString(string key)
        {
            return EditorPrefs.GetString(key);
        }

        public int GetInt(string key)
        {
            return EditorPrefs.GetInt(key);
        }

        public float GetFloat(string key)
        {
            return EditorPrefs.GetFloat(key);
        }

        public void SetString(string key, string value)
        {
            EditorPrefs.SetString(key, value);
        }

        public void SetInt(string key, int value)
        {
            EditorPrefs.SetInt(key, value);
        }

        public void SetFloat(string key, float value)
        {
            EditorPrefs.SetFloat(key, value);
        }

        public bool HasKey(string key)
        {
            return EditorPrefs.HasKey(key);
        }

        public void DeleteKey(string key)
        {
            EditorPrefs.DeleteKey(key);
        }

        public int GetSkinIndex()
        {
            return EditorGUIUtility.skinIndex;
        }

        public bool GetDockedStatus()
        {
            return docked;
        }

        public bool OpenPackage(string id)
        {
            return OpenPackage(id, "default");
        }

        public bool OpenPackage(string id, string action)
        {
            return OpenPackageInternal(id);
        }

        public static bool OpenPackageInternal(string id)
        {
            Match match = s_GeneratedIDRegExp.Match(id);
            if (match.Success && File.Exists(match.Groups[1].Value)) // If id looks like a path name, just try to open that
            {
                AssetDatabase.ImportPackage(match.Groups[1].Value, true);
                return true;
            }
            else
            {
                foreach (PackageInfo package in PackageInfo.GetPackageList())
                {
                    if (package.jsonInfo != "")
                    {
                        JSONValue item = JSONParser.SimpleParse(package.jsonInfo);
                        string itemID = item.Get("id").IsNull() ? null : item["id"].AsString(true);
                        if (itemID != null && itemID == id && File.Exists(package.packagePath))
                        {
                            AssetDatabase.ImportPackage(package.packagePath, true);
                            return true;
                        }
                    }
                }
            }
            Debug.LogError("Unknown package ID " + id);
            return false;
        }

        public void OpenBrowser(string url)
        {
            Application.OpenURL(url);
        }

        public void Download(Package package, DownloadInfo downloadInfo)
        {
            Download(
                downloadInfo.id,
                downloadInfo.url,
                downloadInfo.key,
                package.title,
                package.publisher.label,
                package.category.label,
                null
                );
        }

        public static void Download(string package_id, string url, string key, string package_name,
            string publisher_name, string category_name, AssetStoreUtils.DownloadDoneCallback doneCallback)
        {
            string[] dest = PackageStorePath(publisher_name, category_name,
                    package_name, package_id, url);

            JSONValue existing = JSONParser.SimpleParse(AssetStoreUtils.CheckDownload(package_id, url, dest, key));

            // If the package is actively being downloaded right now just return
            if (existing.Get("in_progress").AsBool(true))
            {
                Debug.Log("Will not download " + package_name + ". Download is already in progress.");
                return;
            }

            // The package is not being downloaded.
            // If the package has previously been partially downloaded then
            // resume that download.
            string existingUrl = existing.Get("download.url").AsString(true);
            string existingKey = existing.Get("download.key").AsString(true);
            bool resumeOK = (existingUrl == url && existingKey == key);

            JSONValue download = new JSONValue();
            download["url"] = url;
            download["key"] = key;
            JSONValue parameters = new JSONValue();
            parameters["download"] = download;

            AssetStoreUtils.Download(package_id, url, dest, key, parameters.ToString(), resumeOK, doneCallback);
        }

        /// <summary>
        /// Create an array consisting of publisherName, categoryName and packageName
        /// This is to be used by AssetStoreUtils.*Download functions
        /// </summary>
        public static string[] PackageStorePath(string publisher_name,
            string category_name,
            string package_name,
            string package_id,
            string url)
        {
            string[] dest = { publisher_name, category_name, package_name };

            for (int i = 0; i < 3; i++)
                dest[i] = s_InvalidPathCharsRegExp.Replace(dest[i], "");

            // If package name cannot be stored as a valid file name, use the package id
            if (dest[2] == "")
                dest[2] = s_InvalidPathCharsRegExp.Replace(package_id, "");

            // If still no valid chars use a mangled url as the file name
            if (dest[2] == "")
                dest[2] = s_InvalidPathCharsRegExp.Replace(url, "");

            return dest;
        }

        public PackageList GetPackageList()
        {
            var packages = new Dictionary<string, Package>();
            var packageInfos = PackageInfo.GetPackageList();

            foreach (PackageInfo info in packageInfos)
            {
                Package package = new Package();
                if (info.jsonInfo == "")
                {
                    package.title = System.IO.Path.GetFileNameWithoutExtension(info.packagePath);
                    package.id = info.packagePath;

                    if (IsBuiltinStandardAsset(info.packagePath))
                    {
                        package.publisher = new LabelAndId { label = "Unity Technologies", id = "1" };
                        package.category = new LabelAndId { label = "Prefab Packages", id = "4" };
                        package.version = "3.5.0.0";
                    }
                }
                else
                {
                    var jsonData = JSONParser.SimpleParse(info.jsonInfo);
                    if (jsonData.IsNull())
                        continue;

                    package.Initialize(jsonData);

                    if (package.id == null)
                    {
                        var linkId = jsonData.Get("link.id");
                        if (!linkId.IsNull())
                            package.id = linkId.AsString();
                        else
                            package.id = info.packagePath;
                    }
                }

                package.local_icon = info.iconURL;
                package.local_path = info.packagePath;

                // If no package with the same ID is in the dictionary yet or if the current package
                // is newer than what we currently have in the dictionary, add the package to the
                // dictionary.
                if (!packages.ContainsKey(package.id) ||
                    packages[package.id].version_id == null ||
                    packages[package.id].version_id == "-1" ||
                    (package.version_id != null && package.version_id != "-1" && Int32.Parse(packages[package.id].version_id) <= Int32.Parse(package.version_id)))
                {
                    packages[package.id] = package;
                }
            }

            var results = packages.Values.ToArray();
            return new PackageList { results = results };
        }

        private bool IsBuiltinStandardAsset(string path)
        {
            return s_StandardPackageRegExp.IsMatch(path);
        }

        private static Regex s_StandardPackageRegExp = new Regex(@"/Standard Packages/(Character\ Controller|Glass\ Refraction\ \(Pro\ Only\)|Image\ Effects\ \(Pro\ Only\)|Light\ Cookies|Light\ Flares|Particles|Physic\ Materials|Projectors|Scripts|Standard\ Assets\ \(Mobile\)|Skyboxes|Terrain\ Assets|Toon\ Shading|Tree\ Creator|Water\ \(Basic\)|Water\ \(Pro\ Only\))\.unitypackage$", RegexOptions.IgnoreCase);
        private static Regex s_GeneratedIDRegExp = new Regex(@"^\{(.*)\}$");
        private static Regex s_InvalidPathCharsRegExp = new Regex(@"[^a-zA-Z0-9() _-]");

        internal bool docked;
        internal string initialOpenURL;

        private static AssetStoreContext s_Instance;

        // Some data is created through reflection in C++ and then
        // passed on to us.  Shut up warning about fields that are
        // never assigned to.
        #pragma warning disable 0649

        // The following classes are used for data interchange with JavaScript.
        // Public fields in them directly translate to respective properties on
        // JavaScript objects.

        public class DownloadInfo
        {
            public string url;
            public string key;
            public string id;
        }

        public class LabelAndId
        {
            public string label;
            public string id;

            public void Initialize(JSONValue json)
            {
                if (json.ContainsKey("label"))
                    label = json["label"].AsString();

                if (json.ContainsKey("id"))
                    id = json["id"].AsString();
            }

            public override string ToString()
            {
                return string.Format("{{label={0}, id={1}}}", label, id);
            }
        }

        public class Link
        {
            public string type;
            public string id;

            public void Initialize(JSONValue json)
            {
                if (json.ContainsKey("type"))
                    type = json["type"].AsString();

                if (json.ContainsKey("id"))
                    id = json["id"].AsString();
            }

            public override string ToString()
            {
                return string.Format("{{type={0}, id={1}}}", type, id);
            }
        }

        public class Package
        {
            public string title;
            public string id;
            public string version;
            public string version_id;
            public string local_icon;
            public string local_path;
            public string pubdate;
            public string description;
            public LabelAndId publisher;
            public LabelAndId category;
            public Link link;

            public void Initialize(JSONValue json)
            {
                if (json.ContainsKey("title"))
                    title = json["title"].AsString();

                if (json.ContainsKey("id"))
                    id = json["id"].AsString();

                if (json.ContainsKey("version"))
                    version = json["version"].AsString();

                if (json.ContainsKey("version_id"))
                    version_id = json["version_id"].AsString();

                if (json.ContainsKey("local_icon"))
                    local_icon = json["local_icon"].AsString();

                if (json.ContainsKey("local_path"))
                    local_path = json["local_path"].AsString();

                if (json.ContainsKey("pubdate"))
                    pubdate = json["pubdate"].AsString();

                if (json.ContainsKey("description"))
                    description = json["description"].AsString();

                if (json.ContainsKey("publisher"))
                {
                    publisher = new LabelAndId();
                    publisher.Initialize(json["publisher"]);
                }

                if (json.ContainsKey("category"))
                {
                    category = new LabelAndId();
                    category.Initialize(json["category"]);
                }

                if (json.ContainsKey("link"))
                {
                    link = new Link();
                    link.Initialize(json["link"]);
                }
            }

            public override string ToString()
            {
                return string.Format
                        ("{{title={0}, id={1}, publisher={2}, category={3}, pubdate={8}, version={4}, version_id={5}, description={9}, link={10}, local_icon={6}, local_path={7}}}",
                        title, id, publisher, category, version, version_id, local_icon, local_path, pubdate, description, link);
            }
        }

        public class PackageList
        {
            public Package[] results;
        }
    }
}
