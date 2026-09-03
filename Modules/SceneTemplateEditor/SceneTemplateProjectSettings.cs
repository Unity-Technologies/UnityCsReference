// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: SceneTemplate not yet converted
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.Search;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityEditor.SceneTemplate
{
    [Serializable]
    class PinState
    {
        public string templateId;
        public bool isEnabled;
    }

    [Serializable]
    [DebuggerDisplay("{type} - {defaultInstantiationMode}")]
    class DependencyTypeInfo
    {
        public DependencyTypeInfo(Type type)
        {
            this.type = type.FullName;
            supportsModification = true;
            defaultInstantiationMode = TemplateInstantiationMode.Reference;
        }

        public DependencyTypeInfo(string type, string label = null)
        {
            m_Label = label;
            this.type = type;
            supportsModification = true;
            defaultInstantiationMode = TemplateInstantiationMode.Reference;
        }

        public DependencyTypeInfo(DependencyTypeInfo src)
        {
            AssignFrom(src);
        }

        string m_Label;
        public string label
        {
            get
            {
                if (m_Label == null)
                {
                    m_Label = ToShortFullName(type);
                }

                return m_Label;
            }
        }

        public void AssignFrom(DependencyTypeInfo src)
        {
            userAdded = src.userAdded;
            type = src.type;
            ignore = src.ignore;
            defaultInstantiationMode = src.defaultInstantiationMode;
            supportsModification = src.supportsModification;
            m_Content = null;
        }

        public static string ToShortFullName(string fullName)
        {
            var firstDot = fullName.IndexOf('.');
            return fullName.Substring(firstDot + 1);
        }

        public bool userAdded;
        public string type;
        [NonSerialized] public bool ignore;
        public TemplateInstantiationMode defaultInstantiationMode;
        [NonSerialized] public bool supportsModification;

        GUIContent m_Content;
        public GUIContent content
        {
            get
            {
                if (m_Content == null)
                {
                    m_Content = new GUIContent(label, null, label);
                }

                return m_Content;
            }
        }
    }

    [Serializable]
    partial class SceneTemplateProjectSettings
    {
        [AutoStaticsCleanupOnCodeReload]
        static SceneTemplateProjectSettings m_Instance;
        [AutoStaticsCleanupOnCodeReload]
        static List<SearchProposition> m_AllTypesPropositions;
        [AutoStaticsCleanupOnCodeReload]
        static float m_MaxLabelWidth;
        const float kMaxLabelWidth = 450f;

        static class Styles
        {
            public static readonly Vector2 typeSelectorWindowSize = new Vector2(350, 200);
            public static readonly GUIContent addTypeContent = L10n.TextContent("Add type...", null, null, null);
            public static readonly float buttonWidth = 65;
            public static readonly float addTypeButtonWidth = 70;
            public static readonly float verticalSpace = 10;
            public static readonly float labelWidth = 300;
        }

        internal enum NewSceneOverride
        {
            NewSceneDialog,
            [InspectorName("Built-in Scene")]
            BuiltinScene
        }

        public const string k_SettingsKey = "Project/SceneTemplates";
        public const string k_Path = "ProjectSettings/SceneTemplateSettings.json";

        public List<PinState> templatePinStates = new List<PinState>();
        [AutoStaticsCleanupOnCodeReload]
        public static List<DependencyTypeInfo> defaultDependencyTypeInfos = new List<DependencyTypeInfo>();
        public List<DependencyTypeInfo> dependencyTypeInfos = new List<DependencyTypeInfo>();
        public DependencyTypeInfo defaultDependencyTypeInfo;

        public static SceneTemplateProjectSettings Get()
        {
            if (m_Instance == null)
            {
                InitDefaultDependencyTypeInfos();

                m_Instance = Load(k_Path);
                if (m_Instance == null)
                {
                    m_Instance = new SceneTemplateProjectSettings();
                }

                m_Instance.SetupDependencyTypeInfos();
            }

            return m_Instance;
        }

        public NewSceneOverride newSceneOverride;

        public bool GetPinState(string id)
        {
            #pragma warning disable UAC2001 // Avoid Linq
            var p = templatePinStates.FirstOrDefault(ps => ps.templateId == id);
#pragma warning restore UAC2001
            return p != null && p.isEnabled;
        }

        public void SetPinState(string id, bool isEnabled)
        {
            #pragma warning disable UAC2001 // Avoid Linq
            var p = templatePinStates.FirstOrDefault(ps => ps.templateId == id);
#pragma warning restore UAC2001
            if (p == null)
            {
                p = new PinState()
                {
                    templateId = id
                };
                templatePinStates.Add(p);
            }
            p.isEnabled = isEnabled;

            Save(k_Path, this);
        }

        public DependencyTypeInfo GetDependencyInfo(Type type)
        {
            var typeId = type.FullName;
            if (typeId == null)
                return defaultDependencyTypeInfo;

            // Direct type match:
            var depInfo = dependencyTypeInfos.Find(di => di.type == typeId);
            if (depInfo != null)
            {
                return depInfo;
            }

            // Partial matching
            depInfo = dependencyTypeInfos.Find(di => typeId.EndsWith(di.type));
            if (depInfo != null)
            {
                return depInfo;
            }

            return defaultDependencyTypeInfo;
        }

        public DependencyTypeInfo GetDependencyInfo(UnityEngine.Object obj)
        {
            if (obj == null)
                return defaultDependencyTypeInfo;

            return GetDependencyInfo(obj.GetType());
        }

        public static void Save(string path = null, SceneTemplateProjectSettings settings = null)
        {
            path = path ?? k_Path;
            settings = settings ?? Get();
            var json = JsonUtility.ToJson(settings, true);
            FileUtil.WriteTextFileToDisk(path, json);
        }

        public static SceneTemplateProjectSettings Load(string path)
        {
            if (!System.IO.File.Exists(k_Path))
                return null;
            try
            {
                var text = System.IO.File.ReadAllText(k_Path);
                return JsonUtility.FromJson<SceneTemplateProjectSettings>(text);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }

        internal static void Reset()
        {
            if (System.IO.File.Exists(k_Path))
            {
                System.IO.File.Delete(k_Path);
            }

            m_Instance = null;
        }

        static void InitDefaultDependencyTypeInfos()
        {
            defaultDependencyTypeInfos = new List<DependencyTypeInfo>();

            defaultDependencyTypeInfos.Add(new DependencyTypeInfo(typeof(MonoScript))
            {
                ignore = true
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo(typeof(Shader))
            {
                ignore = true
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo(typeof(ComputeShader))
            {
                ignore = true
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo(typeof(ShaderVariantCollection))
            {
                ignore = true
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo(typeof(LightingDataAsset))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
                supportsModification = false
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo(typeof(SceneAsset))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Reference,
                supportsModification = true
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo(typeof(GameObject))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo(typeof(Texture))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo(typeof(Texture2D))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo(typeof(Material))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo(typeof(Cubemap))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo(typeof(LightingSettings))
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo("UnityEngine.AnimationClip")
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo("UnityEditor.Animations.AnimatorController")
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo("UnityEngine.AnimatorOverrideController")
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo("UnityEngine.PhysicsMaterial")
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo("UnityEngine.PhysicsMaterial2D")
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo("UnityEngine.Timeline.TimelineAsset")
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo("UnityEditor.Audio.AudioMixerController")
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo("UnityEngine.Rendering.PostProcessing.PostProcessResources")
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo("UnityEngine.Rendering.PostProcessing.PostProcessProfile")
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo("UnityEngine.Rendering.VolumeProfile")
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
            });
            Sort(defaultDependencyTypeInfos);
        }

        static void Sort(List<DependencyTypeInfo> typeInfos)
        {
            typeInfos.Sort((a, b) =>
            {
                if (a.userAdded == b.userAdded)
                    return a.label.CompareTo(b.label);
                return a.userAdded ? 1 : -1;
            });
        }

        void SetupDependencyTypeInfos()
        {
            var needSaving = false;
            if (defaultDependencyTypeInfo == null)
            {
                defaultDependencyTypeInfo = new DependencyTypeInfo("<default_scene_template_dependencies>")
                {
                    ignore = false,
                    defaultInstantiationMode = TemplateInstantiationMode.Reference,
                    supportsModification = true
                };
                needSaving = true;
            }
            else
            {
                defaultDependencyTypeInfo.ignore = false;
                defaultDependencyTypeInfo.supportsModification = true;
            }

            foreach (var defaultDti in defaultDependencyTypeInfos)
            {
                var dti = dependencyTypeInfos.Find(dti => dti.type == defaultDti.type);
                if (dti == null)
                {
                    dti = new DependencyTypeInfo(defaultDti);
                    dependencyTypeInfos.Add(dti);
                    needSaving = true;
                }
                else
                {
                    // These parameters are not considered to be user definable so always override it from default.
                    dti.ignore = defaultDti.ignore;
                    dti.supportsModification = defaultDti.supportsModification;
                }
            }

            Sort(dependencyTypeInfos);

            if (needSaving)
            {
                Save(k_Path, this);
            }
        }

        [UsedImplicitly, SettingsProvider]
        static SettingsProvider CreateSettings()
        {
            return new SettingsProvider(k_SettingsKey, SettingsScope.Project)
            {
                keywords = L10n.Tr(new[] { "unity", "editor", "scene", "clone", "template" }, null),
                activateHandler = (text, rootElement) =>
                {
                    if (m_AllTypesPropositions == null)
                    {
                        var allTypes = TypeCache.GetTypesDerivedFrom<UnityEngine.Object>();
                        #pragma warning disable UAC2001 // Avoid Linq
                        m_AllTypesPropositions = BuildPropositionsFromTypes(allTypes).ToList();
#pragma warning restore UAC2001
                    }
                },
                label = L10n.Tr("Scene Template", null),
                guiHandler = OnGUIHandler
            };
        }

        static IEnumerable<SearchProposition> BuildPropositionsFromTypes(IEnumerable<Type> types)
        {
            foreach (var t in types)
            {
                yield return new SearchProposition(
                    label: DependencyTypeInfo.ToShortFullName(t.FullName),
                    type: t,
                    icon: Search.SearchUtils.GetTypeIcon(t));
            }
        }

        static void OnGUIHandler(string obj)
        {
            if (m_MaxLabelWidth == 0)
            {
                #pragma warning disable UAC2001 // Avoid Linq
                m_MaxLabelWidth = Get().dependencyTypeInfos.Select(ti => ti.content).Max(content => EditorStyles.label.CalcSize(content).x);
#pragma warning restore UAC2001
                m_MaxLabelWidth = Mathf.Min(kMaxLabelWidth, m_MaxLabelWidth);
            }

            var settings = Get();
            using (new SettingsWindow.GUIScope())
            {
                var oldLabelWidth = EditorGUIUtility.labelWidth;
                #pragma warning disable UAL0018 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
                EditorGUIUtility.labelWidth = m_MaxLabelWidth;
                #pragma warning restore UAL0018

                if (Unsupported.IsDeveloperMode())
                {
                    if (GUILayout.Button(L10n.Tr("Clear Scene Template Preferences", null)))
                    {
                        ClearPreferences();
                    }
                }

                EditorGUI.BeginChangeCheck();
                settings.newSceneOverride = (NewSceneOverride)EditorGUILayout.EnumPopup(L10n.TextContent("New Scene Menu", null, null, null), settings.newSceneOverride, GUILayout.Width(m_MaxLabelWidth + 150), GUILayout.ExpandWidth(false));
                if (EditorGUI.EndChangeCheck())
                {
                    Save();
                }

                GUILayout.Space(Styles.verticalSpace);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(L10n.Tr("Default types", null), EditorStyles.boldLabel, GUILayout.Width(m_MaxLabelWidth - 15));
                    GUILayout.Label(L10n.Tr("Clone", null), EditorStyles.boldLabel);
                }

                foreach (var depInfo in settings.dependencyTypeInfos)
                {
                    if (depInfo.ignore || !depInfo.supportsModification)
                        continue;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        var toClone = EditorGUILayout.Toggle(depInfo.content, depInfo.defaultInstantiationMode == TemplateInstantiationMode.Clone, GUILayout.Width(m_MaxLabelWidth + 20));
                        if (EditorGUI.EndChangeCheck())
                        {
                            depInfo.defaultInstantiationMode = toClone ? TemplateInstantiationMode.Clone : TemplateInstantiationMode.Reference;
                            Save();
                        }

                        if (GUILayout.Button(L10n.Tr("Remove", null), GUILayout.Width(Styles.buttonWidth)))
                        {
                            settings.dependencyTypeInfos.Remove(depInfo);
                            Save();
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                GUILayout.Space(Styles.verticalSpace);

                EditorGUI.BeginChangeCheck();
                var clone = EditorGUILayout.Toggle(L10n.TextContent("All Other Types", null, null, null), settings.defaultDependencyTypeInfo.defaultInstantiationMode == TemplateInstantiationMode.Clone);
                if (EditorGUI.EndChangeCheck())
                {
                    settings.defaultDependencyTypeInfo.defaultInstantiationMode = clone ? TemplateInstantiationMode.Clone : TemplateInstantiationMode.Reference;
                    Save();
                }

                EditorGUIUtility.labelWidth = oldLabelWidth;

                GUILayout.Space(Styles.verticalSpace);

                using (new EditorGUILayout.HorizontalScope())
                {
                    var listDropDownBtnRect = EditorGUILayout.GetControlRect(false, GUILayout.Width(Styles.addTypeButtonWidth));
                    if (EditorGUI.DropdownButton(listDropDownBtnRect, Styles.addTypeContent, FocusType.Passive, GUI.skin.button))
                    {
                        #pragma warning disable UAC2001 // Avoid Linq
                        var alreadyAddedTypeIds = settings.dependencyTypeInfos.Select(d => d.type);
#pragma warning restore UAC2001
                        var typeIdsSet = new HashSet<string>(alreadyAddedTypeIds);
                        #pragma warning disable UAC2001 // Avoid Linq
                        var availablePropositions = m_AllTypesPropositions.Where(p => !typeIdsSet.Contains(p.type.FullName)).ToList();
#pragma warning restore UAC2001
                        ListSelectionWindow.Open(listDropDownBtnRect, availablePropositions, selectedIndex =>
                        {
                            if (selectedIndex != -1)
                            {
                                var type = availablePropositions[selectedIndex].type;
                                AddNewType(type);
                            }
                        });
                    }
                }

                GUILayout.Space(Styles.verticalSpace);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(L10n.Tr("Reset Defaults", null)))
                    {
                        ResetDefaults();
                    }
                    GUILayout.FlexibleSpace();
                }

                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }

        static void ClearPreferences()
        {
            EditorPrefs.DeleteKey("SceneTemplateInspectorDetailsFoldout");
            EditorPrefs.DeleteKey("SceneTemplateInspectorThumbnailFoldout");
            EditorPrefs.DeleteKey("SceneTemplatePipelineFoldout");
            EditorPrefs.DeleteKey("SceneTemplateDependenciesFoldout");
            EditorPrefs.DeleteKey(SceneTemplateDialog.GetKeyName("m_Splitter"));
            EditorPrefs.DeleteKey(SceneTemplateDialog.GetKeyName("sizeLevel"));
            EditorPrefs.DeleteKey(SceneTemplateDialog.GetKeyName("m_LastSelectedTemplate"));
        }

        internal static void ResetDefaults()
        {
            var settings = Get();
            settings.dependencyTypeInfos = new List<DependencyTypeInfo>();
            settings.SetupDependencyTypeInfos();
            Save();
        }

        internal static bool CanAddType(Type type)
        {
            var settings = Get();
            var typeId = type.FullName;
            return settings.dependencyTypeInfos.Find(d => d.type == typeId) == null;
        }

        internal static bool AddNewType(Type type)
        {
            if (!CanAddType(type))
                return false;

            var typeId = type.FullName;
            var label = DependencyTypeInfo.ToShortFullName(type.FullName);
            var settings = Get();
            var newDepInfo = new DependencyTypeInfo(typeId, label)
            {
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
                userAdded = true
            };

            var depInfoWidth = EditorStyles.label.CalcSize(newDepInfo.content).x;
            if (depInfoWidth > m_MaxLabelWidth)
            {
                m_MaxLabelWidth = Mathf.Min(kMaxLabelWidth, depInfoWidth);
            }

            settings.dependencyTypeInfos.Add(newDepInfo);
            Sort(settings.dependencyTypeInfos);
            Save();

            return true;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
