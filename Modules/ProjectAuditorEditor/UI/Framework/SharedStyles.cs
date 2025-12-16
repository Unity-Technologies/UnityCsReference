// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal static class SharedStyles
    {
        const int k_RowSize = 22;

        static GUIStyle s_Foldout;
        static GUIStyle s_BoldLabel;
        static GUIStyle s_IconLabel;
        static GUIStyle s_IconLabelLeft;
        static GUIStyle s_Label;
        static GUIStyle s_LabelWithRichText;
        static GUIStyle s_LabelWithRichTextGrey;
        static GUIStyle s_LinkLabel;
        static GUIStyle s_TextArea;

        static GUIStyle s_LabelWithDynamicSize;
        static GUIStyle s_LabelDarkWithDynamicSize;
        static GUIStyle s_TextAreaWithDynamicSize;
        static GUIStyle s_WelcomeTextArea;

        static GUIStyle s_TitleLabel;
        static GUIStyle s_MediumTitleLabel;
        static GUIStyle s_LargeLabel;
        static GUIStyle s_WhiteLargeLabel;

        static GUIStyle s_RowDark;
        static GUIStyle s_RowAlternateDark;
        static GUIStyle s_RowLight;
        static GUIStyle s_RowAlternateLight;
        static GUIStyle s_RowDarkBackground;
        //static GUIStyle s_RowLightBackground;
        static GUIStyle s_RowDarkBackgroundAlternate;
        static GUIStyle s_RowLightBackgroundAlternate;

        static GUIStyle s_TabButtonDark;
        static GUIStyle s_TabButtonLight;
        static GUIStyle s_TabBackgroundDark;
        static GUIStyle s_TabBackgroundLight;
        static GUIStyle s_TabHoverButtonDark;
        static GUIStyle s_TabHoverButtonLight;

        static GUIStyle s_MiniPulldown;

        public static GUIStyle s_DarkTextBoxBackground;
        public static GUIStyle s_LightTextBoxBackground;

        static readonly Color k_TabBottomActiveColor = new Color(0.23f, 0.55f, 0.82f, 1);
        static readonly Color k_TabBottomHoverDarkModeColor = new Color(0.9f, 0.9f, 0.9f, 1);
        static readonly Color k_TabBottomHoverLightModeColor = new Color(0.2f, 0.2f, 0.2f, 1);

        public static bool IsDarkMode => EditorGUIUtility.isProSkin;

        public static Color TabBottomActiveColor => k_TabBottomActiveColor;
        public static Color TabBottomHoverColor =>
            IsDarkMode ? k_TabBottomHoverDarkModeColor : k_TabBottomHoverLightModeColor;

        public static GUIStyle TabButton
        {
            get
            {
                if (IsDarkMode)
                {
                    if (s_TabButtonDark == null)
                    {
                        s_TabButtonDark = new GUIStyle()
                        {
                            normal = { textColor = Color.white },
                            hover = { textColor = Color.white },
                            active = { textColor = Color.white },
                            margin = new RectOffset(0, 0, 0, 0),
                            alignment = TextAnchor.MiddleCenter
                        };
                    }

                    return s_TabButtonDark;
                }
                else
                {
                    if (s_TabButtonLight == null)
                    {
                        s_TabButtonLight = new GUIStyle()
                        {
                            margin = new RectOffset(0, 0, 0, 0),
                            alignment = TextAnchor.MiddleCenter
                        };
                    }

                    return s_TabButtonLight;
                }
            }
        }

        public static GUIStyle TabHoverButton
        {
            get
            {
                if (IsDarkMode)
                {
                    if (s_TabHoverButtonDark == null || s_TabHoverButtonDark.normal.background == null
                        || s_TabHoverButtonDark.hover.background == null || s_TabHoverButtonDark.active.background == null)
                    {
                        var darkBackground = Utility.MakeColorTexture(new Color(0.173f, 0.173f, 0.173f, 1));
                        var lightBackground = Utility.MakeColorTexture(new Color(0.25f, 0.25f, 0.25f, 1));
                        var activatedBackground = Utility.MakeColorTexture(new Color(0.4f, 0.4f, 0.4f, 1));

                        s_TabHoverButtonDark = new GUIStyle()
                        {
                            normal = { background = darkBackground, textColor = Color.white },
                            hover = { background = lightBackground, textColor = Color.white },
                            active = { background = activatedBackground, textColor = Color.white },
                            margin = new RectOffset(0, 0, 0, 0),
                            alignment = TextAnchor.MiddleCenter
                        };
                    }

                    return s_TabHoverButtonDark;
                }
                else
                {
                    if (s_TabHoverButtonLight == null || s_TabHoverButtonLight.hover.background == null
                        || s_TabHoverButtonLight.active.background == null)
                    {
                        var lightBackground = Utility.MakeColorTexture(new Color(0.85f, 0.85f, 0.85f, 1));
                        var activatedBackground = Utility.MakeColorTexture(new Color(0.95f, 0.95f, 0.95f, 1));

                        s_TabHoverButtonLight = new GUIStyle()
                        {
                            normal = { textColor = Color.black },
                            hover = { background = lightBackground, textColor = Color.black },
                            active = { background = activatedBackground, textColor = Color.black },
                            margin = new RectOffset(0, 0, 0, 0),
                            alignment = TextAnchor.MiddleCenter
                        };
                    }

                    return s_TabHoverButtonLight;
                }
            }
        }

        public static GUIStyle TextBoxBackground
        {
            get
            {
                if (IsDarkMode)
                {
                    if (s_DarkTextBoxBackground == null || s_DarkTextBoxBackground.normal.background == null)
                    {
                        var darkBackgroundTex = Utility.MakeColorTexture(new Color(0.173f, 0.173f, 0.173f, 1));

                        s_DarkTextBoxBackground = new GUIStyle()
                        {
                            normal = { background = darkBackgroundTex },
                            border = new RectOffset(2, 2, 2, 2),
                        };
                    }

                    return s_DarkTextBoxBackground;
                }
                else
                {
                    if (s_LightTextBoxBackground == null)
                    {
                        s_LightTextBoxBackground = new GUIStyle();
                    }

                    return s_LightTextBoxBackground;
                }
            }
        }

        public static GUIStyle TabBackground
        {
            get
            {
                if (IsDarkMode)
                {
                    if (s_TabBackgroundDark == null || s_TabBackgroundDark.normal.background == null)
                    {
                        var darkBackgroundTex = Utility.MakeColorTexture(new Color(0.173f, 0.173f, 0.173f, 1));

                        s_TabBackgroundDark = new GUIStyle()
                        {
                            normal = { background = darkBackgroundTex },
                            border = new RectOffset(2, 2, 2, 2),
                        };
                    }

                    return s_TabBackgroundDark;
                }
                else
                {
                    if (s_TabBackgroundLight == null)
                    {
                        s_TabBackgroundLight = new GUIStyle();
                    }

                    return s_TabBackgroundLight;
                }
            }
        }

        public static GUIStyle Foldout
        {
            get
            {
                if (s_Foldout == null)
                    s_Foldout = new GUIStyle(EditorStyles.foldout)
                    {
                        fontStyle = FontStyle.Bold
                    };
                return s_Foldout;
            }
        }

        public static GUIStyle BoldLabel
        {
            get
            {
                if (s_BoldLabel == null)
                    s_BoldLabel = new GUIStyle(EditorStyles.label)
                    {
                        fontStyle = FontStyle.Bold,
                        wordWrap = false
                    };
                return s_BoldLabel;
            }
        }

        public static GUIStyle IconLabel
        {
            get
            {
                if (s_IconLabel == null)
                    s_IconLabel = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        wordWrap = false
                    };
                return s_IconLabel;
            }
        }

        public static GUIStyle IconLabelLeft
        {
            get
            {
                if (s_IconLabelLeft == null)
                    s_IconLabelLeft = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        wordWrap = false
                    };
                return s_IconLabelLeft;
            }
        }

        public static GUIStyle Label
        {
            get
            {
                if (s_Label == null)
                    s_Label = new GUIStyle(EditorStyles.label)
                    {
                        wordWrap = false
                    };
                return s_Label;
            }
        }

        public static GUIStyle LabelRichText
        {
            get
            {
                if (s_LabelWithRichText == null)
                    s_LabelWithRichText = new GUIStyle(EditorStyles.label)
                    {
                        richText = true
                    };
                return s_LabelWithRichText;
            }
        }

        public static GUIStyle LabelRichTextGrey
        {
            get
            {
                if (s_LabelWithRichTextGrey == null)
                {
                    s_LabelWithRichTextGrey = new GUIStyle(EditorStyles.label) { richText = true };
                    s_LabelWithRichTextGrey.active.textColor = Color.grey;
                    s_LabelWithRichTextGrey.hover.textColor = Color.grey;
                    s_LabelWithRichTextGrey.normal.textColor = Color.grey;
                }
                return s_LabelWithRichTextGrey;
            }
        }

        public static GUIStyle LinkLabel
        {
            get
            {
                if (s_LinkLabel == null)
                    s_LinkLabel = new GUIStyle(GetStyle("LinkLabel"))
                    {
                        alignment = TextAnchor.MiddleLeft
                    };
                return s_LinkLabel;
            }
        }

        public static GUIStyle TextArea
        {
            get
            {
                if (s_TextArea == null)
                {
                    s_TextArea = new GUIStyle(EditorStyles.label);
                    s_TextArea.richText = true;
                    s_TextArea.wordWrap = true;
                    s_TextArea.alignment = TextAnchor.UpperLeft;
                }

                return s_TextArea;
            }
        }

        public static GUIStyle LabelWithDynamicSize
        {
            get
            {
                if (s_LabelWithDynamicSize == null)
                    s_LabelWithDynamicSize = new GUIStyle(EditorStyles.label)
                    {
                        wordWrap = false
                    };
                return s_LabelWithDynamicSize;
            }
        }

        public static GUIStyle LabelDarkWithDynamicSize
        {
            get
            {
                if (s_LabelDarkWithDynamicSize == null)
                    s_LabelDarkWithDynamicSize = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = Color.gray },
                        wordWrap = false
                    };
                return s_LabelDarkWithDynamicSize;
            }
        }

        public static GUIStyle TextAreaWithDynamicSize
        {
            get
            {
                if (s_TextAreaWithDynamicSize == null)
                {
                    s_TextAreaWithDynamicSize = new GUIStyle(EditorStyles.label);
                    s_TextAreaWithDynamicSize.richText = true;
                    s_TextAreaWithDynamicSize.wordWrap = true;
                    s_TextAreaWithDynamicSize.alignment = TextAnchor.UpperLeft;
                }

                return s_TextAreaWithDynamicSize;
            }
        }

        public static GUIStyle WelcomeTextArea
        {
            get
            {
                if (s_WelcomeTextArea == null)
                {
                    s_WelcomeTextArea = new GUIStyle(EditorStyles.label);
                    s_WelcomeTextArea.fontSize = 14;
                    s_WelcomeTextArea.richText = true;
                    s_WelcomeTextArea.wordWrap = true;
                    s_WelcomeTextArea.alignment = TextAnchor.UpperCenter;
                }

                return s_WelcomeTextArea;
            }
        }

        public static GUIStyle TitleLabel
        {
            get
            {
                if (s_TitleLabel == null)
                {
                    s_TitleLabel = new GUIStyle(EditorStyles.boldLabel);
                    s_TitleLabel.fontSize = 26;
                    s_TitleLabel.fixedHeight = 34;
                    s_TitleLabel.alignment = TextAnchor.MiddleCenter;
                }
                return s_TitleLabel;
            }
        }

        public static GUIStyle MediumTitleLabel
        {
            get
            {
                if (s_MediumTitleLabel == null)
                {
                    s_MediumTitleLabel = new GUIStyle(EditorStyles.boldLabel);
                    s_MediumTitleLabel.fontSize = 20;
                    s_MediumTitleLabel.fixedHeight = 30;
                }
                return s_MediumTitleLabel;
            }
        }

        public static GUIStyle LargeLabel
        {
            get
            {
                if (s_LargeLabel == null)
                {
                    s_LargeLabel = new GUIStyle(EditorStyles.boldLabel);
                    s_LargeLabel.fontSize = 14;
                    s_LargeLabel.fixedHeight = 22;
                }
                return s_LargeLabel;
            }
        }

        public static GUIStyle WhiteLargeLabel
        {
            get
            {
                if (s_WhiteLargeLabel == null)
                {
                    s_WhiteLargeLabel = new GUIStyle(EditorStyles.boldLabel);
                    s_WhiteLargeLabel.fontSize = 14;
                    s_WhiteLargeLabel.fixedHeight = 22;
                    s_WhiteLargeLabel.normal.textColor = Color.white;
                    s_WhiteLargeLabel.hover.textColor = Color.white;
                }
                return s_WhiteLargeLabel;
            }
        }

        static GUIStyle GetStyle(string styleName)
        {
            var s = GUI.skin.FindStyle(styleName);
            if (s == null)
                s = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
            if (s == null)
            {
                Debug.LogError("Missing built-in guistyle " + styleName);
                s = new GUIStyle();
            }
            return s;
        }

        public static void SetFontDynamicSize(int fontSize)
        {
            LabelWithDynamicSize.fontSize = fontSize;
            TextAreaWithDynamicSize.fontSize = fontSize;
        }

        public static GUIStyle Row
        {
            get
            {
                if (IsDarkMode)
                {
                    if (s_RowDark == null || s_RowDark.normal.background == null)
                    {
                        s_RowDark = new GUIStyle(GUIStyle.none)
                        {
                            normal = { background = Utility.MakeColorTexture(new Color(0.22f, 0.22f, 0.22f, 1.0f)) },
                            fixedHeight = k_RowSize
                        };
                    }

                    return s_RowDark;
                }
                else
                {
                    if (s_RowLight == null)
                    {
                        s_RowLight = new GUIStyle(GUIStyle.none)
                        {
                            fixedHeight = k_RowSize
                        };
                    }

                    return s_RowLight;
                }
            }
        }

        public static GUIStyle RowAlternate
        {
            get
            {
                if (IsDarkMode)
                {
                    if (s_RowAlternateDark == null || s_RowAlternateDark.normal.background == null)
                    {
                        s_RowAlternateDark = new GUIStyle(GUIStyle.none)
                        {
                            normal = { background = Utility.MakeColorTexture(new Color(0.275f, 0.275f, 0.275f, 1.0f)) },
                            fixedHeight = k_RowSize
                        };
                    }

                    return s_RowAlternateDark;
                }
                else
                {
                    if (s_RowAlternateLight == null || s_RowAlternateLight.normal.background == null)
                    {
                        s_RowAlternateLight = new GUIStyle(GUIStyle.none)
                        {
                            normal = { background = Utility.MakeColorTexture(new Color(0.729f, 0.729f, 0.729f, 1.0f)) },
                            fixedHeight = k_RowSize
                        };
                    }

                    return s_RowAlternateLight;
                }
            }
        }


        public static GUIStyle RowBackground
        {
            get
            {
                if (IsDarkMode)
                {
                    if (s_RowDarkBackground == null || s_RowDarkBackground.normal.background == null)
                    {
                        s_RowDarkBackground = new GUIStyle(GUIStyle.none)
                        {
                            normal = { background = Utility.MakeColorTexture(new Color(0.22f, 0.22f, 0.22f, 1.0f)) }
                        };
                    }

                    return s_RowDarkBackground;
                }
                else
                {
                    return GUIStyle.none;
                }
            }
        }

        public static GUIStyle RowBackgroundAlternate
        {
            get
            {
                if (IsDarkMode)
                {
                    if (s_RowDarkBackgroundAlternate == null || s_RowDarkBackgroundAlternate.normal.background == null)
                    {
                        s_RowDarkBackgroundAlternate = new GUIStyle(GUIStyle.none)
                        {
                            normal = { background = Utility.MakeColorTexture(new Color(0.275f, 0.275f, 0.275f, 1.0f)) }
                        };
                    }

                    return s_RowDarkBackgroundAlternate;
                }
                else
                {
                    if (s_RowLightBackgroundAlternate == null || s_RowLightBackgroundAlternate.normal.background == null)
                    {
                        s_RowLightBackgroundAlternate = new GUIStyle(GUIStyle.none)
                        {
                            normal = { background = Utility.MakeColorTexture(new Color(0.729f, 0.729f, 0.729f, 1.0f)) }
                        };
                    }

                    return s_RowLightBackgroundAlternate;
                }
            }
        }

        public static GUIStyle MiniPulldown
        {
            get
            {
                if (s_MiniPulldown == null)
                {
                    s_MiniPulldown = new GUIStyle(EditorStyles.miniPullDown)
                    {
                        alignment = TextAnchor.MiddleCenter,
                    };
                }

                return s_MiniPulldown;
            }
        }
    }
}
