// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Web;
using UnityEditorInternal;
using UnityEditor.Connect;

namespace UnityEditor.Collaboration
{
    internal struct PublishDialogOptions
    {
        public string   Comments;
        public bool     DoPublish;
    }

    internal class CollabPublishDialog : EditorWindow
    {
        public static CollabPublishDialog ShowCollabWindow(string changelist)
        {
            CollabPublishDialog dialog = ScriptableObject.CreateInstance<CollabPublishDialog>();
            dialog.Changelist = changelist;

            var rect = new Rect(100, 100, 600, 225);
            dialog.minSize = new Vector2(rect.width, rect.height);
            dialog.maxSize = new Vector2(rect.width, rect.height);
            dialog.position = rect;
            dialog.ShowModal();

            dialog.m_Parent.window.m_DontSaveToLayout = true;

            return dialog;
        }

        static GUIContent DescribeChangesText = EditorGUIUtility.TextContent("Describe your changes here");
        static GUIContent ChangeAssetsText = EditorGUIUtility.TextContent("Changed assets:");
        static GUIContent PublishText = EditorGUIUtility.TextContent("Publish");
        static GUIContent CancelText = EditorGUIUtility.TextContent("Cancel");

        public Vector2 scrollView;
        public string Changelist;

        public CollabPublishDialog()
        {
            Options.Comments = "";
        }

        public void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(DescribeChangesText);
            Options.Comments = GUILayout.TextArea(Options.Comments, 1000, GUILayout.MinHeight(80));


            GUILayout.Label(ChangeAssetsText);
            scrollView = EditorGUILayout.BeginScrollView(scrollView, false, false);
            GUIStyle style = new GUIStyle();
            Vector2 textSize = style.CalcSize(new GUIContent(Changelist));
            EditorGUILayout.SelectableLabel(Changelist, EditorStyles.textField, GUILayout.ExpandHeight(true), GUILayout.MinHeight(textSize.y));
            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(CancelText))
            {
                Options.DoPublish = false;
                Close();
            }

            if (GUILayout.Button(PublishText))
            {
                Options.DoPublish = true;
                Close();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        public PublishDialogOptions Options;
    };

    internal class CollabCannotPublishDialog : EditorWindow
    {
        public static CollabCannotPublishDialog ShowCollabWindow(string infoMessage)
        {
            CollabCannotPublishDialog dialog = ScriptableObject.CreateInstance<CollabCannotPublishDialog>();
            dialog.InfoMessage = infoMessage;

            var rect = new Rect(100, 100, 600, 150);
            dialog.minSize = new Vector2(rect.width, rect.height);
            dialog.maxSize = new Vector2(rect.width, rect.height);
            dialog.position = rect;
            dialog.ShowModal();

            dialog.m_Parent.window.m_DontSaveToLayout = true;

            return dialog;
        }

        static GUIContent WarningText = EditorGUIUtility.TextContent(string.Format(
                    "Files that have been moved or in a changed folder cannot be selectively published, " +
                    "please use the Publish option in the collab window to publish all your changes."));
        static GUIContent IssuesText = EditorGUIUtility.TextContent("Issues:");
        static GUIContent AcceptText = EditorGUIUtility.TextContent("Accept");

        public Vector2 scrollPosition;
        public string InfoMessage;

        public void OnGUI()
        {
            GUILayout.BeginVertical();
            GUI.skin.label.wordWrap = true;

            GUILayout.BeginVertical();
            GUILayout.Label(WarningText);

            GUILayout.Label(IssuesText);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            GUIStyle warnStyle = new GUIStyle();
            warnStyle.normal.textColor = new Color(1f, 0.28f, 0f);
            GUILayout.Label(string.Format(InfoMessage), warnStyle);
            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(AcceptText))
                Close();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    };
}
