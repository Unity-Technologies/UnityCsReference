// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Text;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine;
using static Unity.Profiling.Editor.IAskAssistantService;

namespace Unity.ProjectAuditor.Editor
{
    internal class ProjectAuditorAssistantController : BaseAssistantController
    {
        public const string k_AssistantRole = "Project Auditor Assistant";
        private const string k_Prompt = "What should I do about this issue from Project Auditor?";

        static class Styles
        {
            public static readonly GUIContent AskAssistant = EditorGUIUtility.TrTextContentWithIcon("Ask Assistant", "Ask Unity AI Assistant for help with fixing this issue", "AISparkle");
        }

        public ProjectAuditorAssistantController() : base(k_AssistantRole)
        {
        }

        public class AIAssistantIssueContext
        {
            public AIAssistantIssueContext(string filename, int line, string issue, string details, string recommendation)
            {
                Filename = filename;
                Line = line;
                Issue = issue;
                Details = details;
                Recommendation = recommendation;
            }

            public string Details { get; }
            public string Filename { get; }
            public string Issue { get; }
            public int Line { get; }
            public string Recommendation { get; }
        }

        public void DrawAskAssistantButton(Descriptor descriptor, ReportItem issue, Action<GUIContent, Action> drawButtonAction)
        {
            if (!Supported)
                return;

            var rect = EditorGUILayout.BeginHorizontal();
            drawButtonAction(Styles.AskAssistant, () =>
            {
                var screenPos = GUIUtility.GUIToScreenPoint(rect.position);
                var screenRect = new Rect(screenPos, rect.size);
                var attachment = new AIAssistantIssueContext(issue.Filename, issue.Line, descriptor.Title, descriptor.Description, descriptor.Recommendation);
                var serviceContext = GetServiceContext(attachment);
                LaunchAssistant(screenRect, serviceContext, k_Prompt);
            });
            EditorGUILayout.EndHorizontal();
        }

        static Context GetServiceContext(AIAssistantIssueContext context)
        {
            var payloadSb = new StringBuilder();
            var fileName = Path.GetFileName(context.Filename);
            payloadSb.AppendLine($"File Name: {context.Filename}, ");
            payloadSb.AppendLine($"Line Number: {context.Line}, ");
            payloadSb.AppendLine($"Issue: \"{context.Issue}\",");
            payloadSb.AppendLine($"Details: \"{context.Details}\",");
            payloadSb.AppendLine($"Recommendation: \"{context.Recommendation}\"");

            return new Context(payloadSb.ToString(), "Project Auditor Issue", fileName, context);
        }
    }
}
