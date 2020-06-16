// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityEditor.SceneTemplate
{
    [Serializable]
    internal class PinState
    {
        public string templateId;
        public bool isEnabled;
    }

    [Serializable]
    [DebuggerDisplay("{type} - {defaultInstantiationMode}")]
    internal class DependencyTypeInfo
    {
        public DependencyTypeInfo(System.Type type)
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
    }

    [Serializable]
    internal class SceneTemplateProjectSettings
    {
        private static SceneTemplateProjectSettings m_Instance;
        private static System.Type m_TypeToAdd;
        private static string m_TypeToAddLabel;
        private static System.Type[] m_AllTypes;
        private static string[] m_AllTypesLabels;

        private static class Styles
        {
            public static Vector2 typeSelectorWindowSize = new Vector2(350, 200);
            public static GUIContent browseTypeContent = new GUIContent("Browse...");
            public static float buttonWidth = 65;
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

        public DependencyTypeInfo GetDependencyInfo(System.Type type)
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

        public static void Save(string path, SceneTemplateProjectSettings settings)
        {
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
            if (System.IO.File.Exists(SceneTemplateProjectSettings.k_Path))
            {
                System.IO.File.Delete(SceneTemplateProjectSettings.k_Path);
            }

            m_Instance = null;
        }

        private static void InitDefaultDependencyTypeInfos()
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
                defaultInstantiationMode = TemplateInstantiationMode.Clone,
                supportsModification = false
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

        private static void Sort(List<DependencyTypeInfo> typeInfos)
        {
            typeInfos.Sort((a, b) =>
            {
                if (a.userAdded == b.userAdded)
                    return a.label.CompareTo(b.label);
                return a.userAdded ? 1 : -1;
            });
        }

        private void SetupDependencyTypeInfos()
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
        private static SettingsProvider CreateSettings()
        {
            return new SettingsProvider(k_SettingsKey, SettingsScope.Project)
            {
                keywords = new[] { "unity", "editor", "scene", "clone", "template" },
                activateHandler = (text, rootElement) =>
                {
                    if (m_AllTypes == null)
                    {
                        m_AllTypes = TypeCache.GetTypesDerivedFrom<UnityEngine.Object>().ToArray();
                        m_AllTypesLabels = m_AllTypes.Select(t => DependencyTypeInfo.ToShortFullName(t.FullName)).ToArray();
                    }
                    m_TypeToAddLabel = "";
                    m_TypeToAdd = null;
                },
                label = "Scene Template",
                guiHandler = OnGUIHandler
            };
        }

        private static void OnGUIHandler(string obj)
        {
            var settings = Get();
            using (new SettingsWindow.GUIScope())
            {
                var oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Styles.labelWidth;


                if (Unsupported.IsDeveloperMode())
                {
                    if (GUILayout.Button("Clear Scene Template Preferences"))
                    {
                        ClearPreferences();
                    }
                }

                settings.newSceneOverride = (NewSceneOverride)EditorGUILayout.EnumPopup(new GUIContent("New Scene Menu"), settings.newSceneOverride, GUILayout.Width(450), GUILayout.ExpandWidth(false));
                GUILayout.Space(Styles.verticalSpace);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Default types", EditorStyles.boldLabel, GUILayout.Width(285));
                    GUILayout.Label("Clone", EditorStyles.boldLabel);
                }

                foreach (var depInfo in settings.dependencyTypeInfos)
                {
                    if (depInfo.ignore || !depInfo.supportsModification)
                        continue;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        var toClone = EditorGUILayout.Toggle(new GUIContent(depInfo.label), depInfo.defaultInstantiationMode == TemplateInstantiationMode.Clone, GUILayout.Width(325));
                        if (EditorGUI.EndChangeCheck())
                        {
                            depInfo.defaultInstantiationMode = toClone ? TemplateInstantiationMode.Clone : TemplateInstantiationMode.Reference;
                            Save(k_Path, settings);
                        }

                        if (GUILayout.Button("Remove", GUILayout.Width(Styles.buttonWidth)))
                        {
                            settings.dependencyTypeInfos.Remove(depInfo);
                            Save(k_Path, settings);
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                GUILayout.Space(Styles.verticalSpace);

                EditorGUI.BeginChangeCheck();
                var clone = EditorGUILayout.Toggle(new GUIContent("All Other Types"), settings.defaultDependencyTypeInfo.defaultInstantiationMode == TemplateInstantiationMode.Clone);
                if (EditorGUI.EndChangeCheck())
                {
                    settings.defaultDependencyTypeInfo.defaultInstantiationMode = clone ? TemplateInstantiationMode.Clone : TemplateInstantiationMode.Reference;
                    Save(k_Path, settings);
                }

                GUILayout.Space(Styles.verticalSpace);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Add Type", EditorStyles.boldLabel, GUILayout.Width(Styles.buttonWidth));
                    EditorGUI.BeginChangeCheck();
                    m_TypeToAddLabel = GUILayout.TextField(m_TypeToAddLabel);
                    if (EditorGUI.EndChangeCheck())
                    {
                        // User has overridden the type.
                        m_TypeToAdd = null;
                    }

                    var listDropDownBtnRect = EditorGUILayout.GetControlRect(false, GUILayout.Width(Styles.buttonWidth));
                    ListSelectionWindow.SelectionButton(listDropDownBtnRect, Styles.typeSelectorWindowSize, Styles.browseTypeContent, GUI.skin.button, m_AllTypesLabels, selectedIndex =>
                    {
                        if (selectedIndex != -1)
                        {
                            m_TypeToAddLabel = m_AllTypesLabels[selectedIndex];
                            m_TypeToAdd = m_AllTypes[selectedIndex];
                        }
                    });

                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(m_TypeToAddLabel)))
                        if (GUILayout.Button("Add", GUILayout.Width(Styles.buttonWidth)))
                        {
                            var typeId = m_TypeToAdd != null ? m_TypeToAdd.FullName : m_TypeToAddLabel;
                            if (settings.dependencyTypeInfos.Find(d => d.type == typeId) == null)
                            {
                                settings.dependencyTypeInfos.Add(new DependencyTypeInfo(typeId, m_TypeToAddLabel)
                                {
                                    defaultInstantiationMode = TemplateInstantiationMode.Clone,
                                    userAdded = true
                                });
                                Sort(settings.dependencyTypeInfos);
                                Save(k_Path, settings);
                                m_TypeToAddLabel = "";
                                m_TypeToAdd = null;
                                GUIUtility.ExitGUI();
                            }
                            else
                            {
                                Debug.LogWarning($"Already a dependency information for type: {typeId}");
                            }
                        }
                }

                GUILayout.Space(Styles.verticalSpace);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Reset Defaults"))
                    {
                        ResetDefaults();
                    }
                    GUILayout.FlexibleSpace();
                }

                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }

        private static void ClearPreferences()
        {
            EditorPrefs.DeleteKey("SceneTemplateInspectorDetailsFoldout");
            EditorPrefs.DeleteKey("SceneTemplateInspectorThumbnailFoldout");
            EditorPrefs.DeleteKey("SceneTemplatePipelineFoldout");
            EditorPrefs.DeleteKey("SceneTemplateDependenciesFoldout");
            EditorPrefs.DeleteKey(SceneTemplateDialog.GetKeyName("m_Splitter"));
            EditorPrefs.DeleteKey(SceneTemplateDialog.GetKeyName("sizeLevel"));
            EditorPrefs.DeleteKey(SceneTemplateDialog.GetKeyName("m_LastSelectedTemplate"));
        }

        private static void ResetDefaults()
        {
            var settings = Get();
            settings.dependencyTypeInfos = new List<DependencyTypeInfo>();
            settings.SetupDependencyTypeInfos();
        }
    }
}
