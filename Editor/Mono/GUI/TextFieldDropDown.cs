// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
    public sealed partial class EditorGUI
    {
        private const string kEmptyDropDownElement = "--empty--";

        static internal string DoTextFieldDropDown(Rect rect, int id, string text, string[] dropDownElements, bool delayed)
        {
            Rect textFieldRect = new Rect(rect.x, rect.y, rect.width - EditorStyles.textFieldDropDown.fixedWidth, rect.height);
            Rect popupRect = new Rect(textFieldRect.xMax, textFieldRect.y, EditorStyles.textFieldDropDown.fixedWidth, rect.height);


            if (delayed)
            {
                text = DelayedTextField(textFieldRect, text, EditorStyles.textFieldDropDownText);
            }
            else
            {
                bool dummy;
                text = DoTextField(s_RecycledEditor, id, textFieldRect, text, EditorStyles.textFieldDropDownText, null, out dummy, false, false, false);
            }


            EditorGUI.BeginChangeCheck();
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            int parameterIndex = EditorGUI.Popup(popupRect, "", -1, dropDownElements.Length > 0 ? dropDownElements : new string[] { kEmptyDropDownElement }, EditorStyles.textFieldDropDown);
            if (EditorGUI.EndChangeCheck() && dropDownElements.Length > 0)
            {
                text = dropDownElements[parameterIndex];
            }
            EditorGUI.indentLevel = oldIndent;
            return text;
        }
    }
}
