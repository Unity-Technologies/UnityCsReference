// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A VisualElement used as a container for <see cref="Port"/>s.
    /// </summary>
    class PortContainer : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<PortContainer> {}

        public static readonly string ussClassName = "ge-port-container";
        public static readonly string portCountClassNamePrefix = ussClassName.WithUssModifier("port-count-");

        string m_CurrentPortCountClassName;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortContainer"/> class.
        /// </summary>
        public PortContainer()
        {
            AddToClassList(ussClassName);
            this.AddStylesheet_Internal("PortContainer.uss");
        }

        public void UpdatePorts(IEnumerable<PortModel> ports, RootView view)
        {
            var uiPorts = this.Query<Port>().ToList();
            var portViewModels = ports?.ToList() ?? new List<PortModel>();

            // Check if we should rebuild ports
            bool rebuildPorts = false;
            if (uiPorts.Count != portViewModels.Count)
            {
                rebuildPorts = true;
            }
            else
            {
                int i = 0;
                foreach (var portModel in portViewModels)
                {
                    if (!Equals(uiPorts[i].PortModel, portModel))
                    {
                        rebuildPorts = true;
                        break;
                    }

                    i++;
                }
            }

            if (rebuildPorts)
            {
                var children = Children().OfType<Port>().ToList();

                foreach (var port in children)
                {
                    Remove(port);
                    port.RemoveFromRootView();
                }

                foreach (var portModel in portViewModels)
                {
                    var ui = ModelViewFactory.CreateUI<Port>(view, portModel);
                    Debug.Assert(ui != null, "GraphElementFactory does not know how to create UI for " + portModel.GetType());
                    Add(ui);

                    ui.AddToRootView(view);
                }
            }
            else
            {
                foreach (var port in uiPorts)
                {
                    port.UpdateFromModel();
                }
            }

            this.ReplaceAndCacheClassName(portCountClassNamePrefix + portViewModels.Count, ref m_CurrentPortCountClassName);
        }
    }
}
