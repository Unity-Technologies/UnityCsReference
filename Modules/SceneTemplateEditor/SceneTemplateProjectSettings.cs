// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
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
        public bool ignore;
        public TemplateInstantiationMode defaultInstantiationMode;
        public bool supportsModification;

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
    class SceneTemplateProjectSettings
    {
        static SceneTemplateProjectSettings m_Instance;
        static List<SearchProposition> m_AllTypesPropositions;
        static float m_MaxLabelWidth;
        const float kMaxLabelWidth = 450f;

        static class Styles
        {
            public static Vector2 typeSelectorWindowSize = new Vector2(350, 200);
            public static GUIContent addTypeContent = L10n.TextContent("Add type...");
            public static float buttonWidth = 65;
            public static float addTypeButtonWidth = 70;
            public static float verticalSpace = 10;
            public static float labelWidth = 300;
        }

        internal enum NewSceneOverride
        {
            NewSceneDialog,
            BuiltinScene
        }

        public const string k_SettingsKey = "Project/SceneTemplates";
        public const string k_Path = "ProjectSettings/SceneTemplateSettings.json";

        public List<PinState> templatePinStates = new List<PinState>();
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
            var p = templatePinStates.FirstOrDefault(ps => ps.templateId == id);
            return p != null && p.isEnabled;
        }

        public void SetPinState(string id, bool isEnabled)
        {
            var p = templatePinStates.FirstOrDefault(ps => ps.templateId == id);
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
            System.IO.File.WriteAllText(path, json);
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
            defaultDependencyTypeInfos.Add(new DependencyTypeInfo("UnityEngine.PhysicMaterial")
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

            foreach (var dependencyTypeInfo in defaultDependencyTypeInfos)
            {
                if (dependencyTypeInfos.Find(dti => dti.type == dependencyTypeInfo.type) == null)
                {
                    var dti = new DependencyTypeInfo(dependencyTypeInfo);
                    dependencyTypeInfos.Add(dti);
                    needSaving = true;
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
                keywords = L10n.Tr(new[] { "unity", "editor", "scene", "clone", "template" }),
                activateHandler = (text, rootElement) =>
                {
                    if (m_AllTypesPropositions == null)
                    {
                        var allTypes = TypeCache.GetTypesDerivedFrom<UnityEngine.Object>();
                        m_AllTypesPropositions = BuildPropositionsFromTypes(allTypes).ToList();
                    }
                },
                label = L10n.Tr("Scene Template"),
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
                m_MaxLabelWidth = Get().dependencyTypeInfos.Select(ti => ti.content).Max(content => EditorStyles.label.CalcSize(content).x);
                m_MaxLabelWidth = Mathf.Min(kMaxLabelWidth, m_MaxLabelWidth);
            }

            var settings = Get();
            using (new SettingsWindow.GUIScope())
            {
                var oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = m_MaxLabelWidth;

                if (Unsupported.IsDeveloperMode())
                {
                    if (GUILayout.Button(L10n.Tr("Clear Scene Template Preferences")))
                    {
                        ClearPreferences();
                    }
                }

                EditorGUI.BeginChangeCheck();
                settings.newSceneOverride = (NewSceneOverride)EditorGUILayout.EnumPopup(L10n.TextContent("New Scene Menu"), settings.newSceneOverride, GUILayout.Width(m_MaxLabelWidth + 150), GUILayout.ExpandWidth(false));
                if (EditorGUI.EndChangeCheck())
                {
                    Save();
                }

                GUILayout.Space(Styles.verticalSpace);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(L10n.Tr("Default types"), EditorStyles.boldLabel, GUILayout.Width(m_MaxLabelWidth - 15));
                    GUILayout.Label(L10n.Tr("Clone"), EditorStyles.boldLabel);
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

                        if (GUILayout.Button(L10n.Tr("Remove"), GUILayout.Width(Styles.buttonWidth)))
                        {
                            settings.dependencyTypeInfos.Remove(depInfo);
                            Save();
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                GUILayout.Space(Styles.verticalSpace);

                EditorGUI.BeginChangeCheck();
                var clone = EditorGUILayout.Toggle(L10n.TextContent("All Other Types"), settings.defaultDependencyTypeInfo.defaultInstantiationMode == TemplateInstantiationMode.Clone);
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
                        var alreadyAddedTypeIds = settings.dependencyTypeInfos.Select(d => d.type);
                        var typeIdsSet = new HashSet<string>(alreadyAddedTypeIds);
                        var availablePropositions = m_AllTypesPropositions.Where(p => !typeIdsSet.Contains(p.type.FullName)).ToList();
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
                    if (GUILayout.Button(L10n.Tr("Reset Defaults")))
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
