// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class VariableInfoTooltip : StyleFieldPopup
    {
        static readonly string s_UssClassName = "unity-builder-inspector__varinfo-tooltip";
        static readonly string s_WarningUssClassName = s_UssClassName + "__warning";

        VariableEditingHandler m_CurrentHandler;
        VariableInfoView m_View;

        public VariableEditingHandler currentHandler => m_CurrentHandler;

        public void SetInfo(VariableInfo info)
        {
            m_View.SetInfo(info);
        }

        public VariableInfoTooltip()
        {
            AddToClassList(s_UssClassName);
            m_View = new VariableInfoView();

            VisualElement warningContainer = new VisualElement();

            warningContainer.AddToClassList(s_WarningUssClassName);

            var warningIcon = new Image();
            var warningLabel = new Label(BuilderConstants.VariableNotSupportedInInlineStyleMessage);

            warningContainer.Add(warningIcon);
            warningContainer.Add(warningLabel);

            m_View.Add(warningContainer);

            Add(m_View);

            // TODO: Will need to bring this back once we can also do the dragger at the same time.
            focusable = true;
        }

        public void Show(VariableEditingHandler handler, VariableInfo info)
        {
            m_CurrentHandler = handler;
            m_View.SetInfo(info);
            anchoredControl = handler.variableField;
            Show();
        }
    }
}
