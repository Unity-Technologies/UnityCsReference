// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Audio;
using UnityEditorInternal;
using UnityEngine.Networking;

namespace UnityEditor
{
    // Helper class to access Unity documentation.
    public partial class Help
    {
        private const string k_MonoScriptReference = "file:///unity/ScriptReference/MonoBehaviour.html";
        private static Dictionary<string, string> m_UrlCache = new Dictionary<string, string>();
        private const string k_AbsoluteURI = "file:///unity/";
        private const string k_AbsoluteFileRef = "file://";
        private const string k_ManualSection = "manual";
        private const string k_ApiSection = "api";
        private static string m_BaseDocumentationUrl;
        private static Dictionary<string, object> m_LocalRedirectionMapping = new Dictionary<string, object>();
        private const string k_RedirectManifest = "redirect.json";

        private static string[] k_DocRedirectServer =
        {
            "",
            "https://docs-redirects.test.it.unity3d.com",
            "https://docs-redirects.stg.it.unity3d.com",
            "https://docs-redirects.prd.it.unity3d.com",
            "https://docs-redirects.unity.com"
        };

        internal enum DocRedirectionServer
        {
            None,
            Test,
            Staging,
            Production,
            PublicRedirect
        }

        internal static DocRedirectionServer docRedirectionServer
        {
            get => (DocRedirectionServer)EditorPrefs.GetInt("Help.docRedirectionServer", (int)DocRedirectionServer.PublicRedirect);
            set
            {
                EditorPrefs.SetInt("Help.docRedirectionServer", (int)value);
                m_BaseDocumentationUrl = null;
                ClearCache();
            }
        }

        internal static string baseDocumentationUrl
        {
            get
            {
                if (m_BaseDocumentationUrl == null)
                {
                    baseDocumentationUrl = GetDocumentationAbsolutePath_Internal();
                }

                return m_BaseDocumentationUrl;
            }

            set
            {
                m_BaseDocumentationUrl = value;
                InitDocumentation();
            }
        }

        public static bool HasHelpForObject(Object obj)
        {
            return HasHelpForObject(obj, true);
        }

        public static string GetHelpURLForObject(Object obj)
        {
            return GetHelpURLForObject(obj, true);
        }

        public static void ShowHelpForObject(Object obj)
        {
            var startTick = DateTime.Now.Ticks;
            var url = GetHelpURLForObject(obj, true);
            if (string.IsNullOrEmpty(url))
                return;

            BrowseURL(url);

            SendHelpRequestedUsabilityEvent(startTick, DateTime.Now.Ticks - startTick, obj, url);
        }

        public static void ShowHelpPage(string page)
        {
            var startTick = DateTime.Now.Ticks;

            var topicUri = new Uri(page);
            var topic = topicUri.GetLeftPart(UriPartial.Path);
            var anchor = topicUri.Fragment;
            var path = FindHelpNamed(topic);

            bool isUrl = Uri.IsWellFormedUriString(path, UriKind.Absolute);

            if (anchor != "")
            {
                path = $"{path}{anchor}";
                if (!isUrl)
                    path = "file://" + path;
            }

            BrowseURL(path);

            SendHelpRequestedUsabilityEvent(startTick, DateTime.Now.Ticks - startTick, null, path);
        }

        public static void BrowseURL(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                Application.OpenURL(url);
            else
                Application.OpenURL($"file://{url}");
        }

        internal static bool HasHelpForObject(Object obj, bool defaultToMonoBehaviour)
        {
            return !string.IsNullOrEmpty(GetHelpURLForObject(obj, defaultToMonoBehaviour));
        }

        internal static string GetNiceHelpNameForObject(Object obj)
        {
            return GetNiceHelpNameForObject(obj, true);
        }

        internal static string GetNiceHelpNameForObject(Object obj, bool defaultToMonoBehaviour)
        {
            var helpTopic = HelpFileNameForObject(obj);
            if (!defaultToMonoBehaviour || HasNamedHelp(helpTopic))
            {
                var dashIndex = helpTopic.IndexOf("-");
                if (dashIndex != -1)
                {
                    return helpTopic.Substring(dashIndex + 1);
                }
            }
            else
            {
                if (obj is Component || obj is MonoScript)
                {
                    return "MonoBehaviour";
                }
            }

            return "";
        }

        internal static string GetHelpURLForObject(Object obj, bool defaultToMonoBehaviour)
        {
            if (obj == null || !obj)
                return "";

            var attrs = obj.GetType().GetCustomAttributes(typeof(HelpURLAttribute), true);
            if (attrs.Length > 0)
            {
                var attr = (HelpURLAttribute)attrs[0];
                var url = attr.m_Url;
                if (!string.IsNullOrEmpty(attr.m_DispatchingFieldName))
                {
                    var field = obj.GetType().GetField(attr.m_DispatchingFieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        var value = field.GetValue(obj);
                        if (value != null)
                        {
                            var valueAttrs = value.GetType().GetCustomAttributes(typeof(HelpURLAttribute), true);
                            if (valueAttrs.Length > 0)
                            {
                                var targetUrl = ((HelpURLAttribute)valueAttrs[0]).m_Url;
                                if (!string.IsNullOrEmpty(targetUrl))
                                {
                                    url = targetUrl;
                                }
                            }
                        }
                    }
                }

                return url;
            }

            var topicForObject = HelpFileNameForObject(obj);
            if (HasNamedHelp(topicForObject))
            {
                return FindHelpNamed(topicForObject);
            }

            if (defaultToMonoBehaviour)
            {
                if (obj is Component || obj is MonoScript)
                {
                    return FindHelpNamed(k_MonoScriptReference);
                }
            }

            return "";
        }

        internal static void ClearCache()
        {
            m_UrlCache.Clear();
        }

        internal static string TranslateURIForRedirection(string uri)
        {
            if (docRedirectionServer != DocRedirectionServer.None && IsLocalPath(uri) == false)
            {
                var version = InternalEditorUtility.GetUnityVersion();
                //case 1300425: The redirection server that launched with 2020.2 badly redirects Manual/index.html and ScriptReference/index.html resulting in a 404
                //Even without the 404, the Manual/index.html and ScriptReference/index.html need the version parameter to be redirected to the matching docs for this version
                if (uri.Equals(string.Join("/", new string[] { baseDocumentationUrl, "Manual", "index.html" }), StringComparison.OrdinalIgnoreCase))
                    uri = $"{baseDocumentationUrl}/?section=manual&version={version.Major}.{version.Minor}";
                if (uri.Equals(string.Join("/", new string[] { baseDocumentationUrl, "ScriptReference", "index.html" }), StringComparison.OrdinalIgnoreCase))
                    uri = $"{baseDocumentationUrl}/?section=api&version={version.Major}.{version.Minor}";
            }
            return uri;
        }

        internal static string FindHelpNamed(string topic)
        {
            if (m_UrlCache.ContainsKey(topic))
            {
                return m_UrlCache[topic];
            }

            var documentPath = "";
            if (topic.StartsWith(k_AbsoluteURI))
            {
                documentPath = GetURLPath(true, baseDocumentationUrl, topic.Substring(k_AbsoluteURI.Length));
            }
            else if (topic.StartsWith(k_AbsoluteFileRef))
            {
                documentPath = topic.Substring(k_AbsoluteFileRef.Length);
            }
            else if (IsLocalPath(topic) == false)
            {
                documentPath = topic;
            }
            else
            {
                topic = UnityWebRequest.UnEscapeURL(topic);
                if (IsLocalPath(baseDocumentationUrl))
                {
                    if (!TryRedirect(ref topic))
                    {
                        topic = $"Manual/{topic}";
                    }
                    documentPath = GetURLPath(true, baseDocumentationUrl, topic);
                }
                else if (docRedirectionServer == DocRedirectionServer.None)
                {
                    topic = $"Manual/{topic}";
                    documentPath = GetURLPath(true, baseDocumentationUrl, topic);
                }
                else
                {
                    documentPath = GetURLPath(false, baseDocumentationUrl, topic);
                    var version = InternalEditorUtility.GetUnityVersion();
                    documentPath += $"?version={version.Major}.{version.Minor}";
                }
            }

            if (IsLocalPath(documentPath))
            {
                if (!File.Exists(documentPath))
                {
                    documentPath = "";
                }
            }

            documentPath = TranslateURIForRedirection(documentPath);

            m_UrlCache[topic] = documentPath;

            return documentPath;
        }

        internal static bool TryRedirect(ref string topicName)
        {
            if (m_LocalRedirectionMapping.ContainsKey(topicName))
            {
                topicName = m_LocalRedirectionMapping[topicName].ToString();
                return true;
            }

            return false;
        }

        internal static string HelpFileNameForObject(Object obj)
        {
            if (obj.GetType().IsSubclassOf(typeof(MonoBehaviour)))
            {
                return $"script-{obj.GetType().Name}";
            }

            if (obj is Terrain)
            {
                return "script-Terrain";
            }

            if (obj is AudioMixerController || obj is AudioMixerGroupController)
            {
                return "class-AudioMixer";
            }

            if (obj is EditorSettings)
            {
                return "class-EditorManager";
            }

            return $"class-{obj.GetType().Name}";
        }

        private static void InitDocumentation()
        {
            ClearCache();
            if (!IsLocalPath(m_BaseDocumentationUrl) && docRedirectionServer != DocRedirectionServer.None)
            {
                m_BaseDocumentationUrl = k_DocRedirectServer[(int)docRedirectionServer];
            }
            else
            {
                m_LocalRedirectionMapping = new Dictionary<string, object>();
                var redirectFile = Path.Combine(m_BaseDocumentationUrl, k_RedirectManifest);
                if (File.Exists(redirectFile))
                {
                    try
                    {
                        var content = File.ReadAllText(redirectFile);
                        if (Json.Deserialize(content) is Dictionary<string, object> jsonData &&
                            jsonData.ContainsKey("redirects") &&
                            jsonData["redirects"] is Dictionary<string, object> redirectManifest)
                        {
                            m_LocalRedirectionMapping = redirectManifest;
                        }
                    }
                    catch (Exception)
                    {
                        Debug.LogError($"Cannot load redirect manifest: {redirectFile}");
                    }
                }
            }
        }

        private static bool IsLocalPath(string path)
        {
            return !path.StartsWith("http://") && !path.StartsWith("https://");
        }

        private static bool IsApiTopic(string topic)
        {
            return topic.StartsWith("class-") || topic.StartsWith("script-");
        }

        private static string GetURLPath(bool forceHtml, params string[] tokens)
        {
            var path = string.Join("/", tokens);
            if (forceHtml && !path.EndsWith(".html"))
                path += ".html";
            return path;
        }

        private static bool HasNamedHelp(string topic)
        {
            if (string.IsNullOrEmpty(topic))
                return false;
            return !string.IsNullOrEmpty(FindHelpNamed(topic));
        }
    }
}
