// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal class Inserter : Manipulator
    {
        private GraphView m_GraphView;
        private IInsertLocation m_InsertLocation;

        public Inserter()
        {
        }

        protected override void RegisterCallbacksOnTarget()
        {
            if (!(target is VisualElement && target is IInsertLocation))
            {
                throw new InvalidOperationException("Manipulator can only be added to an IInsertLocation VisualElement");
            }

            m_InsertLocation = target as IInsertLocation;

            target.RegisterCallback<MouseOverEvent>(OnMouseOver);
            target.RegisterCallback<MouseOutEvent>(OnMouseOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseOverEvent>(OnMouseOver);
            target.UnregisterCallback<MouseOutEvent>(OnMouseOut);

            ResetInsertLocation();
            m_InsertLocation = null;
        }

        private void ResetInsertLocation()
        {
            if (m_GraphView != null && m_GraphView.currentInsertLocation == m_InsertLocation)
            {
                m_GraphView.currentInsertLocation = null;
            }

            m_GraphView = null;
        }

        private void OnMouseOver(MouseOverEvent evt)
        {
            if (evt.button != 0)
                return;

            // Keep track of the graphview in case the target is removed while being hovered over.
            m_GraphView = (target as VisualElement).GetFirstAncestorOfType<GraphView>();

            if (m_GraphView != null)
            {
                m_GraphView.currentInsertLocation = m_InsertLocation;
            }
        }

        private void OnMouseOut(MouseOutEvent evt)
        {
            ResetInsertLocation();
        }
    }
}
