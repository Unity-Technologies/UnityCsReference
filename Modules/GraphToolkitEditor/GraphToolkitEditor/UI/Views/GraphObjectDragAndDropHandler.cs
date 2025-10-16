// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Handles drag and drop of graph assets into the blank page, when there is no active graph.
    /// </summary>
    [UnityRestricted]
    internal class GraphObjectDragAndDropHandler : IDragAndDropHandler
    {
        readonly List<Object> m_DraggedObjects = new();

        ICommandTarget m_CommandTarget;
        IReadOnlyList<Type> m_AcceptedAssetTypes;
        HashSet<string> m_AcceptedExtensions = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphObjectDragAndDropHandler"/> class.
        /// </summary>
        public GraphObjectDragAndDropHandler(ICommandTarget commandTarget, IReadOnlyList<Type> acceptedAssetTypes)
        {
            m_CommandTarget = commandTarget;
            m_AcceptedAssetTypes = acceptedAssetTypes;

            BlankPage.GetExtensionsForAssetTypes(m_AcceptedAssetTypes, m_AcceptedExtensions);
        }

        bool IsObjectAccepted(Object objectReference)
        {
            if (m_AcceptedExtensions.Count > 0)
            {
                var filePath = AssetDatabase.GetAssetPath(objectReference);
                if (string.IsNullOrEmpty(filePath))
                    return false;

                var extension = Path.GetExtension(filePath);
                if (extension.Length <= 1)
                    return false;
                extension = extension.Substring(1);

                if (!m_AcceptedExtensions.Contains(extension))
                    return false;
            }
            else if (objectReference is not GraphObject graphObject || !m_AcceptedAssetTypes.Contains(graphObject.GetType()))
                return false;

            return true;
        }

        /// <inheritdoc />
        public bool CanHandleDrop()
        {
            var objectReferences = DragAndDrop.objectReferences;
            foreach (var objectReference in objectReferences)
            {
                if (!IsObjectAccepted(objectReference))
                    return false;
            }

            return true;
        }

        /// <inheritdoc />
        public void OnDragEnter(DragEnterEvent evt)
        {
            m_DraggedObjects.Clear();
            var objectReferences = DragAndDrop.objectReferences;
            foreach (var objectReference in objectReferences)
            {
                if (IsObjectAccepted(objectReference))
                    m_DraggedObjects.Add(objectReference);
            }
        }

        /// <inheritdoc />
        public void OnDragLeave(DragLeaveEvent evt)
        {
        }

        /// <inheritdoc />
        public void OnDragUpdated(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = m_DraggedObjects.Count > 0 ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Rejected;
            evt.StopPropagation();
        }

        /// <inheritdoc />
        public void OnDragPerform(DragPerformEvent evt)
        {
            if (m_DraggedObjects.Count > 0)
            {
                foreach (var draggedObject in m_DraggedObjects)
                {
                    GraphObject graphObject = draggedObject as GraphObject;
                    if (graphObject == null)
                    {
                        graphObject = GraphObject.LoadGraphObjectAtPath(AssetDatabase.GetAssetPath(draggedObject));
                    }

                    if (graphObject != null)
                    {
                        m_CommandTarget.Dispatch(new LoadGraphCommand(graphObject.GraphModel));
                        break;
                    }
                }

            }
            m_DraggedObjects.Clear();
            evt.StopPropagation();
        }

        /// <inheritdoc />
        public void OnDragExited(DragExitedEvent evt)
        {
            m_DraggedObjects.Clear();
        }
    }
}
