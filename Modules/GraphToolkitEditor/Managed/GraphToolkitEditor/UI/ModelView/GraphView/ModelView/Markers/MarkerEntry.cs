// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Visual element representing a single marker entry in the popup window.
    /// </summary>
    class MarkerEntry : VisualElement
    {
        const string k_USSClassName = "ge-marker-entry";
        static readonly string k_IconContainerUssClassName = k_USSClassName.WithUssElement("icon-container");
        static readonly string k_MessageLabelUssClassName = k_USSClassName.WithUssElement("message");
        static readonly string k_ErrorModifierUssClassName = k_USSClassName.WithUssModifier("error");
        static readonly string k_WarningModifierUssClassName = k_USSClassName.WithUssModifier("warning");
        static readonly string k_InfoModifierUssClassName = k_USSClassName.WithUssModifier("info");
        static readonly string k_GraphLogActionUssClassName = k_USSClassName.WithUssElement("action");
        public static readonly string k_GraphLogActionUssClassName_Hidden = k_GraphLogActionUssClassName.WithUssModifier("hidden");
        public static readonly string k_GraphLogActionUssClassName_Visible = k_GraphLogActionUssClassName.WithUssModifier("visible");

        readonly ErrorMarkerModel m_Model;
        readonly object m_ActionTarget;

        Button m_ActionButton;

        public event Action<ErrorMarkerModel, object> actionClicked;

        public MarkerEntry(ErrorMarkerModel model, Action<ErrorMarkerModel, object> action)
        {
            actionClicked += action;
            m_Model = model ?? throw new ArgumentNullException(nameof(model));
            m_ActionTarget = model.UserData;
            AddToClassList(k_USSClassName);

            var iconContainer = new VisualElement { name = "icon-container" };
            iconContainer.AddToClassList(k_IconContainerUssClassName);
            Add(iconContainer);

            var messageLabel = new Label { name = "message" };
            messageLabel.AddToClassList(k_MessageLabelUssClassName);
            Add(messageLabel);

            m_ActionButton = new Button(OnActionClick) { name = "graph-log-action-description" };
            m_ActionButton.AddToClassList(k_GraphLogActionUssClassName);
            Add(m_ActionButton);

            messageLabel.text = m_Model.ErrorMessage;

            switch (m_Model.ErrorType)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    AddToClassList(k_ErrorModifierUssClassName);
                    break;
                case LogType.Warning:
                    AddToClassList(k_WarningModifierUssClassName);
                    break;
                case LogType.Log:
                    AddToClassList(k_InfoModifierUssClassName);
                    break;
            }

            bool hasAction = m_Model.Action != null && m_ActionTarget != null;
            if (hasAction)
            {
                m_ActionButton.text = m_Model.Action.Description;
                m_ActionButton.AddToClassList(k_GraphLogActionUssClassName_Visible);
                m_ActionButton.RemoveFromClassList(k_GraphLogActionUssClassName_Hidden);
            }
            else
            {
                m_ActionButton.AddToClassList(k_GraphLogActionUssClassName_Hidden);
                m_ActionButton.RemoveFromClassList(k_GraphLogActionUssClassName_Visible);
            }
        }

        void OnActionClick()
        {
            if (m_Model?.Action != null && m_ActionTarget != null)
            {
                actionClicked?.Invoke(m_Model, m_ActionTarget);
            }
        }
    }
}
