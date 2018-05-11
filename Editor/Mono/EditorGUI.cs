// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEditorInternal;
using UnityEditor.Scripting.ScriptCompilation;
using Object = UnityEngine.Object;
using Event = UnityEngine.Event;
using UnityEditor.Build;

namespace UnityEditor
{
    // Class for editor-only GUI. This class contains general purpose 2D additions to UnityGUI.
    // These work pretty much like the normal GUI functions - and also have matching implementations in [[EditorGUILayout]]
    public sealed partial class EditorGUI
    {
        private static RecycledTextEditor activeEditor;

        internal static DelayedTextEditor s_DelayedTextEditor = new DelayedTextEditor();

        internal static RecycledTextEditor s_RecycledEditor = new RecycledTextEditor();
        internal static string s_OriginalText = "";
        internal static string s_RecycledCurrentEditingString;
        internal static double s_RecycledCurrentEditingFloat;
        internal static long s_RecycledCurrentEditingInt;
        private static bool bKeyEventActive = false;

        internal static bool s_DragToPosition = true;
        internal static bool s_Dragged = false;
        internal static bool s_PostPoneMove = false;
        internal static bool s_SelectAllOnMouseUp = true;

        private const double kFoldoutExpandTimeout = 0.7;
        private static double s_FoldoutDestTime;
        private static int s_DragUpdatedOverID = 0;

        private static int s_FoldoutHash = "Foldout".GetHashCode();
        private static int s_TagFieldHash = "s_TagFieldHash".GetHashCode();
        private static int s_PPtrHash = "s_PPtrHash".GetHashCode();
        private static int s_ObjectFieldHash = "s_ObjectFieldHash".GetHashCode();
        private static int s_ToggleHash = "s_ToggleHash".GetHashCode();
        private static int s_ColorHash = "s_ColorHash".GetHashCode();
        private static int s_CurveHash = "s_CurveHash".GetHashCode();
        private static int s_LayerMaskField = "s_LayerMaskField".GetHashCode();
        private static int s_MaskField = "s_MaskField".GetHashCode();
        private static int s_EnumFlagsField = "s_EnumFlagsField".GetHashCode();
        private static int s_GenericField = "s_GenericField".GetHashCode();
        private static int s_PopupHash = "EditorPopup".GetHashCode();
        private static int s_KeyEventFieldHash = "KeyEventField".GetHashCode();
        private static int s_TextFieldHash = "EditorTextField".GetHashCode();
        private static int s_SearchFieldHash = "EditorSearchField".GetHashCode();
        private static int s_TextAreaHash = "EditorTextField".GetHashCode();
        private static int s_PasswordFieldHash = "PasswordField".GetHashCode();
        private static int s_FloatFieldHash = "EditorTextField".GetHashCode();
        private static int s_DelayedTextFieldHash = "DelayedEditorTextField".GetHashCode();
        private static int s_ArraySizeFieldHash = "ArraySizeField".GetHashCode();
        private static int s_SliderHash = "EditorSlider".GetHashCode();
        private static int s_SliderKnobHash = "EditorSliderKnob".GetHashCode();
        private static int s_MinMaxSliderHash = "EditorMinMaxSlider".GetHashCode();
        private static int s_TitlebarHash = "GenericTitlebar".GetHashCode();
        private static int s_ProgressBarHash = "s_ProgressBarHash".GetHashCode();
        private static int s_SelectableLabelHash = "s_SelectableLabel".GetHashCode();
        private static int s_SortingLayerFieldHash = "s_SortingLayerFieldHash".GetHashCode();
        private static int s_TextFieldDropDownHash = "s_TextFieldDropDown".GetHashCode();

        private static int s_DragCandidateState = 0;
        private static float kDragDeadzone = 16;
        private static Vector2 s_DragStartPos;
        private static double s_DragStartValue = 0;
        private static long s_DragStartIntValue = 0;
        private static double s_DragSensitivity = 0;
        private const float kDragSensitivity = .03f;
        internal const float kMiniLabelW = 13;
        internal const float kLabelW = 80;
        internal const float kSpacing = 5;
        internal const float kSpacingSubLabel = 2;
        internal const float kSliderMinW = 60;
        internal const float kSliderMaxW = 100;
        internal const float kSingleLineHeight = 16;
        internal const float kStructHeaderLineHeight = 16;
        internal const float kObjectFieldThumbnailHeight = 64;
        internal const float kObjectFieldMiniThumbnailHeight = 18f;
        internal const float kObjectFieldMiniThumbnailWidth = 32f;
        internal static string kFloatFieldFormatString = "g7";
        internal static string kDoubleFieldFormatString = "g15";
        internal static string kIntFieldFormatString = "#######0";
        internal static int ms_IndentLevel = 0;
        private const float kIndentPerLevel = 15;
        internal const int kControlVerticalSpacing = 2;
        internal const int kVerticalSpacingMultiField = 0;
        internal static string s_UnitString = "";
        internal const int kInspTitlebarIconWidth = 16;
        internal const int kWindowToolbarHeight = 17;
        private static string kEnabledPropertyName = "m_Enabled";
        private const string k_MultiEditValueString = "<multi>";

        private static float[] s_Vector2Floats = {0, 0};
        private static int[] s_Vector2Ints = { 0, 0 };
        private static GUIContent[] s_XYLabels = {EditorGUIUtility.TextContent("X"), EditorGUIUtility.TextContent("Y")};

        private static float[] s_Vector3Floats = {0, 0, 0};
        private static int[] s_Vector3Ints = { 0, 0, 0 };
        private static GUIContent[] s_XYZLabels = {EditorGUIUtility.TextContent("X"), EditorGUIUtility.TextContent("Y"), EditorGUIUtility.TextContent("Z")};

        private static float[] s_Vector4Floats = {0, 0, 0, 0};
        private static GUIContent[] s_XYZWLabels = {EditorGUIUtility.TextContent("X"), EditorGUIUtility.TextContent("Y"), EditorGUIUtility.TextContent("Z"), EditorGUIUtility.TextContent("W")};

        private static GUIContent[] s_WHLabels = {EditorGUIUtility.TextContent("W"), EditorGUIUtility.TextContent("H")};

        private static GUIContent s_CenterLabel = EditorGUIUtility.TextContent("Center");
        private static GUIContent s_ExtentLabel = EditorGUIUtility.TextContent("Extent");
        private static GUIContent s_PositionLabel = EditorGUIUtility.TextContent("Position");
        private static GUIContent s_SizeLabel = EditorGUIUtility.TextContent("Size");

        internal static readonly GUIContent s_ClipingPlanesLabel = EditorGUIUtility.TextContent("Clipping Planes|Distances from the camera to start and stop rendering.");
        internal static readonly GUIContent[] s_NearAndFarLabels = { EditorGUIUtility.TextContent("Near|The closest point relative to the camera that drawing will occur."), EditorGUIUtility.TextContent("Far|The furthest point relative to the camera that drawing will occur.\n") };
        internal const float kNearFarLabelsWidth = 35f;

        private static int s_ColorPickID;

        private static int s_CurveID;
        internal static Color kCurveColor = Color.green;
        internal static Color kCurveBGColor = new Color(0.337f, 0.337f, 0.337f, 1f);
        internal static EditorGUIUtility.SkinnedColor kSplitLineSkinnedColor = new EditorGUIUtility.SkinnedColor(new Color(0.6f, 0.6f, 0.6f, 1.333f), new Color(0.12f, 0.12f, 0.12f, 1.333f));

        private const int kInspTitlebarToggleWidth = 16;
        private const int kInspTitlebarSpacing = 2;
        private static GUIContent s_PropertyFieldTempContent = new GUIContent();
        private static GUIContent s_IconDropDown;
        private static Material s_IconTextureInactive;

        private static bool s_HasPrefixLabel;
        private static GUIContent s_PrefixLabel = new GUIContent((string)null);
        private static Rect s_PrefixTotalRect;
        private static Rect s_PrefixRect;
        private static GUIStyle s_PrefixStyle;
        private static Color s_PrefixGUIColor;

        // Makes the following controls give the appearance of editing multiple different values.
        public static bool showMixedValue
        {
            get { return s_ShowMixedValue; }
            set { s_ShowMixedValue = value; }
        }

        private static bool s_ShowMixedValue;

        private static GUIContent s_MixedValueContent = EditorGUIUtility.TextContent("\u2014|Mixed Values");

        internal static GUIContent mixedValueContent
        {
            get { return s_MixedValueContent; }
        }

        private static Color s_MixedValueContentColor = new Color(1, 1, 1, 0.5f);
        private static Color s_MixedValueContentColorTemp = Color.white;

        internal static void BeginHandleMixedValueContentColor()
        {
            s_MixedValueContentColorTemp = GUI.contentColor;
            GUI.contentColor = showMixedValue ? (GUI.contentColor * s_MixedValueContentColor) : GUI.contentColor;
        }

        internal static void EndHandleMixedValueContentColor()
        {
            GUI.contentColor = s_MixedValueContentColorTemp;
        }

        [RequiredByNativeCode]
        internal static bool IsEditingTextField()
        {
            return RecycledTextEditor.s_ActuallyEditing && activeEditor != null;
        }

        internal static void EndEditingActiveTextField()
        {
            if (activeEditor != null)
            {
                activeEditor.EndEditing();
            }
        }

        public static void FocusTextInControl(string name)
        {
            GUI.FocusControl(name);
            EditorGUIUtility.editingTextField = true;
        }

        // STACKS

        internal static void ClearStacks()
        {
            s_EnabledStack.Clear();
            s_ChangedStack.Clear();
            s_PropertyStack.Clear();
            ScriptAttributeUtility.s_DrawerStack.Clear();
        }

        private static Stack<PropertyGUIData> s_PropertyStack = new Stack<PropertyGUIData>();

        private static Stack<bool> s_EnabledStack = new Stack<bool>();

        // @TODO: API soon to be deprecated but still in a grace period; documentation states that users
        //        are encouraged to use EditorGUI.DisabledScope instead. Uncomment next line when appropriate.
        // [System.Obsolete("Use DisabledScope instead", false)]
        public class DisabledGroupScope : GUI.Scope
        {
            public DisabledGroupScope(bool disabled)
            {
                BeginDisabledGroup(disabled);
            }

            protected override void CloseScope()
            {
                EndDisabledGroup();
            }
        }

        // Create a group of controls that can be disabled.
        // @TODO: API soon to be deprecated but still in a grace period; documentation states that users
        //        are encouraged to use EditorGUI.DisabledScope instead. Uncomment next line when appropriate.
        // [System.Obsolete("Use DisabledScope instead", false)]
        public static void BeginDisabledGroup(bool disabled)
        {
            BeginDisabled(disabled);
        }

        // Ends a disabled group started with BeginDisabledGroup.
        // @TODO: API soon to be deprecated but still in a grace period; documentation states that users
        //        are encouraged to use EditorGUI.DisabledScope instead. Uncomment next line when appropriate.
        // [System.Obsolete("Use DisabledScope instead", false)]
        public static void EndDisabledGroup()
        {
            EndDisabled();
        }

        public struct DisabledScope : IDisposable
        {
            bool m_Disposed;

            public DisabledScope(bool disabled)
            {
                m_Disposed = false;

                BeginDisabled(disabled);
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                if (!GUIUtility.guiIsExiting)
                    EndDisabled();
            }
        }

        // Create a group of controls that can be disabled.
        internal static void BeginDisabled(bool disabled)
        {
            s_EnabledStack.Push(GUI.enabled);
            GUI.enabled &= !disabled;
        }

        // Ends a disabled group started with BeginDisabled.
        internal static void EndDisabled()
        {
            // Stack might have been cleared with ClearStack(), check before pop.
            if (s_EnabledStack.Count > 0)
                GUI.enabled = s_EnabledStack.Pop();
        }

        private static Stack<bool> s_ChangedStack = new Stack<bool>();

        public class ChangeCheckScope : GUI.Scope
        {
            bool m_ChangeChecked;
            bool m_Changed;
            public bool changed
            {
                get
                {
                    if (!m_ChangeChecked)
                    {
                        m_ChangeChecked = true;
                        m_Changed = EndChangeCheck();
                    }
                    return m_Changed;
                }
            }

            public ChangeCheckScope()
            {
                BeginChangeCheck();
            }

            protected override void CloseScope()
            {
                if (!m_ChangeChecked)
                    EndChangeCheck();
            }
        }

        // Check if any control was changed inside a block of code.
        public static void BeginChangeCheck()
        {
            s_ChangedStack.Push(GUI.changed);
            GUI.changed = false;
        }

        // Ends a change check started with BeginChangeCheck ().
        // Note: BeginChangeCheck/EndChangeCheck supports nesting
        // For ex.,
        //   BeginChangeCheck()
        //     BeginChangeCheck()
        //      <GUI control changes>
        //     EndChangeCheck() <-- will return true
        //   EndChangeCheck() <-- will return true
        public static bool EndChangeCheck()
        {
            bool changed = GUI.changed;
            GUI.changed |= s_ChangedStack.Pop();
            return changed;
        }

        internal partial class RecycledTextEditor : TextEditor
        {
            internal static bool s_ActuallyEditing = false; // internal so we can save this state.
            internal static bool s_AllowContextCutOrPaste = true; // e.g. selectable labels only allow for copying

            IMECompositionMode m_IMECompositionModeBackup;

            // *undocumented*
            internal bool IsEditingControl(int id)
            {
                return GUIUtility.keyboardControl == id && controlID == id && s_ActuallyEditing && GUIView.current.hasFocus;
            }

            // *undocumented*
            public virtual void BeginEditing(int id, string newText, Rect position, GUIStyle style, bool multiline, bool passwordField)
            {
                if (IsEditingControl(id))
                {
                    return;
                }
                if (EditorGUI.activeEditor != null)
                {
                    EditorGUI.activeEditor.EndEditing();
                }

                EditorGUI.activeEditor = this;
                controlID = id;
                text = s_OriginalText = newText;
                this.multiline = multiline;
                this.style = style;
                this.position = position;
                isPasswordField = passwordField;
                s_ActuallyEditing = true;
                scrollOffset = Vector2.zero;
                UnityEditor.Undo.IncrementCurrentGroup();

                m_IMECompositionModeBackup = Input.imeCompositionMode;
                Input.imeCompositionMode = IMECompositionMode.On;
            }

            // *undocumented*
            public virtual void EndEditing()
            {
                if (EditorGUI.activeEditor == this)
                {
                    EditorGUI.activeEditor = null;
                }

                controlID = 0;
                s_ActuallyEditing = false;
                s_AllowContextCutOrPaste = true;
                UnityEditor.Undo.IncrementCurrentGroup();

                Input.imeCompositionMode = m_IMECompositionModeBackup;
            }
        }

        // There can be two way something can get focus
        internal sealed partial class DelayedTextEditor : RecycledTextEditor
        {
            private int controlThatHadFocus = 0, messageControl = 0;
            internal string controlThatHadFocusValue = "";
            private GUIView viewThatHadFocus;
            private bool m_CommitCommandSentOnLostFocus;
            private const string CommitCommand = "DelayedControlShouldCommit";

            private bool m_IgnoreBeginGUI = false;

            //*undocumented*
            public void BeginGUI()
            {
                if (m_IgnoreBeginGUI)
                {
                    return;
                }
                if (GUIUtility.keyboardControl == controlID)
                {
                    controlThatHadFocus = GUIUtility.keyboardControl;
                    controlThatHadFocusValue = text;
                    viewThatHadFocus = GUIView.current;
                }
                else
                {
                    controlThatHadFocus = 0;
                }
            }

            //*undocumented*
            public void EndGUI(EventType type)
            {
                int sendID = 0;
                if (controlThatHadFocus != 0 && controlThatHadFocus != GUIUtility.keyboardControl)
                {
                    sendID = controlThatHadFocus;
                    controlThatHadFocus = 0;
                }

                if (sendID != 0 && !m_CommitCommandSentOnLostFocus)
                {
                    messageControl = sendID;
                    //              Debug.Log ("" + messageControl + " lost focus to " + GUIUtility.keyboardControl+ " in " + type+". Sending Message. value:" + controlThatHadFocusValue);
                    m_IgnoreBeginGUI = true;

                    // Explicitly set the keyboardControl for the view that had focus in preparation for the following SendEvent,
                    // but only if the current view is the view that had focus.
                    // This is necessary as GUIView::OnInputEvent (native) will load the old keyboardControl for nested OnGUI calls.
                    if (GUIView.current == viewThatHadFocus)
                        viewThatHadFocus.SetKeyboardControl(GUIUtility.keyboardControl);

                    viewThatHadFocus.SendEvent(EditorGUIUtility.CommandEvent(CommitCommand));
                    m_IgnoreBeginGUI = false;
                    //              Debug.Log ("Afterwards: " + GUIUtility.keyboardControl);
                    messageControl = 0;
                }
            }

            //*undocumented*
            public override void EndEditing()
            {
                //The following block handles the case where a different window is focus while editing delayed text box
                if (Event.current == null)
                {
                    // We set this flag because of a bug that was trigger when you switched focus to another window really fast
                    // right after focusing on the text box. For some reason keyboardControl was changed and the commit message
                    // was being sent twice which caused layout issues.
                    m_CommitCommandSentOnLostFocus = true;
                    m_IgnoreBeginGUI = true;
                    messageControl = controlID;
                    var temp = GUIUtility.keyboardControl;
                    viewThatHadFocus.SetKeyboardControl(0);

                    viewThatHadFocus.SendEvent(EditorGUIUtility.CommandEvent(CommitCommand));
                    m_IgnoreBeginGUI = false;
                    viewThatHadFocus.SetKeyboardControl(temp);
                    messageControl = 0;
                }

                base.EndEditing();
            }

            //*undocumented*
            public string OnGUI(int id, string value, out bool changed)
            {
                Event evt = Event.current;
                if (evt.type == EventType.ExecuteCommand && evt.commandName == CommitCommand && id == messageControl)
                {
                    m_CommitCommandSentOnLostFocus = false;
                    // Only set changed to true if the value has actually changed. Otherwise EditorGUI.EndChangeCheck will report false positives,
                    // which could for example cause unwanted undo's to be registered (in the case of e.g. editing terrain resolution, this can cause several seconds of delay)
                    if (!showMixedValue || controlThatHadFocusValue != k_MultiEditValueString)
                        changed = value != controlThatHadFocusValue;
                    else
                        changed = false;
                    evt.Use();
                    messageControl = 0;
                    return controlThatHadFocusValue;
                }
                changed = false;

                return value;
            }
        }

        internal sealed partial class PopupMenuEvent
        {
            public string commandName;
            public GUIView receiver;

            public PopupMenuEvent(string cmd, GUIView v)
            {
                commandName = cmd;
                receiver = v;
            }

            public void SendEvent()
            {
                if (receiver)
                {
                    receiver.SendEvent(EditorGUIUtility.CommandEvent(commandName));
                }
                else
                {
                    Debug.LogError("BUG: We don't have a receiver set up, please report");
                }
            }
        }

        private static void ShowTextEditorPopupMenu()
        {
            GenericMenu pm = new GenericMenu();
            if (s_RecycledEditor.hasSelection && !s_RecycledEditor.isPasswordField)
            {
                if (RecycledTextEditor.s_AllowContextCutOrPaste)
                {
                    pm.AddItem(EditorGUIUtility.TextContent("Cut"), false, new PopupMenuEvent("Cut", GUIView.current).SendEvent);
                }
                pm.AddItem(EditorGUIUtility.TextContent("Copy"), false, new PopupMenuEvent("Copy", GUIView.current).SendEvent);
            }
            else
            {
                if (RecycledTextEditor.s_AllowContextCutOrPaste)
                {
                    pm.AddDisabledItem(EditorGUIUtility.TextContent("Cut"));
                }
                pm.AddDisabledItem(EditorGUIUtility.TextContent("Copy"));
            }

            if (s_RecycledEditor.CanPaste() && RecycledTextEditor.s_AllowContextCutOrPaste)
            {
                pm.AddItem(EditorGUIUtility.TextContent("Paste"), false, new PopupMenuEvent("Paste", GUIView.current).SendEvent);
            }
            else
            {
                // pm.AddDisabledItem (EditorGUIUtility.TextContent ("Paste"));
            }

            pm.ShowAsContext();
        }

        // Is the platform-dependent "action" modifier key held down? (RO)
        public static bool actionKey
        {
            get
            {
                if (Event.current == null)
                {
                    return false;
                }
                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    return Event.current.command;
                }
                else
                {
                    return Event.current.control;
                }
            }
        }

        internal static void BeginCollectTooltips()
        {
            EditorGUI.isCollectingTooltips = true;
        }

        internal static void EndCollectTooltips()
        {
            EditorGUI.isCollectingTooltips = false;
        }

        /// *listonly*
        public static void DropShadowLabel(Rect position, string text)
        {
            DoDropShadowLabel(position, EditorGUIUtility.TempContent(text), "PreOverlayLabel", .6f);
        }

        /// *listonly*
        public static void DropShadowLabel(Rect position, GUIContent content)
        {
            DoDropShadowLabel(position, content, "PreOverlayLabel", .6f);
        }

        /// *listonly*
        public static void DropShadowLabel(Rect position, string text, GUIStyle style)
        {
            DoDropShadowLabel(position, EditorGUIUtility.TempContent(text), style, .6f);
        }

        // Draws a label with a drop shadow.
        public static void DropShadowLabel(Rect position, GUIContent content, GUIStyle style)
        {
            DoDropShadowLabel(position, content, style, .6f);
        }

        internal static void DoDropShadowLabel(Rect position, GUIContent content, GUIStyle style, float shadowOpa)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            DrawLabelShadow(position, content, style, shadowOpa);
            style.Draw(position, content, false, false, false, false);
        }

        internal static void DrawLabelShadow(Rect position, GUIContent content, GUIStyle style, float shadowOpa)
        {
            Color temp = GUI.color, temp2 = GUI.contentColor, temp3 = GUI.backgroundColor;

            // Draw only background
            GUI.contentColor = new Color(0, 0, 0, 0);
            style.Draw(position, content, false, false, false, false);

            // Blur foreground
            position.y += 1;
            GUI.backgroundColor = new Color(0, 0, 0, 0);
            GUI.contentColor = temp2;
            Draw4(position, content, 1, GUI.color.a * shadowOpa, style);
            Draw4(position, content, 2, GUI.color.a * shadowOpa * .42f, style);

            // Draw final foreground
            GUI.color = temp;
            GUI.backgroundColor = temp3;
        }

        private static void Draw4(Rect position, GUIContent content, float offset, float alpha, GUIStyle style)
        {
            GUI.color = new Color(0, 0, 0, alpha);
            position.y -= offset;
            style.Draw(position, content, false, false, false, false);
            position.y += offset * 2;
            style.Draw(position, content, false, false, false, false);
            position.y -= offset;
            position.x -= offset;
            style.Draw(position, content, false, false, false, false);
            position.x += offset * 2;
            style.Draw(position, content, false, false, false, false);
        }

        // Should we select all text from the current field when the mouse goes up?
        // (We need to keep track of this to support both SwipeSelection & initial click selects all)
        internal static string DoTextField(RecycledTextEditor editor, int id, Rect position, string text, GUIStyle style, string allowedletters, out bool changed, bool reset, bool multiline, bool passwordField)
        {
            Event evt = Event.current;

            // If the text field represents multiple values, the text should always start out being empty when editing it.
            // This empty text will not be saved when simply clicking in the text field, or tabbing to it,
            // since GUI.changed is only set to true if the user alters the string.
            // Nevertheless, we also backup and return the original value if nothing changed.
            // It's just nice that output is the same as input when nothing changed,
            // even if the output should really be ignored when GUI.changed is false.
            string origText = text;

            // We assume the text is actually valid, but we do not want to change the returned value if nothing was changed
            // So we should only check for null string on the internal text and not affect the origText which will be returned if nothing changed
            if (text == null)
            {
                text = string.Empty;
            }

            if (showMixedValue)
            {
                text = k_MultiEditValueString;
            }

            // If we have keyboard control and our window have focus, we need to sync up the editor.
            if (HasKeyboardFocus(id) && Event.current.type != EventType.Layout)
            {
                // If the editor is already set up, we just need to sync position, etc...
                if (editor.IsEditingControl(id))
                {
                    editor.position = position;
                    editor.style = style;
                    editor.controlID = id;
                    editor.multiline = multiline;
                    editor.isPasswordField = passwordField;
                    editor.DetectFocusChange();
                }
                else if (EditorGUIUtility.editingTextField || (evt.GetTypeForControl(id) == EventType.ExecuteCommand && evt.commandName == "NewKeyboardFocus"))
                {
                    // This one is worse: we are the new keyboardControl, but didn't know about it.
                    // this means a Tab operation or setting focus from code.
                    editor.BeginEditing(id, text, position, style, multiline, passwordField);
                    // If cursor is invisible, it's a selectable label, and we don't want to select all automatically
                    if (GUI.skin.settings.cursorColor.a > 0)
                        editor.SelectAll();

                    if (evt.GetTypeForControl(id) == EventType.ExecuteCommand)
                    {
                        evt.Use();
                    }
                }
            }

            // Inform editor that someone removed focus from us.
            if (editor.controlID == id && GUIUtility.keyboardControl != id)
            {
                editor.EndEditing();
            }

            bool mayHaveChanged = false;
            string textBeforeKey = editor.text;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.ValidateCommand:
                    if (GUIUtility.keyboardControl == id)
                    {
                        switch (evt.commandName)
                        {
                            case "Cut":
                            case "Copy":
                                if (editor.hasSelection)
                                {
                                    evt.Use();
                                }
                                break;
                            case "Paste":
                                if (editor.CanPaste())
                                {
                                    evt.Use();
                                }
                                break;
                            case "SelectAll":
                            case "Delete":
                                evt.Use();
                                break;
                            case "UndoRedoPerformed":
                                editor.text = text;
                                evt.Use();
                                break;
                        }
                    }

                    break;
                case EventType.ExecuteCommand:
                    if (GUIUtility.keyboardControl == id)
                    {
                        switch (evt.commandName)
                        {
                            case "OnLostFocus":
                                if (activeEditor != null)
                                {
                                    activeEditor.EndEditing();
                                }
                                evt.Use();
                                break;
                            case "Cut":
                                editor.BeginEditing(id, text, position, style, multiline, passwordField);
                                editor.Cut();
                                mayHaveChanged = true;
                                break;
                            case "Copy":
                                editor.Copy();
                                evt.Use();
                                break;
                            case "Paste":
                                editor.BeginEditing(id, text, position, style, multiline, passwordField);
                                editor.Paste();
                                mayHaveChanged = true;
                                break;
                            case "SelectAll":
                                editor.SelectAll();
                                evt.Use();
                                break;
                            case "Delete":
                                // This "Delete" command stems from a Shift-Delete in the text editor.
                                // On Windows, Shift-Delete in text does a cut whereas on Mac, it does a delete.
                                editor.BeginEditing(id, text, position, style, multiline, passwordField);
                                if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
                                {
                                    editor.Delete();
                                }
                                else
                                {
                                    editor.Cut();
                                }
                                mayHaveChanged = true;
                                evt.Use();
                                break;
                        }
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        if (s_Dragged && s_DragToPosition)
                        {
                            //GUIUtility.keyboardControl = id;
                            //editor.BeginEditing (id, text, position, style, multiline, passwordField);
                            editor.MoveSelectionToAltCursor();
                            mayHaveChanged = true;
                        }
                        else if (s_PostPoneMove)
                        {
                            editor.MoveCursorToPosition(Event.current.mousePosition);
                        }
                        else if (s_SelectAllOnMouseUp)
                        {
                            // If cursor is invisible, it's a selectable label, and we don't want to select all automatically
                            if (GUI.skin.settings.cursorColor.a > 0)
                            {
                                editor.SelectAll();
                            }
                            s_SelectAllOnMouseUp = false;
                        }
                        editor.MouseDragSelectsWholeWords(false);
                        s_DragToPosition = true;
                        s_Dragged = false;
                        s_PostPoneMove = false;
                        if (evt.button == 0)
                        {
                            GUIUtility.hotControl = 0;
                            evt.Use();
                        }
                    }
                    break;
                case EventType.MouseDown:
                    if (position.Contains(evt.mousePosition) && evt.button == 0)
                    {
                        // Does this text field already have focus?
                        if (editor.IsEditingControl(id))
                        { // if so, process the event normally
                            if (Event.current.clickCount == 2 && GUI.skin.settings.doubleClickSelectsWord)
                            {
                                editor.MoveCursorToPosition(Event.current.mousePosition);
                                editor.SelectCurrentWord();
                                editor.MouseDragSelectsWholeWords(true);
                                editor.DblClickSnap(TextEditor.DblClickSnapping.WORDS);
                                s_DragToPosition = false;
                            }
                            else if (Event.current.clickCount == 3 && GUI.skin.settings.tripleClickSelectsLine)
                            {
                                editor.MoveCursorToPosition(Event.current.mousePosition);
                                editor.SelectCurrentParagraph();
                                editor.MouseDragSelectsWholeWords(true);
                                editor.DblClickSnap(TextEditor.DblClickSnapping.PARAGRAPHS);
                                s_DragToPosition = false;
                            }
                            else
                            {
                                // if mouse is over the selection, postpone cursor movement till Mouse Up - this is so we can do correct text dragging.
                                //                      if (editor.hasSelection && editor.IsOverSelection(Event.current.mousePosition))
                                //                          s_PostPoneMove = true;
                                //                      else
                                editor.MoveCursorToPosition(Event.current.mousePosition);
                                s_SelectAllOnMouseUp = false;
                            }
                        }
                        else
                        { // Otherwise, mark this as initial click and begin editing
                            GUIUtility.keyboardControl = id;
                            editor.BeginEditing(id, text, position, style, multiline, passwordField);
                            editor.MoveCursorToPosition(Event.current.mousePosition);
                            // If cursor is invisible, it's a selectable label, and we don't want to select all automatically
                            if (GUI.skin.settings.cursorColor.a > 0)
                            {
                                s_SelectAllOnMouseUp = true;
                            }
                        }

                        GUIUtility.hotControl = id;
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        if (!evt.shift && editor.hasSelection && s_DragToPosition)
                        {
                            editor.MoveAltCursorToPosition(Event.current.mousePosition);
                        }
                        else
                        {
                            if (evt.shift)
                            {
                                editor.MoveCursorToPosition(Event.current.mousePosition);
                            }
                            else
                            {
                                editor.SelectToPosition(Event.current.mousePosition);
                            }

                            s_DragToPosition = false;
                            s_SelectAllOnMouseUp = !editor.hasSelection;
                        }
                        s_Dragged = true;
                        evt.Use();
                    }
                    break;
                case EventType.ContextClick:
                    if (position.Contains(evt.mousePosition))
                    {
                        if (!editor.IsEditingControl(id))
                        { // First click: focus before showing popup
                            GUIUtility.keyboardControl = id;
                            editor.BeginEditing(id, text, position, style, multiline, passwordField);
                            editor.MoveCursorToPosition(Event.current.mousePosition);
                        }
                        ShowTextEditorPopupMenu();
                        Event.current.Use();
                    }

                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl == id)
                    {
                        char c = evt.character;

                        // Let the editor handle all cursor keys, etc...
                        if (editor.IsEditingControl(id) && editor.HandleKeyEvent(evt))
                        {
                            evt.Use();
                            mayHaveChanged = true;
                            break;
                        }

                        if (evt.keyCode == KeyCode.Escape)
                        {
                            if (editor.IsEditingControl(id))
                            {
                                if (style == EditorStyles.toolbarSearchField || style == EditorStyles.searchField)
                                {
                                    s_OriginalText = "";
                                }

                                editor.text = s_OriginalText;

                                editor.EndEditing();
                                mayHaveChanged = true;
                            }
                        }
                        else if (c == '\n' || (int)c == 3)
                        {
                            if (!editor.IsEditingControl(id))
                            {
                                editor.BeginEditing(id, text, position, style, multiline, passwordField);
                                editor.SelectAll();
                            }
                            else
                            {
                                if (!multiline || (evt.alt || evt.shift || evt.control))
                                {
                                    editor.EndEditing();
                                }
                                else
                                {
                                    editor.Insert(c);
                                    mayHaveChanged = true;
                                    break;
                                }
                            }
                            evt.Use();
                        }
                        else if (c == '\t' || evt.keyCode == KeyCode.Tab)
                        {
                            // Only insert tabs if multiline
                            if (multiline && editor.IsEditingControl(id))
                            {
                                bool validTabCharacter = (allowedletters == null || allowedletters.IndexOf(c) != -1);
                                bool validTabEvent = !(evt.alt || evt.shift || evt.control) && c == '\t';
                                if (validTabEvent && validTabCharacter)
                                {
                                    editor.Insert(c);
                                    mayHaveChanged = true;
                                }
                            }
                        }
                        else if ((int)c == 25 || (int)c == 27)
                        {
                            // Note, OS X send characters for the following keys that we need to eat:
                            // ASCII 25: "End Of Medium" on pressing shift tab
                            // ASCII 27: "Escape" on pressing ESC
                        }
                        else if (editor.IsEditingControl(id))
                        {
                            bool validCharacter = (allowedletters == null || allowedletters.IndexOf(c) != -1) && c != 0;
                            if (validCharacter)
                            {
                                editor.Insert(c);
                                mayHaveChanged = true;
                            }
                            else
                            {
                                // If the composition string is not empty, then it's likely that even though we didn't add a printable
                                // character to the string, we should refresh the GUI, to update the composition string.
                                if (Input.compositionString != "")
                                {
                                    editor.ReplaceSelection("");
                                    mayHaveChanged = true;
                                }
                                evt.Use();
                            }
                        }
                    }

                    break;
                case EventType.Repaint:
                    string drawText;
                    if (editor.IsEditingControl(id))
                    {
                        if (showMixedValue && editor.text == k_MultiEditValueString)
                            drawText = string.Empty;
                        else
                            drawText = passwordField ? "".PadRight(editor.text.Length, '*') : editor.text;
                    }
                    else if (showMixedValue)
                    {
                        drawText = s_MixedValueContent.text;
                    }
                    else
                    {
                        drawText = passwordField ? "".PadRight(text.Length, '*') : text;
                    }


                    if (!string.IsNullOrEmpty(s_UnitString) && !passwordField)
                        drawText += " " + s_UnitString;

                    // Only change mousecursor if hotcontrol is not grabbed
                    if (GUIUtility.hotControl == 0)
                    {
                        EditorGUIUtility.AddCursorRect(position, MouseCursor.Text);
                    }

                    if (!editor.IsEditingControl(id))
                    {
                        BeginHandleMixedValueContentColor();
                        style.Draw(position, EditorGUIUtility.TempContent(drawText), id, false);
                        EndHandleMixedValueContentColor();
                    }
                    else
                    {
                        editor.DrawCursor(drawText);
                    }
                    break;
            }

            if (GUIUtility.keyboardControl == id)
            {
                // TODO: remove the need for this with Optimized GUI blocks
                EditorGUIUtility.textFieldInput = EditorGUIUtility.editingTextField;
            }

            // Scroll offset might need to be updated
            editor.UpdateScrollOffsetIfNeeded(evt);

            changed = false;
            if (mayHaveChanged)
            {
                // If some action happened that could change the text AND
                // the text actually changed, then set changed to true.
                // Don't just compare the text only, since it also changes when changing text field.
                // Don't leave out comparing the text though, since it will result in false positives.
                changed = (textBeforeKey != editor.text);
                evt.Use();
            }
            if (changed)
            {
                GUI.changed = true;
                return editor.text;
            }

            RecycledTextEditor.s_AllowContextCutOrPaste = true;

            return origText;
        }

        // KEYEVENTFIELD HERE ===============================================================
        internal static Event KeyEventField(Rect position, Event evt)
        {
            return DoKeyEventField(position, evt, GUI.skin.textField);
        }

        internal static Event DoKeyEventField(Rect position, Event _event, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_KeyEventFieldHash, FocusType.Passive, position);
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    // If the mouse is inside the button, we say that we're the hot control
                    if (position.Contains(evt.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        evt.Use();
                        if (bKeyEventActive)
                        // cancel
                        {
                            bKeyEventActive = false;
                        }
                        else
                        {
                            bKeyEventActive = true;
                        }
                    }
                    return _event;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = id;

                        // If we got the mousedown, the mouseup is ours as well
                        // (no matter if the click was in the button or not)
                        evt.Use();
                    }
                    return _event;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        evt.Use();
                    }
                    break;
                case EventType.Repaint:
                    if (bKeyEventActive)
                    {
                        GUIContent content = EditorGUIUtility.TempContent("[Please press a key]");
                        style.Draw(position, content, id);
                    }
                    else
                    {
                        string str = InternalEditorUtility.TextifyEvent(_event);
                        style.Draw(position, EditorGUIUtility.TempContent(str), id);
                    }
                    break;
                case EventType.KeyDown:
                    if ((GUIUtility.hotControl == id) && bKeyEventActive)
                    {
                        // ignore presses of just modifier keys
                        if (evt.character == '\0')
                        {
                            if (evt.alt && (evt.keyCode == KeyCode.AltGr || evt.keyCode == KeyCode.LeftAlt || evt.keyCode == KeyCode.RightAlt) ||
                                evt.control && (evt.keyCode == KeyCode.LeftControl || evt.keyCode == KeyCode.RightControl) ||
                                evt.command && (evt.keyCode == KeyCode.LeftApple || evt.keyCode == KeyCode.RightApple || evt.keyCode == KeyCode.LeftWindows || evt.keyCode == KeyCode.RightWindows) ||
                                evt.shift && (evt.keyCode == KeyCode.LeftShift || evt.keyCode == KeyCode.RightShift || (int)evt.keyCode == 0))
                            {
                                return _event;
                            }
                        }
                        bKeyEventActive = false;
                        GUI.changed = true;
                        GUIUtility.hotControl = 0;
                        Event e = new Event(evt);
                        evt.Use();
                        return e;
                    }
                    break;
            }
            return _event;
        }

        internal static Rect GetInspectorTitleBarObjectFoldoutRenderRect(Rect rect)
        {
            return new Rect(rect.x + 3f, rect.y + 3f, 16f, 16f);
        }

        static bool IsValidForContextMenu(Object target)
        {
            // if the reference is *really* null, don't allow showing the context menu
            if ((object)target == null)
                return false;

            // UnityEngine.Object overrides == null, which means we might be dealing with an invalid object
            bool isUnityNull = target == null;

            // if MonoBehaviour/Scriptable compares to null, then we are dealing with a missing script
            // for which we still want to display context menu
            if (isUnityNull && (target is MonoBehaviour || target is ScriptableObject))
                return true;

            return !isUnityNull;
        }

        internal static bool DoObjectMouseInteraction(bool foldout, Rect interactionRect, Object[] targetObjs, int id)
        {
            // Always enabled, regardless of editor enabled state
            var enabled = GUI.enabled;
            GUI.enabled = true;

            // Context menu
            Event evt = Event.current;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (interactionRect.Contains(evt.mousePosition))
                    {
                        if (evt.button == 1 && IsValidForContextMenu(targetObjs[0]))
                        {
                            EditorUtility.DisplayObjectContextMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0), targetObjs, 0);
                            evt.Use();
                        }
                        else if (evt.button == 0 && !(Application.platform == RuntimePlatform.OSXEditor && evt.control == true))
                        {
                            GUIUtility.hotControl = id;
                            GUIUtility.keyboardControl = id;
                            DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), id);
                            delay.mouseDownPosition = evt.mousePosition;
                            evt.Use();
                        }
                    }
                    break;

                case EventType.ContextClick:
                    if (interactionRect.Contains(evt.mousePosition) && IsValidForContextMenu(targetObjs[0]))
                    {
                        EditorUtility.DisplayObjectContextMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0), targetObjs, 0);
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        if (interactionRect.Contains(evt.mousePosition))
                        {
                            GUI.changed = true;
                            foldout = !foldout;
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), id);
                        if (delay.CanStartDrag())
                        {
                            GUIUtility.hotControl = 0;
                            DragAndDrop.PrepareStartDrag();
                            DragAndDrop.objectReferences = targetObjs;
                            if (targetObjs.Length > 1)
                            {
                                DragAndDrop.StartDrag("<Multiple>");
                            }
                            else
                            {
                                DragAndDrop.StartDrag(ObjectNames.GetDragAndDropTitle(targetObjs[0]));
                            }
                        }
                        evt.Use();
                    }
                    break;

                case EventType.DragUpdated:
                    if (s_DragUpdatedOverID == id)
                    {
                        if (interactionRect.Contains(evt.mousePosition))
                        {
                            if (Time.realtimeSinceStartup > s_FoldoutDestTime)
                            {
                                foldout = true;
                                HandleUtility.Repaint();
                            }
                        }
                        else
                        {
                            s_DragUpdatedOverID = 0;
                        }
                    }
                    else
                    {
                        if (interactionRect.Contains(evt.mousePosition))
                        {
                            s_DragUpdatedOverID = id;
                            s_FoldoutDestTime = Time.realtimeSinceStartup + kFoldoutExpandTimeout;
                        }
                    }

                    if (interactionRect.Contains(evt.mousePosition))
                    {
                        DragAndDrop.visualMode = InternalEditorUtility.InspectorWindowDrag(targetObjs, false);
                        Event.current.Use();
                    }
                    break;

                case EventType.DragPerform:
                    if (interactionRect.Contains(evt.mousePosition))
                    {
                        DragAndDrop.visualMode = InternalEditorUtility.InspectorWindowDrag(targetObjs, true);
                        DragAndDrop.AcceptDrag();
                        Event.current.Use();
                    }
                    break;

                case EventType.KeyDown:
                    if (EditorGUIUtility.keyboardControl == id)
                    {
                        if (evt.keyCode == KeyCode.LeftArrow)
                        {
                            foldout = false;
                            evt.Use();
                        }
                        if (evt.keyCode == KeyCode.RightArrow)
                        {
                            foldout = true;
                            evt.Use();
                        }
                    }
                    break;
            }

            // Restore enabled state for the editors.
            GUI.enabled = enabled;

            return foldout;
        }

        // This is assumed to be called from the inpector only
        private static void DoObjectFoldoutInternal(bool foldout, Rect interactionRect, Rect renderRect, Object[] targetObjs, int id)
        {
            // Always enabled, regardless of editor enabled state
            var enabled = GUI.enabled;
            GUI.enabled = true;

            // Context menu
            Event evt = Event.current;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Repaint:
                    bool isPressed = GUIUtility.hotControl == id;
                    EditorStyles.foldout.Draw(renderRect, isPressed, isPressed, foldout, false);
                    break;
            }

            // Restore enabled state for the editors.
            GUI.enabled = enabled;
        }

        internal static bool DoObjectFoldout(bool foldout, Rect interactionRect, Rect renderRect, Object[] targetObjs, int id)
        {
            foldout = DoObjectMouseInteraction(foldout, interactionRect, targetObjs, id);
            DoObjectFoldoutInternal(foldout, interactionRect, renderRect, targetObjs, id);
            return foldout;
        }

        // Make a label field. (Useful for showing read-only info.)
        internal static void LabelFieldInternal(Rect position, GUIContent label, GUIContent label2, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_FloatFieldHash, FocusType.Passive, position);
            position = PrefixLabel(position, id, label);
            if (Event.current.type == EventType.Repaint)
            {
                style.Draw(position, label2, id);
            }
        }

        /// *listonly*
        public static bool Toggle(Rect position, bool value)
        {
            int id = GUIUtility.GetControlID(s_ToggleHash, FocusType.Keyboard, position);
            return EditorGUIInternal.DoToggleForward(IndentedRect(position), id, value, GUIContent.none, EditorStyles.toggle);
        }

        /// *listonly*
        public static bool Toggle(Rect position, string label, bool value)
        {
            return Toggle(position, EditorGUIUtility.TempContent(label), value);
        }

        /// *listonly*
        public static bool Toggle(Rect position, bool value, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_ToggleHash, FocusType.Keyboard, position);
            return EditorGUIInternal.DoToggleForward(position, id, value, GUIContent.none, style);
        }

        /// *listonly*
        public static bool Toggle(Rect position, string label, bool value, GUIStyle style)
        {
            return Toggle(position, EditorGUIUtility.TempContent(label), value, style);
        }

        /// *listonly*
        public static bool Toggle(Rect position, GUIContent label, bool value)
        {
            int id = GUIUtility.GetControlID(s_ToggleHash, FocusType.Keyboard, position);
            return EditorGUIInternal.DoToggleForward(PrefixLabel(position, id, label), id, value, GUIContent.none, EditorStyles.toggle);
        }

        // Make a toggle.
        public static bool Toggle(Rect position, GUIContent label, bool value, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_ToggleHash, FocusType.Keyboard, position);
            return EditorGUIInternal.DoToggleForward(PrefixLabel(position, id, label), id, value, GUIContent.none, style);
        }

        // Make a toggle with the label on the right.
        internal static bool ToggleLeftInternal(Rect position, GUIContent label, bool value, GUIStyle labelStyle)
        {
            int id = GUIUtility.GetControlID(s_ToggleHash, FocusType.Keyboard, position);
            Rect toggleRect = IndentedRect(position);
            Rect labelRect = IndentedRect(position);
            labelRect.xMin += EditorStyles.toggle.padding.left;
            HandlePrefixLabel(position, labelRect, label, id, labelStyle);
            return EditorGUIInternal.DoToggleForward(toggleRect, id, value, GUIContent.none, EditorStyles.toggle);
        }

        internal static bool DoToggle(Rect position, int id, bool value, GUIContent content, GUIStyle style)
        {
            return EditorGUIInternal.DoToggleForward(position, id, value, content, style);
        }

        internal static string TextFieldInternal(int id, Rect position, string text, GUIStyle style)
        {
            bool dummy;
            text = DoTextField(s_RecycledEditor, id, IndentedRect(position), text, style, null, out dummy, false, false, false);
            return text;
        }

        /// *listonly*
        internal static string TextFieldInternal(Rect position, string text, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_TextFieldHash, FocusType.Keyboard, position);
            bool dummy;
            text = DoTextField(s_RecycledEditor, id, IndentedRect(position), text, style, null, out dummy, false, false, false);
            return text;
        }

        // Make a text field.
        internal static string TextFieldInternal(Rect position, GUIContent label, string text, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_TextFieldHash, FocusType.Keyboard, position);
            bool dummy;
            text = DoTextField(s_RecycledEditor, id, PrefixLabel(position, id, label), text, style, null, out dummy, false, false, false);
            return text;
        }

        internal static string ToolbarSearchField(int id, Rect position, string text, bool showWithPopupArrow)
        {
            bool dummy;
            Rect textRect = position;
            const float k_CancelButtonWidth = 14f;
            textRect.width -= k_CancelButtonWidth;

            text = DoTextField(s_RecycledEditor, id, textRect, text, showWithPopupArrow ? EditorStyles.toolbarSearchFieldPopup : EditorStyles.toolbarSearchField, null, out dummy, false, false, false);

            Rect buttonRect = position;
            buttonRect.x += position.width - k_CancelButtonWidth;
            buttonRect.width = k_CancelButtonWidth;
            if (GUI.Button(buttonRect, GUIContent.none, text != "" ? EditorStyles.toolbarSearchFieldCancelButton : EditorStyles.toolbarSearchFieldCancelButtonEmpty) && text != "")
            {
                s_RecycledEditor.text = text = "";
                GUIUtility.keyboardControl = 0;
            }

            return text;
        }

        internal static string ToolbarSearchField(Rect position, string[] searchModes, ref int searchMode, string text)
        {
            int id = GUIUtility.GetControlID(s_SearchFieldHash, FocusType.Keyboard, position);
            return ToolbarSearchField(id, position, searchModes, ref searchMode, text);
        }

        internal static string ToolbarSearchField(int id, Rect position, string[] searchModes, ref int searchMode, string text)
        {
            bool hasPopup = searchModes != null;
            if (hasPopup)
            {
                searchMode = PopupCallbackInfo.GetSelectedValueForControl(id, searchMode);

                Rect popupPosition = position;
                popupPosition.width = 20;

                if (Event.current.type == EventType.MouseDown && popupPosition.Contains(Event.current.mousePosition))
                {
                    PopupCallbackInfo.instance = new PopupCallbackInfo(id);
                    EditorUtility.DisplayCustomMenu(position, EditorGUIUtility.TempContent(searchModes), searchMode, PopupCallbackInfo.instance.SetEnumValueDelegate, null);

                    if (s_RecycledEditor.IsEditingControl(id))
                    {
                        Event.current.Use();
                    }
                }
            }

            text = ToolbarSearchField(id, position, text, hasPopup);

            if (hasPopup && text == "" && !s_RecycledEditor.IsEditingControl(id) && Event.current.type == EventType.Repaint)
            {
                const float k_CancelButtonWidth = 14f;
                position.width -= k_CancelButtonWidth;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorStyles.toolbarSearchFieldPopup.Draw(position, EditorGUIUtility.TempContent(searchModes[searchMode]), id, false);
                }
            }

            return text;
        }

        internal static string SearchField(Rect position, string text)
        {
            int id = GUIUtility.GetControlID(s_SearchFieldHash, FocusType.Keyboard, position);
            bool dummy;
            Rect textRect = position;
            textRect.width -= 15;
            text = DoTextField(s_RecycledEditor, id, textRect, text, EditorStyles.searchField, null, out dummy, false, false, false);
            Rect buttonRect = position;
            buttonRect.x += position.width - 15;
            buttonRect.width = 15;
            if (GUI.Button(buttonRect, GUIContent.none, text != "" ? EditorStyles.searchFieldCancelButton : EditorStyles.searchFieldCancelButtonEmpty) && text != "")
            {
                s_RecycledEditor.text = text = "";
                GUIUtility.keyboardControl = 0;
            }
            return text;
        }

        internal static string ScrollableTextAreaInternal(Rect position, string text, ref Vector2 scrollPosition, GUIStyle style)
        {
            if (Event.current.type == EventType.Layout)
                return text;

            int id = EditorGUIUtility.GetControlID(s_TextAreaHash, FocusType.Keyboard, position);

            position = IndentedRect(position);
            float fullTextHeight = style.CalcHeight(GUIContent.Temp(text), position.width);
            Rect viewRect = new Rect(0, 0, position.width, fullTextHeight);

            Vector2 oldStyleScrollValue = style.contentOffset;


            if (position.height < viewRect.height)
            {
                //Scroll bar position
                Rect scrollbarPosition = position;
                scrollbarPosition.width = GUI.skin.verticalScrollbar.fixedWidth;
                scrollbarPosition.height -= 2;
                scrollbarPosition.y += 1;
                scrollbarPosition.x = position.x + position.width - scrollbarPosition.width;

                position.width -= scrollbarPosition.width;

                //textEditor width changed, recalculate Text and viewRect areas.
                fullTextHeight = style.CalcHeight(GUIContent.Temp(text), position.width);
                viewRect = new Rect(0, 0, position.width, fullTextHeight);

                if (position.Contains(Event.current.mousePosition) && Event.current.type == EventType.ScrollWheel)
                {
                    const float mouseWheelMultiplier = 10f;
                    float desiredY = scrollPosition.y + Event.current.delta.y * mouseWheelMultiplier;
                    scrollPosition.y = Mathf.Clamp(desiredY, 0f, viewRect.height);
                    Event.current.Use();
                }


                scrollPosition.y = GUI.VerticalScrollbar(scrollbarPosition, scrollPosition.y, position.height, 0, viewRect.height);


                if (!s_RecycledEditor.IsEditingControl(id))
                {
                    //When not editing we use the style.draw, so we need to change the offset on the style instead of the RecycledEditor.
                    style.contentOffset -= scrollPosition;
                    style.Internal_clipOffset = scrollPosition;
                }
                else
                {
                    //Move the Editor offset to match our scrollbar
                    s_RecycledEditor.scrollOffset.y = scrollPosition.y;
                }
            }
            bool dummy;
            EventType beforeTextFieldEventType = Event.current.type;
            string newValue = DoTextField(s_RecycledEditor, id, position, text, style, null, out dummy, false, true, false);

            //Only update the our scrollPosition if the user has interacted with the TextArea (the current event was used)
            if (beforeTextFieldEventType != Event.current.type)
            {
                scrollPosition = s_RecycledEditor.scrollOffset;
            }

            style.contentOffset = oldStyleScrollValue;
            style.Internal_clipOffset = Vector2.zero;
            return newValue;
        }

        // Make a text area.
        internal static string TextAreaInternal(Rect position, string text, GUIStyle style)
        {
            int id = EditorGUIUtility.GetControlID(s_TextAreaHash, FocusType.Keyboard, position);
            bool dummy;
            text = DoTextField(s_RecycledEditor, id, IndentedRect(position), text, style, null, out dummy, false, true, false);
            return text;
        }

        // Make a selectable label field. (Useful for showing read-only info that can be copy-pasted.)
        internal static void SelectableLabelInternal(Rect position, string text, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_SelectableLabelHash, FocusType.Keyboard, position);
            Event e = Event.current;
            if (GUIUtility.keyboardControl == id && e.GetTypeForControl(id) == EventType.KeyDown)
            {
                switch (e.keyCode)
                {
                    case KeyCode.LeftArrow:
                    case KeyCode.RightArrow:
                    case KeyCode.UpArrow:
                    case KeyCode.DownArrow:
                    case KeyCode.Home:
                    case KeyCode.End:
                    case KeyCode.PageUp:
                    case KeyCode.PageDown:
                        break;
                    case KeyCode.Space:
                        GUIUtility.hotControl = 0;
                        GUIUtility.keyboardControl = 0;
                        break;
                    default:
                        // Prevent using the event if it is the tab character otherwise tabbing keyfocus will break (case 562373)
                        if (e.character != '\t')
                            e.Use();
                        break;
                }
            }

            if (e.type == EventType.ExecuteCommand && (e.commandName == "Paste" || e.commandName == "Cut") && GUIUtility.keyboardControl == id)
            {
                e.Use();
            }


            Color tempCursorColor = GUI.skin.settings.cursorColor;
            GUI.skin.settings.cursorColor = new Color(0, 0, 0, 0);

            RecycledTextEditor.s_AllowContextCutOrPaste = false;

            bool dummy;
            text = DoTextField(s_RecycledEditor, id, IndentedRect(position), text, style, string.Empty, out dummy, false, true, false);

            GUI.skin.settings.cursorColor = tempCursorColor;
        }

        [Obsolete("Use PasswordField instead.")]
        public static string DoPasswordField(int id, Rect position, string password, GUIStyle style)
        {
            bool guiChanged;
            return DoTextField(s_RecycledEditor, id, position, password, style, null, out guiChanged, false, false, true);
        }

        [Obsolete("Use PasswordField instead.")]
        public static string DoPasswordField(int id, Rect position, GUIContent label, string password, GUIStyle style)
        {
            bool guiChanged;
            return DoTextField(s_RecycledEditor, id, PrefixLabel(position, id, label), password, style, null, out guiChanged, false, false, true);
        }

        /// *listonly*
        internal static string PasswordFieldInternal(Rect position, string password, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_PasswordFieldHash, FocusType.Keyboard, position);
            bool guiChanged;
            return DoTextField(s_RecycledEditor, id, IndentedRect(position), password, style, null, out guiChanged, false, false, true);
        }

        // Make a text field where the user can enter a password.
        internal static string PasswordFieldInternal(Rect position, GUIContent label, string password, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_PasswordFieldHash, FocusType.Keyboard, position);
            bool guiChanged;
            return DoTextField(s_RecycledEditor, id, PrefixLabel(position, id, label), password, style, null, out guiChanged, false, false, true);
        }

        /// *listonly*
        internal static float FloatFieldInternal(Rect position, float value, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_FloatFieldHash, FocusType.Keyboard, position);
            return DoFloatField(s_RecycledEditor, IndentedRect(position), new Rect(0, 0, 0, 0), id, value, kFloatFieldFormatString, style, false);
        }

        // Make a text field for entering floats.
        internal static float FloatFieldInternal(Rect position, GUIContent label, float value, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_FloatFieldHash, FocusType.Keyboard, position);
            Rect position2 = PrefixLabel(position, id, label);
            position.xMax = position2.x;
            return DoFloatField(s_RecycledEditor, position2, position, id, value, kFloatFieldFormatString, style, true);
        }

        /// *listonly*
        internal static double DoubleFieldInternal(Rect position, double value, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_FloatFieldHash, FocusType.Keyboard, position);
            return DoDoubleField(s_RecycledEditor, IndentedRect(position), new Rect(0, 0, 0, 0), id, value, kDoubleFieldFormatString, style, false);
        }

        // Make a text field for entering floats.
        internal static double DoubleFieldInternal(Rect position, GUIContent label, double value, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_FloatFieldHash, FocusType.Keyboard, position);
            Rect position2 = PrefixLabel(position, id, label);
            position.xMax = position2.x;
            return DoDoubleField(s_RecycledEditor, position2, position, id, value, kDoubleFieldFormatString, style, true);
        }

        private static double CalculateFloatDragSensitivity(double value)
        {
            if (Double.IsInfinity(value) || Double.IsNaN(value))
            {
                return 0.0;
            }
            return Math.Max(1, Math.Pow(Math.Abs(value), 0.5)) * kDragSensitivity;
        }

        private static long CalculateIntDragSensitivity(long value)
        {
            return (long)System.Math.Max(1, System.Math.Pow(System.Math.Abs((double)value), 0.5) * kDragSensitivity);
        }

        // Handle dragging of value
        private static void DragNumberValue(RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, bool isDouble, ref double doubleVal, ref long longVal, string formatString, GUIStyle style, double dragSensitivity)
        {
            Event evt = Event.current;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (dragHotZone.Contains(evt.mousePosition) && evt.button == 0)
                    {
                        // When clicking the dragging rect ensure that the number field is not
                        // editing: otherwise we dont see the actual value but the editted temp value
                        EditorGUIUtility.editingTextField = false;

                        GUIUtility.hotControl = id;

                        if (activeEditor != null)
                        {
                            activeEditor.EndEditing();
                        }
                        evt.Use();
                        GUIUtility.keyboardControl = id;

                        s_DragCandidateState = 1;
                        s_DragStartValue = doubleVal;
                        s_DragStartIntValue = longVal;
                        s_DragStartPos = evt.mousePosition;
                        s_DragSensitivity = dragSensitivity;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && s_DragCandidateState != 0)
                    {
                        GUIUtility.hotControl = 0;
                        s_DragCandidateState = 0;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        switch (s_DragCandidateState)
                        {
                            case 1:
                                if ((Event.current.mousePosition - s_DragStartPos).sqrMagnitude > kDragDeadzone)
                                {
                                    s_DragCandidateState = 2;
                                    GUIUtility.keyboardControl = id;
                                }
                                evt.Use();
                                break;
                            case 2:
                                // Don't change the editor.content.text here.
                                // Instead, wait for scripting validation to enforce clamping etc. and then
                                // update the editor.content.text in the repaint event.
                                if (isDouble)
                                {
                                    doubleVal += HandleUtility.niceMouseDelta * s_DragSensitivity;
                                    doubleVal = MathUtils.RoundBasedOnMinimumDifference(doubleVal, s_DragSensitivity);
                                }
                                else
                                {
                                    longVal += (long)Math.Round(HandleUtility.niceMouseDelta * s_DragSensitivity);
                                }
                                GUI.changed = true;

                                evt.Use();
                                break;
                        }
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.hotControl == id && evt.keyCode == KeyCode.Escape && s_DragCandidateState != 0)
                    {
                        doubleVal = s_DragStartValue;
                        longVal = s_DragStartIntValue;
                        GUI.changed = true;
                        //              s_LastEditorControl = -1;
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;
                case EventType.Repaint:
                    EditorGUIUtility.AddCursorRect(dragHotZone, MouseCursor.SlideArrow);
                    break;
            }
        }

        internal static float DoFloatField(RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, float value, string formatString, GUIStyle style, bool draggable)
        {
            return DoFloatField(editor, position, dragHotZone, id, value, formatString, style, draggable, Event.current.GetTypeForControl(id) == EventType.MouseDown ? (float)CalculateFloatDragSensitivity(s_DragStartValue) : 0.0f);
        }

        internal static float DoFloatField(RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, float value, string formatString, GUIStyle style, bool draggable, float dragSensitivity)
        {
            long dummy = 0;
            double doubleValue = value;
            DoNumberField(editor, position, dragHotZone, id, true, ref doubleValue, ref dummy, formatString, style, draggable, dragSensitivity);
            return MathUtils.ClampToFloat(doubleValue);
        }

        internal static int DoIntField(RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, int value, string formatString, GUIStyle style, bool draggable, float dragSensitivity)
        {
            double dummy = 0f;
            long longValue = value;
            DoNumberField(editor, position, dragHotZone, id, false, ref dummy, ref longValue, formatString, style, draggable, dragSensitivity);

            return MathUtils.ClampToInt(longValue);
        }

        internal static double DoDoubleField(RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, double value, string formatString, GUIStyle style, bool draggable)
        {
            return DoDoubleField(editor, position, dragHotZone, id, value, formatString, style, draggable, Event.current.GetTypeForControl(id) == EventType.MouseDown ? CalculateFloatDragSensitivity(s_DragStartValue) : 0.0);
        }

        internal static double DoDoubleField(RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, double value, string formatString, GUIStyle style, bool draggable, double dragSensitivity)
        {
            long dummy = 0;
            DoNumberField(editor, position, dragHotZone, id, true, ref value, ref dummy, formatString, style, draggable, dragSensitivity);
            return value;
        }

        internal static long DoLongField(RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, long value, string formatString, GUIStyle style, bool draggable, double dragSensitivity)
        {
            double dummy = 0f;
            DoNumberField(editor, position, dragHotZone, id, false, ref dummy, ref value, formatString, style, draggable, dragSensitivity);
            return value;
        }

        internal static readonly string s_AllowedCharactersForFloat = "inftynaeINFTYNAE0123456789.,-*/+%^()";
        internal static readonly string s_AllowedCharactersForInt = "0123456789-*/+%^()";


        static bool HasKeyboardFocus(int controlID)
        {
            // Every EditorWindow has its own keyboardControl state so we also need to
            // check if the current OS view has focus to determine if the control has actual key focus (gets the input)
            // and not just being a focused control in an unfocused window.
            return (GUIUtility.keyboardControl == controlID && GUIView.current.hasFocus);
        }

        internal static void DoNumberField(RecycledTextEditor editor, Rect position, Rect dragHotZone, int id, bool isDouble, ref double doubleVal, ref long longVal, string formatString, GUIStyle style, bool draggable, double dragSensitivity)
        {
            bool changed;
            string allowedCharacters = isDouble ? s_AllowedCharactersForFloat : s_AllowedCharactersForInt;
            if (draggable)
            {
                DragNumberValue(editor, position, dragHotZone, id, isDouble, ref doubleVal, ref longVal, formatString, style, dragSensitivity);
            }

            Event evt = Event.current;
            string str;
            if (HasKeyboardFocus(id) || (evt.type == EventType.MouseDown && evt.button == 0 && position.Contains(evt.mousePosition)))
            {
                if (!editor.IsEditingControl(id))
                {
                    str = s_RecycledCurrentEditingString = isDouble ? doubleVal.ToString(formatString) : longVal.ToString(formatString);
                }
                else
                {
                    str = s_RecycledCurrentEditingString;
                    // This piece of code will change the actively edited textfield to readjust whenever the underlying value changes.
                    /*
                                    if (isFloat ? (s_RecycledCurrentEditingFloat == floatVal) : (s_RecycledCurrentEditingInt == intVal)) {
                                        str = s_RecycledCurrentEditingString;
                                    }
                    */
                    if (evt.type == EventType.ValidateCommand && evt.commandName == "UndoRedoPerformed")
                    {
                        str = isDouble ? doubleVal.ToString(formatString) : longVal.ToString(formatString);
                    }
                }
            }
            else
            {
                str = isDouble ? doubleVal.ToString(formatString) : longVal.ToString(formatString);
            }

            if (GUIUtility.keyboardControl == id)
            {
                str = DoTextField(editor, id, position, str, style, allowedCharacters, out changed, false, false, false);

                // If we are still actively editing, return the input values
                if (changed)
                {
                    GUI.changed = true;
                    s_RecycledCurrentEditingString = str;

                    // clean up the text
                    if (isDouble)
                    {
                        string lowered = str.ToLower();
                        if (lowered == "inf" || lowered == "infinity")
                        {
                            doubleVal = Double.PositiveInfinity;
                        }
                        else if (lowered == "-inf" || lowered == "-infinity")
                        {
                            doubleVal = Double.NegativeInfinity;
                        }
                        else
                        {
                            // Make sure that comma & period are interchangable.
                            str = str.Replace(',', '.');

                            if (!double.TryParse(str, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out doubleVal))
                            {
                                doubleVal = s_RecycledCurrentEditingFloat = ExpressionEvaluator.Evaluate<double>(str);
                                return;
                            }

                            // Don't allow user to enter NaN - it opens a can of worms that can trigger many latent bugs,
                            // and is not really useful for anything.
                            if (Double.IsNaN(doubleVal))
                            {
                                doubleVal = 0;
                            }

                            s_RecycledCurrentEditingFloat = doubleVal;
                        }
                    }
                    else
                    {
                        if (!long.TryParse(str, out longVal))
                        {
                            longVal = s_RecycledCurrentEditingInt = ExpressionEvaluator.Evaluate<long>(str);
                            return;
                        }
                        s_RecycledCurrentEditingInt = longVal;
                    }
                }
            }
            else
            {
                str = DoTextField(editor, id, position, str, style, allowedCharacters, out changed, false, false, false);
            }
        }

        internal static int ArraySizeField(Rect position, GUIContent label, int value, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_ArraySizeFieldHash, FocusType.Keyboard, position);

            BeginChangeCheck();
            string str = DelayedTextFieldInternal(position, id, label, value.ToString(kIntFieldFormatString), "0123456789-", style);
            if (EndChangeCheck())
            {
                try
                {
                    value = int.Parse(str, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                }
                catch (System.FormatException)
                {
                }
            }
            return value;
        }

        internal static string DelayedTextFieldInternal(Rect position, string value, string allowedLetters, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_DelayedTextFieldHash, FocusType.Keyboard, position);
            return DelayedTextFieldInternal(position, id, GUIContent.none, value, allowedLetters, style);
        }

        internal static string DelayedTextFieldInternal(Rect position, int id, GUIContent label, string value, string allowedLetters, GUIStyle style)
        {
            // Figure out which string should be shown: If we're currently editing we disregard the incoming value
            string str;
            if (HasKeyboardFocus(id))
            {
                // If we just got focus, set up s_RecycledCurrentEditingString
                if (!s_DelayedTextEditor.IsEditingControl(id))
                {
                    str = s_RecycledCurrentEditingString = value;
                }
                else
                {
                    str = s_RecycledCurrentEditingString;
                }
                Event evt = Event.current;
                if (evt.type == EventType.ValidateCommand && evt.commandName == "UndoRedoPerformed")
                {
                    str = value;
                }
            }
            else
            {
                str = value;
            }

            bool changed;
            bool wasChanged = GUI.changed;
            str = s_DelayedTextEditor.OnGUI(id, str, out changed);
            GUI.changed = false;

            if (!changed)
            {
                str = EditorGUI.DoTextField(s_DelayedTextEditor, id, PrefixLabel(position, id, label), str, style, allowedLetters, out changed, false, false, false);
                GUI.changed = false;
                // If we are still actively editing, return the input values
                if (GUIUtility.keyboardControl == id)
                {
                    if (!s_DelayedTextEditor.IsEditingControl(id))
                    {
                        if (value != str)
                        {
                            GUI.changed = true;
                            value = str;
                        }
                    }
                    else
                    {
                        s_RecycledCurrentEditingString = str;
                    }
                }
            }
            else
            {
                GUI.changed = true;
                value = str;
            }
            GUI.changed |= wasChanged;
            return value;
        }

        internal static void DelayedTextFieldInternal(Rect position, int id, SerializedProperty property, string allowedLetters, GUIContent label)
        {
            label = BeginProperty(position, label, property);

            BeginChangeCheck();
            string newValue = DelayedTextFieldInternal(position, id, label, property.stringValue, allowedLetters, EditorStyles.textField);
            if (EndChangeCheck())
                property.stringValue = newValue;

            EndProperty();
        }

        internal static float DelayedFloatFieldInternal(Rect position, GUIContent label, float value, GUIStyle style)
        {
            float oldVal = value;
            float newVal = oldVal;

            EditorGUI.BeginChangeCheck();
            int id = GUIUtility.GetControlID(s_DelayedTextFieldHash, FocusType.Keyboard, position);
            string floatStr = EditorGUI.DelayedTextFieldInternal(position, id, label, oldVal.ToString(), s_AllowedCharactersForFloat, style);
            if (EditorGUI.EndChangeCheck())
            {
                if (float.TryParse(floatStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out newVal) && newVal != oldVal)
                {
                    value = newVal;
                    GUI.changed = true;
                }
            }
            return newVal;
        }

        internal static void DelayedFloatFieldInternal(Rect position, SerializedProperty property, GUIContent label)
        {
            label = BeginProperty(position, label, property);

            BeginChangeCheck();
            float newValue = DelayedFloatFieldInternal(position, label, property.floatValue, EditorStyles.numberField);
            if (EndChangeCheck())
                property.floatValue = newValue;

            EndProperty();
        }

        internal static double DelayedDoubleFieldInternal(Rect position, GUIContent label, double value, GUIStyle style)
        {
            double oldVal = value;
            double newVal = oldVal;

            if (label != null)
                position = EditorGUI.PrefixLabel(position, label);

            EditorGUI.BeginChangeCheck();
            string doubleStr = EditorGUI.DelayedTextFieldInternal(position, oldVal.ToString(), s_AllowedCharactersForFloat, style);
            if (EditorGUI.EndChangeCheck())
            {
                if (double.TryParse(doubleStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out newVal) && newVal != oldVal)
                {
                    value = newVal;
                    GUI.changed = true;
                }
            }
            return newVal;
        }

        internal static int DelayedIntFieldInternal(Rect position, GUIContent label, int value, GUIStyle style)
        {
            int oldVal = value;
            int newVal = oldVal;

            EditorGUI.BeginChangeCheck();
            int id = GUIUtility.GetControlID(s_DelayedTextFieldHash, FocusType.Keyboard, position);
            string intStr = EditorGUI.DelayedTextFieldInternal(position, id, label, oldVal.ToString(), s_AllowedCharactersForInt, style);
            if (EditorGUI.EndChangeCheck())
            {
                if (int.TryParse(intStr, out newVal))
                {
                    if (newVal != oldVal)
                    {
                        value = newVal;
                        GUI.changed = true;
                    }
                }
                else
                {
                    newVal = ExpressionEvaluator.Evaluate<int>(intStr);
                    if (newVal != oldVal)
                    {
                        value = newVal;
                        GUI.changed = true;
                    }
                }
            }
            return newVal;
        }

        internal static void DelayedIntFieldInternal(Rect position, SerializedProperty property, GUIContent label)
        {
            label = BeginProperty(position, label, property);

            BeginChangeCheck();
            int newValue = DelayedIntFieldInternal(position, label, property.intValue, EditorStyles.numberField);
            if (EndChangeCheck())
                property.intValue = newValue;

            EndProperty();
        }

        /// *listonly*
        internal static int IntFieldInternal(Rect position, int value, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_FloatFieldHash, FocusType.Keyboard, position);
            return DoIntField(s_RecycledEditor, IndentedRect(position), new Rect(0, 0, 0, 0), id, value, kIntFieldFormatString, style, false, CalculateIntDragSensitivity(value));
        }

        // Make a text field for entering integers.
        internal static int IntFieldInternal(Rect position, GUIContent label, int value, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_FloatFieldHash, FocusType.Keyboard, position);
            Rect position2 = PrefixLabel(position, id, label);
            position.xMax = position2.x;
            return DoIntField(s_RecycledEditor, position2, position, id, value, kIntFieldFormatString, style, true, CalculateIntDragSensitivity(value));
        }

        /// *listonly*
        internal static long LongFieldInternal(Rect position, long value, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_FloatFieldHash, FocusType.Keyboard, position);
            return DoLongField(s_RecycledEditor, IndentedRect(position), new Rect(0, 0, 0, 0), id, value, kIntFieldFormatString, style, false, CalculateIntDragSensitivity(value));
        }

        // Make a text field for entering integers.
        internal static long LongFieldInternal(Rect position, GUIContent label, long value, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_FloatFieldHash, FocusType.Keyboard, position);
            Rect position2 = PrefixLabel(position, id, label);
            position.xMax = position2.x;
            return DoLongField(s_RecycledEditor, position2, position, id, value, kIntFieldFormatString, style, true, CalculateIntDragSensitivity(value));
        }

        internal static void SliderWithTexture(Rect position, GUIContent label, SerializedProperty property, float leftValue, float rightValue, float power, GUIStyle sliderStyle, GUIStyle thumbStyle, Texture2D sliderBackground)
        {
            // Do slider control
            label = BeginProperty(position, label, property);

            BeginChangeCheck();

            float newValue = SliderWithTexture(position, label, property.floatValue, leftValue, rightValue, power, sliderStyle, thumbStyle, sliderBackground);

            if (EndChangeCheck())
            {
                property.floatValue = newValue;
            }

            EndProperty();
        }

        internal static float SliderWithTexture(Rect position, GUIContent label, float sliderValue, float leftValue, float rightValue, float power, GUIStyle sliderStyle, GUIStyle thumbStyle, Texture2D sliderBackground)
        {
            int id = GUIUtility.GetControlID(s_SliderHash, FocusType.Keyboard, position);
            Rect controlRect = PrefixLabel(position, id, label);
            Rect dragZone = LabelHasContent(label) ? EditorGUIUtility.DragZoneRect(position) : new Rect(); // Ensure dragzone is empty when we have no label
            return DoSlider(controlRect, dragZone, id, sliderValue, leftValue, rightValue, kFloatFieldFormatString, power, sliderStyle, thumbStyle, sliderBackground);
        }

        /// *listonly*
        public static float Slider(Rect position, float value, float leftValue, float rightValue)
        {
            int id = GUIUtility.GetControlID(s_SliderHash, FocusType.Keyboard, position);
            return DoSlider(IndentedRect(position), EditorGUIUtility.DragZoneRect(position), id, value, leftValue, rightValue, kFloatFieldFormatString);
        }

        /// *listonly*
        public static float Slider(Rect position, string label, float value, float leftValue, float rightValue)
        {
            return Slider(position, EditorGUIUtility.TempContent(label), value, leftValue, rightValue);
        }

        // Make a slider the user can drag to change a value between a min and a max.
        public static float Slider(Rect position, GUIContent label, float value, float leftValue, float rightValue)
        {
            return PowerSlider(position, label, value, leftValue, rightValue, 1.0f);
        }

        /// *listonly*
        internal static float PowerSlider(Rect position, string label, float sliderValue, float leftValue, float rightValue, float power)
        {
            return PowerSlider(position, EditorGUIUtility.TempContent(label), sliderValue, leftValue, rightValue, power);
        }

        // Make a power slider the user can drag to change a value between a min and a max.
        internal static float PowerSlider(Rect position, GUIContent label, float sliderValue, float leftValue, float rightValue, float power)
        {
            int id = GUIUtility.GetControlID(s_SliderHash, FocusType.Keyboard, position);
            Rect controlRect = PrefixLabel(position, id, label);
            Rect dragZone = LabelHasContent(label) ? EditorGUIUtility.DragZoneRect(position) : new Rect(); // Ensure dragzone is empty when we have no label
            return DoSlider(controlRect, dragZone, id, sliderValue, leftValue, rightValue, kFloatFieldFormatString, power);
        }

        private static float PowPreserveSign(float f, float p)
        {
            var result = Mathf.Pow(Mathf.Abs(f), p);
            return f < 0.0f ? -result : result;
        }

        private static void DoPropertyContextMenu(SerializedProperty property)
        {
            GenericMenu pm = new GenericMenu();
            SerializedProperty propertyWithPath = property.serializedObject.FindProperty(property.propertyPath);

            ScriptAttributeUtility.GetHandler(property).AddMenuItems(property, pm);

            // Would be nice to allow to set to value of a specific target for properties with children too,
            // but it's not currently supported.
            if (property.hasMultipleDifferentValues && !property.hasVisibleChildren)
            {
                TargetChoiceHandler.AddSetToValueOfTargetMenuItems(pm, propertyWithPath, TargetChoiceHandler.SetToValueOfTarget);
            }

            if (property.serializedObject.targetObjects.Length == 1 && property.isInstantiatedPrefab)
            {
                pm.AddItem(EditorGUIUtility.TextContent("Revert Value to Prefab"), false, TargetChoiceHandler.SetPrefabOverride, propertyWithPath);
            }

            // If property is an element in an array, show duplicate and delete menu options
            if (property.propertyPath.LastIndexOf(']') == property.propertyPath.Length - 1)
            {
                var parentArrayPropertyPath = property.propertyPath.Substring(0, property.propertyPath.LastIndexOf(".Array.data["));
                var parentArrayProperty = property.serializedObject.FindProperty(parentArrayPropertyPath);

                if (!parentArrayProperty.isFixedBuffer)
                {
                    if (pm.GetItemCount() > 0)
                    {
                        pm.AddSeparator("");
                    }
                    pm.AddItem(EditorGUIUtility.TextContent("Duplicate Array Element"), false, (a) =>
                        {
                            TargetChoiceHandler.DuplicateArrayElement(a);
                            EditorGUIUtility.editingTextField = false;
                        }, propertyWithPath);
                    pm.AddItem(EditorGUIUtility.TextContent("Delete Array Element"), false, (a) =>
                        {
                            TargetChoiceHandler.DeleteArrayElement(a);
                            EditorGUIUtility.editingTextField = false;
                        }, propertyWithPath);
                }
            }

            // If shift is held down, show debug menu options
            if (Event.current.shift)
            {
                if (pm.GetItemCount() > 0)
                {
                    pm.AddSeparator("");
                }
                pm.AddItem(EditorGUIUtility.TextContent("Print Property Path"), false, e => Debug.Log(((SerializedProperty)e).propertyPath), propertyWithPath);
            }

            if (EditorApplication.contextualPropertyMenu != null)
            {
                if (pm.GetItemCount() > 0)
                {
                    pm.AddSeparator("");
                }
                EditorApplication.contextualPropertyMenu(pm, property);
            }

            Event.current.Use();
            if (pm.GetItemCount() == 0)
            {
                return;
            }

            pm.ShowAsContext();
        }

        /// *listonly*
        public static void Slider(Rect position, SerializedProperty property, float leftValue, float rightValue)
        {
            Slider(position, property, leftValue, rightValue, property.displayName);
        }

        /// *listonly*
        public static void Slider(Rect position, SerializedProperty property, float leftValue, float rightValue, string label)
        {
            Slider(position, property, leftValue, rightValue, EditorGUIUtility.TempContent(label));
        }

        // Make a slider the user can drag to change a value between a min and a max.
        public static void Slider(Rect position, SerializedProperty property, float leftValue, float rightValue, GUIContent label)
        {
            label = BeginProperty(position, label, property);

            BeginChangeCheck();

            float newValue = Slider(position, label, property.floatValue, leftValue, rightValue);
            if (EndChangeCheck())
            {
                property.floatValue = newValue;
            }

            EndProperty();
        }

        /// *listonly*
        public static int IntSlider(Rect position, int value, int leftValue, int rightValue)
        {
            int id = GUIUtility.GetControlID(s_SliderHash, FocusType.Keyboard, position);
            return Mathf.RoundToInt(DoSlider(IndentedRect(position), EditorGUIUtility.DragZoneRect(position), id, value, leftValue, rightValue, kIntFieldFormatString));
        }

        /// *listonly*
        public static int IntSlider(Rect position, string label, int value, int leftValue, int rightValue)
        {
            return IntSlider(position, EditorGUIUtility.TempContent(label), value, leftValue, rightValue);
        }

        // Make a slider the user can drag to change an integer value between a min and a max.
        public static int IntSlider(Rect position, GUIContent label, int value, int leftValue, int rightValue)
        {
            int id = GUIUtility.GetControlID(s_SliderHash, FocusType.Keyboard, position);
            return Mathf.RoundToInt(DoSlider(PrefixLabel(position, id, label), EditorGUIUtility.DragZoneRect(position), id, value, leftValue, rightValue, kIntFieldFormatString));
        }

        /// *listonly*
        public static void IntSlider(Rect position, SerializedProperty property, int leftValue, int rightValue)
        {
            IntSlider(position, property, leftValue, rightValue, property.displayName);
        }

        /// *listonly*
        public static void IntSlider(Rect position, SerializedProperty property, int leftValue, int rightValue, string label)
        {
            IntSlider(position, property, leftValue, rightValue, EditorGUIUtility.TempContent(label));
        }

        // Make a slider the user can drag to change a value between a min and a max.
        public static void IntSlider(Rect position, SerializedProperty property, int leftValue, int rightValue, GUIContent label)
        {
            label = BeginProperty(position, label, property);

            BeginChangeCheck();

            int newValue = IntSlider(position, label, property.intValue, leftValue, rightValue);
            if (EndChangeCheck())
            {
                property.intValue = newValue;
            }

            EndProperty();
        }

        // Generic method for showing a left aligned and right aligned label within a rect
        internal static void DoTwoLabels(Rect rect, GUIContent leftLabel, GUIContent rightLabel, GUIStyle labelStyle)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            TextAnchor oldAlignment = labelStyle.alignment;

            labelStyle.alignment = TextAnchor.UpperLeft;
            GUI.Label(rect, leftLabel, labelStyle);

            labelStyle.alignment = TextAnchor.UpperRight;
            GUI.Label(rect, rightLabel, labelStyle);

            labelStyle.alignment = oldAlignment;
        }

        private static float DoSlider(Rect position, Rect dragZonePosition, int id, float value, float left, float right, string formatString)
        {
            return DoSlider(position, dragZonePosition, id, value, left, right, formatString, 1f);
        }

        private static float DoSlider(Rect position, Rect dragZonePosition, int id, float value, float left, float right, string formatString, float power)
        {
            return DoSlider(position, dragZonePosition, id, value, left, right, formatString, power, GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb, null);
        }

        private static float DoSlider(Rect position, Rect dragZonePosition, int id, float value, float left, float right, string formatString, float power, GUIStyle sliderStyle, GUIStyle thumbStyle, Texture2D sliderBackground)
        {
            int sliderId = GUIUtility.GetControlID(s_SliderKnobHash, FocusType.Passive, position);
            // Map some nonsensical edge cases to avoid breaking the UI.
            // A slider with such a large range is basically useless, anyway.
            left = Mathf.Clamp(left, float.MinValue, float.MaxValue);
            right = Mathf.Clamp(right, float.MinValue, float.MaxValue);

            float w = position.width;
            if (w >= kSliderMinW + kSpacing + EditorGUIUtility.fieldWidth)
            {
                float sWidth = w - kSpacing - EditorGUIUtility.fieldWidth;
                BeginChangeCheck();

                // While the text field is not being edited, we want to share the keyboard focus with the slider, so we temporarily set it to have focus
                if (EditorGUIUtility.keyboardControl == id && !s_RecycledEditor.IsEditingControl(id))
                {
                    EditorGUIUtility.keyboardControl = sliderId;
                }

                // Remap slider values according to power curve, if it's not identity
                float remapLeft = left;
                float remapRight = right;
                if (power != 1f)
                {
                    remapLeft = PowPreserveSign(left, 1f / power);
                    remapRight = PowPreserveSign(right, 1f / power);
                    value = PowPreserveSign(value, 1f / power);
                }

                Rect sliderRect = new Rect(position.x, position.y, sWidth, position.height);

                if (sliderBackground != null && Event.current.type == EventType.Repaint)
                {
                    Rect bgRect = sliderStyle.overflow.Add(sliderRect);
                    Graphics.DrawTexture(bgRect, sliderBackground, new Rect(.5f / sliderBackground.width, .5f / sliderBackground.height, 1 - 1f / sliderBackground.width, 1 - 1f / sliderBackground.height), 0, 0, 0, 0, Color.grey);
                }

                value = GUI.Slider(sliderRect, value, 0, remapLeft, remapRight, sliderStyle, showMixedValue ? "SliderMixed" : thumbStyle, true, sliderId);
                if (power != 1f)
                {
                    value = PowPreserveSign(value, power);
                    value = Mathf.Clamp(value, Mathf.Min(left, right), Mathf.Max(left, right));
                }

                // Do slider labels if present
                if (EditorGUIUtility.sliderLabels.HasLabels())
                {
                    Color orgColor = GUI.color;
                    GUI.color = GUI.color * new Color(1f, 1f, 1f, 0.5f);
                    Rect labelRect = new Rect(sliderRect.x, sliderRect.y + 10, sliderRect.width, sliderRect.height);
                    DoTwoLabels(labelRect, EditorGUIUtility.sliderLabels.leftLabel, EditorGUIUtility.sliderLabels.rightLabel, EditorStyles.miniLabel);
                    GUI.color = orgColor;
                    EditorGUIUtility.sliderLabels.SetLabels(null, null);
                }

                // The keyboard control id that we want to retain is the "main" one that is used for the combined control, including the text field.
                // Whenever the keyboardControl is the sliderId after calling the slider function, we set it back to this main id,
                // regardless of whether it had keyboard focus before the function call, or just got it.
                if (EditorGUIUtility.keyboardControl == sliderId || EditorGUIUtility.hotControl == sliderId)
                {
                    EditorGUIUtility.keyboardControl = id;
                }

                if (EditorGUIUtility.keyboardControl == id && Event.current.type == EventType.KeyDown && !s_RecycledEditor.IsEditingControl(id)
                    && (Event.current.keyCode == KeyCode.LeftArrow || Event.current.keyCode == KeyCode.RightArrow))
                {
                    // Change by approximately 1/100 of entire range, or 1/10 if holding down shift
                    // But round to nearest power of ten to get nice resulting numbers.
                    float delta = MathUtils.GetClosestPowerOfTen(Mathf.Abs((right - left) * 0.01f));
                    if (formatString == kIntFieldFormatString && delta < 1)
                    {
                        delta = 1;
                    }
                    if (Event.current.shift)
                    {
                        delta *= 10;
                    }

                    // Increment or decrement by just over half the delta.
                    // This means that e.g. if delta is 1, incrementing from 1.0 will go to 2.0,
                    // but incrementing from 0.9 is going to 1.0 rather than 2.0.
                    // This feels more right since 1.0 is the "next" one.
                    if (Event.current.keyCode == KeyCode.LeftArrow)
                    {
                        value -= delta * 0.5001f;
                    }
                    else
                    {
                        value += delta * 0.5001f;
                    }

                    // Now round to a multiple of our delta value so we get a round end result instead of just a round delta.
                    value = MathUtils.RoundToMultipleOf(value, delta);
                    GUI.changed = true;
                    Event.current.Use();
                }
                if (EndChangeCheck())
                {
                    float ValuesPerPixel = (right - left) / (sWidth - GUI.skin.horizontalSlider.padding.horizontal - GUI.skin.horizontalSliderThumb.fixedWidth);
                    value = MathUtils.RoundBasedOnMinimumDifference(value, Mathf.Abs(ValuesPerPixel));
                    if (s_RecycledEditor.IsEditingControl(id))
                    {
                        s_RecycledEditor.EndEditing();
                    }
                }
                value = DoFloatField(s_RecycledEditor, new Rect(position.x + sWidth + kSpacing, position.y, EditorGUIUtility.fieldWidth, position.height), dragZonePosition, id, value, formatString, EditorStyles.numberField, true);
            }
            else
            {
                w = Mathf.Min(EditorGUIUtility.fieldWidth, w);
                position.x = position.xMax - w;
                position.width = w;
                value = DoFloatField(s_RecycledEditor, position, dragZonePosition, id, value, formatString, EditorStyles.numberField, true);
            }
            value = Mathf.Clamp(value, Mathf.Min(left, right), Mathf.Max(left, right));
            return value;
        }

        /// *listonly*
        [System.Obsolete("Switch the order of the first two parameters.")]
        public static void MinMaxSlider(GUIContent label, Rect position, ref float minValue, ref float maxValue, float minLimit, float maxLimit)
        {
            MinMaxSlider(position, label, ref minValue, ref maxValue, minLimit, maxLimit);
        }

        /// *listonly*
        public static void MinMaxSlider(Rect position, string label, ref float minValue, ref float maxValue, float minLimit, float maxLimit)
        {
            MinMaxSlider(position, EditorGUIUtility.TempContent(label), ref minValue, ref maxValue, minLimit, maxLimit);
        }

        /// *listonly*
        public static void MinMaxSlider(Rect position, GUIContent label, ref float minValue, ref float maxValue, float minLimit, float maxLimit)
        {
            int id = GUIUtility.GetControlID(s_MinMaxSliderHash, FocusType.Passive);
            DoMinMaxSlider(PrefixLabel(position, id, label), id, ref minValue, ref maxValue, minLimit, maxLimit);
        }

        // Make a special slider the user can use to specify a range between a min and a max.
        public static void MinMaxSlider(Rect position, ref float minValue, ref float maxValue, float minLimit, float maxLimit)
        {
            DoMinMaxSlider(IndentedRect(position), GUIUtility.GetControlID(s_MinMaxSliderHash, FocusType.Passive), ref minValue, ref maxValue, minLimit, maxLimit);
        }

        private static void DoMinMaxSlider(Rect position, int id, ref float minValue, ref float maxValue, float minLimit, float maxLimit)
        {
            float size = maxValue - minValue;
            BeginChangeCheck();
            EditorGUIExt.DoMinMaxSlider(position, id, ref minValue, ref size, minLimit, maxLimit, minLimit, maxLimit, GUI.skin.horizontalSlider, EditorStyles.minMaxHorizontalSliderThumb, true);
            if (EndChangeCheck())
            {
                maxValue = minValue + size;
            }
        }

        // The indent level of the field labels.
        public static int indentLevel
        {
            get { return ms_IndentLevel; }
            set { ms_IndentLevel = value; }
        }

        internal static float indent
        {
            get { return indentLevel * kIndentPerLevel; }
        }

        public class IndentLevelScope : GUI.Scope
        {
            int m_IndentOffset;
            public IndentLevelScope() : this(1) {}

            public IndentLevelScope(int increment)
            {
                m_IndentOffset = increment;
                EditorGUI.indentLevel += m_IndentOffset;
            }

            protected override void CloseScope()
            {
                EditorGUI.indentLevel -= m_IndentOffset;
            }
        }

        /// *listonly*
        private static int PopupInternal(Rect position, string label, int selectedIndex, string[] displayedOptions, GUIStyle style)
        {
            return PopupInternal(position, EditorGUIUtility.TempContent(label), selectedIndex, EditorGUIUtility.TempContent(displayedOptions), style);
        }

        // Make a generic popup selection field.
        private static int PopupInternal(Rect position, GUIContent label, int selectedIndex, GUIContent[] displayedOptions, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_PopupHash, FocusType.Keyboard, position);
            if (label != null)
                position = PrefixLabel(position, id, label);
            return DoPopup(position, id, selectedIndex, displayedOptions, style);
        }

        // Called from PropertyField
        private static void Popup(Rect position, SerializedProperty property, GUIContent label)
        {
            BeginChangeCheck();
            int idx = EditorGUI.Popup(position, label, property.hasMultipleDifferentValues ? -1 : property.enumValueIndex, EditorGUIUtility.TempContent(property.enumDisplayNames));
            if (EndChangeCheck())
            {
                property.enumValueIndex = idx;
            }
        }

        // Should be called directly - based on int properties.
        internal static void Popup(Rect position, SerializedProperty property, GUIContent[] displayedOptions, GUIContent label)
        {
            label = BeginProperty(position, label, property);
            BeginChangeCheck();
            int idx = EditorGUI.Popup(position, label, property.hasMultipleDifferentValues ? -1 : property.intValue, displayedOptions);
            if (EndChangeCheck())
            {
                property.intValue = idx;
            }
            EndProperty();
        }

        // Make an enum popup selection field.
        private static Enum EnumPopupInternal(Rect position, GUIContent label, Enum selected, GUIStyle style)
        {
            var enumType = selected.GetType();
            if (!enumType.IsEnum)
            {
                throw new ArgumentException("Parameter selected must be of type System.Enum", "selected");
            }

            var enumData = GetNonObsoleteEnumData(enumType);
            var i = Array.IndexOf(enumData.values, selected);
            i = Popup(position, label, i, EditorGUIUtility.TempContent(enumData.displayNames), style);
            return (i < 0 || i >= enumData.values.Length) ? selected : enumData.values[i];
        }

        /// *listonly*
        private static int IntPopupInternal(Rect position, GUIContent label, int selectedValue, GUIContent[] displayedOptions, int[] optionValues, GUIStyle style)
        {
            // value --> index
            int i;
            if (optionValues != null)
            {
                for (i = 0; (i < optionValues.Length) && (selectedValue != optionValues[i]); ++i)
                {
                }
            }
            // value = index
            else
            {
                i = selectedValue;
            }

            i = PopupInternal(position, label, i, displayedOptions, style);

            if (optionValues == null)
            {
                return i;
            }
            // index --> value
            else if (i < 0 || i >= optionValues.Length)
            {
                return selectedValue;
            }
            else
            {
                return optionValues[i];
            }
        }

        internal static void IntPopupInternal(Rect position, SerializedProperty property, GUIContent[] displayedOptions, int[] optionValues, GUIContent label)
        {
            label = BeginProperty(position, label, property);

            BeginChangeCheck();
            int newValue = EditorGUI.IntPopupInternal(position, label, property.intValue, displayedOptions, optionValues, EditorStyles.popup);
            if (EndChangeCheck())
            {
                property.intValue = newValue;
            }

            EndProperty();
        }

        internal static void SortingLayerField(Rect position, GUIContent label, SerializedProperty layerID, GUIStyle style, GUIStyle labelStyle)
        {
            int id = EditorGUIUtility.GetControlID(s_SortingLayerFieldHash, FocusType.Keyboard, position);
            position = EditorGUI.PrefixLabel(position, id, label, labelStyle);

            Event evt = Event.current;
            int selected = EditorGUI.PopupCallbackInfo.GetSelectedValueForControl(id, -1);
            if (selected != -1)
            {
                int[] layerIDs = InternalEditorUtility.sortingLayerUniqueIDs;
                if (selected >= layerIDs.Length)
                {
                    TagManagerInspector.ShowWithInitialExpansion(TagManagerInspector.InitialExpansionState.SortingLayers);
                }
                else
                {
                    layerID.intValue = layerIDs[selected];
                }
            }

            if (evt.type == EventType.MouseDown && position.Contains(evt.mousePosition) || evt.MainActionKeyForControl(id))
            {
                int i = 0;
                int[] layerIDs = InternalEditorUtility.sortingLayerUniqueIDs;
                string[] layerNames = InternalEditorUtility.sortingLayerNames;
                for (i = 0; i < layerIDs.Length; i++)
                {
                    if (layerIDs[i] == layerID.intValue)
                        break;
                }
                ArrayUtility.Add(ref layerNames, "");
                ArrayUtility.Add(ref layerNames, "Add Sorting Layer...");

                EditorGUI.DoPopup(position, id, i, EditorGUIUtility.TempContent(layerNames), style);
            }
            else if (Event.current.type == EventType.Repaint)
            {
                GUIContent layerName;
                if (layerID.hasMultipleDifferentValues)
                    layerName = EditorGUI.mixedValueContent;
                else
                    layerName = EditorGUIUtility.TempContent(InternalEditorUtility.GetSortingLayerNameFromUniqueID(layerID.intValue));
                EditorGUI.showMixedValue = layerID.hasMultipleDifferentValues;
                EditorGUI.BeginHandleMixedValueContentColor();
                style.Draw(position, layerName, id, false);
                EditorGUI.EndHandleMixedValueContentColor();
                EditorGUI.showMixedValue = false;
            }
        }

        // sealed partial class for storing state for popup menus so we can get the info back to OnGUI from the user selection
        internal sealed partial class PopupCallbackInfo
        {
            // The global shared popup state
            public static PopupCallbackInfo instance = null;

            // Name of the command event sent from the popup menu to OnGUI when user has changed selection
            internal const string kPopupMenuChangedMessage = "PopupMenuChanged";

            // The control ID of the popup menu that is currently displayed.
            // Used to pass selection changes back again...
            private int m_ControlID = 0;

            // Which item was selected
            private int m_SelectedIndex = 0;

            // Which view should we send it to.
            private GUIView m_SourceView;

            // *undoc*
            public PopupCallbackInfo(int controlID)
            {
                m_ControlID = controlID;
                m_SourceView = GUIView.current;
            }

            // *undoc*
            public static int GetSelectedValueForControl(int controlID, int selected)
            {
                Event evt = Event.current;
                if (evt.type == EventType.ExecuteCommand && evt.commandName == kPopupMenuChangedMessage)
                {
                    if (instance == null)
                    {
                        Debug.LogError("Popup menu has no instance");
                        return selected;
                    }
                    if (instance.m_ControlID == controlID)
                    {
                        selected = instance.m_SelectedIndex;
                        instance = null;
                        GUI.changed = true;
                        evt.Use();
                    }
                }
                return selected;
            }

            internal void SetEnumValueDelegate(object userData, string[] options, int selected)
            {
                m_SelectedIndex = selected;
                if (m_SourceView)
                {
                    m_SourceView.SendEvent(EditorGUIUtility.CommandEvent(kPopupMenuChangedMessage));
                }
            }
        }


        internal static int DoPopup(Rect position, int controlID, int selected, GUIContent[] popupValues, GUIStyle style)
        {
            selected = PopupCallbackInfo.GetSelectedValueForControl(controlID, selected);

            GUIContent buttonContent = null;
            if (buttonContent == null)
            {
                if (showMixedValue)
                {
                    buttonContent = s_MixedValueContent;
                }
                else if (selected < 0 || selected >= popupValues.Length)
                {
                    buttonContent = GUIContent.none;
                }
                else
                {
                    buttonContent = popupValues[selected];
                }
            }

            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.Repaint:
                    // @TODO: Remove this hack and make all editor styles use the default font instead
                    Font originalFont = style.font;
                    if (originalFont && EditorGUIUtility.GetBoldDefaultFont() && originalFont == EditorStyles.miniFont)
                    {
                        style.font = EditorStyles.miniBoldFont;
                    }

                    BeginHandleMixedValueContentColor();
                    style.Draw(position, buttonContent, controlID, false);
                    EndHandleMixedValueContentColor();

                    style.font = originalFont;
                    break;
                case EventType.MouseDown:
                    if (evt.button == 0 && position.Contains(evt.mousePosition))
                    {
                        if (Application.platform == RuntimePlatform.OSXEditor)
                        {
                            position.y = position.y - selected * 16 - 19;
                        }

                        PopupCallbackInfo.instance = new PopupCallbackInfo(controlID);
                        EditorUtility.DisplayCustomMenu(position, popupValues, showMixedValue ? -1 : selected, PopupCallbackInfo.instance.SetEnumValueDelegate, null);
                        EditorGUIUtility.keyboardControl = controlID;
                        evt.Use();
                    }
                    break;
                case EventType.KeyDown:
                    if (evt.MainActionKeyForControl(controlID))
                    {
                        if (Application.platform == RuntimePlatform.OSXEditor)
                        {
                            position.y = position.y - selected * 16 - 19;
                        }

                        PopupCallbackInfo.instance = new PopupCallbackInfo(controlID);
                        EditorUtility.DisplayCustomMenu(position, popupValues, showMixedValue ? -1 : selected, PopupCallbackInfo.instance.SetEnumValueDelegate, null);
                        evt.Use();
                    }
                    break;
            }
            return selected;
        }

        /// *listonly*
        internal static string TagFieldInternal(Rect position, string tag, GUIStyle style)
        {
            position = IndentedRect(position);

            int id = GUIUtility.GetControlID(s_TagFieldHash, FocusType.Keyboard, position);

            Event evt = Event.current;
            int selected = PopupCallbackInfo.GetSelectedValueForControl(id, -1);
            if (selected != -1)
            {
                string[] tagValues = InternalEditorUtility.tags;
                if (selected >= tagValues.Length)
                {
                    TagManagerInspector.ShowWithInitialExpansion(TagManagerInspector.InitialExpansionState.Tags);
                }
                else
                {
                    tag = tagValues[selected];
                }
            }

            if ((evt.type == EventType.MouseDown && position.Contains(evt.mousePosition)) || evt.MainActionKeyForControl(id))
            {
                int i = 0;
                string[] tagValues = InternalEditorUtility.tags;
                for (i = 0; i < tagValues.Length; i++)
                {
                    if (tagValues[i] == tag)
                    {
                        break;
                    }
                }
                ArrayUtility.Add(ref tagValues, "");
                ArrayUtility.Add(ref tagValues, "Add Tag...");

                DoPopup(position, id, i, EditorGUIUtility.TempContent(tagValues), style);
                return tag;
            }
            else if (Event.current.type == EventType.Repaint)
            {
                BeginHandleMixedValueContentColor();
                style.Draw(position, showMixedValue ? s_MixedValueContent : EditorGUIUtility.TempContent(tag), id, false);
                EndHandleMixedValueContentColor();
            }

            return tag;
        }

        // Make a tag selection field.
        internal static string TagFieldInternal(Rect position, GUIContent label, string tag, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_TagFieldHash, FocusType.Keyboard, position);
            position = PrefixLabel(position, id, label);

            Event evt = Event.current;
            int selected = PopupCallbackInfo.GetSelectedValueForControl(id, -1);
            if (selected != -1)
            {
                string[] tagValues = InternalEditorUtility.tags;
                if (selected >= tagValues.Length)
                {
                    TagManagerInspector.ShowWithInitialExpansion(TagManagerInspector.InitialExpansionState.Tags);
                }
                else
                {
                    tag = tagValues[selected];
                }
            }

            if (evt.type == EventType.MouseDown && position.Contains(evt.mousePosition) || evt.MainActionKeyForControl(id))
            {
                int i = 0;
                string[] tagValues = InternalEditorUtility.tags;
                for (i = 0; i < tagValues.Length; i++)
                {
                    if (tagValues[i] == tag)
                    {
                        break;
                    }
                }
                ArrayUtility.Add(ref tagValues, "");
                ArrayUtility.Add(ref tagValues, "Add Tag...");

                DoPopup(position, id, i, EditorGUIUtility.TempContent(tagValues), style);
                return tag;
            }
            else if (Event.current.type == EventType.Repaint)
            {
                style.Draw(position, EditorGUIUtility.TempContent(tag), id, false);
            }

            return tag;
        }

        // Make a layer selection field.
        //
        // this one is slow, but we only have one of them (in the game object inspector), so we should be ok.
        // ARGH. This code is SO bad - I will refactor and repent after 2.5 - Nicholas
        internal static int LayerFieldInternal(Rect position, GUIContent label, int layer, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_TagFieldHash, FocusType.Keyboard, position);
            position = PrefixLabel(position, id, label);

            Event evt = Event.current;
            bool wasChangedBefore = GUI.changed;
            int selected = PopupCallbackInfo.GetSelectedValueForControl(id, -1);
            if (selected != -1)
            {
                if (selected >= InternalEditorUtility.layers.Length)
                {
                    TagManagerInspector.ShowWithInitialExpansion(TagManagerInspector.InitialExpansionState.Layers);
                    GUI.changed = wasChangedBefore;
                }
                else
                {
                    int count = 0;
                    for (int i = 0; i < 32; i++)
                    {
                        if (InternalEditorUtility.GetLayerName(i).Length != 0)
                        {
                            if (count == selected)
                            {
                                layer = i;
                                break;
                            }
                            count++;
                        }
                    }
                }
            }

            if ((evt.type == EventType.MouseDown && position.Contains(evt.mousePosition)) || evt.MainActionKeyForControl(id))
            {
                int count = 0;
                for (int i = 0; i < 32; i++)
                {
                    if (InternalEditorUtility.GetLayerName(i).Length != 0)
                    {
                        if (i == layer)
                        {
                            break;
                        }
                        count++;
                    }
                }

                string[] layers = InternalEditorUtility.GetLayersWithId();

                ArrayUtility.Add(ref layers, "");
                ArrayUtility.Add(ref layers, "Add Layer...");

                DoPopup(position, id, count, EditorGUIUtility.TempContent(layers), style);
                Event.current.Use();

                return layer;
            }
            else if (evt.type == EventType.Repaint)
            {
                style.Draw(position, EditorGUIUtility.TempContent(InternalEditorUtility.GetLayerName(layer)), id, false);
            }

            return layer;
        }

        struct EnumData
        {
            public Enum[] values;
            public int[] flagValues;
            public string[] displayNames;
            public bool flags;
            public Type underlyingType;
            public bool unsigned;
            public bool serializable;
        }

        private static readonly Dictionary<Type, EnumData> s_NonObsoleteEnumData = new Dictionary<Type, EnumData>();

        private static EnumData GetNonObsoleteEnumData(Type enumType)
        {
            EnumData enumData;
            if (!s_NonObsoleteEnumData.TryGetValue(enumType, out enumData))
            {
                enumData = new EnumData();
                enumData.underlyingType = Enum.GetUnderlyingType(enumType);
                enumData.unsigned =
                    enumData.underlyingType == typeof(byte)
                    || enumData.underlyingType == typeof(ushort)
                    || enumData.underlyingType == typeof(uint)
                    || enumData.underlyingType == typeof(ulong);
                enumData.displayNames = Enum.GetNames(enumType).Where(n => enumType.GetField(n).GetCustomAttributes(typeof(ObsoleteAttribute), false).Length == 0).ToArray();
                enumData.values = enumData.displayNames.Select(n => (Enum)Enum.Parse(enumType, n)).ToArray();
                enumData.flagValues = enumData.unsigned ?
                    enumData.values.Select(v => unchecked((int)Convert.ToUInt64(v))).ToArray() :
                    enumData.values.Select(v => unchecked((int)Convert.ToInt64(v))).ToArray();
                for (int i = 0, length = enumData.displayNames.Length; i < length; ++i)
                    enumData.displayNames[i] = ObjectNames.NicifyVariableName(enumData.displayNames[i]);
                // convert "everything" values to ~0 for unsigned 8- and 16-bit types
                if (enumData.underlyingType == typeof(ushort))
                {
                    for (int i = 0, length = enumData.flagValues.Length; i < length; ++i)
                    {
                        if (enumData.flagValues[i] == 0xFFFFu)
                            enumData.flagValues[i] = ~0;
                    }
                }
                else if (enumData.underlyingType == typeof(byte))
                {
                    for (int i = 0, length = enumData.flagValues.Length; i < length; ++i)
                    {
                        if (enumData.flagValues[i] == 0xFFu)
                            enumData.flagValues[i] = ~0;
                    }
                }
                enumData.flags = enumType.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;
                enumData.serializable = enumData.underlyingType != typeof(long) && enumData.underlyingType != typeof(ulong);
                s_NonObsoleteEnumData[enumType] = enumData;
            }
            return enumData;
        }

        /// *listonly*
        internal static int MaskFieldInternal(Rect position, GUIContent label, int mask, string[] displayedOptions, GUIStyle style)
        {
            var id = GUIUtility.GetControlID(s_MaskField, FocusType.Keyboard, position);
            position = EditorGUI.PrefixLabel(position, id, label);
            return MaskFieldGUI.DoMaskField(position, id, mask, displayedOptions, style);
        }

        internal static int MaskFieldInternal(Rect position, GUIContent label, int mask, string[] displayedOptions, int[] optionValues, GUIStyle style)
        {
            var id = GUIUtility.GetControlID(s_MaskField, FocusType.Keyboard, position);
            position = EditorGUI.PrefixLabel(position, id, label);
            return MaskFieldGUI.DoMaskField(position, id, mask, displayedOptions, optionValues, style);
        }

        // Make a field for masks.
        internal static int MaskFieldInternal(Rect position, int mask, string[] displayedOptions, GUIStyle style)
        {
            var id = GUIUtility.GetControlID(s_MaskField, FocusType.Keyboard, position);
            return MaskFieldGUI.DoMaskField(IndentedRect(position), id, mask, displayedOptions, style);
        }

        public static Enum EnumFlagsField(Rect position, Enum enumValue)
        {
            return EnumFlagsField(position, enumValue, EditorStyles.popup);
        }

        public static Enum EnumFlagsField(Rect position, Enum enumValue, GUIStyle style)
        {
            return EnumFlagsField(position, GUIContent.none, enumValue, style);
        }

        public static Enum EnumFlagsField(Rect position, string label, Enum enumValue)
        {
            return EnumFlagsField(position, label, enumValue, EditorStyles.popup);
        }

        public static Enum EnumFlagsField(Rect position, string label, Enum enumValue, GUIStyle style)
        {
            return EnumFlagsField(position, EditorGUIUtility.TempContent(label), enumValue, style);
        }

        public static Enum EnumFlagsField(Rect position, GUIContent label, Enum enumValue)
        {
            return EnumFlagsField(position, label, enumValue, EditorStyles.popup);
        }

        public static Enum EnumFlagsField(Rect position, GUIContent label, Enum enumValue, GUIStyle style)
        {
            int changedFlags;
            bool changedToValue;
            return EnumFlagsField(position, label, enumValue, out changedFlags, out changedToValue, style);
        }

        // Internal version that also gives you back which flags were changed and what they were changed to.
        internal static Enum EnumFlagsField(Rect position, GUIContent label, Enum enumValue, out int changedFlags, out bool changedToValue, GUIStyle style)
        {
            var enumType = enumValue.GetType();
            if (!enumType.IsEnum)
                throw new ArgumentException("Parameter enumValue must be of type System.Enum", "enumValue");

            var enumData = GetNonObsoleteEnumData(enumType);
            if (!enumData.serializable)
                // this is the same message used in ScriptPopupMenus.cpp
                throw new NotSupportedException(string.Format("Unsupported enum base type for {0}", enumType.Name));

            var id = GUIUtility.GetControlID(s_EnumFlagsField, FocusType.Keyboard, position);
            position = PrefixLabel(position, id, label);

            var flagsInt = EnumFlagsToInt(enumData, enumValue);

            EditorGUI.BeginChangeCheck();
            flagsInt = MaskFieldGUI.DoMaskField(position, id, flagsInt, enumData.displayNames, enumData.flagValues, style, out changedFlags, out changedToValue);
            if (!EditorGUI.EndChangeCheck())
                return enumValue;

            return IntToEnumFlags(enumType, flagsInt);
        }

        public static void ObjectField(Rect position, SerializedProperty property)
        {
            ObjectField(position, property, null, null, EditorStyles.objectField);
        }

        public static void ObjectField(Rect position, SerializedProperty property, GUIContent label)
        {
            ObjectField(position, property, null, label, EditorStyles.objectField);
        }

        public static void ObjectField(Rect position, SerializedProperty property, System.Type objType)
        {
            ObjectField(position, property, objType, null, EditorStyles.objectField);
        }

        public static void ObjectField(Rect position, SerializedProperty property, System.Type objType, GUIContent label)
        {
            ObjectField(position, property, objType, label, EditorStyles.objectField);
        }

        // We don't expose SerializedProperty overloads with custom GUIStyle parameters according to our guidelines,
        // but we have to provide it internally because ParticleSystem uses a style that's different from everything else.
        internal static void ObjectField(Rect position, SerializedProperty property, System.Type objType, GUIContent label, GUIStyle style)
        {
            label = BeginProperty(position, label, property);
            ObjectFieldInternal(position, property, objType, label, style);
            EndProperty();
        }

        // This version doesn't do BeginProperty / EndProperty. It should not be called directly.
        private static void ObjectFieldInternal(Rect position, SerializedProperty property, System.Type objType, GUIContent label, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(s_PPtrHash, FocusType.Keyboard, position);
            position = PrefixLabel(position, id, label);

            bool allowSceneObjects = false;
            if (property != null)
            {
                // @TODO: Check all target objects.
                Object objectBeingEdited = property.serializedObject.targetObject;

                // Allow scene objects if the object being edited is NOT persistent
                if (objectBeingEdited != null && !EditorUtility.IsPersistent(objectBeingEdited))
                {
                    allowSceneObjects = true;
                }
            }
            DoObjectField(position, position, id, null, objType, property, null, allowSceneObjects, style);
        }

        /// *listonly*
        public static Object ObjectField(Rect position, Object obj, System.Type objType, bool allowSceneObjects)
        {
            int id = GUIUtility.GetControlID(s_ObjectFieldHash, FocusType.Keyboard, position);
            return DoObjectField(IndentedRect(position), IndentedRect(position), id, obj, objType, null, null, allowSceneObjects);
        }

        [System.Obsolete("Check the docs for the usage of the new parameter 'allowSceneObjects'.")]
        public static Object ObjectField(Rect position, Object obj, System.Type objType)
        {
            int id = GUIUtility.GetControlID(s_ObjectFieldHash, FocusType.Keyboard, position);
            return DoObjectField(position, position, id, obj, objType, null, null, true);
        }

        /// *listonly*
        public static Object ObjectField(Rect position, string label, Object obj, System.Type objType, bool allowSceneObjects)
        {
            return ObjectField(position, EditorGUIUtility.TempContent(label), obj, objType, allowSceneObjects);
        }

        [System.Obsolete("Check the docs for the usage of the new parameter 'allowSceneObjects'.")]
        public static Object ObjectField(Rect position, string label, Object obj, System.Type objType)
        {
            return ObjectField(position, EditorGUIUtility.TempContent(label), obj, objType, true);
        }

        // Make an object field. You can assign objects either by drag and drop objects or by selecting an object using the Object Picker.
        public static Object ObjectField(Rect position, GUIContent label, Object obj, System.Type objType, bool allowSceneObjects)
        {
            int id = GUIUtility.GetControlID(s_ObjectFieldHash, FocusType.Keyboard, position);
            position = PrefixLabel(position, id, label);
            if (EditorGUIUtility.HasObjectThumbnail(objType) && position.height > EditorGUI.kSingleLineHeight)
            {
                // Make object field with thumbnail quadratic and align to the right
                float size = Mathf.Min(position.width, position.height);
                position.height = size;
                position.xMin = position.xMax - size;
            }
            return DoObjectField(position, position, id, obj, objType, null, null, allowSceneObjects);
        }

        internal static void GetRectsForMiniThumbnailField(Rect position, out Rect thumbRect, out Rect labelRect)
        {
            thumbRect = IndentedRect(position);
            thumbRect.y -= (EditorGUI.kObjectFieldMiniThumbnailHeight - EditorGUI.kSingleLineHeight) * 0.5f; // center around EditorGUI.kSingleLineHeight
            thumbRect.height = EditorGUI.kObjectFieldMiniThumbnailHeight;
            thumbRect.width = EditorGUI.kObjectFieldMiniThumbnailWidth;

            float labelStartX = thumbRect.x + 2 * kIndentPerLevel; // label aligns with indent levels for being able to have the following labels align with this label
            labelRect = new Rect(labelStartX, position.y, thumbRect.x + EditorGUIUtility.labelWidth - labelStartX, position.height);
        }

        // Make a object field with the preview to the left and the label on the right thats fits on a single line height
        internal static Object MiniThumbnailObjectField(Rect position, GUIContent label, Object obj, System.Type objType)
        {
            int id = GUIUtility.GetControlID(s_ObjectFieldHash, FocusType.Keyboard, position);

            Rect thumbRect, labelRect;
            GetRectsForMiniThumbnailField(position, out thumbRect, out labelRect);
            HandlePrefixLabel(position, labelRect, label, id, EditorStyles.label);
            return DoObjectField(thumbRect, thumbRect, id, obj, objType, null, null, false);
        }

        [System.Obsolete("Check the docs for the usage of the new parameter 'allowSceneObjects'.")]
        public static Object ObjectField(Rect position, GUIContent label, Object obj, System.Type objType)
        {
            return ObjectField(position, label, obj, objType, true);
        }

        internal static GameObject GetGameObjectFromObject(Object obj)
        {
            var go = obj as GameObject;
            if (go == null && obj is Component)
                go = ((Component)obj).gameObject;
            return go;
        }

        internal static bool CheckForCrossSceneReferencing(Object obj1, Object obj2)
        {
            // If either object is not a component nor gameobject: cannot become a cross scene reference
            GameObject go = GetGameObjectFromObject(obj1);
            if (go == null)
                return false;

            GameObject go2 = GetGameObjectFromObject(obj2);
            if (go2 == null)
                return false;

            // If either object is a prefab: cannot become a cross scene reference
            if (EditorUtility.IsPersistent(go) || EditorUtility.IsPersistent(go2))
                return false;

            // If either scene is invalid: cannot become a cross scene reference
            if (!go.scene.IsValid() || !go2.scene.IsValid())
                return false;

            return go.scene != go2.scene;
        }

        private static bool ValidateObjectReferenceValue(SerializedProperty property, Object obj, ObjectFieldValidatorOptions options)
        {
            if ((options & ObjectFieldValidatorOptions.ExactObjectTypeValidation) == ObjectFieldValidatorOptions.ExactObjectTypeValidation)
                return property.ValidateObjectReferenceValueExact(obj);

            return property.ValidateObjectReferenceValue(obj);
        }

        internal static Object ValidateObjectFieldAssignment(Object[] references, System.Type objType, SerializedProperty property, ObjectFieldValidatorOptions options)
        {
            if (references.Length > 0)
            {
                bool dragAssignment = DragAndDrop.objectReferences.Length > 0;
                bool isTextureRef = (references[0] != null && references[0].GetType() == typeof(Texture2D));

                if ((objType == typeof(Sprite)) && isTextureRef && dragAssignment)
                {
                    return SpriteUtility.TextureToSprite(references[0] as Texture2D);
                }

                if (property != null)
                {
                    if (references[0] != null && ValidateObjectReferenceValue(property, references[0], options))
                    {
                        if (EditorSceneManager.preventCrossSceneReferences && CheckForCrossSceneReferencing(references[0], property.serializedObject.targetObject))
                            return null;

                        if (objType != null)
                        {
                            if (references[0].GetType() == typeof(GameObject) && typeof(Component).IsAssignableFrom(objType))
                            {
                                GameObject go = (GameObject)references[0];
                                references = go.GetComponents(typeof(Component));
                            }
                            foreach (Object i in references)
                            {
                                if (i != null && objType.IsAssignableFrom(i.GetType()))
                                {
                                    return i;
                                }
                            }
                        }
                        else
                        {
                            return references[0];
                        }
                    }

                    // If array, test against the target arrayElementType, if not test against the target Type.
                    string testElementType = property.type;
                    if (property.type == "vector")
                        testElementType = property.arrayElementType;

                    if ((testElementType == "PPtr<Sprite>" || testElementType == "PPtr<$Sprite>") && isTextureRef && dragAssignment)
                    {
                        return SpriteUtility.TextureToSprite(references[0] as Texture2D);
                    }
                }
                else
                {
                    if (references[0] != null && references[0].GetType() == typeof(GameObject) && typeof(Component).IsAssignableFrom(objType))
                    {
                        GameObject go = (GameObject)references[0];
                        references = go.GetComponents(typeof(Component));
                    }
                    foreach (Object i in references)
                    {
                        if (i != null && objType.IsAssignableFrom(i.GetType()))
                        {
                            return i;
                        }
                    }
                }
            }
            return null;
        }

        private static Object HandleTextureToSprite(Texture2D tex)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(tex));

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i].GetType() == typeof(Sprite))
                {
                    return assets[i];
                }
            }

            return tex;
        }

        // Apply the indentLevel to a control rect
        public static Rect IndentedRect(Rect source)
        {
            float x = indent;
            return new Rect(source.x + x, source.y, source.width - x, source.height);
        }

        // Make an X & Y field for entering a [[Vector2]].
        public static Vector2 Vector2Field(Rect position, string label, Vector2 value)
        {
            return Vector2Field(position, EditorGUIUtility.TempContent(label), value);
        }

        // Make an X & Y field for entering a [[Vector2]].
        public static Vector2 Vector2Field(Rect position, GUIContent label, Vector2 value)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 2);
            position.height = kSingleLineHeight;
            return Vector2Field(position, value);
        }

        // Make an X & Y field for entering a [[Vector2]].
        private static Vector2 Vector2Field(Rect position, Vector2 value)
        {
            s_Vector2Floats[0] = value.x;
            s_Vector2Floats[1] = value.y;
            position.height = kSingleLineHeight;
            BeginChangeCheck();
            MultiFloatField(position, s_XYLabels, s_Vector2Floats);
            if (EndChangeCheck())
            {
                value.x = s_Vector2Floats[0];
                value.y = s_Vector2Floats[1];
            }
            return value;
        }

        // Make an X, Y & Z field for entering a [[Vector3]].
        public static Vector3 Vector3Field(Rect position, string label, Vector3 value)
        {
            return Vector3Field(position, EditorGUIUtility.TempContent(label), value);
        }

        // Make an X, Y & Z field for entering a [[Vector3]].
        public static Vector3 Vector3Field(Rect position, GUIContent label, Vector3 value)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 3);
            position.height = kSingleLineHeight;
            return Vector3Field(position, value);
        }

        // Make an X, Y & Z field for entering a [[Vector3]].
        private static Vector3 Vector3Field(Rect position, Vector3 value)
        {
            s_Vector3Floats[0] = value.x;
            s_Vector3Floats[1] = value.y;
            s_Vector3Floats[2] = value.z;
            position.height = kSingleLineHeight;
            BeginChangeCheck();
            MultiFloatField(position, s_XYZLabels, s_Vector3Floats);
            if (EndChangeCheck())
            {
                value.x = s_Vector3Floats[0];
                value.y = s_Vector3Floats[1];
                value.z = s_Vector3Floats[2];
            }
            return value;
        }

        // Make an X, Y field - not public (use PropertyField instead)
        private static void Vector2Field(Rect position, SerializedProperty property, GUIContent label)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 2);
            position.height = kSingleLineHeight;
            SerializedProperty cur = property.Copy();
            cur.Next(true);
            MultiPropertyField(position, s_XYLabels, cur, PropertyVisibility.All);
        }

        // Make an X, Y and Z field - not public (use PropertyField instead)
        private static void Vector3Field(Rect position, SerializedProperty property, GUIContent label)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 3);
            position.height = kSingleLineHeight;
            SerializedProperty cur = property.Copy();
            cur.Next(true);
            MultiPropertyField(position, s_XYZLabels, cur, PropertyVisibility.All);
        }

        // Make an X, Y, Z and W field - not public (use PropertyField instead)
        static void Vector4Field(Rect position, SerializedProperty property, GUIContent label)
        {
            int id = EditorGUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 4);
            position.height = kSingleLineHeight;
            SerializedProperty cur = property.Copy();
            cur.Next(true);
            MultiPropertyField(position, s_XYZWLabels, cur, PropertyVisibility.All);
        }

        // Make an X, Y, Z & W field for entering a [[Vector4]].
        public static Vector4 Vector4Field(Rect position, string label, Vector4 value)
        {
            return Vector4Field(position, EditorGUIUtility.TempContent(label), value);
        }

        // Make an X, Y, Z & W field for entering a [[Vector4]].
        public static Vector4 Vector4Field(Rect position, GUIContent label, Vector4 value)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 4);
            position.height = kSingleLineHeight;
            return Vector4FieldNoIndent(position, value);
        }

        private static Vector4 Vector4FieldNoIndent(Rect position, Vector4 value)
        {
            s_Vector4Floats[0] = value.x;
            s_Vector4Floats[1] = value.y;
            s_Vector4Floats[2] = value.z;
            s_Vector4Floats[3] = value.w;
            position.height = kSingleLineHeight;
            BeginChangeCheck();
            MultiFloatField(position, s_XYZWLabels, s_Vector4Floats);
            if (EndChangeCheck())
            {
                value.x = s_Vector4Floats[0];
                value.y = s_Vector4Floats[1];
                value.z = s_Vector4Floats[2];
                value.w = s_Vector4Floats[3];
            }
            return value;
        }

        // Make an X & Y int field for entering a [[Vector2Int]].
        public static Vector2Int Vector2IntField(Rect position, string label, Vector2Int value)
        {
            return Vector2IntField(position, EditorGUIUtility.TempContent(label), value);
        }

        // Make an X & Y int field for entering a [[Vector2Int]].
        public static Vector2Int Vector2IntField(Rect position, GUIContent label, Vector2Int value)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 2);
            position.height = kSingleLineHeight;
            return Vector2IntField(position, value);
        }

        // Make an X & Y int field for entering a [[Vector2Int]].
        private static Vector2Int Vector2IntField(Rect position, Vector2Int value)
        {
            s_Vector2Ints[0] = value.x;
            s_Vector2Ints[1] = value.y;
            position.height = kSingleLineHeight;
            BeginChangeCheck();
            MultiIntField(position, s_XYLabels, s_Vector2Ints);
            if (EndChangeCheck())
            {
                value.x = s_Vector2Ints[0];
                value.y = s_Vector2Ints[1];
            }
            return value;
        }

        // Make an X, Y int field - not public (use PropertyField instead)
        private static void Vector2IntField(Rect position, SerializedProperty property, GUIContent label)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 2);
            position.height = kSingleLineHeight;
            SerializedProperty cur = property.Copy();
            cur.Next(true);
            MultiPropertyField(position, s_XYLabels, cur, PropertyVisibility.All);
        }

        // Make an X, Y and Z int field for entering a [[Vector3Int]].
        public static Vector3Int Vector3IntField(Rect position, string label, Vector3Int value)
        {
            return Vector3IntField(position, EditorGUIUtility.TempContent(label), value);
        }

        // Make an X, Y and Z int field for entering a [[Vector3Int]].
        public static Vector3Int Vector3IntField(Rect position, GUIContent label, Vector3Int value)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 3);
            position.height = kSingleLineHeight;
            return Vector3IntField(position, value);
        }

        // Make an X, Y and Z int field for entering a [[Vector3Int]].
        private static Vector3Int Vector3IntField(Rect position, Vector3Int value)
        {
            s_Vector3Ints[0] = value.x;
            s_Vector3Ints[1] = value.y;
            s_Vector3Ints[2] = value.z;
            position.height = kSingleLineHeight;
            BeginChangeCheck();
            MultiIntField(position, s_XYZLabels, s_Vector3Ints);
            if (EndChangeCheck())
            {
                value.x = s_Vector3Ints[0];
                value.y = s_Vector3Ints[1];
                value.z = s_Vector3Ints[2];
            }
            return value;
        }

        // Make an X, Y and Z int field - not public (use PropertyField instead)
        private static void Vector3IntField(Rect position, SerializedProperty property, GUIContent label)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 3);
            position.height = kSingleLineHeight;
            SerializedProperty cur = property.Copy();
            cur.Next(true);
            MultiPropertyField(position, s_XYZLabels, cur, PropertyVisibility.All);
        }

        /// *listonly*
        public static Rect RectField(Rect position, Rect value)
        {
            return RectFieldNoIndent(IndentedRect(position), value);
        }

        private static Rect RectFieldNoIndent(Rect position, Rect value)
        {
            position.height = kSingleLineHeight;
            s_Vector2Floats[0] = value.x;
            s_Vector2Floats[1] = value.y;
            BeginChangeCheck();
            MultiFloatField(position, s_XYLabels, s_Vector2Floats);
            if (EndChangeCheck())
            {
                value.x = s_Vector2Floats[0];
                value.y = s_Vector2Floats[1];
            }
            position.y += kSingleLineHeight + kVerticalSpacingMultiField;
            s_Vector2Floats[0] = value.width;
            s_Vector2Floats[1] = value.height;
            BeginChangeCheck();
            MultiFloatField(position, s_WHLabels, s_Vector2Floats);
            if (EndChangeCheck())
            {
                value.width = s_Vector2Floats[0];
                value.height = s_Vector2Floats[1];
            }
            return value;
        }

        /// *listonly*
        public static Rect RectField(Rect position, string label, Rect value)
        {
            return RectField(position, EditorGUIUtility.TempContent(label), value);
        }

        // Make an X, Y, W & H field for entering a [[Rect]].
        public static Rect RectField(Rect position, GUIContent label, Rect value)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 2);
            return RectFieldNoIndent(position, value);
        }

        // Make an X, Y, W & H for Rect using SerializedProperty (not public)
        private static void RectField(Rect position, SerializedProperty property, GUIContent label)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 2);
            position.height = kSingleLineHeight;

            SerializedProperty cur = property.Copy();
            cur.Next(true);
            MultiPropertyField(position, s_XYLabels, cur, PropertyVisibility.All);
            position.y += kSingleLineHeight + kVerticalSpacingMultiField;

            MultiPropertyField(position, s_WHLabels, cur, PropertyVisibility.All);
        }

        /// *listonly*
        public static RectInt RectIntField(Rect position, RectInt value)
        {
            position.height = kSingleLineHeight;
            s_Vector2Ints[0] = value.x;
            s_Vector2Ints[1] = value.y;
            BeginChangeCheck();
            MultiIntField(position, s_XYLabels, s_Vector2Ints);
            if (EndChangeCheck())
            {
                value.x = s_Vector2Ints[0];
                value.y = s_Vector2Ints[1];
            }
            position.y += kSingleLineHeight;
            s_Vector2Ints[0] = value.width;
            s_Vector2Ints[1] = value.height;
            BeginChangeCheck();
            MultiIntField(position, s_WHLabels, s_Vector2Ints);
            if (EndChangeCheck())
            {
                value.width = s_Vector2Ints[0];
                value.height = s_Vector2Ints[1];
            }
            return value;
        }

        /// *listonly*
        public static RectInt RectIntField(Rect position, string label, RectInt value)
        {
            return RectIntField(position, EditorGUIUtility.TempContent(label), value);
        }

        // Make an X, Y, W & H field for entering a [[Rect]].
        public static RectInt RectIntField(Rect position, GUIContent label, RectInt value)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 2);
            position.height = kSingleLineHeight;
            return RectIntField(position, value);
        }

        // Make an X, Y, W & H for RectInt using SerializedProperty (not public)
        private static void RectIntField(Rect position, SerializedProperty property, GUIContent label)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 2);
            position.height = kSingleLineHeight;

            SerializedProperty cur = property.Copy();
            cur.Next(true);
            MultiPropertyField(position, s_XYLabels, cur, PropertyVisibility.All);
            position.y += kSingleLineHeight + kVerticalSpacingMultiField;

            MultiPropertyField(position, s_WHLabels, cur, PropertyVisibility.All);
        }

        private static Rect DrawBoundsFieldLabelsAndAdjustPositionForValues(Rect position, bool drawOutside, GUIContent firstContent, GUIContent secondContent)
        {
            const float kBoundsTextWidth = 53;

            if (drawOutside)
                position.xMin -= kBoundsTextWidth;

            GUI.Label(position, firstContent, EditorStyles.label);
            position.y += kSingleLineHeight + kVerticalSpacingMultiField;
            GUI.Label(position, secondContent, EditorStyles.label);

            position.y -= kSingleLineHeight + kVerticalSpacingMultiField;
            position.xMin += kBoundsTextWidth;

            return position;
        }

        /// *listonly*
        public static Bounds BoundsField(Rect position, Bounds value)
        {
            return BoundsFieldNoIndent(IndentedRect(position), value, false);
        }

        public static Bounds BoundsField(Rect position, string label, Bounds value)
        {
            return BoundsField(position, EditorGUIUtility.TempContent(label), value);
        }

        // Make Center & Extents field for entering a [[Bounds]].
        public static Bounds BoundsField(Rect position, GUIContent label, Bounds value)
        {
            if (!LabelHasContent(label))
                return BoundsFieldNoIndent(IndentedRect(position), value, false);

            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 3);
            if (EditorGUIUtility.wideMode)
                position.y += kSingleLineHeight + kVerticalSpacingMultiField;

            return BoundsFieldNoIndent(position, value, true);
        }

        private static Bounds BoundsFieldNoIndent(Rect position, Bounds value, bool isBelowLabel)
        {
            position.height = kSingleLineHeight;
            position = DrawBoundsFieldLabelsAndAdjustPositionForValues(position, EditorGUIUtility.wideMode && isBelowLabel, s_CenterLabel, s_ExtentLabel);

            value.center = Vector3Field(position, value.center);
            position.y += kSingleLineHeight + kVerticalSpacingMultiField;
            value.extents = Vector3Field(position, value.extents);
            return value;
        }

        // private (use PropertyField)
        private static void BoundsField(Rect position, SerializedProperty property, GUIContent label)
        {
            bool hasLabel = LabelHasContent(label);
            if (hasLabel)
            {
                int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
                position = MultiFieldPrefixLabel(position, id, label, 3);
                if (EditorGUIUtility.wideMode)
                    position.y += kSingleLineHeight + kVerticalSpacingMultiField;
            }
            position.height = kSingleLineHeight;
            position = DrawBoundsFieldLabelsAndAdjustPositionForValues(position, EditorGUIUtility.wideMode && hasLabel, s_CenterLabel, s_ExtentLabel);

            SerializedProperty cur = property.Copy();
            cur.Next(true);
            cur.Next(true);
            MultiPropertyField(position, s_XYZLabels, cur, PropertyVisibility.All);
            cur.Next(true);
            position.y += kSingleLineHeight + kVerticalSpacingMultiField;
            MultiPropertyField(position, s_XYZLabels, cur, PropertyVisibility.All);
        }

        /// *listonly*
        public static BoundsInt BoundsIntField(Rect position, BoundsInt value)
        {
            return BoundsIntFieldNoIndent(IndentedRect(position), value, false);
        }

        public static BoundsInt BoundsIntField(Rect position, string label, BoundsInt value)
        {
            return BoundsIntField(position, EditorGUIUtility.TempContent(label), value);
        }

        public static BoundsInt BoundsIntField(Rect position, GUIContent label, BoundsInt value)
        {
            if (!LabelHasContent(label))
                return BoundsIntFieldNoIndent(IndentedRect(position), value, false);

            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, 3);
            if (EditorGUIUtility.wideMode)
                position.y += kSingleLineHeight + kVerticalSpacingMultiField;

            return BoundsIntFieldNoIndent(position, value, true);
        }

        private static BoundsInt BoundsIntFieldNoIndent(Rect position, BoundsInt value, bool isBelowLabel)
        {
            position.height = kSingleLineHeight;
            position = DrawBoundsFieldLabelsAndAdjustPositionForValues(position, EditorGUIUtility.wideMode && isBelowLabel, s_PositionLabel, s_SizeLabel);

            value.position = Vector3IntField(position, value.position);
            position.y += kSingleLineHeight + kVerticalSpacingMultiField;
            value.size = Vector3IntField(position, value.size);
            return value;
        }

        // private (use PropertyField)
        private static void BoundsIntField(Rect position, SerializedProperty property, GUIContent label)
        {
            bool hasLabel = LabelHasContent(label);
            if (hasLabel)
            {
                int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
                position = MultiFieldPrefixLabel(position, id, label, 3);
                if (EditorGUIUtility.wideMode)
                    position.y += kSingleLineHeight + kVerticalSpacingMultiField;
            }
            position.height = kSingleLineHeight;
            position = DrawBoundsFieldLabelsAndAdjustPositionForValues(position, EditorGUIUtility.wideMode && hasLabel, s_PositionLabel, s_SizeLabel);

            SerializedProperty cur = property.Copy();
            cur.Next(true);
            cur.Next(true);
            MultiPropertyField(position, s_XYZLabels, cur, PropertyVisibility.All);
            cur.Next(true);
            position.y += kSingleLineHeight + kVerticalSpacingMultiField;
            MultiPropertyField(position, s_XYZLabels, cur, PropertyVisibility.All);
        }

        public static void MultiFloatField(Rect position, GUIContent label, GUIContent[] subLabels, float[] values)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, subLabels.Length);
            position.height = kSingleLineHeight;
            MultiFloatField(position, subLabels, values);
        }

        public static void MultiFloatField(Rect position, GUIContent[] subLabels, float[] values)
        {
            MultiFloatField(position, subLabels, values, kMiniLabelW);
        }

        internal static void MultiFloatField(Rect position, GUIContent[] subLabels, float[] values, float labelWidth)
        {
            int eCount = values.Length;
            float w = (position.width - (eCount - 1) * kSpacingSubLabel) / eCount;
            Rect nr = new Rect(position);
            nr.width = w;
            float t = EditorGUIUtility.labelWidth;
            int l = EditorGUI.indentLevel;
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = 0;
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = EditorGUI.FloatField(nr, subLabels[i], values[i]);
                nr.x += w + kSpacingSubLabel;
            }
            EditorGUIUtility.labelWidth = t;
            EditorGUI.indentLevel = l;
        }

        public static void MultiIntField(Rect position, GUIContent[] subLabels, int[] values)
        {
            MultiIntField(position, subLabels, values, kMiniLabelW);
        }

        internal static void MultiIntField(Rect position, GUIContent[] subLabels, int[] values, float labelWidth)
        {
            int eCount = values.Length;
            float w = (position.width - (eCount - 1) * kSpacingSubLabel) / eCount;
            Rect nr = new Rect(position);
            nr.width = w;
            float t = EditorGUIUtility.labelWidth;
            int l = EditorGUI.indentLevel;
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = 0;
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = EditorGUI.IntField(nr, subLabels[i], values[i]);
                nr.x += w + kSpacingSubLabel;
            }
            EditorGUIUtility.labelWidth = t;
            EditorGUI.indentLevel = l;
        }

        public static void MultiPropertyField(Rect position, GUIContent[] subLabels, SerializedProperty valuesIterator, GUIContent label)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = MultiFieldPrefixLabel(position, id, label, subLabels.Length);
            position.height = kSingleLineHeight;
            MultiPropertyField(position, subLabels, valuesIterator);
        }

        public static void MultiPropertyField(Rect position, GUIContent[] subLabels, SerializedProperty valuesIterator)
        {
            MultiPropertyField(position, subLabels, valuesIterator, PropertyVisibility.OnlyVisible);
        }

        internal enum PropertyVisibility
        {
            All,
            OnlyVisible
        }

        private static void MultiPropertyField(Rect position, GUIContent[] subLabels, SerializedProperty valuesIterator, PropertyVisibility visibility)
        {
            MultiPropertyField(position, subLabels, valuesIterator, visibility, kMiniLabelW, null);
        }

        internal static void MultiPropertyField(Rect position, GUIContent[] subLabels, SerializedProperty valuesIterator, PropertyVisibility visibility, float labelWidth, bool[] disabledMask)
        {
            int eCount = subLabels.Length;
            float w = (position.width - (eCount - 1) * kSpacingSubLabel) / eCount;
            Rect nr = new Rect(position);
            nr.width = w;
            float t = EditorGUIUtility.labelWidth;
            int l = EditorGUI.indentLevel;
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.indentLevel = 0;
            for (int i = 0; i < subLabels.Length; i++)
            {
                if (disabledMask != null)
                    BeginDisabled(disabledMask[i]);
                EditorGUI.PropertyField(nr, valuesIterator, subLabels[i]);
                if (disabledMask != null)
                    EndDisabled();
                nr.x += w + kSpacingSubLabel;

                switch (visibility)
                {
                    case PropertyVisibility.All:
                        valuesIterator.Next(false);
                        break;

                    case PropertyVisibility.OnlyVisible:
                        valuesIterator.NextVisible(false);
                        break;
                }
            }
            EditorGUIUtility.labelWidth = t;
            EditorGUI.indentLevel = l;
        }

        // Make a property field that look like a multi property field (but is made up of individual properties)
        internal static void PropertiesField(Rect position, GUIContent label, SerializedProperty[] properties, GUIContent[] propertyLabels, float propertyLabelsWidth)
        {
            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            Rect fieldPosition = PrefixLabel(position, id, label);
            fieldPosition.height = kSingleLineHeight;

            float oldLabelWidth = EditorGUIUtility.labelWidth;
            int oldIndentLevel = EditorGUI.indentLevel;

            EditorGUIUtility.labelWidth = propertyLabelsWidth;
            EditorGUI.indentLevel = 0;
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyField(fieldPosition, properties[i], propertyLabels[i]);
                fieldPosition.y += kSingleLineHeight + kVerticalSpacingMultiField;
            }

            EditorGUI.indentLevel = oldIndentLevel;
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        internal static int CycleButton(Rect position, int selected, GUIContent[] options, GUIStyle style)
        {
            if (selected >= options.Length || selected < 0)
            {
                selected = 0;
                GUI.changed = true;
            }
            if (GUI.Button(position, options[selected], style))
            {
                selected++;
                GUI.changed = true;
                if (selected >= options.Length)
                {
                    selected = 0;
                }
            }
            return selected;
        }

        /// *listonly*
        public static Color ColorField(Rect position, Color value)
        {
            int id = GUIUtility.GetControlID(s_ColorHash, FocusType.Keyboard, position);
            return DoColorField(IndentedRect(position), id, value, true, true, false, null);
        }

        /// *listonly*
        internal static Color ColorField(Rect position, Color value, bool showEyedropper, bool showAlpha)
        {
            int id = GUIUtility.GetControlID(s_ColorHash, FocusType.Keyboard, position);
            return DoColorField(position, id, value, showEyedropper, showAlpha, false, null);
        }

        /// *listonly*
        public static Color ColorField(Rect position, string label, Color value)
        {
            return ColorField(position, EditorGUIUtility.TempContent(label), value);
        }

        // Make a field for selecting a [[Color]].
        public static Color ColorField(Rect position, GUIContent label, Color value)
        {
            int id = GUIUtility.GetControlID(s_ColorHash, FocusType.Keyboard, position);
            return DoColorField(PrefixLabel(position, id, label), id, value, true, true, false, null);
        }

        /// *listonly*
        internal static Color ColorField(Rect position, GUIContent label, Color value, bool showEyedropper, bool showAlpha)
        {
            int id = GUIUtility.GetControlID(s_ColorHash, FocusType.Keyboard, position);
            return DoColorField(PrefixLabel(position, id, label), id, value, showEyedropper, showAlpha, false, null);
        }

        public static Color ColorField(Rect position, GUIContent label, Color value, bool showEyedropper, bool showAlpha, bool hdr, ColorPickerHDRConfig hdrConfig)
        {
            int id = GUIUtility.GetControlID(s_ColorHash, FocusType.Keyboard, position);
            return DoColorField(PrefixLabel(position, id, label), id, value, showEyedropper, showAlpha, hdr, hdrConfig);
        }

        private static Color DoColorField(Rect position, int id, Color value, bool showEyedropper, bool showAlpha, bool hdr, ColorPickerHDRConfig hdrConfig)
        {
            Event evt = Event.current;
            GUIStyle style = EditorStyles.colorField;
            Color origColor = value;
            value = showMixedValue ? Color.white : value;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (showEyedropper)
                    {
                        position.width -= 20;
                    }

                    if (position.Contains(evt.mousePosition))
                    {
                        switch (evt.button)
                        {
                            case 0:
                                // Left click: Show the ColorPicker
                                GUIUtility.keyboardControl = id;
                                showMixedValue = false;
                                ColorPicker.Show(GUIView.current, value, showAlpha, hdr, hdrConfig);
                                GUIUtility.ExitGUI();
                                break;

                            case 1:
                                // Right click: Show color context menu
                                // See ExecuteCommand section below to see handling for copy & paste
                                GUIUtility.keyboardControl = id;

                                var names = new[] {"Copy", "Paste"};
                                var enabled = new[] {true, ColorClipboard.HasColor()};

                                EditorUtility.DisplayCustomMenu(
                                position,
                                names,
                                enabled,
                                null,
                                delegate(object data, string[] options, int selected)
                            {
                                if (selected == 0)
                                {
                                    Event e = EditorGUIUtility.CommandEvent("Copy");
                                    GUIView.current.SendEvent(e);
                                }
                                else if (selected == 1)
                                {
                                    Event e = EditorGUIUtility.CommandEvent("Paste");
                                    GUIView.current.SendEvent(e);
                                }
                            },
                                null);
                                return origColor;
                        }
                    }

                    if (showEyedropper)
                    {
                        position.width += 20;
                        if (position.Contains(evt.mousePosition))
                        {
                            GUIUtility.keyboardControl = id;
                            EyeDropper.Start(GUIView.current);
                            s_ColorPickID = id;
                            GUIUtility.ExitGUI();
                        }
                    }
                    break;
                case EventType.Repaint:
                    Rect position2;
                    if (showEyedropper)
                    {
                        position2 = style.padding.Remove(position);
                    }
                    else
                    {
                        position2 = position;
                    }

                    // Draw color field
                    if (showEyedropper && s_ColorPickID == id)
                    {
                        Color c = EyeDropper.GetPickedColor();
                        c.a = value.a;
                        EditorGUIUtility.DrawColorSwatch(position2, c, showAlpha, hdr);
                    }
                    else
                    {
                        EditorGUIUtility.DrawColorSwatch(position2, value, showAlpha, hdr);
                    }

                    if (showEyedropper)
                    {
                        style.Draw(position, GUIContent.none, id); // Draw box outline and eyedropper icon
                    }
                    else
                    {
                        EditorStyles.colorPickerBox.Draw(position, GUIContent.none, id); // Draw box outline
                    }

                    break;

                case EventType.ValidateCommand:

                    switch (evt.commandName)
                    {
                        case "UndoRedoPerformed":
                            // Set color in ColorPicker in case an undo/redo has been made
                            if (GUIUtility.keyboardControl == id && ColorPicker.visible)
                                ColorPicker.color = value;
                            break;

                        case "Copy":
                        case "Paste":
                            evt.Use();
                            break;
                    }
                    break;

                case EventType.ExecuteCommand:

                    if (GUIUtility.keyboardControl == id)
                    {
                        switch (evt.commandName)
                        {
                            case "EyeDropperUpdate":
                                HandleUtility.Repaint();
                                break;
                            case "EyeDropperClicked":
                            {
                                GUI.changed = true;
                                HandleUtility.Repaint();
                                Color c = EyeDropper.GetLastPickedColor();
                                c.a = value.a;
                                s_ColorPickID = 0;
                                return c;
                            }
                            case "EyeDropperCancelled":
                                HandleUtility.Repaint();
                                s_ColorPickID = 0;
                                break;
                            case "ColorPickerChanged":
                                GUI.changed = true;
                                HandleUtility.Repaint();
                                return ColorPicker.color;

                            case "Copy":
                                ColorClipboard.SetColor(value);
                                evt.Use();
                                break;

                            case "Paste":
                                Color colorFromClipboard;
                                if (ColorClipboard.TryGetColor(hdr, out colorFromClipboard))
                                {
                                    // Do not change alpha if color field is not showing alpha
                                    if (!showAlpha)
                                        colorFromClipboard.a = origColor.a;

                                    origColor = colorFromClipboard;

                                    GUI.changed = true;
                                    evt.Use();
                                }
                                break;
                        }
                    }
                    break;
                case EventType.KeyDown:
                    if (evt.MainActionKeyForControl(id))
                    {
                        Event.current.Use();
                        showMixedValue = false;
                        ColorPicker.Show(GUIView.current, value, showAlpha, hdr, hdrConfig);
                        GUIUtility.ExitGUI();
                    }
                    break;
            }
            return origColor;
        }

        internal static Color ColorSelector(Rect activatorRect, Rect renderRect, int id, Color value)
        {
            Event evt = Event.current;
            Color origColor = value;
            value = showMixedValue ? Color.white : value;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (activatorRect.Contains(evt.mousePosition))
                    {
                        evt.Use();
                        GUIUtility.keyboardControl = id;
                        showMixedValue = false;
                        ColorPicker.Show(GUIView.current, value, false, false, null);
                        GUIUtility.ExitGUI();
                    }
                    break;
                case EventType.Repaint:

                    if (renderRect.height > 0f && renderRect.width > 0)
                        DrawRect(renderRect, value);
                    break;

                case EventType.ValidateCommand:
                    if (evt.commandName == "UndoRedoPerformed")
                    {
                        // Set color in ColorPicker in case an undo/redo has been made
                        if (GUIUtility.keyboardControl == id && ColorPicker.visible)
                            ColorPicker.color = value;
                    }
                    break;

                case EventType.ExecuteCommand:
                    if (GUIUtility.keyboardControl == id)
                    {
                        switch (evt.commandName)
                        {
                            case "ColorPickerChanged":
                            {
                                evt.Use();
                                GUI.changed = true;
                                HandleUtility.Repaint();
                                return ColorPicker.color;
                            }
                        }
                    }
                    break;
                case EventType.KeyDown:
                    if (evt.MainActionKeyForControl(id))
                    {
                        evt.Use();
                        showMixedValue = false;
                        ColorPicker.Show(GUIView.current, value, false, false, null);
                        GUIUtility.ExitGUI();
                    }
                    break;
            }
            return origColor;
        }

        /// *listonly*
        public static AnimationCurve CurveField(Rect position, AnimationCurve value)
        {
            int id = GUIUtility.GetControlID(s_CurveHash, FocusType.Keyboard, position);
            return DoCurveField(IndentedRect(position), id, value, kCurveColor, new Rect(), null);
        }

        /// *listonly*
        public static AnimationCurve CurveField(Rect position, string label, AnimationCurve value)
        {
            return CurveField(position, EditorGUIUtility.TempContent(label), value);
        }

        /// *listonly*
        public static AnimationCurve CurveField(Rect position, GUIContent label, AnimationCurve value)
        {
            int id = GUIUtility.GetControlID(s_CurveHash, FocusType.Keyboard, position);
            return DoCurveField(PrefixLabel(position, id, label), id, value, kCurveColor, new Rect(), null);
        }

        // Variants with settings
        /// *listonly*
        public static AnimationCurve CurveField(Rect position, AnimationCurve value, Color color, Rect ranges)
        {
            int id = GUIUtility.GetControlID(s_CurveHash, FocusType.Keyboard, position);
            return DoCurveField(IndentedRect(position), id, value, color, ranges, null);
        }

        /// *listonly*
        public static AnimationCurve CurveField(Rect position, string label, AnimationCurve value, Color color, Rect ranges)
        {
            return CurveField(position, EditorGUIUtility.TempContent(label), value, color, ranges);
        }

        // Make a field for editing an [[AnimationCurve]].
        public static AnimationCurve CurveField(Rect position, GUIContent label, AnimationCurve value, Color color, Rect ranges)
        {
            int id = GUIUtility.GetControlID(s_CurveHash, FocusType.Keyboard, position);
            return DoCurveField(PrefixLabel(position, id, label), id, value, color, ranges, null);
        }

        /// *listonly*
        public static void CurveField(Rect position, SerializedProperty property, Color color, Rect ranges)
        {
            CurveField(position, property, color, ranges, null);
        }

        // Make a field for editing an [[AnimationCurve]].
        public static void CurveField(Rect position, SerializedProperty property, Color color, Rect ranges, GUIContent label)
        {
            label = BeginProperty(position, label, property);

            int id = GUIUtility.GetControlID(s_CurveHash, FocusType.Keyboard, position);
            DoCurveField(PrefixLabel(position, id, label), id, null, color, ranges, property);

            EndProperty();
        }

        private static void SetCurveEditorWindowCurve(AnimationCurve value, SerializedProperty property, Color color)
        {
            if (property != null)
            {
                CurveEditorWindow.curve = property.hasMultipleDifferentValues ? new AnimationCurve() : property.animationCurveValue;
            }
            else
            {
                CurveEditorWindow.curve = value;
            }
            CurveEditorWindow.color = color;
        }

        private static AnimationCurve DoCurveField(Rect position, int id, AnimationCurve value, Color color, Rect ranges, SerializedProperty property)
        {
            Event evt = Event.current;

            // Avoid crash problems caused by textures with zero or negative sizes!
            position.width = Mathf.Max(position.width, 2);
            position.height = Mathf.Max(position.height, 2);

            if (GUIUtility.keyboardControl == id && Event.current.type != EventType.Layout)
            {
                if (s_CurveID != id)
                {
                    s_CurveID = id;
                    if (CurveEditorWindow.visible)
                    {
                        SetCurveEditorWindowCurve(value, property, color);
                        ShowCurvePopup(GUIView.current, ranges);
                    }
                }
                else
                {
                    if (CurveEditorWindow.visible && Event.current.type == EventType.Repaint)
                    {
                        SetCurveEditorWindowCurve(value, property, color);
                        CurveEditorWindow.instance.Repaint();
                    }
                }
            }

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (position.Contains(evt.mousePosition))
                    {
                        s_CurveID = id;
                        GUIUtility.keyboardControl = id;
                        SetCurveEditorWindowCurve(value, property, color);
                        ShowCurvePopup(GUIView.current, ranges);
                        evt.Use();
                        GUIUtility.ExitGUI();
                    }
                    break;
                case EventType.Repaint:
                    Rect position2 = position;
                    position2.y += 1;
                    position2.height -= 1;
                    if (ranges != new Rect())
                    {
                        EditorGUIUtility.DrawCurveSwatch(position2, value, property, color, kCurveBGColor, ranges);
                    }
                    else
                    {
                        EditorGUIUtility.DrawCurveSwatch(position2, value, property, color, kCurveBGColor);
                    }
                    EditorStyles.colorPickerBox.Draw(position2, GUIContent.none, id, false);
                    break;
                case EventType.ExecuteCommand:
                    if (s_CurveID == id)
                    {
                        switch (evt.commandName)
                        {
                            case "CurveChanged":
                                GUI.changed = true;
                                AnimationCurvePreviewCache.ClearCache();
                                HandleUtility.Repaint();
                                if (property != null)
                                {
                                    property.animationCurveValue = CurveEditorWindow.curve;
                                    if (property.hasMultipleDifferentValues)
                                    {
                                        Debug.LogError("AnimationCurve SerializedProperty hasMultipleDifferentValues is true after writing.");
                                    }
                                }
                                return CurveEditorWindow.curve;
                        }
                    }
                    break;
                case EventType.KeyDown:
                    if (evt.MainActionKeyForControl(id))
                    {
                        s_CurveID = id;
                        SetCurveEditorWindowCurve(value, property, color);
                        ShowCurvePopup(GUIView.current, ranges);
                        evt.Use();
                        GUIUtility.ExitGUI();
                    }

                    break;
            }
            return value;
        }

        private static void ShowCurvePopup(GUIView viewToUpdate, Rect ranges)
        {
            CurveEditorSettings settings = new CurveEditorSettings();
            if (ranges.width > 0 && ranges.height > 0 && ranges.width != Mathf.Infinity && ranges.height != Mathf.Infinity)
            {
                settings.hRangeMin = ranges.xMin;
                settings.hRangeMax = ranges.xMax;
                settings.vRangeMin = ranges.yMin;
                settings.vRangeMax = ranges.yMax;
            }
            CurveEditorWindow.instance.Show(GUIView.current, settings);
        }

        private static bool ValidTargetForIconSelection(Object[] targets)
        {
            // @TODO: Handle icon dropdown for multiple objects.
            return ((targets[0] as MonoScript) || (targets[0] as GameObject)) && targets.Length == 1;
        }

        internal static void ObjectIconDropDown(Rect position, Object[] targets, bool showLabelIcons, Texture2D nullIcon, SerializedProperty iconProperty)
        {
            if (s_IconTextureInactive == null)
            {
                s_IconTextureInactive = (Material)EditorGUIUtility.LoadRequired("Inspectors/InactiveGUI.mat");
            }
            if (Event.current.type == EventType.Repaint)
            {
                Texture2D icon = null;
                if (!iconProperty.hasMultipleDifferentValues)
                {
                    icon = AssetPreview.GetMiniThumbnail(targets[0]);
                }
                if (icon == null)
                {
                    icon = nullIcon;
                }
                Vector2 iconSize = new Vector2(position.width, position.height);
                if (icon)
                {
                    iconSize.x = Mathf.Min(icon.width, iconSize.x);
                    iconSize.y = Mathf.Min(icon.height, iconSize.y);
                }
                Rect iconRect = new Rect(position.x + position.width / 2 - iconSize.x / 2, position.y + position.height / 2 - iconSize.y / 2 , iconSize.x, iconSize.y);
                GameObject obj = targets[0] as GameObject;

                bool isInactive = obj && (!EditorUtility.IsPersistent(targets[0]) && (!obj.activeSelf || !obj.activeInHierarchy));

                if (isInactive)
                {
                    var imageAspect = (float)icon.width / (float)icon.height;
                    Rect iconScreenRect = new Rect();
                    Rect sourceRect = new Rect();
                    GUI.CalculateScaledTextureRects(iconRect, ScaleMode.ScaleToFit, imageAspect, ref iconScreenRect, ref sourceRect);
                    Graphics.DrawTexture(iconScreenRect, icon, sourceRect, 0, 0, 0, 0, new Color(.5f, .5f, .5f, 1f), s_IconTextureInactive);
                }
                else
                {
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                }

                if (ValidTargetForIconSelection(targets))
                {
                    if (s_IconDropDown == null)
                    {
                        s_IconDropDown = EditorGUIUtility.IconContent("Icon Dropdown");
                    }
                    GUIStyle.none.Draw(new Rect(Mathf.Max(position.x + 2, iconRect.x - 6), iconRect.yMax - iconRect.height * 0.2f, 13, 8), s_IconDropDown, false, false, false, false);
                }
            }

            if (EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Passive, GUIStyle.none))
            {
                if (ValidTargetForIconSelection(targets))
                {
                    if (IconSelector.ShowAtPosition(targets[0], position, showLabelIcons))
                    {
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        // Titlebar without foldout
        public static void InspectorTitlebar(Rect position, Object[] targetObjs)
        {
            GUIStyle baseStyle = GUIStyle.none;
            int id = GUIUtility.GetControlID(s_TitlebarHash, FocusType.Keyboard, position);
            DoInspectorTitlebar(position, id, true, targetObjs, baseStyle);
        }

        /// *listonly*
        public static bool InspectorTitlebar(Rect position, bool foldout, Object targetObj, bool expandable)
        {
            return InspectorTitlebar(position, foldout, new Object[] {targetObj}, expandable);
        }

        // foldable titlebar.
        public static bool InspectorTitlebar(Rect position, bool foldout, Object[] targetObjs, bool expandable)
        {
            GUIStyle baseStyle = EditorStyles.inspectorTitlebar;

            // Important to get controlId for the foldout first, so it gets keyboard focus before the toggle does.
            int id = GUIUtility.GetControlID(s_TitlebarHash, FocusType.Keyboard, position);
            DoInspectorTitlebar(position, id, foldout, targetObjs, baseStyle);
            foldout = DoObjectMouseInteraction(foldout, position, targetObjs, id);

            if (expandable)
            {
                Rect renderRect = GetInspectorTitleBarObjectFoldoutRenderRect(position);
                DoObjectFoldoutInternal(foldout, position, renderRect, targetObjs, id);
            }

            return foldout;
        }

        // Make an inspector-window-like titlebar.
        internal static void DoInspectorTitlebar(Rect position, int id, bool foldout, Object[] targetObjs, GUIStyle baseStyle)
        {
            GUIStyle textStyle = EditorStyles.inspectorTitlebarText;
            GUIStyle iconButtonStyle = EditorStyles.iconButton;

            Vector2 settingsElementSize = iconButtonStyle.CalcSize(GUIContents.titleSettingsIcon);

            Rect iconRect = new Rect(position.x + baseStyle.padding.left, position.y + baseStyle.padding.top, kInspTitlebarIconWidth, kInspTitlebarIconWidth);
            Rect settingsRect = new Rect(position.xMax - baseStyle.padding.right - kInspTitlebarSpacing - kInspTitlebarIconWidth, iconRect.y, settingsElementSize.x, settingsElementSize.y);
            Rect textRect = new Rect(iconRect.xMax + kInspTitlebarSpacing + kInspTitlebarSpacing + kInspTitlebarIconWidth, iconRect.y, 100, iconRect.height);
            textRect.xMax = settingsRect.xMin - kInspTitlebarSpacing;

            Event evt = Event.current;

            int enabled = -1;
            foreach (Object targetObj in targetObjs)
            {
                int thisEnabled = EditorUtility.GetObjectEnabled(targetObj);
                if (enabled == -1)
                {
                    enabled = thisEnabled;
                }
                else if (enabled != thisEnabled)
                {
                    enabled = -2;
                }
            }

            if (enabled != -1)
            {
                bool enabledState = enabled != 0;
                EditorGUI.showMixedValue = enabled == -2;
                Rect toggleRect = iconRect;
                toggleRect.x = iconRect.xMax + kInspTitlebarSpacing;
                BeginChangeCheck();

                Color previousColor = GUI.backgroundColor;
                bool animated = AnimationMode.IsPropertyAnimated(targetObjs[0], kEnabledPropertyName);
                if (animated)
                {
                    Color animatedColor = AnimationMode.animatedPropertyColor;
                    if (AnimationMode.InAnimationRecording())
                        animatedColor = AnimationMode.recordedPropertyColor;
                    else if (AnimationMode.IsPropertyCandidate(targetObjs[0], kEnabledPropertyName))
                        animatedColor = AnimationMode.candidatePropertyColor;

                    animatedColor.a *= GUI.color.a;
                    GUI.backgroundColor = animatedColor;
                }

                int toggleId = GUIUtility.GetControlID(s_TitlebarHash, FocusType.Keyboard, position);
                enabledState = EditorGUIInternal.DoToggleForward(toggleRect, toggleId, enabledState, GUIContent.none, EditorStyles.toggle);

                if (animated)
                {
                    GUI.backgroundColor = previousColor;
                }

                if (EndChangeCheck())
                {
                    Undo.RecordObjects(targetObjs, (enabledState ? "Enable" : "Disable") + " Component" + (targetObjs.Length > 1 ? "s" : ""));

                    foreach (Object targetObj in targetObjs)
                    {
                        EditorUtility.SetObjectEnabled(targetObj, enabledState);
                    }
                }

                EditorGUI.showMixedValue = false;

                if (toggleRect.Contains(Event.current.mousePosition))
                {
                    // It's necessary to handle property context menu here in right mouse down because
                    // component contextual menu will override this otherwise.
                    if ((evt.type == EventType.MouseDown && evt.button == 1) || evt.type == EventType.ContextClick)
                    {
                        SerializedObject serializedObject = new SerializedObject(targetObjs[0]);
                        DoPropertyContextMenu(serializedObject.FindProperty(kEnabledPropertyName));
                        evt.Use();
                    }
                }
            }

            Rect headerItemRect = settingsRect;
            headerItemRect.x -= kInspTitlebarIconWidth + kInspTitlebarSpacing;
            headerItemRect = EditorGUIUtility.DrawEditorHeaderItems(headerItemRect, targetObjs);
            textRect.xMax = headerItemRect.xMin - kInspTitlebarSpacing;


            if (evt.type == EventType.Repaint)
            {
                var icon = AssetPreview.GetMiniThumbnail(targetObjs[0]);
                GUIStyle.none.Draw(iconRect, EditorGUIUtility.TempContent(icon), false, false, false, false);
            }

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (settingsRect.Contains(evt.mousePosition))
                    {
                        EditorUtility.DisplayObjectContextMenu(settingsRect, targetObjs, 0);
                        evt.Use();
                    }
                    break;
                case EventType.Repaint:
                    baseStyle.Draw(position, GUIContent.none, id, foldout);
                    position = baseStyle.padding.Remove(position);
                    textStyle.Draw(textRect, EditorGUIUtility.TempContent(ObjectNames.GetInspectorTitle(targetObjs[0])), id, foldout);
                    iconButtonStyle.Draw(settingsRect, GUIContents.titleSettingsIcon, id, foldout);
                    break;
            }
        }

        internal static bool ToggleTitlebar(Rect position, GUIContent label, bool foldout, ref bool toggleValue)
        {
            // Important to get controlId for the foldout first, so it gets keyboard focus before the toggle does.
            int id = GUIUtility.GetControlID(s_TitlebarHash, FocusType.Keyboard, position);

            GUIStyle baseStyle = EditorStyles.inspectorTitlebar;
            GUIStyle textStyle = EditorStyles.inspectorTitlebarText;
            GUIStyle foldoutStyle = EditorStyles.foldout;

            Rect toggleRect = new Rect(position.x + baseStyle.padding.left, position.y + baseStyle.padding.top, kInspTitlebarIconWidth, kInspTitlebarIconWidth);
            Rect textRect = new Rect(toggleRect.xMax + kInspTitlebarSpacing, toggleRect.y, 200, kInspTitlebarIconWidth);

            int toggleId = GUIUtility.GetControlID(s_TitlebarHash, FocusType.Keyboard, position);

            toggleValue = EditorGUIInternal.DoToggleForward(toggleRect, toggleId, toggleValue, GUIContent.none, EditorStyles.toggle);

            if (Event.current.type == EventType.Repaint)
            {
                baseStyle.Draw(position, GUIContent.none, id, foldout);
                foldoutStyle.Draw(GetInspectorTitleBarObjectFoldoutRenderRect(position), GUIContent.none, id, foldout);
                position = baseStyle.padding.Remove(position);
                textStyle.Draw(textRect, label, id, foldout);
            }

            return EditorGUIInternal.DoToggleForward(IndentedRect(position), id, foldout, GUIContent.none, GUIStyle.none);
        }


        internal static bool FoldoutTitlebar(Rect position, GUIContent label, bool foldout, bool skipIconSpacing)
        {
            // Important to get controlId for the foldout first, so it gets keyboard focus before the toggle does.
            int id = GUIUtility.GetControlID(s_TitlebarHash, FocusType.Keyboard, position);

            if (Event.current.type == EventType.Repaint)
            {
                GUIStyle baseStyle = EditorStyles.inspectorTitlebar;
                GUIStyle textStyle = EditorStyles.inspectorTitlebarText;
                GUIStyle foldoutStyle = EditorStyles.foldout;
                Rect textRect = new Rect(position.x + baseStyle.padding.left + kInspTitlebarSpacing + (skipIconSpacing ? 0 : kInspTitlebarIconWidth), position.y + baseStyle.padding.top, 200, kInspTitlebarIconWidth);

                baseStyle.Draw(position, GUIContent.none, id, foldout);
                foldoutStyle.Draw(GetInspectorTitleBarObjectFoldoutRenderRect(position), GUIContent.none, id, foldout);
                position = baseStyle.padding.Remove(position);
                textStyle.Draw(textRect, EditorGUIUtility.TempContent(label.text), id, foldout);
            }

            return EditorGUIInternal.DoToggleForward(IndentedRect(position), id, foldout, GUIContent.none, GUIStyle.none);
        }

        [EditorHeaderItem(typeof(Object), -1000)]
        internal static bool HelpIconButton(Rect position, Object[] objs)
        {
            var obj = objs[0];
            bool isDevBuild = Unsupported.IsDeveloperBuild();
            // For efficiency, only check in development builds if this script is a user script.
            bool monoBehaviourFallback = !isDevBuild;
            if (!monoBehaviourFallback)
            {
                EditorCompilation.TargetAssemblyInfo[] allTargetAssemblies = EditorCompilationInterface.GetTargetAssemblies();

                string assemblyName = obj.GetType().Assembly.ToString();
                for (int i = 0; i < allTargetAssemblies.Length; ++i)
                {
                    if (assemblyName == allTargetAssemblies[i].Name)
                    {
                        monoBehaviourFallback = true;
                        break;
                    }
                }
            }
            bool hasHelp = Help.HasHelpForObject(obj, monoBehaviourFallback);
            if (hasHelp || isDevBuild)
            {
                Color oldColor = GUI.color;
                GUIContent content = new GUIContent(EditorGUI.GUIContents.helpIcon);
                string helpTopic = Help.GetNiceHelpNameForObject(obj, monoBehaviourFallback);
                if (isDevBuild && !hasHelp)
                {
                    GUI.color = Color.yellow;
                    bool isScript = obj is MonoBehaviour;
                    string pageName = (isScript ? "script-" : "sealed partial class-") + helpTopic;

                    content.tooltip = string.Format(
                            @"Could not find Reference page for {0} ({1}).
Docs for this object is missing or all docs are missing.
This warning only shows up in development builds.", helpTopic, pageName);
                }
                else
                {
                    content.tooltip = string.Format("Open Reference for {0}.", helpTopic);
                }

                GUIStyle helpIconStyle = EditorStyles.iconButton;
                if (GUI.Button(position, content, helpIconStyle))
                {
                    Help.ShowHelpForObject(obj);
                }

                GUI.color = oldColor;
                return true;
            }
            return false;
        }

        // Make a label with a foldout arrow to the left of it.
        internal static bool FoldoutInternal(Rect position, bool foldout, GUIContent content, bool toggleOnLabelClick, GUIStyle style)
        {
            Rect origPosition = position;
            if (EditorGUIUtility.hierarchyMode)
            {
                int offset = (EditorStyles.foldout.padding.left - EditorStyles.label.padding.left);
                position.xMin -= offset;
            }

            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            EventType eventType = Event.current.type;
            // special case test, so we are able to receive mouse events when we are disabled. This allows the foldout to still be expanded/contracted when disabled.
            if (!GUI.enabled && GUIClip.enabled && (Event.current.rawType == EventType.MouseDown || Event.current.rawType == EventType.MouseDrag || Event.current.rawType == EventType.MouseUp))
            {
                eventType = Event.current.rawType;
            }
            switch (eventType)
            {
                case EventType.MouseDown:
                    // If the mouse is inside the button, we say that we're the hot control
                    if (position.Contains(Event.current.mousePosition) && Event.current.button == 0)
                    {
                        GUIUtility.keyboardControl = GUIUtility.hotControl = id;
                        Event.current.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;

                        // If we got the mousedown, the mouseup is ours as well
                        // (no matter if the click was in the button or not)
                        Event.current.Use();

                        // toggle the passed-in value if the mouse was over the button & return true
                        Rect clickRect = position;
                        if (!toggleOnLabelClick)
                        {
                            clickRect.width = style.padding.left;
                            clickRect.x += indent;
                        }
                        if (clickRect.Contains(Event.current.mousePosition))
                        {
                            GUI.changed = true;
                            return !foldout;
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        Event.current.Use();
                    }
                    break;
                case EventType.Repaint:
                    EditorStyles.foldoutSelected.Draw(position, GUIContent.none, id, s_DragUpdatedOverID == id);

                    Rect drawRect = new Rect(position.x + indent, position.y, EditorGUIUtility.labelWidth - indent, position.height);

                    // If mixed values, indicate it in the collapsed foldout field so it's easy to see at a glance if anything
                    // in the Inspector has different values. Don't show it when expanded, since the difference will be visible further down.
                    if (showMixedValue && !foldout)
                    {
                        style.Draw(drawRect, content, id, foldout);

                        BeginHandleMixedValueContentColor();
                        Rect fieldPosition = origPosition;
                        fieldPosition.xMin += EditorGUIUtility.labelWidth;
                        EditorStyles.label.Draw(fieldPosition, s_MixedValueContent, id, false);
                        EndHandleMixedValueContentColor();
                    }
                    else
                    {
                        style.Draw(drawRect, content, id, foldout);
                    }

                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl == id)
                    {
                        KeyCode kc = Event.current.keyCode;
                        if ((kc == KeyCode.LeftArrow && foldout == true) || (kc == KeyCode.RightArrow && foldout == false))
                        {
                            foldout = !foldout;
                            GUI.changed = true;
                            Event.current.Use();
                        }
                    }
                    break;
                case EventType.DragUpdated:
                    if (s_DragUpdatedOverID == id)
                    {
                        if (position.Contains(Event.current.mousePosition))
                        {
                            if (Time.realtimeSinceStartup > s_FoldoutDestTime)
                            {
                                foldout = true;
                                Event.current.Use();
                            }
                        }
                        else
                        {
                            s_DragUpdatedOverID = 0;
                        }
                    }
                    else
                    {
                        if (position.Contains(Event.current.mousePosition))
                        {
                            s_DragUpdatedOverID = id;
                            s_FoldoutDestTime = Time.realtimeSinceStartup + kFoldoutExpandTimeout;
                            Event.current.Use();
                        }
                    }
                    break;
                case EventType.DragExited:
                    if (s_DragUpdatedOverID == id)
                    {
                        s_DragUpdatedOverID = 0;
                        Event.current.Use();
                    }
                    break;
            }
            return foldout;
        }

        // Make a progress bar.
        public static void ProgressBar(Rect position, float value, string text)
        {
            int id = GUIUtility.GetControlID(s_ProgressBarHash, FocusType.Keyboard, position);
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Repaint:
                    EditorStyles.progressBarBack.Draw(position, false, false, false, false);
                    Rect barRect = new Rect(position);
                    value = Mathf.Clamp01(value);
                    barRect.width *= value;
                    EditorStyles.progressBarBar.Draw(barRect, false, false, false, false);
                    EditorStyles.progressBarText.Draw(position, text, false, false, false, false);
                    break;
            }
        }

        // Make a help box with a message to the user.
        public static void HelpBox(Rect position, string message, MessageType type)
        {
            GUI.Label(position, EditorGUIUtility.TempContent(message, EditorGUIUtility.GetHelpIcon(type)), EditorStyles.helpBox);
        }

        internal static bool LabelHasContent(GUIContent label)
        {
            if (label == null)
            {
                return true;
            }
            // @TODO: find out why checking for GUIContent.none doesn't work
            return label.text != string.Empty || label.image != null;
        }

        private static void DrawTextDebugHelpers(Rect labelPosition)
        {
            Color oldColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(labelPosition.x, labelPosition.y, labelPosition.width, 4), EditorGUIUtility.whiteTexture);
            GUI.color = Color.cyan;
            GUI.DrawTexture(new Rect(labelPosition.x, labelPosition.yMax - 4, labelPosition.width, 4), EditorGUIUtility.whiteTexture);
            GUI.color = oldColor;
        }

        internal static void PrepareCurrentPrefixLabel(int controlId)
        {
            if (s_HasPrefixLabel)
            {
                if (!string.IsNullOrEmpty(s_PrefixLabel.text))
                {
                    Color curColor = GUI.color;
                    GUI.color = s_PrefixGUIColor;
                    EditorGUI.HandlePrefixLabel(EditorGUI.s_PrefixTotalRect, EditorGUI.s_PrefixRect, EditorGUI.s_PrefixLabel, controlId, EditorGUI.s_PrefixStyle);
                    GUI.color = curColor;
                }

                s_HasPrefixLabel = false;
            }
        }

        // Draw a prefix label and select the corresponding control if the label is clicked.
        // If no id or an id of 0 is specified, the id of the next control will be used.
        // For regular inline controls, the PrefixLabel method should be used,
        // but HandlePrefixLabel can be used for customized label placement.
        internal static void HandlePrefixLabelInternal(Rect totalPosition, Rect labelPosition, GUIContent label, int id, GUIStyle style)
        {
            // If we don't know the controlID at this time, delay the handling of the prefix label until the next GUIUtility.GetControlID
            if (id == 0 && label != null)
            {
                s_PrefixLabel.text = label.text;
                s_PrefixLabel.image = label.image;
                s_PrefixLabel.tooltip = label.tooltip;
                s_PrefixTotalRect = totalPosition;
                s_PrefixRect = labelPosition;
                s_PrefixStyle = style;
                s_PrefixGUIColor = GUI.color;
                s_HasPrefixLabel = true;
                return;
            }

            // Control highlighting
            if (Highlighter.searchMode == HighlightSearchMode.PrefixLabel ||
                Highlighter.searchMode == HighlightSearchMode.Auto)
            {
                Highlighter.Handle(totalPosition, label.text);
            }

            //DrawTextDebugHelpers (labelPosition);

            switch (Event.current.type)
            {
                case EventType.Repaint:
                    labelPosition.width += 1;
                    style.DrawPrefixLabel(labelPosition, label, id);
                    break;
                case EventType.MouseDown:
                    if (Event.current.button == 0 && labelPosition.Contains(Event.current.mousePosition))
                    {
                        if (EditorGUIUtility.CanHaveKeyboardFocus(id))
                            GUIUtility.keyboardControl = id;
                        EditorGUIUtility.editingTextField = false;
                        HandleUtility.Repaint();
                    }
                    break;
            }
        }

        // Make a label in front of some control.
        public static Rect PrefixLabel(Rect totalPosition, GUIContent label)
        {
            return PrefixLabel(totalPosition, 0, label, EditorStyles.label);
        }

        public static Rect PrefixLabel(Rect totalPosition, GUIContent label, GUIStyle style)
        {
            return PrefixLabel(totalPosition, 0, label, style);
        }

        public static Rect PrefixLabel(Rect totalPosition, int id, GUIContent label)
        {
            return PrefixLabel(totalPosition, id, label, EditorStyles.label);
        }

        public static Rect PrefixLabel(Rect totalPosition, int id, GUIContent label, GUIStyle style)
        {
            if (!LabelHasContent(label))
            {
                return IndentedRect(totalPosition);
            }

            Rect labelPosition = new Rect(totalPosition.x + indent, totalPosition.y, EditorGUIUtility.labelWidth - indent, kSingleLineHeight);
            Rect fieldPosition = new Rect(totalPosition.x + EditorGUIUtility.labelWidth, totalPosition.y, totalPosition.width - EditorGUIUtility.labelWidth, totalPosition.height);
            HandlePrefixLabel(totalPosition, labelPosition, label, id, style);
            return fieldPosition;
        }

        internal static Rect MultiFieldPrefixLabel(Rect totalPosition, int id, GUIContent label, int columns)
        {
            if (!LabelHasContent(label))
            {
                return IndentedRect(totalPosition);
            }

            if (EditorGUIUtility.wideMode)
            {
                Rect labelPosition = new Rect(totalPosition.x + indent, totalPosition.y, EditorGUIUtility.labelWidth - indent, kSingleLineHeight);
                Rect fieldPosition = totalPosition;
                fieldPosition.xMin += EditorGUIUtility.labelWidth;


                if (columns > 1)
                {
                    // Tweak where the first sub-label is positioned to make it look like it lines up better.
                    labelPosition.width -= 1;
                    fieldPosition.xMin -= 1;
                }

                // If there are 2 columns we use the same column widths as if there had been 3 columns
                // in order to make columns line up neatly.
                if (columns == 2)
                {
                    float columnWidth = (fieldPosition.width - (3 - 1) * kSpacingSubLabel) / 3f;
                    fieldPosition.xMax -= (columnWidth + kSpacingSubLabel);
                }

                HandlePrefixLabel(totalPosition, labelPosition, label, id);
                return fieldPosition;
            }
            else
            {
                Rect labelPosition = new Rect(totalPosition.x + indent, totalPosition.y, totalPosition.width - indent, kSingleLineHeight);
                Rect fieldPosition = totalPosition;
                fieldPosition.xMin += indent + kIndentPerLevel;
                fieldPosition.yMin += kSingleLineHeight + kVerticalSpacingMultiField;
                HandlePrefixLabel(totalPosition, labelPosition, label, id);
                return fieldPosition;
            }
        }

        public class PropertyScope : GUI.Scope
        {
            public GUIContent content { get; protected set; }

            public PropertyScope(Rect totalPosition, GUIContent label, SerializedProperty property)
            {
                content = BeginProperty(totalPosition, label, property);
            }

            protected override void CloseScope()
            {
                EndProperty();
            }
        }

        // Create a Property wrapper, useful for making regular GUI controls work with [[SerializedProperty]].
        public static GUIContent BeginProperty(Rect totalPosition, GUIContent label, SerializedProperty property)
        {
            Highlighter.HighlightIdentifier(totalPosition, property.propertyPath);

            if (s_PendingPropertyKeyboardHandling != null)
            {
                DoPropertyFieldKeyboardHandling(s_PendingPropertyKeyboardHandling);
            }

            // Properties can be nested, so A BeginProperty may not be followed by its corresponding EndProperty
            // before there have been one or more pairs of BeginProperty/EndProperty in between.
            // The keyboard handling for a property (that handles duplicate and delete commands for array items)
            // uses EditorGUI.lastControlID so it has to be executed for a property before any possible child
            // properties are handled. However, it can't be done in it's own BeginProperty, because the controlID
            // for the property is not yet known at that point. For that reason we mark the keyboard handling as
            // pending and handle it either the next BeginProperty call (for the first child property) or if there's
            // no child properties, then in the matching EndProperty call.
            s_PendingPropertyKeyboardHandling = property;

            if (property == null)
            {
                string error = (label == null ? "" : label.text + ": ") + "SerializedProperty is null";
                HelpBox(totalPosition, "null", MessageType.Error);
                throw new NullReferenceException(error);
            }

            s_PropertyFieldTempContent.text = (label == null) ? L10n.Tr(property.displayName) : label.text; // no necessary to be translated.
            s_PropertyFieldTempContent.tooltip = EditorGUI.isCollectingTooltips ? ((label == null) ? property.tooltip : label.tooltip) : null;
            string attributeTooltip = ScriptAttributeUtility.GetHandler(property).tooltip;
            if (attributeTooltip != null)
                s_PropertyFieldTempContent.tooltip = attributeTooltip;
            s_PropertyFieldTempContent.image = (label == null) ? null : label.image;

            // In inspector debug mode & when holding down alt. Show the property path of the property.
            if (Event.current.alt && property.serializedObject.inspectorMode != InspectorMode.Normal)
            {
                s_PropertyFieldTempContent.tooltip = s_PropertyFieldTempContent.text = property.propertyPath;
            }

            bool wasBoldDefaultFont = EditorGUIUtility.GetBoldDefaultFont();
            if (property.serializedObject.targetObjects.Length == 1 && property.isInstantiatedPrefab)
            {
                EditorGUIUtility.SetBoldDefaultFont(property.prefabOverride);
            }

            s_PropertyStack.Push(new PropertyGUIData(property, totalPosition, wasBoldDefaultFont, GUI.enabled, GUI.backgroundColor));
            var targetObjectTypeName = property.serializedObject.targetObject != null ?
                property.serializedObject.targetObject.GetType().AssemblyQualifiedName : null;
            GUIDebugger.LogBeginProperty(targetObjectTypeName, property.propertyPath, totalPosition);
            showMixedValue = property.hasMultipleDifferentValues;

            if (property.isAnimated)
            {
                Color animatedColor = AnimationMode.animatedPropertyColor;
                if (AnimationMode.InAnimationRecording())
                    animatedColor = AnimationMode.recordedPropertyColor;
                else if (property.isCandidate)
                    animatedColor = AnimationMode.candidatePropertyColor;

                animatedColor.a *= GUI.backgroundColor.a;
                GUI.backgroundColor = animatedColor;
            }

            GUI.enabled &= property.editable;

            return s_PropertyFieldTempContent;
        }

        // Ends a Property wrapper started with ::ref::BeginProperty.
        public static void EndProperty()
        {
            GUIDebugger.LogEndProperty();

            showMixedValue = false;
            PropertyGUIData data = s_PropertyStack.Pop();
            // Context menu
            // Handle context menu in EndProperty instead of BeginProperty. This ensures that child properties
            // get the event rather than parent properties when clicking inside the child property rects, but the menu can
            // still be invoked for the parent property by clicking inside the parent rect but outside the child rects.
            if (Event.current.type == EventType.ContextClick && data.totalPosition.Contains(Event.current.mousePosition))
            {
                DoPropertyContextMenu(data.property);
            }

            EditorGUIUtility.SetBoldDefaultFont(data.wasBoldDefaultFont);
            GUI.enabled = data.wasEnabled;
            GUI.backgroundColor = data.color;

            if (s_PendingPropertyKeyboardHandling != null)
            {
                DoPropertyFieldKeyboardHandling(s_PendingPropertyKeyboardHandling);
            }

            // Wait with deleting the property until the property stack is empty in order to avoid
            // deleting a property in the middle of GUI calls that's dependent on it existing.
            if (s_PendingPropertyDelete != null && s_PropertyStack.Count == 0)
            {
                // For SerializedProperty iteration reasons, if the property we delete is the current property,
                // we have to delete on the actual iterator property rather than a copy of it,
                // otherwise we get an error next time we call NextVisible on the iterator property.
                if (s_PendingPropertyDelete.propertyPath == data.property.propertyPath)
                {
                    data.property.DeleteCommand();
                }
                else
                {
                    s_PendingPropertyDelete.DeleteCommand();
                }
                s_PendingPropertyDelete = null;
            }
        }

        private static SerializedProperty s_PendingPropertyKeyboardHandling = null;
        private static SerializedProperty s_PendingPropertyDelete = null;

        private static void DoPropertyFieldKeyboardHandling(SerializedProperty property)
        {
            // Delete & Duplicate commands
            if (Event.current.type == EventType.ExecuteCommand || Event.current.type == EventType.ValidateCommand)
            {
                if (GUIUtility.keyboardControl == EditorGUIUtility.s_LastControlID && (Event.current.commandName == "Delete" || Event.current.commandName == "SoftDelete"))
                {
                    if (Event.current.type == EventType.ExecuteCommand)
                    {
                        // Wait with deleting the property until the property stack is empty. See EndProperty.
                        s_PendingPropertyDelete = property.Copy();
                    }
                    Event.current.Use();
                }
                if (GUIUtility.keyboardControl == EditorGUIUtility.s_LastControlID && Event.current.commandName == "Duplicate")
                {
                    if (Event.current.type == EventType.ExecuteCommand)
                    {
                        property.DuplicateCommand();
                    }
                    Event.current.Use();
                }
            }
            s_PendingPropertyKeyboardHandling = null;
        }

        // Make a field for layer masks.
        internal static void LayerMaskField(Rect position, SerializedProperty property, GUIContent label)
        {
            LayerMaskField(position, property, label, EditorStyles.layerMaskField);
        }

        internal static void LayerMaskField(Rect position, SerializedProperty property, GUIContent label, GUIStyle style)
        {
            LayerMaskField(position, property.layerMaskBits, property, label, style, SetLayerMaskValueDelegate);
        }

        internal static void LayerMaskField(Rect position, UInt32 layers, GUIContent label, EditorUtility.SelectMenuItemFunction callback)
        {
            LayerMaskField(position, layers, null, label, EditorStyles.layerMaskField, callback);
        }

        internal static void LayerMaskField(Rect position, UInt32 layers, SerializedProperty property, GUIContent label, GUIStyle style, EditorUtility.SelectMenuItemFunction callback)
        {
            int id = GUIUtility.GetControlID(s_LayerMaskField, FocusType.Keyboard, position);
            if (label != null)
            {
                position = PrefixLabel(position, id, label);
            }
            Event evt = Event.current;
            if (evt.type == EventType.Repaint)
            {
                if (showMixedValue)
                {
                    BeginHandleMixedValueContentColor();
                    style.Draw(position, s_MixedValueContent, id, false);
                    EndHandleMixedValueContentColor();
                }
                else
                {
                    style.Draw(position, EditorGUIUtility.TempContent(SerializedProperty.GetLayerMaskStringValue(layers)), id, false);
                }
            }
            else if ((evt.type == EventType.MouseDown && position.Contains(evt.mousePosition)) || evt.MainActionKeyForControl(id))
            {
                SerializedProperty propertyWithPath = (property != null) ? property.serializedObject.FindProperty(property.propertyPath) : null;
                var userData = new Tuple<SerializedProperty, UInt32>(propertyWithPath, layers);
                EditorUtility.DisplayCustomMenu(position, SerializedProperty.GetLayerMaskNames(layers), (property != null && property.hasMultipleDifferentValues) ? new int[0] : SerializedProperty.GetLayerMaskSelectedIndex(layers), callback, userData);
                Event.current.Use();
                EditorGUIUtility.keyboardControl = id;
            }
        }

        internal static void SetLayerMaskValueDelegate(object userData, string[] options, int selected)
        {
            var data = (Tuple<SerializedProperty, UInt32>)userData;
            if (data.Item1 != null)
            {
                data.Item1.ToggleLayerMaskAtIndex(selected);
                data.Item1.serializedObject.ApplyModifiedProperties();
                data.Item2 = data.Item1.layerMaskBits;
            }
        }

        // Helper function for helping with debugging the editor
        internal static void ShowRepaints()
        {
            if (Unsupported.IsDeveloperBuild())
            {
                Color temp = GUI.backgroundColor;
                GUI.backgroundColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1f);
                var texture = EditorStyles.radioButton.normal.background;
                var size = new Vector2(texture.width, texture.height);
                GUI.Label(new Rect(Vector2.zero, EditorGUIUtility.PixelsToPoints(size)), string.Empty, EditorStyles.radioButton);
                GUI.backgroundColor = temp;
            }
        }

        // Draws the alpha channel of a texture within a rectangle.
        internal static void DrawTextureAlphaInternal(Rect position, Texture image, ScaleMode scaleMode, float imageAspect)
        {
            DrawPreviewTextureInternal(position, image, alphaMaterial, scaleMode, imageAspect);
        }

        // Draws texture transparantly using the alpha channel.
        internal static void DrawTextureTransparentInternal(Rect position, Texture image, ScaleMode scaleMode, float imageAspect)
        {
            if (imageAspect == 0f && image == null)
            {
                Debug.LogError("Please specify an image or a imageAspect");
                return;
            }

            if (imageAspect == 0f)
            {
                imageAspect = (float)image.width / (float)image.height;
            }

            DrawTransparencyCheckerTexture(position, scaleMode, imageAspect);

            if (image != null)
            {
                DrawPreviewTexture(position, image, transparentMaterial, scaleMode, imageAspect);
            }
        }

        internal static void DrawTransparencyCheckerTexture(Rect position, ScaleMode scaleMode, float imageAspect)
        {
            Rect screenRect = new Rect();
            Rect sourceRect = new Rect();

            GUI.CalculateScaledTextureRects(position, scaleMode, imageAspect, ref screenRect, ref sourceRect);

            GUI.DrawTextureWithTexCoords(
                screenRect,
                transparentCheckerTexture,
                new Rect(
                    (float)screenRect.width * -.5f / (float)transparentCheckerTexture.width,
                    (float)screenRect.height * -.5f / (float)transparentCheckerTexture.height,
                    (float)screenRect.width / (float)transparentCheckerTexture.width,
                    (float)screenRect.height / (float)transparentCheckerTexture.height),
                false);
        }

        // Draws the texture within a rectangle.
        internal static void DrawPreviewTextureInternal(Rect position, Texture image, Material mat, ScaleMode scaleMode, float imageAspect)
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (imageAspect == 0)
                {
                    imageAspect = (float)image.width / (float)image.height;
                }

                if (mat == null)
                {
                    mat = GetMaterialForSpecialTexture(image);
                }

                GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear) && !TextureUtil.GetLinearSampled(image);

                RenderTexture rt = image as RenderTexture;
                bool manualResolve = (rt != null) && rt.bindTextureMS;

                if (manualResolve)
                {
                    var desc = rt.descriptor;
                    desc.bindMS = false;
                    desc.msaaSamples = 1;
                    RenderTexture resolved = RenderTexture.GetTemporary(desc);
                    resolved.Create();
                    rt.ResolveAntiAliasedSurface(resolved);
                    image = resolved;
                }

                if (mat == null)
                {
                    // fallback to GUI.DrawTexture() with blit material
                    GUI.DrawTexture(position, image, scaleMode, false, imageAspect);
                }
                else
                {
                    Rect screenRect = new Rect();
                    Rect sourceRect = new Rect();

                    GUI.CalculateScaledTextureRects(position, scaleMode, imageAspect, ref screenRect, ref sourceRect);
                    Texture2D t2d = image as Texture2D;
                    if (t2d != null && TextureUtil.GetUsageMode(image) == TextureUsageMode.AlwaysPadded)
                    {
                        // In case of always padded textures, only show the non-padded area
                        sourceRect.width *= (float)t2d.width / TextureUtil.GetGPUWidth(t2d);
                        sourceRect.height *= (float)t2d.height / TextureUtil.GetGPUHeight(t2d);
                    }
                    Graphics.DrawTexture(screenRect, image, sourceRect, 0, 0, 0, 0, GUI.color, mat);
                }

                GL.sRGBWrite = false;
                if (manualResolve)
                {
                    RenderTexture.ReleaseTemporary(image as RenderTexture);
                }
            }
        }

        // Gets a material for a special texture type: RGBM or doubleLDR lightmap or a DXT5nm normal map.
        internal static Material GetMaterialForSpecialTexture(Texture t)
        {
            if (t == null) return null;

            TextureUsageMode usage = TextureUtil.GetUsageMode(t);
            TextureFormat format = TextureUtil.GetTextureFormat(t);
            if (usage == TextureUsageMode.RealtimeLightmapRGBM || usage == TextureUsageMode.BakedLightmapRGBM || usage == TextureUsageMode.RGBMEncoded)
                return lightmapRGBMMaterial;
            else if (usage == TextureUsageMode.BakedLightmapDoubleLDR)
                return lightmapDoubleLDRMaterial;
            else if (usage == TextureUsageMode.BakedLightmapFullHDR)
                return lightmapFullHDRMaterial;
            else if (usage == TextureUsageMode.NormalmapDXT5nm || (usage == TextureUsageMode.NormalmapPlain && format == TextureFormat.BC5))
                return normalmapMaterial;
            else if (TextureUtil.IsAlphaOnlyTextureFormat(format))
                return alphaMaterial;
            return null;
        }

        internal static Material alphaMaterial
        {
            get { return EditorGUIUtility.LoadRequired("Previews/PreviewAlphaMaterial.mat") as Material; }
        }

        internal static Material transparentMaterial
        {
            get { return EditorGUIUtility.LoadRequired("Previews/PreviewTransparentMaterial.mat") as Material; }
        }

        internal static Texture2D transparentCheckerTexture
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return EditorGUIUtility.LoadRequired("Previews/Textures/textureCheckerDark.png") as Texture2D;
                }
                else
                {
                    return EditorGUIUtility.LoadRequired("Previews/Textures/textureChecker.png") as Texture2D;
                }
            }
        }

        internal static Material lightmapRGBMMaterial
        {
            get { return EditorGUIUtility.LoadRequired("Previews/PreviewEncodedLightmapRGBMMaterial.mat") as Material; }
        }

        internal static Material lightmapDoubleLDRMaterial
        {
            get { return EditorGUIUtility.LoadRequired("Previews/PreviewEncodedLightmapDoubleLDRMaterial.mat") as Material; }
        }

        internal static Material lightmapFullHDRMaterial
        {
            get { return EditorGUIUtility.LoadRequired("Previews/PreviewEncodedLightmapFullHDRMaterial.mat") as Material; }
        }

        internal static Material normalmapMaterial
        {
            get { return EditorGUIUtility.LoadRequired("Previews/PreviewEncodedNormalsMaterial.mat") as Material; }
        }


        private static void SetExpandedRecurse(SerializedProperty property, bool expanded)
        {
            SerializedProperty search = property.Copy();
            search.isExpanded = expanded;

            int depth = search.depth;
            while (search.NextVisible(true) && search.depth > depth)
            {
                if (search.hasVisibleChildren)
                {
                    search.isExpanded = expanded;
                }
            }
        }

        // Get the height needed for a ::ref::PropertyField control, not including its children and not taking custom PropertyDrawers into account.
        internal static float GetSinglePropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property == null)
                return kSingleLineHeight;
            return GetPropertyHeight(property.propertyType, label);
        }

        // Get the height needed for a simple builtin control type.
        public static float GetPropertyHeight(SerializedPropertyType type, GUIContent label)
        {
            if (type == SerializedPropertyType.Vector3 || type == SerializedPropertyType.Vector2 || type == SerializedPropertyType.Vector4 ||
                type == SerializedPropertyType.Vector3Int || type == SerializedPropertyType.Vector2Int)
            {
                return (!LabelHasContent(label) || EditorGUIUtility.wideMode ? 0f : kStructHeaderLineHeight) + kSingleLineHeight;
            }

            if (type == SerializedPropertyType.Rect || type == SerializedPropertyType.RectInt)
            {
                return (!LabelHasContent(label) || EditorGUIUtility.wideMode ? 0f : kStructHeaderLineHeight) + kSingleLineHeight * 2 + kVerticalSpacingMultiField;
            }

            // Bounds field has label on its own line even in wide mode because the words "center" and "extends"
            // would otherwise eat too much of the label space.
            if (type == SerializedPropertyType.Bounds || type == SerializedPropertyType.BoundsInt)
            {
                return (!LabelHasContent(label) ? 0f : kStructHeaderLineHeight) + kSingleLineHeight * 2 + kVerticalSpacingMultiField;
            }

            return kSingleLineHeight;
        }

        // Get the height needed for a ::ref::PropertyField control.
        internal static float GetPropertyHeightInternal(SerializedProperty property, GUIContent label, bool includeChildren)
        {
            return ScriptAttributeUtility.GetHandler(property).GetHeight(property, label, includeChildren);
        }

        public static bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return ScriptAttributeUtility.GetHandler(property).CanCacheInspectorGUI(property);
        }

        internal static bool HasVisibleChildFields(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Rect:
                case SerializedPropertyType.RectInt:
                case SerializedPropertyType.Bounds:
                case SerializedPropertyType.BoundsInt:
                    return false;
            }
            return property.hasVisibleChildren;
        }

        // Make a field for [[SerializedProperty]].
        internal static bool PropertyFieldInternal(Rect position, SerializedProperty property, GUIContent label, bool includeChildren)
        {
            return ScriptAttributeUtility.GetHandler(property).OnGUI(position, property, label, includeChildren);
        }

        static string s_ArrayMultiInfoFormatString = EditorGUIUtility.TextContent("This field cannot display arrays with more than {0} elements when multiple objects are selected.").text;
        static GUIContent s_ArrayMultiInfoContent = new GUIContent();

        internal static bool DefaultPropertyField(Rect position, SerializedProperty property, GUIContent label)
        {
            label = BeginProperty(position, label, property);

            SerializedPropertyType type = property.propertyType;

            bool childrenAreExpanded = false;

            // Should we inline? All one-line vars as well as Vector2, Vector3, Rect and Bounds properties are inlined.
            if (!HasVisibleChildFields(property))
            {
                switch (type)
                {
                    case SerializedPropertyType.Integer:
                    {
                        BeginChangeCheck();
                        long newValue = EditorGUI.LongField(position, label, property.longValue);
                        if (EndChangeCheck())
                        {
                            property.longValue = newValue;
                        }
                        break;
                    }
                    case SerializedPropertyType.Float:
                    {
                        BeginChangeCheck();

                        // Necessary to check for float type to get correct string formatting for float and double.
                        bool isFloat = property.type == "float";
                        double newValue = isFloat ? EditorGUI.FloatField(position, label, property.floatValue) :
                            EditorGUI.DoubleField(position, label, property.doubleValue);
                        if (EndChangeCheck())
                        {
                            property.doubleValue = newValue;
                        }
                        break;
                    }
                    case SerializedPropertyType.String:
                    {
                        BeginChangeCheck();
                        string newValue = EditorGUI.TextField(position, label, property.stringValue);
                        if (EndChangeCheck())
                        {
                            property.stringValue = newValue;
                        }
                        break;
                    }
                    // Multi @todo: Needs style work
                    case SerializedPropertyType.Boolean:
                    {
                        BeginChangeCheck();
                        bool newValue = EditorGUI.Toggle(position, label, property.boolValue);
                        if (EndChangeCheck())
                        {
                            property.boolValue = newValue;
                        }
                        break;
                    }
                    case SerializedPropertyType.Color:
                    {
                        BeginChangeCheck();
                        Color newColor = EditorGUI.ColorField(position, label, property.colorValue);
                        if (EndChangeCheck())
                        {
                            property.colorValue = newColor;
                        }
                        break;
                    }
                    case SerializedPropertyType.ArraySize:
                    {
                        BeginChangeCheck();
                        int newValue = EditorGUI.ArraySizeField(position, label, property.intValue, EditorStyles.numberField);
                        if (EndChangeCheck())
                        {
                            property.intValue = newValue;
                        }
                        break;
                    }
                    case SerializedPropertyType.FixedBufferSize:
                    {
                        EditorGUI.IntField(position, label, property.intValue);
                        break;
                    }
                    case SerializedPropertyType.Enum:
                    {
                        EditorGUI.Popup(position, property, label);
                        break;
                    }
                    // Multi @todo: Needs testing for texture types
                    case SerializedPropertyType.ObjectReference:
                    {
                        EditorGUI.ObjectFieldInternal(position, property, null, label, EditorStyles.objectField);
                        break;
                    }
                    case SerializedPropertyType.LayerMask:
                    {
                        EditorGUI.LayerMaskField(position, property, label);
                        break;
                    }
                    case SerializedPropertyType.Character:
                    {
                        char[] value = { (char)property.intValue };

                        bool wasChanged = GUI.changed;
                        GUI.changed = false;
                        string newValue = EditorGUI.TextField(position, label, new string(value));
                        if (GUI.changed)
                        {
                            if (newValue.Length == 1)
                            {
                                property.intValue = newValue[0];
                            }
                            // Value didn't get changed after all
                            else
                            {
                                GUI.changed = false;
                            }
                        }
                        GUI.changed |= wasChanged;
                        break;
                    }
                    case SerializedPropertyType.AnimationCurve:
                    {
                        int id = GUIUtility.GetControlID(s_CurveHash, FocusType.Keyboard, position);
                        EditorGUI.DoCurveField(PrefixLabel(position, id, label), id, null, kCurveColor, new Rect(), property);
                        break;
                    }
                    case SerializedPropertyType.Gradient:
                    {
                        int id = GUIUtility.GetControlID(s_CurveHash, FocusType.Keyboard, position);
                        EditorGUI.DoGradientField(PrefixLabel(position, id, label), id, null, property, false);
                        break;
                    }
                    case SerializedPropertyType.Vector3:
                    {
                        Vector3Field(position, property, label);
                        break;
                    }
                    case SerializedPropertyType.Vector4:
                    {
                        Vector4Field(position, property, label);
                        break;
                    }
                    case SerializedPropertyType.Vector2:
                    {
                        Vector2Field(position, property, label);
                        break;
                    }
                    case SerializedPropertyType.Vector2Int:
                    {
                        Vector2IntField(position, property, label);
                        break;
                    }
                    case SerializedPropertyType.Vector3Int:
                    {
                        Vector3IntField(position, property, label);
                        break;
                    }
                    case SerializedPropertyType.Rect:
                    {
                        RectField(position, property, label);
                        break;
                    }
                    case SerializedPropertyType.RectInt:
                    {
                        RectIntField(position, property, label);
                        break;
                    }
                    case SerializedPropertyType.Bounds:
                    {
                        BoundsField(position, property, label);
                        break;
                    }
                    case SerializedPropertyType.BoundsInt:
                    {
                        BoundsIntField(position, property, label);
                        break;
                    }
                    default:
                    {
                        int genericID = GUIUtility.GetControlID(s_GenericField, FocusType.Keyboard, position);
                        EditorGUI.PrefixLabel(position, genericID, label);
                        break;
                    }
                }
            }
            // Handle Foldout
            else
            {
                Event tempEvent = new Event(Event.current);

                // Handle the actual foldout first, since that's the one that supports keyboard control.
                // This makes it work more consistent with PrefixLabel.
                childrenAreExpanded = property.isExpanded;

                bool newChildrenAreExpanded = childrenAreExpanded;
                using (new EditorGUI.DisabledScope(!property.editable))
                {
                    GUIStyle foldoutStyle = (DragAndDrop.activeControlID == -10) ? EditorStyles.foldoutPreDrop : EditorStyles.foldout;
                    newChildrenAreExpanded = EditorGUI.Foldout(position, childrenAreExpanded, s_PropertyFieldTempContent, true, foldoutStyle);
                }


                if (childrenAreExpanded && property.isArray && property.arraySize > property.serializedObject.maxArraySizeForMultiEditing && property.serializedObject.isEditingMultipleObjects)
                {
                    Rect boxRect = position;
                    boxRect.xMin += EditorGUIUtility.labelWidth - EditorGUI.indent;

                    s_ArrayMultiInfoContent.text = s_ArrayMultiInfoContent.tooltip = string.Format(s_ArrayMultiInfoFormatString, property.serializedObject.maxArraySizeForMultiEditing);
                    EditorGUI.LabelField(boxRect, GUIContent.none, s_ArrayMultiInfoContent, EditorStyles.helpBox);
                }

                if (newChildrenAreExpanded != childrenAreExpanded)
                {
                    // Recursive set expanded
                    if (Event.current.alt)
                    {
                        SetExpandedRecurse(property, newChildrenAreExpanded);
                    }
                    // Expand one element only
                    else
                    {
                        property.isExpanded = newChildrenAreExpanded;
                    }
                }
                childrenAreExpanded = newChildrenAreExpanded;


                // Check for drag & drop events here, to add objects to an array by dragging to the foldout.
                // The event may have already been used by the Foldout control above, but we want to also use it here,
                // so we use the event copy we made prior to calling the Foldout method.

                // We need to use last s_LastControlID here to ensure we do not break duplicate functionality (fix for case 598389)
                // If we called GetControlID here s_LastControlID would be incremented and would not longer be in sync with GUIUtililty.keyboardFocus that
                // is used for duplicating (See DoPropertyFieldKeyboardHandling)
                int id = EditorGUIUtility.s_LastControlID;
                switch (tempEvent.type)
                {
                    case EventType.DragExited:
                        if (GUI.enabled)
                        {
                            HandleUtility.Repaint();
                        }

                        break;
                    case EventType.DragUpdated:
                    case EventType.DragPerform:

                        if (position.Contains(tempEvent.mousePosition) && GUI.enabled)
                        {
                            Object[] references = DragAndDrop.objectReferences;

                            // Check each single object, so we can add multiple objects in a single drag.
                            Object[] oArray = new Object[1];
                            bool didAcceptDrag = false;
                            foreach (Object o in references)
                            {
                                oArray[0] = o;
                                Object validatedObject = ValidateObjectFieldAssignment(oArray, null, property, ObjectFieldValidatorOptions.None);
                                if (validatedObject != null)
                                {
                                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                                    if (tempEvent.type == EventType.DragPerform)
                                    {
                                        property.AppendFoldoutPPtrValue(validatedObject);
                                        didAcceptDrag = true;
                                        DragAndDrop.activeControlID = 0;
                                    }
                                    else
                                    {
                                        DragAndDrop.activeControlID = id;
                                    }
                                }
                            }
                            if (didAcceptDrag)
                            {
                                GUI.changed = true;
                                DragAndDrop.AcceptDrag();
                            }
                        }
                        break;
                }
            }

            EndProperty();

            return childrenAreExpanded;
        }

        internal static void DrawLegend(Rect position, Color color, string label, bool enabled)
        {
            position = new Rect(position.x + 2, position.y + 2, position.width - 2, position.height - 2);
            Color oldCol = GUI.backgroundColor;
            if (enabled)
            {
                GUI.backgroundColor = color;
            }
            else
            {
                // Tweaked to work on both light and dark skin:
                GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.45f);
            }

            GUI.Label(position, label, "ProfilerPaneSubLabel");
            GUI.backgroundColor = oldCol;
        }

        internal static string TextFieldDropDown(Rect position, string text, string[] dropDownElement)
        {
            return TextFieldDropDown(position, GUIContent.none, text, dropDownElement);
        }

        internal static string TextFieldDropDown(Rect position, GUIContent label, string text, string[] dropDownElement)
        {
            int id = EditorGUIUtility.GetControlID(s_TextFieldDropDownHash, FocusType.Keyboard, position);
            return EditorGUI.DoTextFieldDropDown(PrefixLabel(position, id, label), id, text, dropDownElement, false);
        }

        internal static string DelayedTextFieldDropDown(Rect position, string text, string[] dropDownElement)
        {
            return DelayedTextFieldDropDown(position, GUIContent.none, text, dropDownElement);
        }

        internal static string DelayedTextFieldDropDown(Rect position, GUIContent label, string text, string[] dropDownElement)
        {
            int id = EditorGUIUtility.GetControlID(s_TextFieldDropDownHash, FocusType.Keyboard, position);
            return EditorGUI.DoTextFieldDropDown(PrefixLabel(position, id, label), id, text, dropDownElement, true);
        }

        public static bool DropdownButton(Rect position, GUIContent content, FocusType focusType)
        {
            return DropdownButton(position, content, focusType, "MiniPullDown");
        }

        public static bool DropdownButton(Rect position, GUIContent content, FocusType focusType, GUIStyle style)
        {
            int id =  EditorGUIUtility.GetControlID(s_DropdownButtonHash, focusType, position);
            return DropdownButton(id, position, content, style);
        }

        // A button that returns true on mouse down - like a popup button
        internal static bool DropdownButton(int id, Rect position, GUIContent content, GUIStyle style)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.Repaint:
                    if (showMixedValue)
                    {
                        BeginHandleMixedValueContentColor();
                        style.Draw(position, s_MixedValueContent, id, false);
                        EndHandleMixedValueContentColor();
                    }
                    else
                        style.Draw(position, content, id, false);
                    break;
                case EventType.MouseDown:
                    if (position.Contains(evt.mousePosition) && evt.button == 0)
                    {
                        Event.current.Use();
                        return true;
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl == id && evt.character == ' ')
                    {
                        Event.current.Use();
                        return true;
                    }
                    break;
            }
            return false;
        }

        static int EnumFlagsToInt(EnumData enumData, Enum enumValue)
        {
            if (enumData.unsigned)
            {
                if (enumData.underlyingType == typeof(uint))
                {
                    return unchecked((int)Convert.ToUInt32(enumValue));
                }
                // ensure unsigned 16- and 8-bit variants will display using "Everything" label
                else if (enumData.underlyingType == typeof(ushort))
                {
                    var unsigned = Convert.ToUInt16(enumValue);
                    return unchecked(unsigned == ushort.MaxValue ? ~0 : (int)unsigned);
                }
                else
                {
                    var unsigned = Convert.ToByte(enumValue);
                    return unchecked(unsigned == byte.MaxValue ? ~0 : (int)unsigned);
                }
            }

            return Convert.ToInt32(enumValue);
        }

        static Enum IntToEnumFlags(Type enumType, int value)
        {
            var enumData = GetNonObsoleteEnumData(enumType);

            // parsing a string seems to be the only way to go from a flags int to an enum value
            if (enumData.unsigned)
            {
                if (enumData.underlyingType == typeof(uint))
                {
                    var unsigned = unchecked((uint)value);
                    return Enum.Parse(enumType, unsigned.ToString()) as Enum;
                }
                else if (enumData.underlyingType == typeof(ushort))
                {
                    var unsigned = unchecked((ushort)value);
                    return Enum.Parse(enumType, unsigned.ToString()) as Enum;
                }
                else
                {
                    var unsigned = unchecked((byte)value);
                    return Enum.Parse(enumType, unsigned.ToString()) as Enum;
                }
            }

            return Enum.Parse(enumType, value.ToString()) as Enum;
        }

        internal static bool isCollectingTooltips
        {
            get { return s_CollectingToolTips; }
            set { s_CollectingToolTips = value; }
        }

        internal static bool s_CollectingToolTips;
    }


    // Auto-layouted version of [[EditorGUI]]
    sealed partial class EditorGUILayout
    {
        // @TODO: Make private (and rename to not claim it's a constant). Shouldn't really be used outside of EditorGUI.
        // Places that use this directly should likely use GetControlRect instead.
        internal static float kLabelFloatMinW
        {
            get { return EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing; }
        }

        internal static float kLabelFloatMaxW
        {
            get { return EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth + EditorGUI.kSpacing; }
        }

        internal static Rect s_LastRect;

        internal const float kPlatformTabWidth = 30;

        internal static SavedBool s_SelectedDefault = new SavedBool("Platform.ShownDefaultTab", true);


        /// *listonly*
        public static void LabelField(string label, params GUILayoutOption[] options)
        {
            LabelField(GUIContent.none, EditorGUIUtility.TempContent(label), EditorStyles.label, options);
        }

        /// *listonly*
        public static void LabelField(string label, GUIStyle style, params GUILayoutOption[] options)
        {
            LabelField(GUIContent.none, EditorGUIUtility.TempContent(label), style, options);
        }

        /// *listonly*
        public static void LabelField(GUIContent label, params GUILayoutOption[] options)
        {
            LabelField(GUIContent.none, label, EditorStyles.label, options);
        }

        /// *listonly*
        public static void LabelField(GUIContent label, GUIStyle style, params GUILayoutOption[] options)
        {
            LabelField(GUIContent.none, label, style, options);
        }

        /// *listonly*
        public static void LabelField(string label, string label2, params GUILayoutOption[] options)
        {
            LabelField(new GUIContent(label), EditorGUIUtility.TempContent(label2), EditorStyles.label, options);
        }

        /// *listonly*
        public static void LabelField(string label, string label2, GUIStyle style, params GUILayoutOption[] options)
        {
            LabelField(new GUIContent(label), EditorGUIUtility.TempContent(label2), style, options);
        }

        /// *listonly*
        public static void LabelField(GUIContent label, GUIContent label2, params GUILayoutOption[] options)
        {
            LabelField(label, label2, EditorStyles.label, options);
        }

        // Make a label field. (Useful for showing read-only info.)
        public static void LabelField(GUIContent label, GUIContent label2, GUIStyle style, params GUILayoutOption[] options)
        {
            if (!style.wordWrap)
            {
                // If we don't need wordwrapping, just allocate the standard space to avoid corner case layout issues
                Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, options);
                EditorGUI.LabelField(r, label, label2, style);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                PrefixLabel(label, style);
                Rect r = GUILayoutUtility.GetRect(label2, style, options);
                int oldIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                EditorGUI.LabelField(r, label2, style);
                EditorGUI.indentLevel = oldIndent;
                EditorGUILayout.EndHorizontal();
            }
        }

        /// *listonly*
        internal static bool LinkLabel(string label, params GUILayoutOption[] options)
        {
            return LinkLabel(EditorGUIUtility.TempContent(label), options);
        }

        /// *listonly*
        internal static bool LinkLabel(GUIContent label, params GUILayoutOption[] options)
        {
            var position = s_LastRect = GUILayoutUtility.GetRect(label, EditorStyles.linkLabel, options);

            Handles.color = EditorStyles.linkLabel.normal.textColor;
            Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
            Handles.color = Color.white;

            EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

            return GUI.Button(position, label, EditorStyles.linkLabel);
        }

        /// *listonly*
        public static bool Toggle(bool value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetToggleRect(false, options);
            return EditorGUI.Toggle(r, value);
        }

        /// *listonly*
        public static bool Toggle(string label, bool value, params GUILayoutOption[] options)
        {
            return Toggle(EditorGUIUtility.TempContent(label), value, options);
        }

        /// *listonly*
        public static bool Toggle(GUIContent label, bool value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetToggleRect(true, options);
            return EditorGUI.Toggle(r, label, value);
        }

        /// *listonly*
        public static bool Toggle(bool value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetToggleRect(false, options);
            return EditorGUI.Toggle(r, value, style);
        }

        /// *listonly*
        public static bool Toggle(string label, bool value, GUIStyle style, params GUILayoutOption[] options)
        {
            return Toggle(EditorGUIUtility.TempContent(label), value, style, options);
        }

        // Make a toggle.
        public static bool Toggle(GUIContent label, bool value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetToggleRect(true, options);
            return EditorGUI.Toggle(r, label, value, style);
        }

        /// *listonly*
        public static bool ToggleLeft(string label, bool value, params GUILayoutOption[] options)
        {
            return ToggleLeft(EditorGUIUtility.TempContent(label), value, options);
        }

        /// *listonly*
        public static bool ToggleLeft(GUIContent label, bool value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, options);
            return EditorGUI.ToggleLeft(r, label, value);
        }

        /// *listonly*
        public static bool ToggleLeft(string label, bool value, GUIStyle labelStyle, params GUILayoutOption[] options)
        {
            return ToggleLeft(EditorGUIUtility.TempContent(label), value, labelStyle, options);
        }

        // Make a toggle with the label on the right.
        public static bool ToggleLeft(GUIContent label, bool value, GUIStyle labelStyle, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, options);
            return EditorGUI.ToggleLeft(r, label, value, labelStyle);
        }

        /// *listonly*
        public static string TextField(string text, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.textField, options);
            return EditorGUI.TextField(r, text);
        }

        /// *listonly*
        public static string TextField(string text, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.TextField(r, text, style);
        }

        /// *listonly*
        public static string TextField(string label, string text, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.textField, options);
            return EditorGUI.TextField(r, label, text);
        }

        /// *listonly*
        public static string TextField(string label, string text, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.TextField(r, label, text, style);
        }

        /// *listonly*
        public static string TextField(GUIContent label, string text, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.textField, options);
            return EditorGUI.TextField(r, label, text);
        }

        // Make a text field.
        public static string TextField(GUIContent label, string text, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.TextField(r, label, text, style);
        }

        /// *listonly*
        public static string DelayedTextField(string text, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.textField, options);
            return EditorGUI.DelayedTextField(r, text);
        }

        /// *listonly*
        public static string DelayedTextField(string text, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.DelayedTextField(r, text, style);
        }

        /// *listonly*
        public static string DelayedTextField(string label, string text, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.textField, options);
            return EditorGUI.DelayedTextField(r, label, text);
        }

        /// *listonly*
        public static string DelayedTextField(string label, string text, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.DelayedTextField(r, label, text, style);
        }

        /// *listonly*
        public static string DelayedTextField(GUIContent label, string text, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.textField, options);
            return EditorGUI.DelayedTextField(r, label, text);
        }

        // Make a delayed text field.
        public static string DelayedTextField(GUIContent label, string text, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.DelayedTextField(r, label, text, style);
        }

        /// *listonly*
        public static void DelayedTextField(SerializedProperty property, params GUILayoutOption[] options)
        {
            EditorGUILayout.DelayedTextField(property, null, options);
        }

        // Make a delayed text field.
        public static void DelayedTextField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(EditorGUI.LabelHasContent(label), EditorGUI.kSingleLineHeight, EditorStyles.textField, options);
            EditorGUI.DelayedTextField(r, property, label);
        }

        internal static string ToolbarSearchField(string text, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GUILayoutUtility.GetRect(0, kLabelFloatMaxW * 1.5f, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchField, options);
            int i = 0;
            return EditorGUI.ToolbarSearchField(r, null, ref i, text);
        }

        internal static string ToolbarSearchField(string text, string[] searchModes, ref int searchMode, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GUILayoutUtility.GetRect(0, kLabelFloatMaxW * 1.5f, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchField, options);
            return EditorGUI.ToolbarSearchField(r, searchModes, ref searchMode, text);
        }

        /// *listonly*
        public static string TextArea(string text, params GUILayoutOption[] options)
        { return TextArea(text, EditorStyles.textField, options); }
        // Make a text area.
        public static string TextArea(string text, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GUILayoutUtility.GetRect(EditorGUIUtility.TempContent(text), style, options);
            return EditorGUI.TextArea(r, text, style);
        }

        /// *listonly*
        public static void SelectableLabel(string text, params GUILayoutOption[] options)
        {
            SelectableLabel(text, EditorStyles.label, options);
        }

        // Make a selectable label field. (Useful for showing read-only info that can be copy-pasted.)
        public static void SelectableLabel(string text, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight * 2, style, options);
            EditorGUI.SelectableLabel(r, text, style);
        }

        internal static Event KeyEventField(Event e, params GUILayoutOption[] options)
        {
            GUIContent content = EditorGUIUtility.TempContent("[Please press a key]");
            Rect r = GUILayoutUtility.GetRect(content, GUI.skin.textField, options);
            return EditorGUI.KeyEventField(r, e);
        }

        /// *listonly*
        public static string PasswordField(string password, params GUILayoutOption[] options)
        {
            return PasswordField(password, EditorStyles.textField, options);
        }

        /// *listonly*
        public static string PasswordField(string password, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.PasswordField(r, password, style);
        }

        /// *listonly*
        public static string PasswordField(string label, string password, params GUILayoutOption[] options)
        {
            return PasswordField(EditorGUIUtility.TempContent(label), password, EditorStyles.textField, options);
        }

        /// *listonly*
        public static string PasswordField(string label, string password, GUIStyle style, params GUILayoutOption[] options)
        {
            return PasswordField(EditorGUIUtility.TempContent(label), password, style, options);
        }

        /// *listonly*
        public static string PasswordField(GUIContent label, string password, params GUILayoutOption[] options)
        {
            return PasswordField(label, password, EditorStyles.textField, options);
        }

        // Make a text field where the user can enter a password.
        public static string PasswordField(GUIContent label, string password, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.PasswordField(r, label, password, style);
        }

        // Peak smoothing should be handled by client. Input: value and peak is normalized values (0 - 1).
        internal static void VUMeterHorizontal(float value, float peak, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            EditorGUI.VUMeter.HorizontalMeter(r, value, peak, EditorGUI.VUMeter.horizontalVUTexture, Color.grey);
        }

        // Auto-smoothing of peak
        internal static void VUMeterHorizontal(float value, ref EditorGUI.VUMeter.SmoothingData data, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            EditorGUI.VUMeter.HorizontalMeter(r, value, ref data, EditorGUI.VUMeter.horizontalVUTexture, Color.grey);
        }

        /// *listonly*
        public static float FloatField(float value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            return EditorGUI.FloatField(r, value);
        }

        /// *listonly*
        public static float FloatField(float value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.FloatField(r, value, style);
        }

        /// *listonly*
        public static float FloatField(string label, float value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            return EditorGUI.FloatField(r, label, value);
        }

        /// *listonly*
        public static float FloatField(string label, float value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.FloatField(r, label, value, style);
        }

        /// *listonly*
        public static float FloatField(GUIContent label, float value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            return EditorGUI.FloatField(r, label, value);
        }

        // Make a text field for entering float values.
        public static float FloatField(GUIContent label, float value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.FloatField(r, label, value, style);
        }

        /// *listonly*
        public static float DelayedFloatField(float value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            return EditorGUI.DelayedFloatField(r, value);
        }

        /// *listonly*
        public static float DelayedFloatField(float value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.DelayedFloatField(r, value, style);
        }

        /// *listonly*
        public static float DelayedFloatField(string label, float value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            return EditorGUI.DelayedFloatField(r, label, value);
        }

        /// *listonly*
        public static float DelayedFloatField(string label, float value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.DelayedFloatField(r, label, value, style);
        }

        /// *listonly*
        public static float DelayedFloatField(GUIContent label, float value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            return EditorGUI.DelayedFloatField(r, label, value);
        }

        // Make a delayed text field for entering float values.
        public static float DelayedFloatField(GUIContent label, float value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.DelayedFloatField(r, label, value, style);
        }

        /// *listonly*
        public static void DelayedFloatField(SerializedProperty property, params GUILayoutOption[] options)
        {
            EditorGUILayout.DelayedFloatField(property, null, options);
        }

        // Make a delayed text field for entering float values.
        public static void DelayedFloatField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(EditorGUI.LabelHasContent(label), EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            EditorGUI.DelayedFloatField(r, property, label);
        }

        /// *listonly*
        public static double DoubleField(double value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            return EditorGUI.DoubleField(r, value);
        }

        /// *listonly*
        public static double DoubleField(double value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.DoubleField(r, value, style);
        }

        /// *listonly*
        public static double DoubleField(string label, double value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            return EditorGUI.DoubleField(r, label, value);
        }

        /// *listonly*
        public static double DoubleField(string label, double value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.DoubleField(r, label, value, style);
        }

        /// *listonly*
        public static double DoubleField(GUIContent label, double value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            return EditorGUI.DoubleField(r, label, value);
        }

        // Make a text field for entering double values.
        public static double DoubleField(GUIContent label, double value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.DoubleField(r, label, value, style);
        }

        /// *listonly*
        public static double DelayedDoubleField(double value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            return EditorGUI.DelayedDoubleField(r, value);
        }

        /// *listonly*
        public static double DelayedDoubleField(double value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.DelayedDoubleField(r, value, style);
        }

        /// *listonly*
        public static double DelayedDoubleField(string label, double value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            return EditorGUI.DelayedDoubleField(r, label, value);
        }

        /// *listonly*
        public static double DelayedDoubleField(string label, double value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.DelayedDoubleField(r, label, value, style);
        }

        /// *listonly*
        public static double DelayedDoubleField(GUIContent label, double value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            return EditorGUI.DelayedDoubleField(r, label, value);
        }

        // Make a delayed text field for entering double values.
        public static double DelayedDoubleField(GUIContent label, double value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.DelayedDoubleField(r, label, value, style);
        }

        /// *listonly*
        public static int IntField(int value, params GUILayoutOption[] options)
        {
            return IntField(value, EditorStyles.numberField, options);
        }

        /// *listonly*
        public static int IntField(int value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.IntField(r, value, style);
        }

        /// *listonly*
        public static int IntField(string label, int value, params GUILayoutOption[] options)
        {
            return IntField(label, value, EditorStyles.numberField, options);
        }

        /// *listonly*
        public static int IntField(string label, int value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.IntField(r, label, value, style);
        }

        /// *listonly*
        public static int IntField(GUIContent label, int value, params GUILayoutOption[] options)
        {
            return IntField(label, value, EditorStyles.numberField, options);
        }

        // Make a text field for entering integers.
        public static int IntField(GUIContent label, int value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.IntField(r, label, value, style);
        }

        /// *listonly*
        public static int DelayedIntField(int value, params GUILayoutOption[] options)
        {
            return DelayedIntField(value, EditorStyles.numberField, options);
        }

        /// *listonly*
        public static int DelayedIntField(int value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.DelayedIntField(r, value, style);
        }

        /// *listonly*
        public static int DelayedIntField(string label, int value, params GUILayoutOption[] options)
        {
            return DelayedIntField(label, value, EditorStyles.numberField, options);
        }

        /// *listonly*
        public static int DelayedIntField(string label, int value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.DelayedIntField(r, label, value, style);
        }

        /// *listonly*
        public static int DelayedIntField(GUIContent label, int value, params GUILayoutOption[] options)
        {
            return DelayedIntField(label, value, EditorStyles.numberField, options);
        }

        // Make a text field for entering integers.
        public static int DelayedIntField(GUIContent label, int value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.DelayedIntField(r, label, value, style);
        }

        /// *listonly*
        public static void DelayedIntField(SerializedProperty property, params GUILayoutOption[] options)
        {
            DelayedIntField(property, null, options);
        }

        // Make a text field for entering integers.
        public static void DelayedIntField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(EditorGUI.LabelHasContent(label), EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
            EditorGUI.DelayedIntField(r, property, label);
        }

        /// *listonly*
        public static long LongField(long value, params GUILayoutOption[] options)
        {
            return LongField(value, EditorStyles.numberField, options);
        }

        /// *listonly*
        public static long LongField(long value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.LongField(r, value, style);
        }

        /// *listonly*
        public static long LongField(string label, long value, params GUILayoutOption[] options)
        {
            return LongField(label, value, EditorStyles.numberField, options);
        }

        /// *listonly*
        public static long LongField(string label, long value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.LongField(r, label, value, style);
        }

        /// *listonly*
        public static long LongField(GUIContent label, long value, params GUILayoutOption[] options)
        {
            return LongField(label, value, EditorStyles.numberField, options);
        }

        // Make a text field for entering integers.
        public static long LongField(GUIContent label, long value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.LongField(r, label, value, style);
        }

        /// *listonly*
        public static float Slider(float value, float leftValue, float rightValue, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetSliderRect(false, options);
            return EditorGUI.Slider(r, value, leftValue, rightValue);
        }

        /// *listonly*
        public static float Slider(string label, float value, float leftValue, float rightValue, params GUILayoutOption[] options)
        {
            return Slider(EditorGUIUtility.TempContent(label), value, leftValue, rightValue, options);
        }

        // Make a slider the user can drag to change a value between a min and a max.
        public static float Slider(GUIContent label, float value, float leftValue, float rightValue, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetSliderRect(true, options);
            return EditorGUI.Slider(r, label, value, leftValue, rightValue);
        }

        /// *listonly*
        public static void Slider(SerializedProperty property, float leftValue, float rightValue, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetSliderRect(false, options);
            EditorGUI.Slider(r, property, leftValue, rightValue);
        }

        internal static void SliderWithTexture(GUIContent label, SerializedProperty property, float leftValue, float rightValue, float power, GUIStyle sliderStyle, GUIStyle thumbStyle, Texture2D sliderBackground, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetSliderRect(false, options);
            EditorGUI.SliderWithTexture(r, label, property, leftValue, rightValue, power, sliderStyle, thumbStyle, sliderBackground);
        }

        /// *listonly*
        public static void Slider(SerializedProperty property, float leftValue, float rightValue, string label, params GUILayoutOption[] options)
        {
            Slider(property, leftValue, rightValue, EditorGUIUtility.TempContent(label), options);
        }

        // Make a slider the user can drag to change a value between a min and a max.
        public static void Slider(SerializedProperty property, float leftValue, float rightValue, GUIContent label, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetSliderRect(true, options);
            EditorGUI.Slider(r, property, leftValue, rightValue, label);
        }

        /// *listonly*
        internal static float PowerSlider(string label, float value, float leftValue, float rightValue, float power, params GUILayoutOption[] options)
        {
            return PowerSlider(EditorGUIUtility.TempContent(label), value, leftValue, rightValue, power, options);
        }

        // Make a power slider the user can drag to change a value between a min and a max.
        internal static float PowerSlider(GUIContent label, float value, float leftValue, float rightValue, float power, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetSliderRect(true, options);
            return EditorGUI.PowerSlider(r, label, value, leftValue, rightValue, power);
        }

        /// *listonly*
        public static int IntSlider(int value, int leftValue, int rightValue, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetSliderRect(false, options);
            return EditorGUI.IntSlider(r, value, leftValue, rightValue);
        }

        /// *listonly*
        public static int IntSlider(string label, int value, int leftValue, int rightValue, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetSliderRect(true, options);
            return EditorGUI.IntSlider(r, label, value, leftValue, rightValue);
        }

        // Make a slider the user can drag to change an integer value between a min and a max.
        public static int IntSlider(GUIContent label, int value, int leftValue, int rightValue, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetSliderRect(true, options);
            return EditorGUI.IntSlider(r, label, value, leftValue, rightValue);
        }

        /// *listonly*
        public static void IntSlider(SerializedProperty property, int leftValue, int rightValue, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetSliderRect(false, options);
            EditorGUI.IntSlider(r, property, leftValue, rightValue, property.displayName);
        }

        /// *listonly*
        public static void IntSlider(SerializedProperty property, int leftValue, int rightValue, string label, params GUILayoutOption[] options)
        {
            IntSlider(property, leftValue, rightValue, EditorGUIUtility.TempContent(label), options);
        }

        // Make a slider the user can drag to change an integer value between a min and a max.
        public static void IntSlider(SerializedProperty property, int leftValue, int rightValue, GUIContent label, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetSliderRect(true, options);
            EditorGUI.IntSlider(r, property, leftValue, rightValue, label);
        }

        /// *listonly*
        public static void MinMaxSlider(ref float minValue, ref float maxValue, float minLimit, float maxLimit, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetSliderRect(false, options);
            EditorGUI.MinMaxSlider(r, ref minValue, ref maxValue, minLimit, maxLimit);
        }

        /// *listonly*
        public static void MinMaxSlider(string label, ref float minValue, ref float maxValue, float minLimit, float maxLimit, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetSliderRect(true, options);
            EditorGUI.MinMaxSlider(r, label, ref minValue, ref maxValue, minLimit, maxLimit);
        }

        // Make a special slider the user can use to specify a range between a min and a max.
        public static void MinMaxSlider(GUIContent label, ref float minValue, ref float maxValue, float minLimit, float maxLimit, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetSliderRect(true, options);
            EditorGUI.MinMaxSlider(r, label, ref minValue, ref maxValue, minLimit, maxLimit);
        }

        /// *listonly*
        public static int Popup(int selectedIndex, string[] displayedOptions, params GUILayoutOption[] options)
        {
            return Popup(selectedIndex, displayedOptions, EditorStyles.popup, options);
        }

        /// *listonly*
        public static int Popup(int selectedIndex, string[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.Popup(r, selectedIndex, displayedOptions, style);
        }

        /// *listonly*
        public static int Popup(int selectedIndex, GUIContent[] displayedOptions, params GUILayoutOption[] options)
        {
            return Popup(selectedIndex, displayedOptions, EditorStyles.popup, options);
        }

        /// *listonly*
        public static int Popup(int selectedIndex, GUIContent[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.Popup(r, selectedIndex, displayedOptions, style);
        }

        /// *listonly*
        public static int Popup(string label, int selectedIndex, string[] displayedOptions, params GUILayoutOption[] options)
        {
            return Popup(label, selectedIndex, displayedOptions, EditorStyles.popup, options);
        }

        /// *listonly*
        public static int Popup(string label, int selectedIndex, string[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.Popup(r, label, selectedIndex, displayedOptions, style);
        }

        /// *listonly*
        public static int Popup(GUIContent label, int selectedIndex, GUIContent[] displayedOptions, params GUILayoutOption[] options)
        {
            return Popup(label, selectedIndex, displayedOptions, EditorStyles.popup, options);
        }

        // Make a generic popup selection field.
        public static int Popup(GUIContent label, int selectedIndex, GUIContent[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.Popup(r, label, selectedIndex, displayedOptions, style);
        }

        internal static void Popup(SerializedProperty property, GUIContent[] displayedOptions, params GUILayoutOption[] options)
        {
            Popup(property, displayedOptions, null, options);
        }

        internal static void Popup(SerializedProperty property, GUIContent[] displayedOptions, GUIContent label, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup, options);
            EditorGUI.Popup(r, property, displayedOptions, label);
        }

        /// *listonly*
        public static System.Enum EnumPopup(System.Enum selected, params GUILayoutOption[] options)
        {
            return EnumPopup(selected, EditorStyles.popup, options);
        }

        /// *listonly*
        public static System.Enum EnumPopup(System.Enum selected, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.EnumPopup(r, selected, style);
        }

        /// *listonly*
        public static System.Enum EnumPopup(string label, System.Enum selected, params GUILayoutOption[] options)
        {
            return EnumPopup(label, selected, EditorStyles.popup, options);
        }

        /// *listonly*
        public static System.Enum EnumPopup(string label, System.Enum selected, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.EnumPopup(r, GUIContent.Temp(label), selected, style);
        }

        /// *listonly*
        public static System.Enum EnumPopup(GUIContent label, System.Enum selected, params GUILayoutOption[] options)
        {
            return EnumPopup(label, selected, EditorStyles.popup, options);
        }

        // Make an enum popup selection field.
        public static System.Enum EnumPopup(GUIContent label, System.Enum selected, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.EnumPopup(r, label, selected, style);
        }

        /// *listonly*
        public static int IntPopup(int selectedValue, string[] displayedOptions, int[] optionValues, params GUILayoutOption[] options)
        {
            return IntPopup(selectedValue, displayedOptions, optionValues, EditorStyles.popup, options);
        }

        /// *listonly*
        public static int IntPopup(int selectedValue, string[] displayedOptions, int[] optionValues, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.IntPopup(r,  selectedValue, displayedOptions, optionValues, style);
        }

        /// *listonly*
        public static int IntPopup(int selectedValue, GUIContent[] displayedOptions, int[] optionValues, params GUILayoutOption[] options)
        {
            return IntPopup(selectedValue, displayedOptions, optionValues, EditorStyles.popup, options);
        }

        /// *listonly*
        public static int IntPopup(int selectedValue, GUIContent[] displayedOptions, int[] optionValues, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.IntPopup(r,  GUIContent.none, selectedValue, displayedOptions, optionValues, style);
        }

        /// *listonly*
        public static int IntPopup(string label, int selectedValue, string[] displayedOptions, int[] optionValues, params GUILayoutOption[] options)
        {
            return IntPopup(label, selectedValue, displayedOptions, optionValues, EditorStyles.popup, options);
        }

        /// *listonly*
        public static int IntPopup(string label, int selectedValue, string[] displayedOptions, int[] optionValues, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.IntPopup(r, label, selectedValue, displayedOptions, optionValues, style);
        }

        /// *listonly*
        public static int IntPopup(GUIContent label, int selectedValue, GUIContent[] displayedOptions, int[] optionValues, params GUILayoutOption[] options)
        {
            return IntPopup(label, selectedValue, displayedOptions, optionValues, EditorStyles.popup, options);
        }

        // Make an integer popup selection field.
        public static int IntPopup(GUIContent label, int selectedValue, GUIContent[] displayedOptions, int[] optionValues, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.IntPopup(r, label, selectedValue, displayedOptions, optionValues, style);
        }

        /// *listonly*
        public static void IntPopup(SerializedProperty property, GUIContent[] displayedOptions, int[] optionValues, params GUILayoutOption[] options)
        {
            IntPopup(property, displayedOptions, optionValues, null, options);
        }

        // Make an integer popup selection field.
        public static void IntPopup(SerializedProperty property, GUIContent[] displayedOptions, int[] optionValues, GUIContent label, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup, options);
            EditorGUI.IntPopup(r, property, displayedOptions, optionValues, label);
        }

        [System.Obsolete("This function is obsolete and the style is not used.")]
        public static void IntPopup(SerializedProperty property, GUIContent[] displayedOptions, int[] optionValues, GUIContent label, GUIStyle style, params GUILayoutOption[] options)
        {
            IntPopup(property, displayedOptions, optionValues, label, options);
        }

        /// *listonly*
        public static string TagField(string tag, params GUILayoutOption[] options)
        {
            return TagField(tag, EditorStyles.popup, options);
        }

        /// *listonly*
        public static string TagField(string tag, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.TagField(r, tag, style);
        }

        /// *listonly*
        public static string TagField(string label, string tag, params GUILayoutOption[] options)
        {
            return TagField(EditorGUIUtility.TempContent(label), tag, EditorStyles.popup, options);
        }

        /// *listonly*
        public static string TagField(string label, string tag, GUIStyle style, params GUILayoutOption[] options)
        {
            return TagField(EditorGUIUtility.TempContent(label), tag, style, options);
        }

        /// *listonly*
        public static string TagField(GUIContent label, string tag, params GUILayoutOption[] options)
        {
            return TagField(label, tag, EditorStyles.popup, options);
        }

        // Make a tag selection field.
        public static string TagField(GUIContent label, string tag, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.TagField(r, label, tag, style);
        }

        /// *listonly*
        public static int LayerField(int layer, params GUILayoutOption[] options)
        {
            return LayerField(layer, EditorStyles.popup, options);
        }

        /// *listonly*
        public static int LayerField(int layer, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.LayerField(r, layer, style);
        }

        /// *listonly*
        public static int LayerField(string label, int layer, params GUILayoutOption[] options)
        {
            return LayerField(EditorGUIUtility.TempContent(label), layer, EditorStyles.popup, options);
        }

        /// *listonly*
        public static int LayerField(string label, int layer, GUIStyle style, params GUILayoutOption[] options)
        {
            return LayerField(EditorGUIUtility.TempContent(label), layer, style, options);
        }

        /// *listonly*
        public static int LayerField(GUIContent label, int layer, params GUILayoutOption[] options)
        {
            return LayerField(label, layer, EditorStyles.popup, options);
        }

        // Make a layer selection field.
        public static int LayerField(GUIContent label, int layer, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.LayerField(r, label, layer, style);
        }

        /// *listonly*
        public static int MaskField(GUIContent label, int mask, string[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
        {
            var r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.MaskField(r, label, mask, displayedOptions, style);
        }

        /// *listonly*
        public static int MaskField(string label, int mask, string[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
        {
            var r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.MaskField(r, label, mask, displayedOptions, style);
        }

        /// *listonly*
        public static int MaskField(GUIContent label, int mask, string[] displayedOptions, params GUILayoutOption[] options)
        {
            var r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup, options);
            return EditorGUI.MaskField(r, label, mask, displayedOptions, EditorStyles.popup);
        }

        /// *listonly*
        public static int MaskField(string label, int mask, string[] displayedOptions, params GUILayoutOption[] options)
        {
            var r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.popup, options);
            return EditorGUI.MaskField(r, label, mask, displayedOptions, EditorStyles.popup);
        }

        /// *listonly*
        public static int MaskField(int mask, string[] displayedOptions, GUIStyle style, params GUILayoutOption[] options)
        {
            var r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.MaskField(r, mask, displayedOptions, style);
        }

        // Make a field for masks.
        public static int MaskField(int mask, string[] displayedOptions, params GUILayoutOption[] options)
        {
            var r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.popup, options);
            return EditorGUI.MaskField(r, mask, displayedOptions, EditorStyles.popup);
        }

        public static Enum EnumFlagsField(Enum enumValue, params GUILayoutOption[] options)
        {
            return EnumFlagsField(enumValue, EditorStyles.popup, options);
        }

        public static Enum EnumFlagsField(Enum enumValue, GUIStyle style, params GUILayoutOption[] options)
        {
            var position = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.EnumFlagsField(position, enumValue, style);
        }

        public static Enum EnumFlagsField(string label, Enum enumValue, params GUILayoutOption[] options)
        {
            return EnumFlagsField(label, enumValue, EditorStyles.popup, options);
        }

        public static Enum EnumFlagsField(string label, Enum enumValue, GUIStyle style, params GUILayoutOption[] options)
        {
            return EnumFlagsField(EditorGUIUtility.TempContent(label), enumValue, style, options);
        }

        public static Enum EnumFlagsField(GUIContent label, Enum enumValue, params GUILayoutOption[] options)
        {
            return EnumFlagsField(label, enumValue, EditorStyles.popup, options);
        }

        public static Enum EnumFlagsField(GUIContent label, Enum enumValue, GUIStyle style, params GUILayoutOption[] options)
        {
            var position = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, style, options);
            return EditorGUI.EnumFlagsField(position, label, enumValue, style);
        }

        [System.Obsolete("Check the docs for the usage of the new parameter 'allowSceneObjects'.")]
        public static Object ObjectField(Object obj, System.Type objType, params GUILayoutOption[] options)
        {
            return ObjectField(obj, objType, true, options);
        }

        /// *listonly*
        public static Object ObjectField(Object obj, System.Type objType, bool allowSceneObjects, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, options);
            return EditorGUI.ObjectField(r, obj, objType, allowSceneObjects);
        }

        [System.Obsolete("Check the docs for the usage of the new parameter 'allowSceneObjects'.")]
        public static Object ObjectField(string label, Object obj, System.Type objType, params GUILayoutOption[] options)
        {
            return ObjectField(label, obj, objType, true, options);
        }

        /// *listonly*
        public static Object ObjectField(string label, Object obj, System.Type objType, bool allowSceneObjects, params GUILayoutOption[] options)
        {
            return ObjectField(EditorGUIUtility.TempContent(label), obj, objType, allowSceneObjects, options);
        }

        [System.Obsolete("Check the docs for the usage of the new parameter 'allowSceneObjects'.")]
        public static Object ObjectField(GUIContent label, Object obj, System.Type objType, params GUILayoutOption[] options)
        {
            return ObjectField(label, obj, objType, true, options);
        }

        // Make an object field. You can assign objects either by drag'n drop objects or by selecting an object using the Object Picker.
        public static Object ObjectField(GUIContent label, Object obj, System.Type objType, bool allowSceneObjects, params GUILayoutOption[] options)
        {
            float height;
            if (EditorGUIUtility.HasObjectThumbnail(objType))
            {
                height = EditorGUI.kObjectFieldThumbnailHeight;
            }
            else
            {
                height = EditorGUI.kSingleLineHeight;
            }
            Rect r = s_LastRect = GetControlRect(true, height, options);
            return EditorGUI.ObjectField(r, label, obj, objType, allowSceneObjects);
        }

        /// *listonly*
        public static void ObjectField(SerializedProperty property, params GUILayoutOption[] options)
        {
            ObjectField(property, (GUIContent)null, options);
        }

        /// *listonly*
        public static void ObjectField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.objectField, options);
            EditorGUI.ObjectField(r, property, label);
        }

        /// *listonly*
        public static void ObjectField(SerializedProperty property, System.Type objType, params GUILayoutOption[] options)
        {
            ObjectField(property, objType, null, options);
        }

        // Make an object field. You can assign objects either by drag'n drop objects or by selecting an object using the Object Picker.
        public static void ObjectField(SerializedProperty property, System.Type objType, GUIContent label, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.objectField, options);
            EditorGUI.ObjectField(r, property, objType, label);
        }

        internal static Object MiniThumbnailObjectField(GUIContent label, Object obj, System.Type objType, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, options);
            return EditorGUI.MiniThumbnailObjectField(r, label, obj, objType);
        }

        /// *listonly*
        public static Vector2 Vector2Field(string label, Vector2 value, params GUILayoutOption[] options)
        {
            return Vector2Field(EditorGUIUtility.TempContent(label), value, options);
        }

        // Make an X & Y field for entering a [[Vector2]].
        public static Vector2 Vector2Field(GUIContent label, Vector2 value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector2, label), EditorStyles.numberField, options);
            return EditorGUI.Vector2Field(r, label, value);
        }

        /// *listonly*
        public static Vector3 Vector3Field(string label, Vector3 value, params GUILayoutOption[] options)
        {
            return Vector3Field(EditorGUIUtility.TempContent(label), value, options);
        }

        // Make an X, Y & Z field for entering a [[Vector3]].
        public static Vector3 Vector3Field(GUIContent label, Vector3 value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, label), EditorStyles.numberField, options);
            return EditorGUI.Vector3Field(r, label, value);
        }

        // Make an X, Y, Z & W field for entering a [[Vector4]].
        public static Vector4 Vector4Field(string label, Vector4 value, params GUILayoutOption[] options)
        {
            return Vector4Field(EditorGUIUtility.TempContent(label), value, options);
        }

        // Make an X, Y, Z & W field for entering a [[Vector4]].
        public static Vector4 Vector4Field(GUIContent label, Vector4 value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector4, label), EditorStyles.numberField, options);
            return EditorGUI.Vector4Field(r, label, value);
        }

        /// *listonly*
        public static Vector2Int Vector2IntField(string label, Vector2Int value, params GUILayoutOption[] options)
        {
            return Vector2IntField(EditorGUIUtility.TempContent(label), value, options);
        }

        // Make an X & Y field for entering a [[Vector2Int]].
        public static Vector2Int Vector2IntField(GUIContent label, Vector2Int value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector2Int, label), EditorStyles.numberField, options);
            return EditorGUI.Vector2IntField(r, label, value);
        }

        /// *listonly*
        public static Vector3Int Vector3IntField(string label, Vector3Int value, params GUILayoutOption[] options)
        {
            return Vector3IntField(EditorGUIUtility.TempContent(label), value, options);
        }

        // Make an X, Y & Z field for entering a [[Vector3Int]].
        public static Vector3Int Vector3IntField(GUIContent label, Vector3Int value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3Int, label), EditorStyles.numberField, options);
            return EditorGUI.Vector3IntField(r, label, value);
        }

        /// *listonly*
        public static Rect RectField(Rect value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.GetPropertyHeight(SerializedPropertyType.Rect, GUIContent.none), EditorStyles.numberField, options);
            return EditorGUI.RectField(r, value);
        }

        /// *listonly*
        public static Rect RectField(string label, Rect value, params GUILayoutOption[] options)
        {
            return RectField(EditorGUIUtility.TempContent(label), value, options);
        }

        // Make an X, Y, W & H field for entering a [[Rect]].
        public static Rect RectField(GUIContent label, Rect value, params GUILayoutOption[] options)
        {
            bool hasLabel = EditorGUI.LabelHasContent(label);
            float height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Rect, label);
            Rect r = s_LastRect = GetControlRect(hasLabel, height, EditorStyles.numberField, options);
            return EditorGUI.RectField(r, label, value);
        }

        /// *listonly*
        public static RectInt RectIntField(RectInt value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.GetPropertyHeight(SerializedPropertyType.RectInt, GUIContent.none), EditorStyles.numberField, options);
            return EditorGUI.RectIntField(r, value);
        }

        /// *listonly*
        public static RectInt RectIntField(string label, RectInt value, params GUILayoutOption[] options)
        {
            return RectIntField(EditorGUIUtility.TempContent(label), value, options);
        }

        // Make an X, Y, W & H field for entering a [[RectInt]].
        public static RectInt RectIntField(GUIContent label, RectInt value, params GUILayoutOption[] options)
        {
            bool hasLabel = EditorGUI.LabelHasContent(label);
            float height = EditorGUI.GetPropertyHeight(SerializedPropertyType.RectInt, label);
            Rect r = s_LastRect = GetControlRect(hasLabel, height, EditorStyles.numberField, options);
            return EditorGUI.RectIntField(r, label, value);
        }

        /// *listonly*
        public static Bounds BoundsField(Bounds value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.GetPropertyHeight(SerializedPropertyType.Bounds, GUIContent.none), EditorStyles.numberField, options);
            return EditorGUI.BoundsField(r, value);
        }

        /// *listonly*
        public static Bounds BoundsField(string label, Bounds value, params GUILayoutOption[] options)
        {
            return BoundsField(EditorGUIUtility.TempContent(label), value, options);
        }

        // Make Center & Extents field for entering a [[Bounds]].
        public static Bounds BoundsField(GUIContent label, Bounds value, params GUILayoutOption[] options)
        {
            bool hasLabel = EditorGUI.LabelHasContent(label);
            float height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Bounds, label);
            Rect r = s_LastRect = GetControlRect(hasLabel, height, EditorStyles.numberField, options);
            return EditorGUI.BoundsField(r, label, value);
        }

        /// *listonly*
        public static BoundsInt BoundsIntField(BoundsInt value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.GetPropertyHeight(SerializedPropertyType.BoundsInt, GUIContent.none), EditorStyles.numberField, options);
            return EditorGUI.BoundsIntField(r, value);
        }

        /// *listonly*
        public static BoundsInt BoundsIntField(string label, BoundsInt value, params GUILayoutOption[] options)
        {
            return BoundsIntField(EditorGUIUtility.TempContent(label), value, options);
        }

        // Make Center & Extents field for entering a [[BoundsInt]].
        public static BoundsInt BoundsIntField(GUIContent label, BoundsInt value, params GUILayoutOption[] options)
        {
            bool hasLabel = EditorGUI.LabelHasContent(label);
            float height = EditorGUI.GetPropertyHeight(SerializedPropertyType.BoundsInt, label);
            Rect r = s_LastRect = GetControlRect(hasLabel, height, EditorStyles.numberField, options);
            return EditorGUI.BoundsIntField(r, label, value);
        }

        // Make a property field that look like a multi property field (but is made up of individual properties)
        internal static void PropertiesField(GUIContent label, SerializedProperty[] properties, GUIContent[] propertyLabels, float propertyLabelsWidth, params GUILayoutOption[] options)
        {
            bool hasLabel = EditorGUI.LabelHasContent(label);
            float height = EditorGUI.kSingleLineHeight * properties.Length + EditorGUI.kVerticalSpacingMultiField * (properties.Length - 1);
            Rect r = s_LastRect = GetControlRect(hasLabel, height, EditorStyles.numberField, options);
            EditorGUI.PropertiesField(r, label, properties, propertyLabels, propertyLabelsWidth);
        }

        internal static int CycleButton(int selected, GUIContent[] options, GUIStyle style)
        {
            if (GUILayout.Button(options[selected], style))
            {
                selected++;
                if (selected >= options.Length)
                {
                    selected = 0;
                }
            }
            return selected;
        }

        /// *listonly*
        public static Color ColorField(Color value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
            return EditorGUI.ColorField(r, value);
        }

        /// *listonly*
        public static Color ColorField(string label, Color value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
            return EditorGUI.ColorField(r, label, value);
        }

        // Make a field for selecting a [[Color]].
        public static Color ColorField(GUIContent label, Color value, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
            return EditorGUI.ColorField(r, label, value);
        }

        public static Color ColorField(GUIContent label, Color value, bool showEyedropper, bool showAlpha, bool hdr, ColorPickerHDRConfig hdrConfig, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
            return EditorGUI.ColorField(r, label, value, showEyedropper, showAlpha, hdr, hdrConfig);
        }

        /// *listonly*
        public static AnimationCurve CurveField(AnimationCurve value, params GUILayoutOption[] options)
        {
            // TODO Change style
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
            return EditorGUI.CurveField(r, value);
        }

        /// *listonly*
        public static AnimationCurve CurveField(string label, AnimationCurve value, params GUILayoutOption[] options)
        {
            // TODO Change style
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
            return EditorGUI.CurveField(r, label, value);
        }

        /// *listonly*
        public static AnimationCurve CurveField(GUIContent label, AnimationCurve value, params GUILayoutOption[] options)
        {
            // TODO Change style
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
            return EditorGUI.CurveField(r, label, value);
        }

        // Variants with settings
        /// *listonly*
        public static AnimationCurve CurveField(AnimationCurve value, Color color, Rect ranges, params GUILayoutOption[] options)
        {
            // TODO Change style
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
            return EditorGUI.CurveField(r, value, color, ranges);
        }

        /// *listonly*
        public static AnimationCurve CurveField(string label, AnimationCurve value, Color color, Rect ranges, params GUILayoutOption[] options)
        {
            // TODO Change style
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
            return EditorGUI.CurveField(r, label, value, color, ranges);
        }

        // Make a field for editing an [[AnimationCurve]].
        public static AnimationCurve CurveField(GUIContent label, AnimationCurve value, Color color, Rect ranges, params GUILayoutOption[] options)
        {
            // TODO Change style
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
            return EditorGUI.CurveField(r, label, value, color, ranges);
        }

        /// *listonly*
        public static void CurveField(SerializedProperty property, Color color, Rect ranges, params GUILayoutOption[] options)
        {
            CurveField(property, color, ranges, null, options);
        }

        // Make a field for editing an [[AnimationCurve]].
        public static void CurveField(SerializedProperty property, Color color, Rect ranges, GUIContent label, params GUILayoutOption[] options)
        {
            // TODO Change style
            Rect r = s_LastRect = GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.colorField, options);
            EditorGUI.CurveField(r, property, color, ranges, label);
        }

        /// *listonly*
        public static bool InspectorTitlebar(bool foldout, Object targetObj)
        {
            return InspectorTitlebar(foldout, targetObj, true);
        }

        public static bool InspectorTitlebar(bool foldout, Object targetObj, bool expandable)
        {
            return EditorGUI.InspectorTitlebar(GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebar), foldout,
                targetObj, expandable);
        }

        // Make an inspector-window-like titlebar.
        public static bool InspectorTitlebar(bool foldout, Object[] targetObjs)
        {
            return InspectorTitlebar(foldout, targetObjs, true);
        }

        public static bool InspectorTitlebar(bool foldout, Object[] targetObjs, bool expandable)
        {
            return EditorGUI.InspectorTitlebar(GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebar), foldout,
                targetObjs, expandable);
        }

        public static void InspectorTitlebar(Object[] targetObjs)
        {
            EditorGUI.InspectorTitlebar(GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebar), targetObjs);
        }

        // Make an foldout with a toggle and title
        internal static bool ToggleTitlebar(bool foldout, GUIContent label, ref bool toggleValue)
        {
            return EditorGUI.ToggleTitlebar(GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebar), label, foldout, ref toggleValue);
        }

        internal static bool ToggleTitlebar(bool foldout, GUIContent label, SerializedProperty property)
        {
            bool toggleValue = property.boolValue;
            EditorGUI.BeginChangeCheck();
            foldout = EditorGUI.ToggleTitlebar(GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebar), label, foldout, ref toggleValue);
            if (EditorGUI.EndChangeCheck())
                property.boolValue = toggleValue;

            return foldout;
        }

        internal static bool FoldoutTitlebar(bool foldout, GUIContent label, bool skipIconSpacing)
        {
            return EditorGUI.FoldoutTitlebar(GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebar), label, foldout, skipIconSpacing);
        }

        // Make a label with a foldout arrow to the left of it.
        internal static bool FoldoutInternal(bool foldout, GUIContent content, bool toggleOnLabelClick, GUIStyle style)
        {
            Rect r = s_LastRect = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, EditorGUIUtility.fieldWidth, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, style);
            return EditorGUI.Foldout(r, foldout, content, toggleOnLabelClick, style);
        }

        internal static void LayerMaskField(UInt32 layers, GUIContent label, EditorUtility.SelectMenuItemFunction callback, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, options);
            EditorGUI.LayerMaskField(r, layers, label, callback);
        }

        internal static void LayerMaskField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(true, EditorGUI.kSingleLineHeight, options);
            EditorGUI.LayerMaskField(r, property, label);
        }

        /// *listonly*
        public static void HelpBox(string message, MessageType type)
        {
            EditorGUILayout.LabelField(GUIContent.none, EditorGUIUtility.TempContent(message, EditorGUIUtility.GetHelpIcon(type)), EditorStyles.helpBox);
        }

        // Make a help box with a message to the user.
        public static void HelpBox(string message, MessageType type, bool wide)
        {
            EditorGUILayout.LabelField(wide ? GUIContent.none : EditorGUIUtility.blankContent,
                EditorGUIUtility.TempContent(message, EditorGUIUtility.GetHelpIcon(type)),
                EditorStyles.helpBox);
        }

        // Make a label in front of some control.
        internal static void PrefixLabelInternal(GUIContent label, GUIStyle followingStyle, GUIStyle labelStyle)
        {
            float p = followingStyle.margin.left;
            if (!EditorGUI.LabelHasContent(label))
            {
                GUILayoutUtility.GetRect(EditorGUI.indent - p, EditorGUI.kSingleLineHeight, followingStyle, GUILayout.ExpandWidth(false));
                return;
            }

            Rect r = GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth - p, EditorGUI.kSingleLineHeight, followingStyle, GUILayout.ExpandWidth(false));
            r.xMin += EditorGUI.indent;
            EditorGUI.HandlePrefixLabel(r, r, label, 0, labelStyle);
        }

        // Make a small space between the previous control and the following.
        public static void Space()
        {
            GUILayoutUtility.GetRect(6, 6);
        }

        //[System.Obsolete ("Use Space() instead")]
        // Make this function Obsolete when someone has time to _rename_ all
        // the Standard Packages to Space(), as currently it shows tons of
        // warnings.
        // Same for the graphic tests.
        // *undoc*
        public static void Separator()
        {
            Space();
        }

        public class ToggleGroupScope : GUI.Scope
        {
            public bool enabled { get; protected set; }

            public ToggleGroupScope(string label, bool toggle)
            {
                enabled = BeginToggleGroup(label, toggle);
            }

            public ToggleGroupScope(GUIContent label, bool toggle)
            {
                enabled = BeginToggleGroup(label, toggle);
            }

            protected override void CloseScope()
            {
                EndToggleGroup();
            }
        }

        /// *listonly*
        public static bool BeginToggleGroup(string label, bool toggle)
        {
            return BeginToggleGroup(EditorGUIUtility.TempContent(label), toggle);
        }

        // Begin a vertical group with a toggle to enable or disable all the controls within at once.
        public static bool BeginToggleGroup(GUIContent label, bool toggle)
        {
            toggle = EditorGUILayout.ToggleLeft(label, toggle, EditorStyles.boldLabel);
            EditorGUI.BeginDisabled(!toggle);
            GUILayout.BeginVertical();

            return toggle;
        }

        // Close a group started with ::ref::BeginToggleGroup
        public static void EndToggleGroup()
        {
            GUILayout.EndVertical();
            EditorGUI.EndDisabled();
        }

        public class HorizontalScope : GUI.Scope
        {
            public Rect rect { get; protected set; }

            public HorizontalScope(params GUILayoutOption[] options)
            {
                rect = BeginHorizontal(options);
            }

            public HorizontalScope(GUIStyle style, params GUILayoutOption[] options)
            {
                rect = BeginHorizontal(style, options);
            }

            internal HorizontalScope(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
            {
                rect = BeginHorizontal(content, style, options);
            }

            protected override void CloseScope()
            {
                EndHorizontal();
            }
        }

        /// *listonly*
        public static Rect BeginHorizontal(params GUILayoutOption[] options)
        {
            return BeginHorizontal(GUIContent.none, GUIStyle.none, options);
        }

        // Begin a horizontal group and get its rect back.
        public static Rect BeginHorizontal(GUIStyle style, params GUILayoutOption[] options)
        {
            return BeginHorizontal(GUIContent.none, style, options);
        }

        // public static Rect BeginHorizontal (string text, params GUILayoutOption[] options)                       { return BeginHorizontal (EditorGUIUtility.TempContent (text), GUIStyle.none, options); }
        /// *listonly*
        // public static Rect BeginHorizontal (Texture image, params GUILayoutOption[] options)                 { return BeginHorizontal (EditorGUIUtility.TempContent (image), GUIStyle.none, options); }
        /// *listonly*
        // public static Rect BeginHorizontal (GUIContent content, params GUILayoutOption[] options)                { return BeginHorizontal (content, GUIStyle.none, options); }
        /// *listonly*
        // public static Rect BeginHorizontal (string text, GUIStyle style, params GUILayoutOption[] options)           { return BeginHorizontal (EditorGUIUtility.TempContent (text), style, options); }
        /// *listonly*
        // public static Rect BeginHorizontal (Texture image, GUIStyle style, params GUILayoutOption[] options)     { return BeginHorizontal (EditorGUIUtility.TempContent (image), style, options); }
        internal static Rect BeginHorizontal(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayoutGroup g = GUILayoutUtility.BeginLayoutGroup(style, options, typeof(GUILayoutGroup));
            g.isVertical = false;
            if (style != GUIStyle.none || content != GUIContent.none)
            {
                GUI.Box(g.rect, GUIContent.none, style);
            }
            return g.rect;
        }

        // Close a group started with BeginHorizontal
        public static void EndHorizontal()
        {
            GUILayout.EndHorizontal();
        }

        public class VerticalScope : GUI.Scope
        {
            public Rect rect { get; protected set; }

            public VerticalScope(params GUILayoutOption[] options)
            {
                rect = BeginVertical(options);
            }

            public VerticalScope(GUIStyle style, params GUILayoutOption[] options)
            {
                rect = BeginVertical(style, options);
            }

            internal VerticalScope(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
            {
                rect = BeginVertical(content, style, options);
            }

            protected override void CloseScope()
            {
                EndVertical();
            }
        }

        /// *listonly*
        public static Rect BeginVertical(params GUILayoutOption[] options)
        {
            return BeginVertical(GUIContent.none, GUIStyle.none, options);
        }

        // Begin a vertical group and get its rect back.
        public static Rect BeginVertical(GUIStyle style, params GUILayoutOption[] options)
        {
            return BeginVertical(GUIContent.none, style, options);
        }

        /// *listonly*
        // public static Rect BeginVertical (string text, params GUILayoutOption[] options)                     { return BeginVertical (EditorGUIUtility.TempContent (text), GUIStyle.none, options); }
        /// *listonly*
        // public static Rect BeginVertical (Texture image, params GUILayoutOption[] options)                   { return BeginVertical (EditorGUIUtility.TempContent (image), GUIStyle.none, options); }
        /// *listonly*
        // public static Rect BeginVertical (GUIContent content, params GUILayoutOption[] options)              { return BeginVertical (content, GUIStyle.none, options); }
        /// *listonly*
        // public static Rect BeginVertical (string text, GUIStyle style, params GUILayoutOption[] options)         { return BeginVertical (EditorGUIUtility.TempContent (text), style, options); }
        /// *listonly*
        // public static Rect BeginVertical (Texture image, GUIStyle style, params GUILayoutOption[] options)       { return BeginVertical (EditorGUIUtility.TempContent (image), style, options); }
        internal static Rect BeginVertical(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayoutGroup g = GUILayoutUtility.BeginLayoutGroup(style, options, typeof(GUILayoutGroup));
            g.isVertical = true;
            if (style != GUIStyle.none || content != GUIContent.none)
            {
                GUI.Box(g.rect, GUIContent.none, style);
            }
            return g.rect;
        }

        // Close a group started with BeginVertical
        public static void EndVertical()
        {
            GUILayout.EndVertical();
        }

        public class ScrollViewScope : GUI.Scope
        {
            public Vector2 scrollPosition { get; protected set; }
            public bool handleScrollWheel { get; set; }

            public ScrollViewScope(Vector2 scrollPosition, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, options);
            }

            public ScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, options);
            }

            public ScrollViewScope(Vector2 scrollPosition, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, horizontalScrollbar, verticalScrollbar, options);
            }

            public ScrollViewScope(Vector2 scrollPosition, GUIStyle style, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, style, options);
            }

            public ScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background, options);
            }

            internal ScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, options);
            }

            protected override void CloseScope()
            {
                EndScrollView(handleScrollWheel);
            }
        }

        /// *listonly*
        public static Vector2 BeginScrollView(Vector2 scrollPosition, params GUILayoutOption[] options)
        {
            return BeginScrollView(scrollPosition, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.scrollView, options);
        }

        /// *listonly*
        public static Vector2 BeginScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, params GUILayoutOption[] options)
        {
            return BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.scrollView, options);
        }

        /// *listonly*
        public static Vector2 BeginScrollView(Vector2 scrollPosition, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
        {
            return BeginScrollView(scrollPosition, false, false, horizontalScrollbar, verticalScrollbar, GUI.skin.scrollView, options);
        }

        /// *listonly*
        public static Vector2 BeginScrollView(Vector2 scrollPosition, GUIStyle style, params GUILayoutOption[] options)
        {
            string name = style.name;

            GUIStyle vertical = GUI.skin.FindStyle(name + "VerticalScrollbar");
            if (vertical == null)
            {
                vertical = GUI.skin.verticalScrollbar;
            }
            GUIStyle horizontal = GUI.skin.FindStyle(name + "HorizontalScrollbar");
            if (horizontal == null)
            {
                horizontal = GUI.skin.horizontalScrollbar;
            }
            return BeginScrollView(scrollPosition, false, false, horizontal, vertical, style, options);
        }

        internal static Vector2 BeginScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options)
        {
            return BeginScrollView(scrollPosition, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, GUI.skin.scrollView, options);
        }

        // Begin an automatically layouted scrollview.
        public static Vector2 BeginScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
        {
            GUIScrollGroup g = (GUIScrollGroup)GUILayoutUtility.BeginLayoutGroup(background, null, typeof(GUIScrollGroup));
            switch (Event.current.type)
            {
                case EventType.Layout:
                    g.resetCoords = true;
                    g.isVertical = true;
                    g.stretchWidth = 1;
                    g.stretchHeight = 1;
                    g.verticalScrollbar = verticalScrollbar;
                    g.horizontalScrollbar = horizontalScrollbar;
                    g.ApplyOptions(options);
                    break;
                default:
                    break;
            }
            return EditorGUIInternal.DoBeginScrollViewForward(g.rect, scrollPosition, new Rect(0, 0, g.clientWidth, g.clientHeight), alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background);
        }

        internal class VerticalScrollViewScope : GUI.Scope
        {
            public Vector2 scrollPosition { get; protected set; }
            public bool handleScrollWheel { get; set; }

            public VerticalScrollViewScope(Vector2 scrollPosition, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginVerticalScrollView(scrollPosition, options);
            }

            public VerticalScrollViewScope(Vector2 scrollPosition, bool alwaysShowVertical, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginVerticalScrollView(scrollPosition, alwaysShowVertical, verticalScrollbar, background, options);
            }

            protected override void CloseScope()
            {
                EndScrollView(handleScrollWheel);
            }
        }

        internal static Vector2 BeginVerticalScrollView(Vector2 scrollPosition, params GUILayoutOption[] options)
        {
            return BeginVerticalScrollView(scrollPosition, false, GUI.skin.verticalScrollbar, GUI.skin.scrollView, options);
        }

        // Begin an automatically layouted scrollview.
        internal static Vector2 BeginVerticalScrollView(Vector2 scrollPosition, bool alwaysShowVertical, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
        {
            GUIScrollGroup g = (GUIScrollGroup)GUILayoutUtility.BeginLayoutGroup(background, null, typeof(GUIScrollGroup));
            switch (Event.current.type)
            {
                case EventType.Layout:
                    g.resetCoords = true;
                    g.isVertical = true;
                    g.stretchWidth = 1;
                    g.stretchHeight = 1;
                    g.verticalScrollbar = verticalScrollbar;
                    g.horizontalScrollbar = GUIStyle.none;
                    g.allowHorizontalScroll = false;
                    g.ApplyOptions(options);
                    break;
                default:
                    break;
            }
            return EditorGUIInternal.DoBeginScrollViewForward(g.rect, scrollPosition, new Rect(0, 0, g.clientWidth, g.clientHeight), false, alwaysShowVertical, GUI.skin.horizontalScrollbar, verticalScrollbar, background);
        }

        internal class HorizontalScrollViewScope : GUI.Scope
        {
            public Vector2 scrollPosition { get; protected set; }
            public bool handleScrollWheel { get; set; }

            public HorizontalScrollViewScope(Vector2 scrollPosition, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginHorizontalScrollView(scrollPosition, options);
            }

            public HorizontalScrollViewScope(Vector2 scrollPosition, bool alwaysShowHorizontal, GUIStyle horizontalScrollbar, GUIStyle background, params GUILayoutOption[] options)
            {
                handleScrollWheel = true;
                this.scrollPosition = BeginHorizontalScrollView(scrollPosition, alwaysShowHorizontal, horizontalScrollbar, background, options);
            }

            protected override void CloseScope()
            {
                EndScrollView(handleScrollWheel);
            }
        }

        internal static Vector2 BeginHorizontalScrollView(Vector2 scrollPosition, params GUILayoutOption[] options)
        {
            return BeginHorizontalScrollView(scrollPosition, false, GUI.skin.horizontalScrollbar, GUI.skin.scrollView, options);
        }

        // Begin an automatically layouted scrollview.

        internal static Vector2 BeginHorizontalScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal, GUIStyle horizontalScrollbar, GUIStyle background, params GUILayoutOption[] options)
        {
            GUIScrollGroup g = (GUIScrollGroup)GUILayoutUtility.BeginLayoutGroup(background, null, typeof(GUIScrollGroup));
            switch (Event.current.type)
            {
                case EventType.Layout:
                    g.resetCoords = true;
                    g.isVertical = true;
                    g.stretchWidth = 1;
                    g.stretchHeight = 1;
                    g.verticalScrollbar = GUIStyle.none;
                    g.horizontalScrollbar = horizontalScrollbar;
                    g.allowHorizontalScroll = true;
                    g.allowVerticalScroll = false;
                    g.ApplyOptions(options);
                    break;
                default:
                    break;
            }
            return EditorGUIInternal.DoBeginScrollViewForward(g.rect, scrollPosition, new Rect(0, 0, g.clientWidth, g.clientHeight), alwaysShowHorizontal, false, horizontalScrollbar, GUI.skin.verticalScrollbar, background);
        }

        // Ends a scrollview started with a call to BeginScrollView.
        public static void EndScrollView()
        {
            GUILayout.EndScrollView(true);
        }

        internal static void EndScrollView(bool handleScrollWheel)
        {
            GUILayout.EndScrollView(handleScrollWheel);
        }

        /// *listonly*
        public static bool PropertyField(SerializedProperty property, params GUILayoutOption[] options)
        {
            return PropertyField(property, null, false, options);
        }

        /// *listonly*
        public static bool PropertyField(SerializedProperty property, GUIContent label, params GUILayoutOption[] options)
        {
            return PropertyField(property, label, false, options);
        }

        /// *listonly*
        public static bool PropertyField(SerializedProperty property, bool includeChildren, params GUILayoutOption[] options)
        {
            return PropertyField(property, null, includeChildren, options);
        }

        // Make a field for [[SerializedProperty]].
        public static bool PropertyField(SerializedProperty property, GUIContent label, bool includeChildren, params GUILayoutOption[] options)
        {
            return ScriptAttributeUtility.GetHandler(property).OnGUILayout(property, label, includeChildren, options);
        }

        public static Rect GetControlRect(params GUILayoutOption[] options)
        {
            return GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.layerMaskField, options);
        }

        public static Rect GetControlRect(bool hasLabel, params GUILayoutOption[] options)
        {
            return GetControlRect(hasLabel, EditorGUI.kSingleLineHeight, EditorStyles.layerMaskField, options);
        }

        public static Rect GetControlRect(bool hasLabel, float height, params GUILayoutOption[] options)
        {
            return GetControlRect(hasLabel, height, EditorStyles.layerMaskField, options);
        }

        public static Rect GetControlRect(bool hasLabel, float height, GUIStyle style, params GUILayoutOption[] options)
        {
            return GUILayoutUtility.GetRect(
                hasLabel ? kLabelFloatMinW : EditorGUIUtility.fieldWidth,
                kLabelFloatMaxW,
                height, height, style, options);
        }

        internal static Rect GetSliderRect(bool hasLabel, params GUILayoutOption[] options)
        {
            return GUILayoutUtility.GetRect(
                hasLabel ? kLabelFloatMinW : EditorGUIUtility.fieldWidth,
                kLabelFloatMaxW + EditorGUI.kSpacing + EditorGUI.kSliderMaxW,
                EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        }

        internal static Rect GetToggleRect(bool hasLabel, params GUILayoutOption[] options)
        {
            // Toggle is 10 pixels wide while float fields are EditorGUIUtility.fieldWidth pixels wide.
            // Store difference in variable and add to min and max width values used for float fields.
            float toggleAdjust = (10 - EditorGUIUtility.fieldWidth);
            return GUILayoutUtility.GetRect(
                hasLabel ? kLabelFloatMinW + toggleAdjust : EditorGUIUtility.fieldWidth + toggleAdjust,
                kLabelFloatMaxW + toggleAdjust,
                EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.numberField, options);
        }

        public class FadeGroupScope : GUI.Scope
        {
            public bool visible { get; protected set; }
            float m_Value;

            public FadeGroupScope(float value)
            {
                visible = BeginFadeGroup(value);
                m_Value = value;
            }

            protected override void CloseScope()
            {
                // See comments with BeginFadeGroup
                if (m_Value != 0 && m_Value != 1)
                    EndFadeGroup();
            }
        }

        public static bool BeginFadeGroup(float value)
        {
            // Fade groups interfere with layout even when the fade group is collapsed because
            // the margins of the elements beofre and after are added together instead of overlapping.
            // This creates unwanted extra space between controls if there's an inactive fade group in between.
            // We avoid this by simply not having the fade group if it's collapsed.
            if (value == 0)
            {
                return false;
            }
            // Active fade groups interfere with styles that have overflow, because the overflow part gets clipped.
            // We avoid this by simply not having the fade group if it's fully expanded.
            if (value == 1)
            {
                return true;
            }

            GUILayoutFadeGroup g = (GUILayoutFadeGroup)GUILayoutUtility.BeginLayoutGroup(GUIStyle.none, null, typeof(GUILayoutFadeGroup));
            g.isVertical = true;
            g.resetCoords = true;
            g.fadeValue = value;
            g.wasGUIEnabled = GUI.enabled;
            g.guiColor = GUI.color;
            if (value != 0.0f && value != 1.0f && Event.current.type == EventType.MouseDown)
            {
                Event.current.Use();
            }

            // We don't want the fade group gui clip to be used for calculating the label width of controls in this fade group, so we lock the context width.
            EditorGUIUtility.LockContextWidth();
            GUI.BeginGroup(g.rect);

            return value != 0;
        }

        public static void EndFadeGroup()
        {
            // If we're inside a fade group, end it here.
            // See BeginFadeGroup for details on why it's not always present.
            GUILayoutFadeGroup g = EditorGUILayoutUtilityInternal.topLevel as GUILayoutFadeGroup;
            if (g != null)
            {
                GUI.EndGroup();
                EditorGUIUtility.UnlockContextWidth();
                GUI.enabled = g.wasGUIEnabled;
                GUI.color = g.guiColor;
                GUILayoutUtility.EndLayoutGroup();
            }
        }

        internal static int BeginPlatformGrouping(BuildPlatform[] platforms, GUIContent defaultTab)
        {
            return BeginPlatformGrouping(platforms, defaultTab, GUI.skin.box);
        }

        internal static int BeginPlatformGrouping(BuildPlatform[] platforms, GUIContent defaultTab, GUIStyle style)
        {
            int selectedPlatform = -1;
            for (int i = 0; i < platforms.Length; i++)
            {
                if (platforms[i].targetGroup == EditorUserBuildSettings.selectedBuildTargetGroup)
                    selectedPlatform = i;
            }
            if (selectedPlatform == -1)
            {
                s_SelectedDefault.value = true;
                selectedPlatform = 0;
            }

            int selected = defaultTab == null ? selectedPlatform : (s_SelectedDefault.value ? -1 : selectedPlatform);

            bool tempEnabled = GUI.enabled;
            GUI.enabled = true;
            EditorGUI.BeginChangeCheck();
            Rect r = EditorGUILayout.BeginVertical(style);
            r.width--;
            int buttonCount = platforms.Length;
            int buttonHeight = 18;
            GUIStyle buttonStyle = EditorStyles.toolbarButton;

            // Make the widget that shows what is available
            if (defaultTab != null)
            {
                if (GUI.Toggle(new Rect(r.x, r.y, r.width - buttonCount * kPlatformTabWidth, buttonHeight), selected == -1, defaultTab, buttonStyle))
                    selected = -1;
            }
            for (int i = 0; i < buttonCount; i++)
            {
                Rect buttonRect;
                if (defaultTab != null)
                {
                    buttonRect = new Rect(r.xMax - (buttonCount - i) * kPlatformTabWidth, r.y, kPlatformTabWidth, buttonHeight);
                }
                else
                {
                    int left = Mathf.RoundToInt(i * r.width / buttonCount);
                    int right = Mathf.RoundToInt((i + 1) * r.width / buttonCount);
                    buttonRect = new Rect(r.x + left, r.y, right - left, buttonHeight);
                }

                if (GUI.Toggle(buttonRect, selected == i, new GUIContent(platforms[i].smallIcon, platforms[i].tooltip), buttonStyle))
                    selected = i;
            }

            // GUILayout.Space doesn't expand to available width, so use GetRect instead
            GUILayoutUtility.GetRect(10, buttonHeight);

            GUI.enabled = tempEnabled;

            // Important that we only actually set the selectedBuildTargetGroup if the user clicked the button.
            // If the current selectedBuildTargetGroup is one that is not among the tabs (because the build target
            // is not supported), then this should not be changed unless the user explicitly does so.
            // Otherwise, if the build window is open at the same time, the unsupported build target groups will
            // not be selectable in the build window.
            if (EditorGUI.EndChangeCheck())
            {
                if (defaultTab == null)
                {
                    EditorUserBuildSettings.selectedBuildTargetGroup = platforms[selected].targetGroup;
                }
                else
                {
                    if (selected < 0)
                    {
                        s_SelectedDefault.value = true;
                    }
                    else
                    {
                        EditorUserBuildSettings.selectedBuildTargetGroup = platforms[selected].targetGroup;
                        s_SelectedDefault.value = false;
                    }
                }

                // Repaint build window, if open.
                Object[] buildWindows = Resources.FindObjectsOfTypeAll(typeof(BuildPlayerWindow));
                for (int i = 0; i < buildWindows.Length; i++)
                {
                    BuildPlayerWindow buildWindow = buildWindows[i] as BuildPlayerWindow;
                    if (buildWindow != null)
                        buildWindow.Repaint();
                }
            }

            return selected;
        }

        internal static void EndPlatformGrouping()
        {
            EditorGUILayout.EndVertical();
        }

        internal static void MultiSelectionObjectTitleBar(Object[] objects)
        {
            string text = objects[0].name + " (" + ObjectNames.NicifyVariableName(ObjectNames.GetTypeName(objects[0])) + ")";
            if (objects.Length > 1)
            {
                text += " and " + (objects.Length - 1) + " other" + (objects.Length > 2 ? "s" : "");
            }
            GUILayoutOption[] options = {  GUILayout.Height(16f) };
            GUILayout.Label(EditorGUIUtility.TempContent(text, AssetPreview.GetMiniThumbnail(objects[0])), EditorStyles.boldLabel, options);
        }

        // Returns true if specified bit is true for all targets
        internal static bool BitToggleField(string label, SerializedProperty bitFieldProperty, int flag)
        {
            bool toggle = (bitFieldProperty.intValue & flag) != 0;
            bool different = (bitFieldProperty.hasMultipleDifferentValuesBitwise & flag) != 0;
            EditorGUI.showMixedValue = different;
            EditorGUI.BeginChangeCheck();
            toggle = EditorGUILayout.Toggle(label, toggle);
            if (EditorGUI.EndChangeCheck())
            {
                // If toggle has mixed values, always set all to true when clicking it
                if (different)
                {
                    toggle = true;
                }
                different = false;
                int bitIndex = -1;
                for (int i = 0; i < 32; i++)
                {
                    if (((1 << i) & flag) != 0)
                    {
                        bitIndex = i;
                        break;
                    }
                }
                bitFieldProperty.SetBitAtIndexForAllTargetsImmediate(bitIndex, toggle);
            }
            EditorGUI.showMixedValue = false;
            return toggle && !different;
        }

        internal static void SortingLayerField(GUIContent label, SerializedProperty layerID, GUIStyle style)
        {
            Rect r = EditorGUILayout.s_LastRect = EditorGUILayout.GetControlRect(false, EditorGUI.kSingleLineHeight, style);
            EditorGUI.SortingLayerField(r, label, layerID, style, EditorStyles.label);
        }

        internal static string TextFieldDropDown(string text, string[] dropDownElement)
        {
            return TextFieldDropDown(GUIContent.none, text, dropDownElement);
        }

        internal static string TextFieldDropDown(GUIContent label, string text, string[] dropDownElement)
        {
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.textField);
            return EditorGUI.TextFieldDropDown(rect, label, text, dropDownElement);
        }

        internal static string DelayedTextFieldDropDown(string text, string[] dropDownElement)
        {
            return DelayedTextFieldDropDown(GUIContent.none, text, dropDownElement);
        }

        internal static string DelayedTextFieldDropDown(GUIContent label, string text, string[] dropDownElement)
        {
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.textFieldDropDownText);
            return EditorGUI.DelayedTextFieldDropDown(rect, label, text, dropDownElement);
        }

        // A button that returns true on mouse down - like a popup button
        public static bool DropdownButton(GUIContent content, FocusType focusType, params GUILayoutOption[] options)
        {
            return DropdownButton(content, focusType, "MiniPullDown", options);
        }

        // A button that returns true on mouse down - like a popup button
        public static bool DropdownButton(GUIContent content, FocusType focusType, GUIStyle style, params GUILayoutOption[] options)
        {
            s_LastRect = GUILayoutUtility.GetRect(content, style, options);
            return EditorGUI.DropdownButton(s_LastRect, content, focusType, style);
        }
    }
}
