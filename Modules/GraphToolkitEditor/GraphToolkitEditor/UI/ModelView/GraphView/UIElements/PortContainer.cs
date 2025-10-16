// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.InternalBridge;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using TextElement = UnityEngine.UIElements.TextElement;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A VisualElement used as a container for <see cref="Port"/>s.
    /// </summary>
    [UnityRestricted]
    internal class PortContainer : VisualElement
    {
        /// <summary>
        /// The USS class name added to a <see cref="PortContainer"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-port-container";

        /// <summary>
        /// The USS class name prefix for the port count of a <see cref="PortContainer"/>.
        /// </summary>
        public static readonly string portCountUssClassNamePrefix = ussClassName.WithUssModifier("port-count-");

        string m_CurrentPortCountClassName;
        bool m_SetupLabelWidth;
        float m_MaxLabelWidth;
        bool m_SetCountModifierOnParent;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortContainer"/> class.
        /// </summary>
        /// <param name="setupLabelWidth">Whether the label width should be computed based on the largest label.</param>
        /// <param name="maxLabelWidth">If <paramref name="setupLabelWidth"/> is true, sets the maximum width for the labels.</param>
        /// <param name="setCountModifierOnParent">Whether to set the class for the modifier of the number of port on parent <see cref="VisualElement"/> too.</param>
        public PortContainer(bool setupLabelWidth, float maxLabelWidth, bool setCountModifierOnParent = false)
        {
            m_SetupLabelWidth = setupLabelWidth;
            m_MaxLabelWidth = maxLabelWidth;
            m_SetCountModifierOnParent = setCountModifierOnParent;
            AddToClassList(ussClassName);
            this.AddPackageStylesheet("PortContainer.uss");

            RegisterCallback<GeometryChangedEvent>(e => UpdateLayout());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortContainer"/> class.
        /// </summary>
        public PortContainer() : this(false, float.PositiveInfinity)
        { }

        /// <summary>
        /// Updates the ports in this container.
        /// </summary>
        /// <param name="visitor">The visitor to use to update the view, which contains additional information on the work to perform.</param>
        /// <param name="ports"> The ports to update.</param>
        /// <param name="view"> The root view.</param>
        /// <remarks>
        /// 'UpdatePorts' updates the ports within the container and reflects any changes or updates based on the provided <see cref="UpdateFromModelVisitor"/> and <see cref="RootView"/>.
        /// This method allows for dynamic updates to port configurations, so the ports in the container are kept in sync with the underlying data model.
        /// </remarks>
        public virtual void UpdatePorts(UpdateFromModelVisitor visitor, IEnumerable<PortModel> ports, RootView view)
        {
            using var dispose = GetTempPortList(out var uiPorts);
            var portViewModels = ports as IReadOnlyList<PortModel> ?? new List<PortModel>(ports);

            foreach (var uiPort in uiPorts)
            {
                if (!portViewModels.Contains(uiPort.PortModel))
                {
                    Remove(uiPort);
                    uiPort.RemoveFromRootView();
                }
            }

            int uiPortCount = uiPorts.Count;

            for (int i = 0; i < portViewModels.Count; ++i)
            {
                if (uiPortCount > i)
                {
                    if (uiPorts[i].PortModel == portViewModels[i])
                    {
                        uiPorts[i].UpdateView(visitor);
                    }
                    else
                    {
                        int existing = (uiPortCount > i + 1) ? uiPorts.FindIndex(i + 1, t => t.PortModel == portViewModels[i]) : -1;
                        if (existing != -1)
                        {
                            var uiPort = uiPorts[existing];
                            uiPorts.RemoveAt(existing);
                            uiPorts.Insert(i, uiPort);
                            Insert(i, uiPort);
                            uiPort.UpdateView(visitor);
                        }
                        else
                        {
                            var ui = ModelViewFactory.CreateUI<Port>(view, portViewModels[i]);
                            Debug.Assert(ui != null, "GraphElementFactory does not know how to create UI for " + portViewModels[i].GetType());
                            Insert(i, ui);
                            uiPorts.Insert(i, ui);
                            uiPortCount++;
                        }
                    }
                }
                else
                {
                    var ui = ModelViewFactory.CreateUI<Port>(view, portViewModels[i]);
                    Debug.Assert(ui != null, "GraphElementFactory does not know how to create UI for " + portViewModels[i].GetType());
                    Add(ui);
                }
            }

            schedule.Execute(UpdateLayout).StartingIn(0);

            var newCountModifier = portCountUssClassNamePrefix + portViewModels.Count;
            if (m_SetCountModifierOnParent)
            {
                if (newCountModifier != m_CurrentPortCountClassName)
                {
                    parent.RemoveFromClassList(m_CurrentPortCountClassName);
                    parent.AddToClassList(newCountModifier);
                }
            }
            this.ReplaceAndCacheClassName(newCountModifier, ref m_CurrentPortCountClassName);
        }


        PooledObject<List<Port>> GetTempPortList(out List<Port> tempPortList)
        {
            var dispose = ListPool<Port>.Get(out tempPortList);
            foreach (var port in Children())
            {
                if (port is Port p)
                {
                    tempPortList.Add(p);
                }
            }

            return dispose;
        }

        /// <summary>
        /// Updates the container layout, computing the needed label width if configured.
        /// </summary>
        public void UpdateLayout()
        {
            using var dispose = GetTempPortList(out var uiPorts);
            if (uiPorts.Count == 0)
                return;

            var nodeModel = uiPorts[0].PortModel.NodeModel;
            if (nodeModel == null)
                return;

            if (m_SetupLabelWidth)
            {
                float maxPortConnectorWidth = 0;
                float minLabelX = float.MaxValue;
                foreach (var port in uiPorts)
                {
                    if (!port.PortModel.IsConnected() && nodeModel is ICollapsible collapsibleNode && collapsibleNode.Collapsed)
                        continue;
                    var label = port.Label;


                    var labelWidth = 0.0f;
                    var labelX = 0.0f;
                    if (label != null && label.resolvedStyle.fontSize != 0)
                    {
                        labelWidth = GetLabelTextWidth(label);
                        var labelPos = label.parent.ChangeCoordinatesTo(this, label.localBound.position);

                        labelX = labelPos.x;
                        if (minLabelX > labelX)
                            minLabelX = labelPos.x;
                    }

                    var fullWidth = labelWidth + labelX;
                    if (fullWidth > maxPortConnectorWidth)
                        maxPortConnectorWidth = fullWidth;
                }

                if (float.IsFinite(m_MaxLabelWidth))
                    maxPortConnectorWidth = Mathf.Min(m_MaxLabelWidth + minLabelX, maxPortConnectorWidth);

                foreach (var port in uiPorts)
                {
                    var label = port.Label;
                    if (label != null)
                    {
                        var labelPos = label.parent.ChangeCoordinatesTo(this, label.localBound.position);
                        var labelX = labelPos.x;

                        label.style.minWidth = maxPortConnectorWidth - labelX;
                        if (float.IsFinite(m_MaxLabelWidth))
                            label.style.maxWidth = m_MaxLabelWidth;
                    }
                }
            }
        }

        static float GetLabelTextWidth(TextElement element)
        {
            return element.GetTextWidth();
        }

        /// <summary>
        /// Prepare the ports for culling by saving their global center.
        /// </summary>
        /// <param name="cullingReference">A visual element that will not be culled and can be used as a reference for the port location.</param>
        public void PrepareCulling(VisualElement cullingReference)
        {
            using var dispose = GetTempPortList(out var uiPorts);
            foreach (var port in uiPorts)
            {
                port.PrepareCulling(cullingReference);
            }
        }

        /// <summary>
        /// Clear culling cache.
        /// </summary>
        public void ClearCulling()
        {
            using var dispose = GetTempPortList(out var uiPorts);
            foreach (var port in uiPorts)
            {
                port.ClearCulling();
            }
        }
    }
}
