// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.TextCore.Text;

namespace UnityEditor.TextCore.Text
{
    [CustomPropertyDrawer(typeof(UnicodeLineBreakingRules))]
    internal class UnicodeLineBreakingRulesPropertyDrawer : PropertyDrawer
    {
        static readonly GUIContent s_KoreanSpecificRules = new GUIContent("Use Modern Rules", "Determines if traditional or modern line breaking rules will be used to control line breaking. Traditional line breaking rules use the Leading and Following Character rules whereas Modern uses spaces for line breaking.");


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty prop_LeadingCharactersAsset = property.FindPropertyRelative("m_LeadingCharacters");
            SerializedProperty prop_FollowingCharactersAsset = property.FindPropertyRelative("m_FollowingCharacters");
            SerializedProperty prop_UseModernHangulLineBreakingRules = property.FindPropertyRelative("m_UseModernHangulLineBreakingRules");

            // We get Rect since a valid position may not be provided by the caller.
            Rect rect = new Rect(position.x, position.y, position.width, 49);

            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, 18), prop_LeadingCharactersAsset);
            EditorGUI.PropertyField(new Rect(rect.x, rect.y + 20, rect.width, 18), prop_FollowingCharactersAsset);

            EditorGUI.LabelField(new Rect(rect.x, rect.y + 45, rect.width, 18), new GUIContent("Korean Line Breaking Rules"), EditorStyles.boldLabel);

            EditorGUI.indentLevel += 1;
            EditorGUI.PropertyField(new Rect(rect.x, rect.y + 65, rect.width, 18), prop_UseModernHangulLineBreakingRules, s_KoreanSpecificRules);
            EditorGUI.indentLevel -= 1;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 80f;
        }
    }
}
