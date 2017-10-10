// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor
{
    class SubModuleUI : ModuleUI
    {
        SerializedProperty m_SubEmitters;

        private int m_CheckObjectIndex = -1;

        // Keep in sync with enum in ParticleSystemCommmon.h
        public enum SubEmitterType
        {
            None = -1,
            Birth = 0,
            Collision,
            Death,
            TypesMax,
        };

        class Texts
        {
            public GUIContent create = EditorGUIUtility.TextContent("|Create and assign a Particle System as sub emitter");
            public GUIContent inherit = EditorGUIUtility.TextContent("Inherit");

            public GUIContent[] subEmitterTypes = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Birth"),
                EditorGUIUtility.TextContent("Collision"),
                EditorGUIUtility.TextContent("Death")
            };

            // Keep in sync with SubModule::InheritedProperties
            public string[] propertyTypes =
            {
                "Color",
                "Size",
                "Rotation",
                "Lifetime"
            };
        }
        private static Texts s_Texts;


        public SubModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "SubModule", displayName)
        {
            m_ToolTip = "Sub emission of particles. This allows each particle to emit particles in another system.";
            Init(); // Init when created because we need to query if we have any subemitters referenced
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_SubEmitters != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_SubEmitters = GetProperty("subEmitters");
        }

        void CreateSubEmitter(SerializedProperty objectRefProp, int index, SubEmitterType type)
        {
            GameObject subEmitter = m_ParticleSystemUI.m_ParticleEffectUI.CreateParticleSystem(m_ParticleSystemUI.m_ParticleSystems[0], type);
            subEmitter.name = "SubEmitter" + index;
            objectRefProp.objectReferenceValue = subEmitter.GetComponent<ParticleSystem>();
        }

        void Update()
        {
            if (m_CheckObjectIndex >= 0)
            {
                // Wait until the ObjectSelector is closed before checking object
                if (!ObjectSelector.isVisible)
                {
                    SerializedProperty subEmitterData = m_SubEmitters.GetArrayElementAtIndex(m_CheckObjectIndex);
                    SerializedProperty subEmitter = subEmitterData.FindPropertyRelative("emitter");
                    Object obj = subEmitter.objectReferenceValue;
                    ParticleSystem newSubEmitter = obj as ParticleSystem;
                    if (newSubEmitter != null)
                    {
                        bool validSubemitter = true;

                        if (ValidateSubemitter(newSubEmitter))
                        {
                            string errorMsg = ParticleSystemEditorUtils.CheckCircularReferences(newSubEmitter);
                            if (errorMsg.Length == 0)
                            {
                                // Ok there is no circular references, now check if its a child
                                if (!CheckIfChild(obj))
                                    validSubemitter = false;
                            }
                            else
                            {
                                // Circular references detected
                                string circularRefErrorMsg = string.Format("'{0}' could not be assigned as subemitter on '{1}' due to circular referencing!\nBacktrace: {2} \n\nReference will be removed.", newSubEmitter.gameObject.name, m_ParticleSystemUI.m_ParticleSystems[0].gameObject.name, errorMsg);
                                EditorUtility.DisplayDialog("Circular References Detected", circularRefErrorMsg, "Ok");
                                validSubemitter = false;
                            }
                        }
                        else
                        {
                            validSubemitter = false;
                        }

                        if (!validSubemitter)
                        {
                            subEmitter.objectReferenceValue = null; // remove invalid reference
                            m_ParticleSystemUI.ApplyProperties();
                            m_ParticleSystemUI.m_ParticleEffectUI.m_Owner.Repaint();
                        }
                    }

                    // Cleanup
                    m_CheckObjectIndex = -1;
                    EditorApplication.update -= Update;
                }
            }
        }

        internal static bool IsChild(ParticleSystem subEmitter, ParticleSystem root)
        {
            if (subEmitter == null || root == null)
                return false;

            ParticleSystem subRoot = ParticleSystemEditorUtils.GetRoot(subEmitter);
            return subRoot == root;
        }

        private bool ValidateSubemitter(ParticleSystem subEmitter)
        {
            if (subEmitter == null)
                return false;

            ParticleSystem root = ParticleSystemEditorUtils.GetRoot(m_ParticleSystemUI.m_ParticleSystems[0]);
            if (root.gameObject.activeInHierarchy && !subEmitter.gameObject.activeInHierarchy)
            {
                string kReparentText = "The assigned sub emitter is part of a prefab and can therefore not be assigned.";
                EditorUtility.DisplayDialog("Invalid Sub Emitter", kReparentText, "Ok");
                return false;
            }

            if (!root.gameObject.activeInHierarchy && subEmitter.gameObject.activeInHierarchy)
            {
                string kReparentText = "The assigned sub emitter is part of a scene object and can therefore not be assigned to a prefab.";
                EditorUtility.DisplayDialog("Invalid Sub Emitter", kReparentText, "Ok");
                return false;
            }


            return true;
        }

        private bool CheckIfChild(Object subEmitter)
        {
            ParticleSystem root = ParticleSystemEditorUtils.GetRoot(m_ParticleSystemUI.m_ParticleSystems[0]);
            ParticleSystem ps = subEmitter as ParticleSystem;
            if (IsChild(ps, root))
            {
                return true;
            }

            string kReparentText = string.Format("The assigned sub emitter is not a child of the current root particle system GameObject: '{0}' and is therefore NOT considered a part of the current effect. Do you want to reparent it?", root.gameObject.name);
            if (EditorUtility.DisplayDialog(
                    "Reparent GameObjects",
                    kReparentText,
                    "Yes, Reparent",
                    "No, Remove"))
            {
                if (EditorUtility.IsPersistent(subEmitter))
                {
                    var newGo = Object.Instantiate(subEmitter) as GameObject;
                    if (newGo != null)
                    {
                        newGo.transform.parent = m_ParticleSystemUI.m_ParticleSystems[0].transform;
                        newGo.transform.localPosition = Vector3.zero;
                        newGo.transform.localRotation = Quaternion.identity;
                    }
                }
                else
                {
                    if (ps != null)
                    {
                        Undo.SetTransformParent(ps.gameObject.transform.transform, m_ParticleSystemUI.m_ParticleSystems[0].transform, "Reparent sub emitter");
                    }
                }

                return true;
            }
            else if (ps != null)
            {
                // Clear sub-emitters that have been deselected, to avoid having their particles left paused in the Scene View (case 946999)
                ps.Clear(true);
            }

            return false;
        }

        private List<Object> GetSubEmitterProperties()
        {
            List<Object> props = new List<Object>();
            var enumerator = m_SubEmitters.GetEnumerator();

            while (enumerator.MoveNext())
            {
                SerializedProperty subEmitterData = (SerializedProperty)enumerator.Current;
                props.Add(subEmitterData.FindPropertyRelative("emitter").objectReferenceValue);
            }

            return props;
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            // only allow sub-emitter editing in single edit mode
            if (m_ParticleSystemUI.multiEdit)
            {
                EditorGUILayout.HelpBox("Sub Emitter editing is only available when editing a single Particle System", MessageType.Info, true);
                return;
            }

            // get array of subemitters
            List<Object> props = GetSubEmitterProperties();

            // update gui
            GUILayout.BeginHorizontal(GUILayout.Height(EditorGUI.kSingleLineHeight));
            GUILayout.Label("", ParticleSystemStyles.Get().label, GUILayout.ExpandWidth(true));
            GUILayout.Label(s_Texts.inherit, ParticleSystemStyles.Get().label, GUILayout.Width(120));
            GUILayout.EndHorizontal();
            for (int i = 0; i < m_SubEmitters.arraySize; i++)
            {
                ShowSubEmitter(i);
            }

            // get new list of subemitters, so we can check for changes
            List<Object> props2 = GetSubEmitterProperties();

            // validate any new subemitters we assigned
            for (int i = 0; i < Mathf.Min(props.Count, props2.Count); i++)
            {
                if (props[i] != props2[i])
                {
                    if (m_CheckObjectIndex == -1)
                        EditorApplication.update += Update; // ensure its not added more than once

                    // We need to let the ObjectSelector finish its SendEvent and therefore delay showing dialog
                    m_CheckObjectIndex = i;

                    // Clear sub-emitters that have been deselected, to avoid having their particles left paused in the Scene View (case 946999)
                    ParticleSystem ps = props[i] as ParticleSystem;
                    if (ps)
                        ps.Clear(true);
                }
            }
        }

        private void ShowSubEmitter(int index)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(EditorGUI.kSingleLineHeight));

            SerializedProperty subEmitterData = m_SubEmitters.GetArrayElementAtIndex(index);
            SerializedProperty subEmitter = subEmitterData.FindPropertyRelative("emitter");
            SerializedProperty type = subEmitterData.FindPropertyRelative("type");
            SerializedProperty properties = subEmitterData.FindPropertyRelative("properties");

            GUIPopup(GUIContent.none, type, s_Texts.subEmitterTypes, GUILayout.MaxWidth(80));
            GUILayout.Label("", ParticleSystemStyles.Get().label, GUILayout.Width(4));
            GUIObject(GUIContent.none, subEmitter);
            var olPlusStyle = new GUIStyle("OL Plus");
            if (subEmitter.objectReferenceValue == null)
            {
                GUILayout.Label("", ParticleSystemStyles.Get().label, GUILayout.Width(8));
                GUILayout.BeginVertical(GUILayout.Width(16), GUILayout.Height(olPlusStyle.fixedHeight));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(GUIContent.none, ParticleSystemStyles.Get().plus))
                {
                    CreateSubEmitter(subEmitter, index, (SubEmitterType)type.intValue);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.Label("", ParticleSystemStyles.Get().label, GUILayout.Width(24));
            }
            GUIMask(GUIContent.none, properties, s_Texts.propertyTypes, GUILayout.Width(100));
            GUILayout.Label("", ParticleSystemStyles.Get().label, GUILayout.Width(8));

            // add plus button to first element
            if (index == 0)
            {
                if (GUILayout.Button(GUIContent.none, olPlusStyle, GUILayout.Width(16)))
                {
                    m_SubEmitters.InsertArrayElementAtIndex(m_SubEmitters.arraySize);
                    SerializedProperty newSubEmitterData = m_SubEmitters.GetArrayElementAtIndex(m_SubEmitters.arraySize - 1);
                    SerializedProperty newSubEmitter = newSubEmitterData.FindPropertyRelative("emitter");
                    newSubEmitter.objectReferenceValue = null;
                }
            }
            // add minus button to all other elements
            else
            {
                if (GUILayout.Button(GUIContent.none, new GUIStyle("OL Minus"), GUILayout.Width(16)))
                    m_SubEmitters.DeleteArrayElementAtIndex(index);
            }

            GUILayout.EndHorizontal();
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            text += "\nSub Emitters module is enabled.";
        }
    }
} // namespace UnityEditor
