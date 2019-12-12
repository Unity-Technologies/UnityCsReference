// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    public enum StickyNoteChange
    {
        Title,
        Contents,
        Theme,
        FontSize,
        Position,
    }
    public class StickyNoteChangeEvent : EventBase<StickyNoteChangeEvent>
    {
        public static StickyNoteChangeEvent GetPooled(StickyNote target, StickyNoteChange change)
        {
            var evt = GetPooled();
            evt.target = target;
            evt.change = change;
            return evt;
        }

        public StickyNoteChange change {get; protected set; }
    }
    public enum StickyNoteTheme
    {
        Classic,
        Black
    }
    public enum StickyNoteFontSize
    {
        Small,
        Medium,
        Large,
        Huge
    }

    public class StickyNote : GraphElement, IResizable
    {
        public new class UxmlFactory : UxmlFactory<StickyNote> {}


        StickyNoteTheme m_Theme = StickyNoteTheme.Classic;
        public StickyNoteTheme theme
        {
            get
            {
                return m_Theme;
            }
            set
            {
                if (m_Theme != value)
                {
                    m_Theme = value;
                    UpdateThemeClasses();
                }
            }
        }

        StickyNoteFontSize m_FontSize = StickyNoteFontSize.Medium;
        public StickyNoteFontSize fontSize
        {
            get {return m_FontSize; }
            set
            {
                if (m_FontSize != value)
                {
                    m_FontSize = value;
                    UpdateSizeClasses();
                }
            }
        }

        Label m_Title;
        TextField m_TitleField;
        Label m_Contents;
        TextField m_ContentsField;

        public StickyNote() : this(Vector2.zero)
        {}

        public StickyNote(Vector2 position) : this("UXML/GraphView/StickyNote.uxml", position)
        {
            styleSheets.Add(EditorGUIUtility.Load("StyleSheets/GraphView/Selectable.uss") as StyleSheet);
            styleSheets.Add(EditorGUIUtility.Load("StyleSheets/GraphView/StickyNote.uss") as StyleSheet);
        }

        public StickyNote(string uiFile, Vector2 position)
        {
            var tpl = Resources.Load<VisualTreeAsset>(uiFile);
            if (tpl == null)
                tpl = EditorGUIUtility.Load(uiFile) as VisualTreeAsset;

            tpl.CloneTree(this);

            capabilities = Capabilities.Movable | Capabilities.Deletable | Capabilities.Ascendable | Capabilities.Selectable | Capabilities.Copiable;

            m_Title = this.Q<Label>(name: "title");
            if (m_Title != null)
                m_Title.RegisterCallback<MouseDownEvent>(OnTitleMouseDown);

            m_TitleField = this.Q<TextField>(name: "title-field");
            if (m_TitleField != null)
            {
                m_TitleField.style.display = DisplayStyle.None;
                m_TitleField.Q("unity-text-input").RegisterCallback<BlurEvent>(OnTitleBlur);
                m_TitleField.RegisterCallback<ChangeEvent<string>>(OnTitleChange);
            }


            m_Contents = this.Q<Label>(name: "contents");
            if (m_Contents != null)
            {
                m_ContentsField = m_Contents.Q<TextField>(name: "contents-field");
                if (m_ContentsField != null)
                {
                    m_ContentsField.style.display = DisplayStyle.None;
                    m_ContentsField.multiline = true;
                    m_ContentsField.Q("unity-text-input").RegisterCallback<BlurEvent>(OnContentsBlur);
                }
                m_Contents.RegisterCallback<MouseDownEvent>(OnContentsMouseDown);
            }

            SetPosition(new Rect(position, defaultSize));

            AddToClassList("sticky-note");
            AddToClassList("selectable");
            UpdateThemeClasses();
            UpdateSizeClasses();

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        void OnFitToText(DropdownMenuAction a)
        {
            FitText(false);
        }

        public void FitText(bool onlyIfSmaller)
        {
            Vector2 preferredTitleSize = Vector2.zero;
            if (!string.IsNullOrEmpty(m_Title.text))
                preferredTitleSize = m_Title.MeasureTextSize(m_Title.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined); // This is the size of the string with the current title font and such

            preferredTitleSize += AllExtraSpace(m_Title);
            preferredTitleSize.x += m_Title.ChangeCoordinatesTo(this, Vector2.zero).x + resolvedStyle.width - m_Title.ChangeCoordinatesTo(this, new Vector2(m_Title.layout.width, 0)).x;

            Vector2 preferredContentsSizeOneLine = m_Contents.MeasureTextSize(m_Contents.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);

            Vector2 contentExtraSpace = AllExtraSpace(m_Contents);
            preferredContentsSizeOneLine += contentExtraSpace;

            Vector2 extraSpace = new Vector2(resolvedStyle.width, resolvedStyle.height) - m_Contents.ChangeCoordinatesTo(this, new Vector2(m_Contents.layout.width, m_Contents.layout.height));
            extraSpace += m_Title.ChangeCoordinatesTo(this, Vector2.zero);
            preferredContentsSizeOneLine += extraSpace;

            float width = 0;
            float height = 0;
            // The content in one line is smaller than the current width.
            // Set the width to fit both title and content.
            // Set the height to have only one line in the content
            if (preferredContentsSizeOneLine.x < Mathf.Max(preferredTitleSize.x, resolvedStyle.width))
            {
                width = Mathf.Max(preferredContentsSizeOneLine.x, preferredTitleSize.x);
                height = preferredContentsSizeOneLine.y + preferredTitleSize.y;
            }
            else // The width is not enough for the content: keep the width or use the title width if bigger.
            {
                width = Mathf.Max(preferredTitleSize.x + extraSpace.x, resolvedStyle.width);
                float contextWidth = width - extraSpace.x - contentExtraSpace.x;
                Vector2 preferredContentsSize = m_Contents.MeasureTextSize(m_Contents.text, contextWidth, MeasureMode.Exactly, 0, MeasureMode.Undefined);

                preferredContentsSize += contentExtraSpace;

                height = preferredTitleSize.y + preferredContentsSize.y + extraSpace.y;
            }
            if (!onlyIfSmaller || resolvedStyle.width < width)
                style.width = width;
            if (!onlyIfSmaller || resolvedStyle.height < height)
                style.height = height;
            if (this is IResizable)
            {
                (this as IResizable).OnResized();
            }
        }

        void UpdateThemeClasses()
        {
            foreach (StickyNoteTheme value in System.Enum.GetValues(typeof(StickyNoteTheme)))
            {
                if (m_Theme != value)
                    RemoveFromClassList("theme-" + value.ToString().ToLower());
                else
                    AddToClassList("theme-" + value.ToString().ToLower());
            }
        }

        void UpdateSizeClasses()
        {
            foreach (StickyNoteFontSize value in System.Enum.GetValues(typeof(StickyNoteFontSize)))
            {
                if (m_FontSize != value)
                    RemoveFromClassList("size-" + value.ToString().ToLower());
                else
                    AddToClassList("size-" + value.ToString().ToLower());
            }
        }

        public static readonly Vector2 defaultSize = new Vector2(200, 160);

        public void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is StickyNote)
            {
                if (theme == StickyNoteTheme.Black)
                    evt.menu.AppendAction("Light Theme", OnChangeTheme, e => DropdownMenuAction.Status.Normal, StickyNoteTheme.Classic);
                else
                    evt.menu.AppendAction("Dark Theme", OnChangeTheme, e => DropdownMenuAction.Status.Normal, StickyNoteTheme.Black);

                foreach (StickyNoteFontSize value in System.Enum.GetValues(typeof(StickyNoteFontSize)))
                {
                    evt.menu.AppendAction(value.ToString() + " Text Size", OnChangeSize, e => DropdownMenuAction.Status.Normal, value);
                }
                evt.menu.AppendSeparator();

                evt.menu.AppendAction("Fit To Text", OnFitToText, e => DropdownMenuAction.Status.Normal);
                evt.menu.AppendSeparator();
            }
        }

        void OnTitleChange(EventBase e)
        {
            title = m_TitleField.value;
        }

        const string fitTextClass = "fit-text";

        public override void SetPosition(Rect rect)
        {
            style.left = rect.x;
            style.top = rect.y;
            style.width = rect.width;
            style.height = rect.height;
        }

        public override Rect GetPosition()
        {
            return new Rect(resolvedStyle.left, resolvedStyle.top, resolvedStyle.width, resolvedStyle.height);
        }

        public override void UpdatePresenterPosition()
        {
            base.UpdatePresenterPosition();

            NotifyChange(StickyNoteChange.Position);
        }

        public virtual void OnStartResize()
        {}

        public virtual void OnResized()
        {
            NotifyChange(StickyNoteChange.Position);
        }

        public string contents
        {
            get {return m_Contents.text; }
            set
            {
                if (m_Contents != null)
                {
                    m_Contents.text = value;
                }
            }
        }

        public override string title
        {
            get {return m_Title.text; }
            set
            {
                if (m_Title != null)
                {
                    m_Title.text = value;

                    if (!string.IsNullOrEmpty(m_Title.text))
                    {
                        m_Title.RemoveFromClassList("empty");
                    }
                    else
                    {
                        m_Title.AddToClassList("empty");
                    }
                }
            }
        }

        void OnChangeTheme(DropdownMenuAction action)
        {
            theme = (StickyNoteTheme)action.userData;
            NotifyChange(StickyNoteChange.Theme);
        }

        void OnChangeSize(DropdownMenuAction action)
        {
            fontSize = (StickyNoteFontSize)action.userData;
            NotifyChange(StickyNoteChange.FontSize);

            FitText(true);
        }

        void OnTitleBlur(BlurEvent e)
        {
            title = m_TitleField.value;
            m_TitleField.style.display = DisplayStyle.None;

            m_Title.UnregisterCallback<GeometryChangedEvent>(OnTitleRelayout);

            //Notify change
            NotifyChange(StickyNoteChange.Title);
        }

        void OnContentsBlur(BlurEvent e)
        {
            bool changed = m_Contents.text != m_ContentsField.value;
            m_Contents.text = m_ContentsField.value;
            m_ContentsField.style.display = DisplayStyle.None;

            //Notify change
            if (changed)
            {
                NotifyChange(StickyNoteChange.Contents);
            }
        }

        void OnTitleRelayout(GeometryChangedEvent e)
        {
            UpdateTitleFieldRect();
        }

        void UpdateTitleFieldRect()
        {
            Rect rect = m_Title.layout;
            m_Title.parent.ChangeCoordinatesTo(m_TitleField.parent, rect);

            m_TitleField.style.left = rect.xMin - 1;
            m_TitleField.style.right = rect.yMin + m_Title.resolvedStyle.marginTop;
            m_TitleField.style.width = rect.width - m_Title.resolvedStyle.marginLeft - m_Title.resolvedStyle.marginRight;
            m_TitleField.style.height = rect.height - m_Title.resolvedStyle.marginTop - m_Title.resolvedStyle.marginBottom;
        }

        void OnTitleMouseDown(MouseDownEvent e)
        {
            if (e.button == (int)MouseButton.LeftMouse && e.clickCount == 2)
            {
                m_TitleField.RemoveFromClassList("empty");
                m_TitleField.value = m_Title.text;
                m_TitleField.style.display = DisplayStyle.Flex;
                UpdateTitleFieldRect();
                m_Title.RegisterCallback<GeometryChangedEvent>(OnTitleRelayout);

                m_TitleField.Q(TextField.textInputUssName).Focus();
                m_TitleField.SelectAll();

                e.StopPropagation();
                e.PreventDefault();
            }
        }

        void NotifyChange(StickyNoteChange change)
        {
            panel.dispatcher.Dispatch(StickyNoteChangeEvent.GetPooled(this, change), panel, DispatchMode.Queued);
        }

        void OnContentsMouseDown(MouseDownEvent e)
        {
            if (e.button == (int)MouseButton.LeftMouse && e.clickCount == 2)
            {
                m_ContentsField.value = m_Contents.text;
                m_ContentsField.style.display = DisplayStyle.Flex;
                m_ContentsField.Q(TextField.textInputUssName).Focus();
                e.StopPropagation();
                e.PreventDefault();
            }
        }

        static Vector2 AllExtraSpace(VisualElement element)
        {
            return new Vector2(
                element.resolvedStyle.marginLeft + element.resolvedStyle.marginRight + element.resolvedStyle.paddingLeft + element.resolvedStyle.paddingRight + element.resolvedStyle.borderRightWidth + element.resolvedStyle.borderLeftWidth,
                element.resolvedStyle.marginTop + element.resolvedStyle.marginBottom + element.resolvedStyle.paddingTop + element.resolvedStyle.paddingBottom + element.resolvedStyle.borderBottomWidth + element.resolvedStyle.borderTopWidth
            );
        }
    }
}
