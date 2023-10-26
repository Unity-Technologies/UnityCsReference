// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Inspector.GraphicsSettingsInspectors
{
    [CustomPropertyDrawer(typeof(IRenderPipelineGraphicsSettings), useForChildren: true)]
    class RenderPipelineGraphicsSettingsPropertyDrawer : PropertyDrawer
    {
        public static bool IsEmpty(SerializedProperty property, out string warnings)
        {
            if (!property.hasVisibleChildren)
            {
                warnings = $"This {nameof(IRenderPipelineGraphicsSettings)} has no visible children. Consider using {nameof(HideInInspector)} if you want to completely hide the setting.";
                return true;
            }

            warnings = string.Empty;
            return false;
        }

        // Used in Unit tests
        public static IEnumerable<SerializedProperty> VisibleChildrenEnumerator(SerializedProperty property)
            => ChildrenEnumerator(property, includeInvisible: false, deepSearch: false, p => true);

        internal static IEnumerable<SerializedProperty> ChildrenEnumerator(SerializedProperty property, bool includeInvisible, bool deepSearch, Func<SerializedProperty, bool> selector = null)
        {
            if (!property.hasVisibleChildren)
                yield break;

            selector ??= p => true;

            var iterator = property.Copy();
            var end = iterator.GetEndProperty(includeInvisible);

            // Move to the first child property
            if (includeInvisible)
                iterator.Next(true);
            else
                iterator.NextVisible(true);

            do
            {
                if (selector(iterator))
                    yield return iterator;
                if (includeInvisible)
                    iterator.Next(deepSearch);
                else
                    iterator.NextVisible(deepSearch);
            } while (!SerializedProperty.EqualContents(iterator, end)); // Move to the next sibling property
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IsEmpty(property, out var warnings))
            {
                EditorGUI.HelpBox(position, warnings, MessageType.Warning);
                return;
            }

            int baseNameLength = property.propertyPath.Length + 1;
            foreach (var child in VisibleChildrenEnumerator(property))
            {
                Rect childPosition = position;
                childPosition.height = EditorGUI.GetPropertyHeight(child);
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(childPosition, child);
                if (EditorGUI.EndChangeCheck())
                {
                    child.serializedObject.ApplyModifiedProperties();
                    var settings = property.boxedValue as IRenderPipelineGraphicsSettings;
                    settings.NotifyValueChanged(child.propertyPath.Substring(baseNameLength));
                }
                position.y += childPosition.height + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0f;
            foreach (var child in VisibleChildrenEnumerator(property))
                height += EditorGUI.GetPropertyHeight(child) + EditorGUIUtility.standardVerticalSpacing;
            if (height > 0)
                height -= EditorGUIUtility.standardVerticalSpacing; //remove last one
            return height;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();

            if (IsEmpty(property, out var warnings))
            {
                root.Add(new HelpBox(warnings, HelpBoxMessageType.Warning));
                return root;
            }

            foreach (var child in VisibleChildrenEnumerator(property))
            {
                var propertyField = new PropertyField(child);
                propertyField.RegisterCallback(PropagateOnChangeRecursivelyAtBinding(propertyField));
                root.Add(propertyField);
            }

            return root;
        }

        EventCallback<SerializedPropertyBindEvent> PropagateOnChangeRecursivelyAtBinding(PropertyField propertyField)
        {
            EventCallback<SerializedPropertyBindEvent> callback = null;
            callback = (SerializedPropertyBindEvent evt) => {
                UQueryState<VisualElement> childs;
                if(evt.bindProperty.isArray
                    || (childs = propertyField.Query(name: "unity-content").Visible().Build())
                        .AtIndex(0) == null) //no child
                {
                    // Fields that have no childs or array (handle all resizing and cell changes directly)
                    bool isBindingTime = true;
                    propertyField.RegisterCallback<SerializedPropertyChangeEvent>(evt =>
                    {
                        if (isBindingTime)
                        {
                            isBindingTime = false;
                            return;
                        }
                        AddNotificationRequest(evt);
                    });
                }
                else
                {
                    // Propagate to custom struct and class childs
                    childs.ForEach(e => e.Query<PropertyField>().Visible().ForEach(p => p.RegisterCallback(PropagateOnChangeRecursivelyAtBinding(p))));
                }
                
                propertyField.UnregisterCallback(callback);
                callback = null;
            };
            return callback;
        }

        void AddNotificationRequest(SerializedPropertyChangeEvent evt)
            => Notifier.AddNotificationRequest(evt.changedProperty);
    }

    // Aim is to batch notification for case where several are trigerred.
    // Example:
    //  - Modifying a color, several channel amongst r, g, b, a can be altered with same editor modification
    //  - This result in several time the SerializedPropertyChangeEvent being raised.
    // Also at binding we do not want to raise notification. As there is a previous SerializedObject state to
    // compare, we can trim that, even for Arrays that have a lot of elements to bind/update.
    static internal class Notifier
    {
        struct NotificationRequest
        {
            public IRenderPipelineGraphicsSettings rpgs;
            public string path;

            public NotificationRequest(SerializedProperty property)
            {
                ExtractFirstPropertyNameInPath(property, out path, out var baseLength);
                rpgs = GetRenderPipelineGraphicsSettings(property, baseLength);
            }

            //This extraction start at IRenderPipelineGraphicsSettings as a root
            //m_Settings.m_SettingsList.m_List.Array.data[x].embeddedobject.property -> embeddedobject
            static void ExtractFirstPropertyNameInPath(SerializedProperty property, out string propertyName, out int baseLength)
            {
                propertyName = property.propertyPath;
                baseLength = propertyName.IndexOf(']') + 2;
                   
                int length = propertyName.IndexOf('.', baseLength) - baseLength;
                propertyName = length < 0
                    ? propertyName.Substring(baseLength)
                    : propertyName.Substring(baseLength, length);
            }
                
            static IRenderPipelineGraphicsSettings GetRenderPipelineGraphicsSettings(SerializedProperty property, int baseLength)
                => property.serializedObject.FindProperty(property.propertyPath.Substring(0, baseLength - 1)).boxedValue as IRenderPipelineGraphicsSettings;

            static public bool operator ==(NotificationRequest a, NotificationRequest b)
                => a.rpgs.GetType() == b.rpgs.GetType() && a.path == b.path;
                
            static public bool operator !=(NotificationRequest a, NotificationRequest b)
                => !(a == b);

            public override bool Equals(object obj)
                => obj is NotificationRequest request && request == this;

            public override int GetHashCode()
                => HashCode.Combine(rpgs, path);
        }

        static HashSet<NotificationRequest> s_NotificationRequests = new();
        static bool s_Scoped = false;
        static Dictionary<Type, SerializedObject> s_SharedPreviousStates = null;

        [InitializeOnLoadMethod]
        static void OnDomainReload()
            => RenderPipelineManager.activeRenderPipelineCreated += RecomputeDictionary;

        //internal for tests
        internal static void RecomputeDictionary()
        {
            s_SharedPreviousStates = new();
            EditorGraphicsSettings.ForEachPipelineSettings(gs => {
                if (gs != null)
                    s_SharedPreviousStates[gs.GetType()] = new SerializedObject(gs);
            });
        }

        static void UpdatePreviousState(SerializedProperty property)
            => GetPreviousState(property).Update();

        static SerializedObject GetPreviousState(SerializedProperty property)
        {
            var type = property.serializedObject.targetObject.GetType();
            return s_SharedPreviousStates[type];
        }

        static public void AddNotificationRequest(SerializedProperty property)
        {
            if (property == null)
                throw new ArgumentException("Property cannot be null.", $"{nameof(property)}");

            //If GraphicsSettings window was open when project open, s_SharedPreviousStates can still be null when we call this.
            if (s_SharedPreviousStates == null)
                RecomputeDictionary();
            
            bool dataEquals = SerializedProperty.DataEquals(property, GetPreviousState(property).FindProperty(property.propertyPath));
            if (dataEquals && !s_Scoped)
                return;

            NotificationRequest candidate = new(property);
            if (dataEquals && s_NotificationRequests.Contains(candidate))
                return;

            UpdatePreviousState(property);
            Register(candidate);
        }

        static void Register(NotificationRequest verifiedRequest)
        {
            if (!s_Scoped && s_NotificationRequests.Count == 0)
                EditorApplication.CallDelayed(Notify);

            s_NotificationRequests.Add(verifiedRequest);
        }

        static void Notify()
        {
            foreach (var request in s_NotificationRequests)
                request.rpgs.NotifyValueChanged(request.path);
            s_NotificationRequests.Clear();
        }

        static void BeginScope()
        {
            //If GraphicsSettings window was open when project open, s_SharedPreviousStates can still be null when we create Scope.
            if (s_SharedPreviousStates == null)
                RecomputeDictionary();

            s_Scoped = true;
        }

        static void EndScope()
        {
            s_Scoped = false;
            Notify();
        }

        //For bulk changes like in Reset, we actually need to notify different field even if the serialization change only once
        //Note that this can also be used to iterate over collection of IRenderPipelineGRaphicsSettings.
        internal struct Scope : IDisposable
        {
            SerializedProperty propertyToInspect;
            SerializedProperty stateBeforeChange;

            public Scope(SerializedProperty property, bool updateStateNow = true)
            {
                BeginScope();
                propertyToInspect = property;
                stateBeforeChange = updateStateNow
                    ? CreateUnlinkedDataStateFromCurrentDataInMemory(property)
                    : CreateUnlinkedDataStateFromPreviousState(property);
            }

            static SerializedProperty CreateUnlinkedDataStateFromCurrentDataInMemory(SerializedProperty property)
                => new SerializedObject(GetPreviousState(property).targetObject).FindProperty(property.propertyPath);

            static SerializedProperty CreateUnlinkedDataStateFromPreviousState(SerializedProperty property)
            {
                SerializedObject oldStateReconstructed = new SerializedObject(GetPreviousState(property).targetObject);

                //Any array size difference will cause type tree to have different hash due to the varying size. 
                //To be able to copy the data we first need to fix any child array property
                foreach (var dynamicSizeChild in RenderPipelineGraphicsSettingsPropertyDrawer.ChildrenEnumerator(property, includeInvisible: true, deepSearch: true, p => p.isArray))
                {
                    //AnimationCurv are detected 2 time as an array: ac.m_Curve and ac.m_Curve.Array Only the second one have a size
                    var size = GetPreviousState(property).FindProperty($"{dynamicSizeChild.propertyPath}.size");
                    if (size == null)
                        continue;

                    oldStateReconstructed.CopyFromSerializedProperty(size);
                }

                oldStateReconstructed.CopyFromSerializedProperty(GetPreviousState(property).FindProperty(property.propertyPath));
                return oldStateReconstructed.FindProperty(property.propertyPath);
            }

            void IDisposable.Dispose()
            {
                // in case we iterate with next, go back to original path
                if (propertyToInspect.propertyPath != stateBeforeChange.propertyPath)
                    propertyToInspect = propertyToInspect.serializedObject.FindProperty(stateBeforeChange.propertyPath);

                RecursivelyNotify();
                EndScope();
            }
            
            void RecursivelyNotify()
            {
                void Recurse(SerializedProperty newIt, SerializedProperty oldIt, SerializedProperty end)
                {
                    do
                    {
                        if (newIt.hasVisibleChildren)
                        {
                            if (newIt.isArray && newIt.arraySize != oldIt.arraySize)
                            {
                                AddNotificationRequest(newIt);
                                newIt.NextVisible(false);
                                oldIt.NextVisible(false);
                                continue;
                            }

                            var childEnd = newIt.GetEndProperty();

                            var childNewIt = newIt.Copy();
                            var childOldIt = oldIt.Copy();
                            childNewIt.NextVisible(true);
                            childOldIt.NextVisible(true);

                            Recurse(childNewIt, childOldIt, childEnd);
                        }
                        else
                        {
                            if (!SerializedProperty.DataEquals(newIt, oldIt))
                                AddNotificationRequest(newIt);
                        }

                        newIt.NextVisible(false);
                        oldIt.NextVisible(false);
                    }
                    while (!SerializedProperty.EqualContents(newIt, end));
                }
                var end = propertyToInspect.GetEndProperty();
                Recurse(propertyToInspect, stateBeforeChange, end);
            }
        }
    }
}
