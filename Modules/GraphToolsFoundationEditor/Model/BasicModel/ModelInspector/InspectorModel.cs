// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// View model for the inspector.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class InspectorModel : Model, IHasTitle
    {
        const string k_InspectorTitle = "Inspector";
        const string k_GraphInspectorTitle = "Graph Inspector";
        const string k_NodeInspectorTitle = "Node Inspector";
        const string k_NodesInspectorTitle = "Nodes Inspector";
        const string k_VariableInspectorTitle = "Variable Inspector";
        const string k_VariablesInspectorTitle = "Variables Inspector";
        const string k_WireInspectorTitle = "Wire Inspector";
        const string k_WiresInspectorTitle = "Wires Inspector";
        const string k_NodePropertiesTitle = "Node Properties";
        const string k_WirePropertiesTitle = "Wire Properties";
        const string k_AdvancedNodePropertiesTitle = "Advanced Properties";
        const string k_AdvancedVariablePropertiesTitle = "Advanced Properties";
        const string k_AdvancedWirePropertiesTitle = "Advanced Properties";
        const string k_GraphSettingsTitle = "Graph Settings";
        const string k_AdvancedGraphSettingsTitle = "Advanced Settings";

        [SerializeField]
        string m_Title;

        /// <summary>
        /// The List of sections to be displayed on GraphInspector.
        /// </summary>
        [SerializeReference]
        protected List<InspectorSectionModel> m_SectionModels;

        [SerializeField]
        Vector2 m_ScrollOffset;

        /// <inheritdoc />
        public virtual string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        /// <inheritdoc />
        public virtual string DisplayTitle => Title;

        /// <summary>
        /// The list of inspection sections.
        /// </summary>
        public virtual IReadOnlyList<InspectorSectionModel> Sections => m_SectionModels;

        public virtual Vector2 ScrollOffset
        {
            get => m_ScrollOffset;
            set => m_ScrollOffset = value;
        }

        /// <summary>
        /// The models that will be inspected.
        /// </summary>
        public IEnumerable<Model> InspectedModels
        {
            set
            {
                SetTitleFromModels(value);
                SetSectionsFromModels(value);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InspectorModel"/> class.
        /// </summary>
        public InspectorModel()
        {
            m_SectionModels = new List<InspectorSectionModel>();
        }

        protected virtual void SetSectionsFromModels(IEnumerable<Model> inspectedModels)
        {
            m_SectionModels.Clear();

            if (inspectedModels.Count() == 1)
            {
                var inspectedModel = inspectedModels.First();
                if (inspectedModel == null)
                    return;

                switch (inspectedModel)
                {
                    case AbstractNodeModel _:
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = null,
                            Collapsible = false,
                            SectionType = SectionType.Settings
                        });
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = k_NodePropertiesTitle,
                            Collapsed = false,
                            SectionType = SectionType.Properties
                        });
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = k_AdvancedNodePropertiesTitle,
                            Collapsed = false,
                            SectionType = SectionType.Advanced
                        });
                        break;
                    case VariableDeclarationModel _:
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = null,
                            Collapsible = false,
                            SectionType = SectionType.Settings
                        });
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = k_AdvancedVariablePropertiesTitle,
                            Collapsed = false,
                            SectionType = SectionType.Advanced
                        });
                        break;
                    case WireModel _:
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = null,
                            Collapsible = false,
                            SectionType = SectionType.Settings
                        });
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = k_WirePropertiesTitle,
                            Collapsed = false,
                            SectionType = SectionType.Properties
                        });
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = k_AdvancedWirePropertiesTitle,
                            Collapsed = false,
                            SectionType = SectionType.Advanced
                        });
                        break;
                    case GraphModel _:
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = k_GraphSettingsTitle,
                            Collapsed = false,
                            SectionType = SectionType.Settings
                        });
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = k_AdvancedGraphSettingsTitle,
                            Collapsed = true,
                            SectionType = SectionType.Advanced
                        });
                        break;
                }
            }
            else
            {
                var type = ModelHelpers.GetCommonBaseType(inspectedModels);
                if (typeof(AbstractNodeModel).IsAssignableFrom(type))
                {
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = null,
                        Collapsible = false,
                        SectionType = SectionType.Settings
                    });
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = k_NodePropertiesTitle,
                        Collapsed = false,
                        SectionType = SectionType.Properties
                    });
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = k_AdvancedNodePropertiesTitle,
                        Collapsed = false,
                        SectionType = SectionType.Advanced
                    });
                }
                else if (typeof(VariableDeclarationModel).IsAssignableFrom(type))
                {
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = null,
                        Collapsible = false,
                        SectionType = SectionType.Settings
                    });
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = k_AdvancedVariablePropertiesTitle,
                        Collapsed = false,
                        SectionType = SectionType.Advanced
                    });
                }
                else if (typeof(WireModel).IsAssignableFrom(type))
                {
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = null,
                        Collapsed = false,
                        SectionType = SectionType.Settings
                    });
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = k_WirePropertiesTitle,
                        Collapsed = false,
                        SectionType = SectionType.Properties
                    });
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = k_AdvancedWirePropertiesTitle,
                        Collapsed = true,
                        SectionType = SectionType.Advanced
                    });
                }
                else if (typeof(GraphModel).IsAssignableFrom(type))
                {
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = k_GraphSettingsTitle,
                        Collapsed = false,
                        SectionType = SectionType.Settings
                    });
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = k_AdvancedGraphSettingsTitle,
                        Collapsed = true,
                        SectionType = SectionType.Advanced
                    });
                }
            }
        }

        bool SetTitleFromModels(IEnumerable<Model> inspectedModels)
        {
            var currentTitle = m_Title;

            if (!inspectedModels.Skip(1).Any())
            {
                var inspectedModel = inspectedModels.First();
                if (inspectedModel is IHasTitle hasTitle)
                {
                    m_Title = hasTitle.Title;
                }

                if (string.IsNullOrEmpty(m_Title))
                {
                    switch (inspectedModel)
                    {
                        case AbstractNodeModel _:
                            m_Title = k_NodeInspectorTitle;
                            break;
                        case VariableDeclarationModel _:
                            m_Title = k_VariableInspectorTitle;
                            break;
                        case WireModel _:
                            m_Title = k_WireInspectorTitle;
                            break;
                        case GraphModel _:
                            m_Title = k_GraphInspectorTitle;
                            break;
                        default:
                            m_Title = k_InspectorTitle;
                            break;
                    }
                }
            }
            else
            {
                var type = ModelHelpers.GetCommonBaseType(inspectedModels);

                if (typeof(AbstractNodeModel).IsAssignableFrom(type))
                    m_Title = k_NodesInspectorTitle;
                if (typeof(VariableDeclarationModel).IsAssignableFrom(type))
                    m_Title = k_VariablesInspectorTitle;
                if (typeof(WireModel).IsAssignableFrom(type))
                    m_Title = k_WiresInspectorTitle;
                else
                    m_Title = k_InspectorTitle;
            }

            return m_Title != currentTitle;
        }
    }
}
