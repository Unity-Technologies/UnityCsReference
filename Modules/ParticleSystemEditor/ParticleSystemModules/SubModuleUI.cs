// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    class SubModuleUI : ModuleUI
    {
        SerializedProperty m_SubEmitters;

        private int m_CheckObjectIndex = -1;
        private Object m_PreviousSubEmitter;

        // Keep in sync with enum in ParticleSystemCommmon.h
        public enum SubEmitterType
        {
            None = -1,
            Birth = 0,
            Collision,
            Death,
            Trigger,
            Manual,
            TypesMax,
        }

        class Texts
        {
            public GUIContent create = EditorGUIUtility.TrTextContent("", "Create and assign a Particle System as sub-emitter.");
            public GUIContent inherit = EditorGUIUtility.TrTextContent("Inherit", "Determines what properties to inherit from the Parent System.");
            public GUIContent emitProbability = EditorGUIUtility.TrTextContent("Emit Probability", "Determines the proportion of sub-emitter spawn events that successfully triggers the associated sub-emitter.");
            public GUIContent invalidSubEmitterParenting = EditorGUIUtility.TrTextContent("The following sub-emitters are not descendents of their owning Particle System. Reparent the sub-emitters so that they are children of their owning system:");

            public GUIContent[] subEmitterTypes = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Birth"),
                EditorGUIUtility.TrTextContent("Collision"),
                EditorGUIUtility.TrTextContent("Death"),
                EditorGUIUtility.TrTextContent("Trigger"),
                EditorGUIUtility.TrTextContent("Manual")
            };

            // Keep in sync with SubModule::InheritedProperties
            public string[] propertyTypes =
            {
                "Color",
                "Size",
                "Rotation",
                "Lifetime",
                "Duration"
            };
        }
        private static Texts s_Texts;

        ReorderableList m_EmittersList;

        static readonly float kSubEmitterPadding = EditorGUIUtility.standardVerticalSpacing * 2;

        public SubModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "SubModule", displayName)
        {
            m_ToolTip = L10n.Tr("Sub emission of particles. This allows each particle to emit particles in another system.");
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
            m_EmittersList = new ReorderableList(m_SubEmitters.m_SerializedObject, m_SubEmitters, true, false, true, true);
            m_EmittersList.headerHeight = 0;
            m_EmittersList.drawElementCallback = DrawSubEmitterElementCallback;
            m_EmittersList.elementHeight = kSingleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 4 + kSubEmitterPadding * 2;
            m_EmittersList.drawElementBackgroundCallback = DrawSubEmitterElementBackgroundCallback;
            m_EmittersList.onAddCallback = OnAddSubEmitterElementCallback;
        }

        void OnAddSubEmitterElementCallback(ReorderableList list)
        {
            m_SubEmitters.InsertArrayElementAtIndex(m_SubEmitters.arraySize);
            SerializedProperty newSubEmitterData = m_SubEmitters.GetArrayElementAtIndex(m_SubEmitters.arraySize - 1);
            SerializedProperty newSubEmitter = newSubEmitterData.FindPropertyRelative("emitter");
            newSubEmitter.objectReferenceValue = null;
        }

        static void DrawSubEmitterElementBackgroundCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            GUI.Label(rect, GUIContent.none, EditorStyles.helpBox);
            ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, isActive, isFocused, true);
        }

        void DrawSubEmitterElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            var subEmitterData = m_SubEmitters.GetArrayElementAtIndex(index);
            var subEmitter = subEmitterData.FindPropertyRelative("emitter");
            var type = subEmitterData.FindPropertyRelative("type");
            var properties = subEmitterData.FindPropertyRelative("properties");
            var emitProbability = subEmitterData.FindPropertyRelative("emitProbability");

            rect.height = kSingleLineHeight;
            rect.y += kSubEmitterPadding;

            Rect typeRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth - EditorGUI.kSpacing, rect.height);
            Rect objectRect = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y, rect.width - EditorGUIUtility.labelWidth - EditorGUI.kSpacing * 3, rect.height);
            GUIPopup(typeRect, GUIContent.none, type, s_Texts.subEmitterTypes);
            GUIObject(objectRect, GUIContent.none, subEmitter, null);
            if (subEmitter.objectReferenceValue == null)
            {
                Rect buttonRect = new Rect(objectRect.xMax + EditorGUI.kSpacing, rect.y + 4 , ParticleSystemStyles.Get().plus.fixedWidth, rect.height);
                if (GUI.Button(buttonRect, s_Texts.create, ParticleSystemStyles.Get().plus))
                {
                    CreateSubEmitter(subEmitter, index, (SubEmitterType)type.intValue);
                }
            }

            rect.y += EditorGUIUtility.standardVerticalSpacing + rect.height;
            GUIMask(rect, s_Texts.inherit, properties, s_Texts.propertyTypes);
            rect.y += EditorGUIUtility.standardVerticalSpacing + rect.height;
            GUIFloat(rect, s_Texts.emitProbability, emitProbability);
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
                            string errorMsg = ParticleSystemEffectUtils.CheckCircularReferences(newSubEmitter);
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
                                EditorUtility.DisplayDialog("Circular References Detected", circularRefErrorMsg, "OK");
                                validSubemitter = false;
                            }
                        }
                        else
                        {
                            validSubemitter = false;
                        }

                        if (!validSubemitter)
                        {
                            subEmitter.objectReferenceValue = m_PreviousSubEmitter; // revert invalid reference
                            m_ParticleSystemUI.ApplyProperties();
                            m_ParticleSystemUI.m_ParticleEffectUI.m_Owner.Repaint();
                        }
                    }

                    // Cleanup
                    m_CheckObjectIndex = -1;
                    m_PreviousSubEmitter = null;
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
                EditorUtility.DisplayDialog("Invalid Sub Emitter", kReparentText, "OK");
                return false;
            }

            if (!root.gameObject.activeInHierarchy && subEmitter.gameObject.activeInHierarchy)
            {
                string kReparentText = "The assigned sub emitter is part of a scene object and can therefore not be assigned to a prefab.";
                EditorUtility.DisplayDialog("Invalid Sub Emitter", kReparentText, "OK");
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

            if (PrefabUtility.IsPartOfAnyPrefab(ps.gameObject) && !PrefabUtility.IsAnyPrefabInstanceRoot(ps.gameObject))
            {
                string kPrefabReparentWarn = $"The assigned sub emitter is not part of the current effect because it is not a child of the current root Particle System GameObject: '{root.gameObject.name}'. The Particle System cannot be moved because it is a child of a Prefab instance.";
                EditorUtility.WarnPrefab(ps.gameObject, "Reparent GameObjects", kPrefabReparentWarn, "OK");
                return false;
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

            m_EmittersList.DoLayoutList();

            // show warnings for any subemitters that are not a descendent of the owning system
            string invalidSubEmitters = "";
            foreach (var ps in m_ParticleSystemUI.m_ParticleSystems)
            {
                // skip this for Presets - traversing the parent hierarchy doesn't work because they aren't real GameObjects.
                if ((ps.gameObject.hideFlags & HideFlags.NotEditable) != 0)
                    continue;

                var subEmitters = ps.subEmitters;
                for (int i = 0; i < subEmitters.subEmittersCount; i++)
                {
                    var subEmitter = subEmitters.GetSubEmitterSystem(i);
                    if (subEmitter != null)
                    {
                        bool valid = false;

                        var currentParent = subEmitter.transform.parent;
                        while (currentParent != null)
                        {
                            if (currentParent == ps.transform)
                            {
                                valid = true;
                                break;
                            }

                            currentParent = currentParent.parent;
                        }

                        if (!valid)
                            invalidSubEmitters += string.Format(" - {0}\n", subEmitter.name);
                    }
                }
            }

            if (invalidSubEmitters.Length > 0)
                EditorGUILayout.HelpBox(string.Format("{0}\n{1}", s_Texts.invalidSubEmitterParenting.text, invalidSubEmitters.TrimEnd('\n')), MessageType.Error, true);

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
                    m_PreviousSubEmitter = props[i];

                    // Clear sub-emitters that have been deselected, to avoid having their particles left paused in the Scene View (case 946999)
                    ParticleSystem ps = props[i] as ParticleSystem;
                    if (ps)
                        ps.Clear(true);
                }
            }
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            text += "\nSub Emitters module is enabled.";
        }
    }
} // namespace UnityEditor
