// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public sealed partial class EditorGUI
    {
        [Flags]
        internal enum ObjectFieldValidatorOptions
        {
            None = 0,
            ExactObjectTypeValidation = (1 << 0)
        }

        internal delegate Object ObjectFieldValidator(Object[] references, System.Type objType, SerializedProperty property, ObjectFieldValidatorOptions options);

        internal static Object DoObjectField(Rect position, Rect dropRect, int id, Object obj, System.Type objType, SerializedProperty property, ObjectFieldValidator validator, bool allowSceneObjects)
        {
            return DoObjectField(position, dropRect, id, obj, objType, property, validator, allowSceneObjects, EditorStyles.objectField);
        }

        internal enum ObjectFieldVisualType { IconAndText, LargePreview, MiniPreview }

        // when current event is mouse click, this function pings the object, or
        // if shift/control is pressed and object is a texture, pops up a large texture
        // preview window
        internal static void PingObjectOrShowPreviewOnClick(Object targetObject, Rect position)
        {
            if (targetObject == null)
                return;

            Event evt = Event.current;
            // ping object
            bool anyModifiersPressed = evt.shift || evt.control;
            if (!anyModifiersPressed)
            {
                EditorGUIUtility.PingObject(targetObject);
                return;
            }

            // show large object preview popup; right now only for textures
            if (targetObject is Texture)
            {
                PopupWindowWithoutFocus.Show(
                    new RectOffset(6, 3, 0, 3).Add(position),
                    new ObjectPreviewPopup(targetObject),
                    new[] { PopupLocationHelper.PopupLocation.Left, PopupLocationHelper.PopupLocation.Below, PopupLocationHelper.PopupLocation.Right });
            }
        }

        static Object AssignSelectedObject(SerializedProperty property, ObjectFieldValidator validator, System.Type objectType, Event evt)
        {
            Object[] references = { ObjectSelector.GetCurrentObject() };
            Object assigned = validator(references, objectType, property, ObjectFieldValidatorOptions.None);

            // Assign the value
            if (property != null)
                property.objectReferenceValue = assigned;

            GUI.changed = true;
            evt.Use();
            return assigned;
        }

        internal static Object DoObjectField(Rect position, Rect dropRect, int id, Object obj, System.Type objType, SerializedProperty property, ObjectFieldValidator validator, bool allowSceneObjects, GUIStyle style)
        {
            if (validator == null)
                validator = ValidateObjectFieldAssignment;
            Event evt = Event.current;
            EventType eventType = evt.type;

            // special case test, so we continue to ping/select objects with the object field disabled
            if (!GUI.enabled && GUIClip.enabled && (Event.current.rawType == EventType.MouseDown))
                eventType = Event.current.rawType;

            bool hasThumbnail = EditorGUIUtility.HasObjectThumbnail(objType);

            // Determine visual type
            ObjectFieldVisualType visualType = ObjectFieldVisualType.IconAndText;
            if (hasThumbnail && position.height <= kObjectFieldMiniThumbnailHeight && position.width <= kObjectFieldMiniThumbnailWidth)
                visualType = ObjectFieldVisualType.MiniPreview;
            else if (hasThumbnail && position.height > kSingleLineHeight)
                visualType = ObjectFieldVisualType.LargePreview;

            Vector2 oldIconSize = EditorGUIUtility.GetIconSize();
            if (visualType == ObjectFieldVisualType.IconAndText)
                EditorGUIUtility.SetIconSize(new Vector2(12, 12));  // Have to be this small to fit inside a single line height ObjectField
            else if (visualType == ObjectFieldVisualType.LargePreview)
                EditorGUIUtility.SetIconSize(new Vector2(64, 64));

            switch (eventType)
            {
                case EventType.DragExited:
                    if (GUI.enabled)
                        HandleUtility.Repaint();

                    break;
                case EventType.DragUpdated:
                case EventType.DragPerform:

                    if (dropRect.Contains(Event.current.mousePosition) && GUI.enabled)
                    {
                        Object[] references = DragAndDrop.objectReferences;
                        Object validatedObject = validator(references, objType, property, ObjectFieldValidatorOptions.None);

                        if (validatedObject != null)
                        {
                            // If scene objects are not allowed and object is a scene object then clear
                            if (!allowSceneObjects && !EditorUtility.IsPersistent(validatedObject))
                                validatedObject = null;
                        }

                        if (validatedObject != null)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                            if (eventType == EventType.DragPerform)
                            {
                                if (property != null)
                                    property.objectReferenceValue = validatedObject;
                                else
                                    obj = validatedObject;

                                GUI.changed = true;
                                DragAndDrop.AcceptDrag();
                                DragAndDrop.activeControlID = 0;
                            }
                            else
                            {
                                DragAndDrop.activeControlID = id;
                            }
                            Event.current.Use();
                        }
                    }
                    break;
                case EventType.MouseDown:
                    // Ignore right clicks
                    if (Event.current.button != 0)
                        break;
                    if (position.Contains(Event.current.mousePosition))
                    {
                        // Get button rect for Object Selector
                        Rect buttonRect;
                        switch (visualType)
                        {
                            case ObjectFieldVisualType.IconAndText:
                            case ObjectFieldVisualType.MiniPreview:
                                buttonRect = new Rect(position.xMax - 15, position.y, 15, position.height);
                                break;
                            case ObjectFieldVisualType.LargePreview:
                                buttonRect = new Rect(position.xMax - 36, position.yMax - 14, 36, 14);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        EditorGUIUtility.editingTextField = false;

                        if (buttonRect.Contains(Event.current.mousePosition))
                        {
                            if (GUI.enabled)
                            {
                                GUIUtility.keyboardControl = id;
                                ObjectSelector.get.Show(obj, objType, property, allowSceneObjects);
                                ObjectSelector.get.objectSelectorID = id;

                                evt.Use();
                                GUIUtility.ExitGUI();
                            }
                        }
                        else
                        {
                            Object actualTargetObject = property != null ? property.objectReferenceValue : obj;
                            Component com = actualTargetObject as Component;
                            if (com)
                                actualTargetObject = com.gameObject;
                            if (showMixedValue)
                                actualTargetObject = null;

                            // One click shows where the referenced object is, or pops up a preview
                            if (Event.current.clickCount == 1)
                            {
                                GUIUtility.keyboardControl = id;

                                PingObjectOrShowPreviewOnClick(actualTargetObject, position);
                                evt.Use();
                            }
                            // Double click opens the asset in external app or changes selection to referenced object
                            else if (Event.current.clickCount == 2)
                            {
                                if (actualTargetObject)
                                {
                                    AssetDatabase.OpenAsset(actualTargetObject);
                                    GUIUtility.ExitGUI();
                                }
                                evt.Use();
                            }
                        }
                    }
                    break;
                case EventType.ExecuteCommand:
                    string commandName = evt.commandName;
                    if (commandName == "ObjectSelectorUpdated" && ObjectSelector.get.objectSelectorID == id && GUIUtility.keyboardControl == id && (property == null || !property.isScript))
                        return AssignSelectedObject(property, validator, objType, evt);
                    else if (commandName == "ObjectSelectorClosed" && ObjectSelector.get.objectSelectorID == id && GUIUtility.keyboardControl == id && property != null && property.isScript)
                    {
                        if (ObjectSelector.get.GetInstanceID() == 0)
                        {
                            // User canceled object selection; don't apply
                            evt.Use();
                            break;
                        }
                        return AssignSelectedObject(property, validator, objType, evt);
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl == id)
                    {
                        if (evt.keyCode == KeyCode.Backspace || evt.keyCode == KeyCode.Delete)
                        {
                            if (property != null)
                                property.objectReferenceValue = null;
                            else
                                obj = null;

                            GUI.changed = true;
                            evt.Use();
                        }

                        // Apparently we have to check for the character being space instead of the keyCode,
                        // otherwise the Inspector will maximize upon pressing space.
                        if (evt.MainActionKeyForControl(id))
                        {
                            ObjectSelector.get.Show(obj, objType, property, allowSceneObjects);
                            ObjectSelector.get.objectSelectorID = id;
                            evt.Use();
                            GUIUtility.ExitGUI();
                        }
                    }
                    break;
                case EventType.Repaint:
                    GUIContent temp;
                    if (showMixedValue)
                    {
                        temp = s_MixedValueContent;
                    }
                    else if (property != null)
                    {
                        temp = EditorGUIUtility.TempContent(property.objectReferenceStringValue, AssetPreview.GetMiniThumbnail(property.objectReferenceValue));
                        obj = property.objectReferenceValue;
                        if (obj != null)
                        {
                            Object[] references = { obj };
                            if (EditorSceneManager.preventCrossSceneReferences && CheckForCrossSceneReferencing(obj, property.serializedObject.targetObject))
                            {
                                if (!EditorApplication.isPlaying)
                                    temp = EditorGUIUtility.TempContent("Scene mismatch (cross scene references not supported)");
                                else
                                    temp.text = temp.text + string.Format(" ({0})", GetGameObjectFromObject(obj).scene.name);
                            }
                            else if (validator(references, objType, property, ObjectFieldValidatorOptions.ExactObjectTypeValidation) == null)
                                temp = EditorGUIUtility.TempContent("Type mismatch");
                        }
                    }
                    else
                    {
                        temp = EditorGUIUtility.ObjectContent(obj, objType);
                    }

                    switch (visualType)
                    {
                        case ObjectFieldVisualType.IconAndText:
                            BeginHandleMixedValueContentColor();
                            style.Draw(position, temp, id, DragAndDrop.activeControlID == id);
                            EndHandleMixedValueContentColor();
                            break;
                        case ObjectFieldVisualType.LargePreview:
                            DrawObjectFieldLargeThumb(position, id, obj, temp);
                            break;
                        case ObjectFieldVisualType.MiniPreview:
                            DrawObjectFieldMiniThumb(position, id, obj, temp);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
            }

            EditorGUIUtility.SetIconSize(oldIconSize);

            return obj;
        }

        private static void DrawObjectFieldLargeThumb(Rect position, int id, Object obj, GUIContent content)
        {
            GUIStyle thumbStyle = EditorStyles.objectFieldThumb;
            thumbStyle.Draw(position, GUIContent.none, id, DragAndDrop.activeControlID == id);

            if (obj != null && !showMixedValue)
            {
                bool isCubemap = obj is Cubemap;
                bool isSprite = obj is Sprite;
                Rect thumbRect = thumbStyle.padding.Remove(position);

                if (isCubemap || isSprite)
                {
                    Texture2D t2d = AssetPreview.GetAssetPreview(obj);
                    if (t2d != null)
                    {
                        if (isSprite || t2d.alphaIsTransparency)
                            DrawTextureTransparent(thumbRect, t2d);
                        else
                            DrawPreviewTexture(thumbRect, t2d);
                    }
                    else
                    {
                        // Preview not loaded -> Draw icon
                        thumbRect.x += (thumbRect.width - content.image.width) / 2f;
                        thumbRect.y += (thumbRect.height - content.image.width) / 2f;
                        GUIStyle.none.Draw(thumbRect, content.image, false, false, false, false);

                        // Keep repaint until the object field has a proper preview
                        HandleUtility.Repaint();
                    }
                }
                else
                {
                    // Draw texture
                    Texture2D t2d = content.image as Texture2D;
                    if (t2d != null && t2d.alphaIsTransparency)
                        DrawTextureTransparent(thumbRect, t2d);
                    else
                        DrawPreviewTexture(thumbRect, content.image);
                }
            }
            else
            {
                GUIStyle s2 = thumbStyle.name + "Overlay";
                BeginHandleMixedValueContentColor();

                s2.Draw(position, content, id);
                EndHandleMixedValueContentColor();
            }
            GUIStyle s3 = thumbStyle.name + "Overlay2";
            s3.Draw(position, EditorGUIUtility.TempContent("Select"), id);
        }

        private static void DrawObjectFieldMiniThumb(Rect position, int id, Object obj, GUIContent content)
        {
            GUIStyle thumbStyle = EditorStyles.objectFieldMiniThumb;
            position.width = EditorGUI.kObjectFieldMiniThumbnailWidth;
            BeginHandleMixedValueContentColor();
            bool hover = obj != null; // we use hover texture for enhancing the border if we have a reference
            bool on =  DragAndDrop.activeControlID == id;
            bool keyFocus = GUIUtility.keyboardControl == id;
            thumbStyle.Draw(position, hover, false, on, keyFocus);
            EndHandleMixedValueContentColor();

            if (obj != null && !showMixedValue)
            {
                Rect thumbRect = new Rect(position.x + 1, position.y + 1, position.height - 2, position.height - 2); // subtract 1 px border
                Texture2D t2d = content.image as Texture2D;
                if (t2d != null && t2d.alphaIsTransparency)
                    DrawTextureTransparent(thumbRect, t2d);
                else
                    DrawPreviewTexture(thumbRect, content.image);

                // Tooltip
                if (thumbRect.Contains(Event.current.mousePosition))
                    GUI.Label(thumbRect, GUIContent.Temp(string.Empty, "Ctrl + Click to show preview"));
            }
        }

        internal static Object DoDropField(Rect position, int id, System.Type objType, ObjectFieldValidator validator, bool allowSceneObjects, GUIStyle style)
        {
            if (validator == null)
                validator = ValidateObjectFieldAssignment;
            Event evt = Event.current;
            EventType eventType = evt.type;

            // special case test, so we continue to ping/select objects with the object field disabled
            if (!GUI.enabled && GUIClip.enabled && (Event.current.rawType == EventType.MouseDown))
                eventType = Event.current.rawType;

            switch (eventType)
            {
                case EventType.DragExited:
                    if (GUI.enabled)
                        HandleUtility.Repaint();
                    break;
                case EventType.DragUpdated:
                case EventType.DragPerform:

                    if (position.Contains(Event.current.mousePosition) && GUI.enabled)
                    {
                        Object[] references = DragAndDrop.objectReferences;
                        Object validatedObject = validator(references, objType, null, ObjectFieldValidatorOptions.None);

                        if (validatedObject != null)
                        {
                            // If scene objects are not allowed and object is a scene object then clear
                            if (!allowSceneObjects && !EditorUtility.IsPersistent(validatedObject))
                                validatedObject = null;
                        }

                        if (validatedObject != null)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                            if (eventType == EventType.DragPerform)
                            {
                                GUI.changed = true;
                                DragAndDrop.AcceptDrag();
                                DragAndDrop.activeControlID = 0;
                                Event.current.Use();
                                return validatedObject;
                            }
                            else
                            {
                                DragAndDrop.activeControlID = id;
                                Event.current.Use();
                            }
                        }
                    }
                    break;
                case EventType.Repaint:
                    style.Draw(position, GUIContent.none, id, DragAndDrop.activeControlID == id);
                    break;
            }
            return null;
        }
    }

    internal class ObjectPreviewPopup : PopupWindowContent
    {
        readonly Editor m_Editor;
        readonly GUIContent m_ObjectName;
        const float kToolbarHeight = 17f;

        internal class Styles
        {
            public readonly GUIStyle toolbar = "preToolbar";
            public readonly GUIStyle toolbarText = "preToolbar2";
            public GUIStyle background = "preBackground";
        }
        Styles s_Styles;

        public ObjectPreviewPopup(Object previewObject)
        {
            if (previewObject == null)
            {
                Debug.LogError("ObjectPreviewPopup: Check object is not null, before trying to show it!");
                return;
            }
            m_ObjectName = new GUIContent(previewObject.name, AssetDatabase.GetAssetPath(previewObject));   // Show path as tooltip on label
            m_Editor = Editor.CreateEditor(previewObject);
        }

        public override void OnClose()
        {
            if (m_Editor != null)
                Editor.DestroyImmediate(m_Editor);
        }

        public override void OnGUI(Rect rect)
        {
            if (m_Editor == null)
            {
                editorWindow.Close();
                return;
            }

            if (s_Styles == null)
                s_Styles = new Styles();


            // Toolbar
            GUILayout.BeginArea(new Rect(rect.x, rect.y, rect.width, kToolbarHeight), s_Styles.toolbar);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            m_Editor.OnPreviewSettings();
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();

            const float kMaxSettingsWidth = 140f;
            GUI.Label(new Rect(rect.x + 5f, rect.y, rect.width - kMaxSettingsWidth, kToolbarHeight), m_ObjectName, s_Styles.toolbarText);

            // Object preview
            Rect previewRect = new Rect(rect.x, rect.y + kToolbarHeight, rect.width, rect.height - kToolbarHeight);
            m_Editor.OnPreviewGUI(previewRect, s_Styles.background);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300f, 300f + kToolbarHeight);
        }
    }
}
