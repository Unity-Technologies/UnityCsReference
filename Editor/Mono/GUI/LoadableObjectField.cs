// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEditor.StyleSheets;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    public sealed partial class EditorGUI
    {
        static bool loadableIsProSkin = false;

        static Texture2D s_LoadableStripesTexture;
        static Texture2D LoadableStripesTexture
        {
            get
            {
                CheckSelectedTheme();
                if (s_LoadableStripesTexture == null)
                {
                    if (loadableIsProSkin)
                        s_LoadableStripesTexture = LoadableStripesTextureWrapMode("Builtin Skins/DarkSkin/Images/LoadableReferenceStripes_Small_d.png");
                    else
                        s_LoadableStripesTexture = LoadableStripesTextureWrapMode("Builtin Skins/LightSkin/Images/LoadableReferenceStripes_Small_l.png");
                }
                return s_LoadableStripesTexture;
            }
        }

        static Texture2D s_LoadableStripesTextureFocus;
        static Texture2D LoadableStripesTextureFocus
        {
            get
            {
                CheckSelectedTheme();
                if (s_LoadableStripesTextureFocus == null)
                {
                    if (loadableIsProSkin)
                        s_LoadableStripesTextureFocus = LoadableStripesTextureWrapMode("Builtin Skins/DarkSkin/Images/LoadableReferenceStripes_Small_Focus_d.png");
                    else
                        s_LoadableStripesTextureFocus = LoadableStripesTextureWrapMode("Builtin Skins/LightSkin/Images/LoadableReferenceStripes_Small_Focus_l.png");
                }
                return s_LoadableStripesTextureFocus;
            }
        }

        static void CheckSelectedTheme()
        {
            if (loadableIsProSkin != EditorGUIUtility.isProSkin)
            {
                s_LoadableStripesTexture = null;
                s_LoadableStripesTextureFocus = null;
                loadableIsProSkin = EditorGUIUtility.isProSkin;
            }
        }

        static Texture2D LoadableStripesTextureWrapMode(string path)
        {
            var texture = EditorGUIUtility.Load(path) as Texture2D;
            if (texture != null)
                texture.wrapMode = TextureWrapMode.Repeat;
            return texture;
        }

        static Rect DrawLoadableStripeBackground(Rect position, bool isDragging)
        {
            var stripeRect = new Rect(
                position.x + 1f,
                position.y + 2f,
                position.width - 3f,
                position.height - 4f);

            Texture2D tex = null;
            if (isDragging)
                tex = LoadableStripesTextureFocus;
            else
                tex = LoadableStripesTexture;

            if (tex != null)
            {
                var texCoords = new Rect(0, 0, stripeRect.width / tex.width, stripeRect.height / tex.height);
                GUI.DrawTextureWithTexCoords(stripeRect, tex, texCoords, true);
            }
            return stripeRect;
        }

        /// <summary>
        /// Draws an ObjectField with a striped background for Loadable references.
        /// Event handling and behaviour are shared with ObjectField via a custom repaint delegate.
        /// </summary>
        [VisibleToOtherModules]
        internal static Object DoLoadableObjectField(Rect position, Rect dropRect, int id, Object obj, Object objBeingEdited, Type objType, SerializedProperty property, ObjectFieldValidator validator, bool excludeSceneAssets)
        {
            if (validator == null)
                validator = ValidateObjectFieldAssignment;

            // Show property context menu on right-click (ObjectField is called with property: null so it doesn't show one).
            if (Event.current.type == EventType.ContextClick && position.Contains(Event.current.mousePosition))
            {
                var contextMenu = new GenericMenu();
                if (property != null && FillPropertyContextMenu(property, null, contextMenu) != null)
                    contextMenu.AddSeparator("");
                contextMenu.AddItem(GUIContent.Temp(L10n.Tr("Properties...")), false, () => PropertyEditor.OpenPropertyEditor(obj));
                var mousePos = Event.current.mousePosition;
                contextMenu.DropDown(new Rect(mousePos.x, mousePos.y, 0, 0));
                Event.current.Use();
                return obj;
            }

            // Do not pass property: LoadableReference is not a PPtr (reading objectReferenceEntityIdValue fails). Caller assigns return value via property.loadableReferenceValue.
            return DoObjectField(position, dropRect, id, obj, objBeingEdited, objType, null, validator, false, DrawLoadableObjectFieldIconAndText, excludeSceneAssets);
        }

        static void DrawLoadableObjectFieldIconAndText(Rect position, int id, Object obj, GUIContent content, ObjectFieldVisualType visualType, GUIStyle objectFieldStyle, GUIStyle buttonStyle)
        {
            bool isHovering = position.Contains(Event.current.mousePosition);
            bool isDragging = DragAndDrop.activeControlID == id;
            bool hasFocus = GUIUtility.keyboardControl == id;

            objectFieldStyle.Draw(position, GUIContent.none, id, DragAndDrop.activeControlID == id, isHovering || isDragging);

            // GUIStyle.Draw above uses GUIContent.none so the Highlighter won't see the actual text.
            // Register content text explicitly so Highlighter.Highlight can find this control.
            Highlighter.Handle(position, content.text);

            Rect stripeRect = DrawLoadableStripeBackground(position, isDragging);
            Rect contentRect = new Rect(position.x, stripeRect.y, position.width, stripeRect.height);
            Rect buttonRectFull = GetButtonRect(visualType, position);

            var contentColor = GUI.contentColor;
            var oldAlpha = contentColor.a;
            try
            {
                if (obj == null)
                {
                    contentColor.a = k_NullObjectReferenceOpacity;
                    GUI.contentColor = contentColor;
                }

                BeginHandleMixedValueContentColor();

                var icon = (obj == null || content.image == null) ? null : content.image;
                var contentForDraw = new GUIContent(content.text, icon, content.tooltip);

                objectFieldStyle.Internal_DrawContent(contentRect, contentForDraw, isHovering, false, isDragging, hasFocus,
                    false, false, Vector2.zero, Vector2.zero, Color.clear, Color.clear, GUI.contentColor,
                    0, 0, 0, 0, false, false);

                string objectTooltip = LoadableObjectIdEditorUtility.GetLoadableObjectIdTooltip();
                if (Event.current.type == EventType.Repaint && isHovering)
                    GUIStyle.SetMouseTooltip(objectTooltip, position);

                Rect buttonRect = buttonStyle.margin.Remove(buttonRectFull);
                buttonStyle.Draw(buttonRect, GUIContent.none, id, DragAndDrop.activeControlID == id, buttonRect.Contains(Event.current.mousePosition));

                EndHandleMixedValueContentColor();
            }
            finally
            {
                contentColor.a = oldAlpha;
                GUI.contentColor = contentColor;
            }
        }
    }
}
