// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    static class VisualTreeBuilderExtensions
    {
        public static IMBox Box(this VisualTreeBuilder cache, Rect position, GUIContent content, GUIStyle style)
        {
            IMBox box;
            cache.NextElement(out box);
            box.GenerateControlID();
            box.position = position;
            box.content = content;
            box.style = style;
            return box;
        }

        public static IMButton Button(this VisualTreeBuilder cache, Rect position, GUIContent content, GUIStyle style)
        {
            IMButton button;
            cache.NextElement(out button);
            button.GenerateControlID();
            button.position = position;
            button.content = content;
            button.style = style;
            return button;
        }

        public static IMButtonGrid ButtonGrid(this VisualTreeBuilder cache, Rect position, int selected, GUIContent[] contents, int xCount, GUIStyle style, GUIStyle firstStyle, GUIStyle midStyle, GUIStyle lastStyle)
        {
            IMButtonGrid grid;
            cache.NextElement(out grid);
            grid.GenerateControlID();
            grid.position = position;
            grid.contents = contents;
            grid.style = style;
            grid.xCount = xCount;
            grid.selected = selected;
            grid.firstStyle = firstStyle;
            grid.midStyle = midStyle;
            grid.lastStyle = lastStyle;
            return grid;
        }

        public static IMImage DrawTexture(this VisualTreeBuilder cache, Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend, float imageAspect)
        {
            IMImage imageWidget;
            cache.NextElement(out imageWidget);
            imageWidget.position = position;
            imageWidget.image = image;
            imageWidget.scaleMode = scaleMode;
            imageWidget.alphaBlend = alphaBlend;
            imageWidget.imageAspect = imageAspect;
            return imageWidget;
        }

        public static IMGroup Group(this VisualTreeBuilder cache, Rect position, GUIContent content, GUIStyle style)
        {
            IMGroup group;
            cache.NextView(out group);
            group.GenerateControlID();
            group.position = position;
            group.content = content;
            group.style = style;
            return group;
        }

        public static IMLabel Label(this VisualTreeBuilder cache, Rect position, GUIContent content, GUIStyle style)
        {
            IMLabel label;
            cache.NextElement(out label);
            label.position = position;
            label.content = content;
            label.style = style;
            return label;
        }

        public static IMTextField PasswordField(this VisualTreeBuilder cache, Rect position, string passwordToShow, string password, char maskChar, int maxLength, GUIStyle style)
        {
            GUIContent t = GUIContent.Temp(passwordToShow);
            IMTextField field;
            if (TouchScreenKeyboard.isSupported)
                field = cache.TextField(position, GUIUtility.GetControlID(FocusType.Keyboard), t, false, maxLength, style, password, maskChar);
            else
                field = cache.TextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, false, maxLength, style, password, maskChar);
            return field;
        }

        public static IMRepeatButton RepeatButton(this VisualTreeBuilder cache, Rect position, GUIContent content, GUIStyle style, FocusType focusType)
        {
            IMRepeatButton button;
            cache.NextElement(out button);
            button.GenerateControlID();
            button.position = position;
            button.style = style;
            button.content = content;
            button.focusType = focusType;
            return button;
        }

        public static IMSlider Slider(this VisualTreeBuilder cache, Rect position, float value, float size, float start, float end, GUIStyle sliderStyle, GUIStyle thumbStyle, bool horiz, int id)
        {
            IMSlider slider;
            cache.NextElement(out slider);
            if (id != 0)
            {
                slider.AssignControlID(id);
            }
            else
            {
                slider.GenerateControlID();
            }
            slider.SetProperties(position, value, size, start, end, sliderStyle, thumbStyle, horiz);
            return slider;
        }

        public static IMScrollView ScrollView(this VisualTreeBuilder cache, Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background)
        {
            IMScrollView scrollView;
            cache.NextView(out scrollView);
            scrollView.GenerateControlID();
            scrollView.SetProperties(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background);
            return scrollView;
        }

        public static IMScroller Scroller(this VisualTreeBuilder cache, Rect position, float value, float size, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, GUIStyle leftButton, GUIStyle rightButton, bool horiz)
        {
            IMScroller scroller;
            cache.NextElement(out scroller);
            scroller.GenerateControlID();
            scroller.SetProperties(position, value, size, leftValue, rightValue, slider, thumb, leftButton, rightButton, horiz);
            return scroller;
        }

        public static IMTextField TextField(this VisualTreeBuilder cache, Rect position, int id, GUIContent content, bool multiline, int maxLength, GUIStyle style, string secureText, char maskChar)
        {
            if (TouchScreenKeyboard.isSupported)
            {
                IMTouchScreenTextField textField;
                cache.NextElement(out textField);
                if (id != 0)
                    textField.AssignControlID(id);
                else
                    textField.GenerateControlID();
                textField.position = position;
                textField.content = content;
                textField.style = style;
                textField.maxLength = maxLength;
                textField.multiline = multiline;
                textField.secureText = secureText;
                textField.maskChar = maskChar;
                return textField;
            }
            else // Not supported means we have a physical keyboard attached
            {
                IMKeyboardTextField textField;
                cache.NextElement(out textField);
                if (id != 0)
                    textField.AssignControlID(id);
                else
                    textField.GenerateControlID();
                textField.position = position;
                textField.content = content;
                textField.style = style;
                textField.maxLength = maxLength;
                textField.multiline = multiline;
                return textField;
            }
        }

        public static IMToggle Toggle(this VisualTreeBuilder cache, Rect position, int id, bool value, GUIContent content, GUIStyle style)
        {
            IMToggle toggle;
            cache.NextElement(out toggle);
            if (id != 0)
                toggle.AssignControlID(id);
            else
                toggle.GenerateControlID();
            toggle.position = position;
            toggle.content = content;
            toggle.style = style;
            toggle.value = value;
            return toggle;
        }
    }
}
