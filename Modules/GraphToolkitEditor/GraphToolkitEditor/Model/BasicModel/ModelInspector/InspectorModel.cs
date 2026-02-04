// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// View model for the inspector.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class InspectorModel : Model, IHasTitle
    {
        const string k_InspectorTitle = "Inspector";
        const string k_GraphInspectorTitle = "Graph Inspector";
        const string k_NodeInspectorTitle = "Node Inspector";
        const string k_NodesInspectorTitle = "Nodes Inspector";
        const string k_VariableInspectorTitle = "Variable Inspector";
        const string k_VariablesInspectorTitle = "Variables Inspector";
        const string k_GroupInspectorTitle = "Group Inspector";
        const string k_GroupsInspectorTitle = "Groups Inspector";
        const string k_WireInspectorTitle = "Wire Inspector";
        const string k_WiresInspectorTitle = "Wires Inspector";
        const string k_NodePropertiesTitle = "Node Properties";
        const string k_VariablePropertiesTitle = "Variable Properties";
        const string k_WirePropertiesTitle = "Wire Properties";
        const string k_AdvancedNodePropertiesTitle = "Advanced Properties";
        const string k_AdvancedVariablePropertiesTitle = "Advanced Properties";
        const string k_AdvancedWirePropertiesTitle = "Advanced Properties";
        const string k_GraphSettingsTitle = "Graph Settings";
        const string k_AdvancedGraphSettingsTitle = "Advanced Settings";
        const string k_PlacematInspectorTitle = "Placemat Inspector";
        const string k_PlacematsInspectorTitle = "Placemats Inspector";
        const string k_StickyNoteInspectorTitle = "Sticky Note Inspector";
        const string k_StickyNotesInspectorTitle = "Sticky Notes Inspector";
        const string k_PlacematPropertiesTitle = "Placemat Properties";
        const string k_ModelPropertiesTitle = "Object Properties";
        const string k_TransitionsPropertiesTitle = "Transitions";

        [SerializeField]
        string m_Title;

        /// <summary>
        /// The List of sections to be displayed on GraphInspector.
        /// </summary>
        [SerializeReference]
        protected List<InspectorSectionModel> m_SectionModels;

        /// <inheritdoc />
        public virtual string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        /// <summary>
        /// The list of inspection sections.
        /// </summary>
        public virtual IReadOnlyList<InspectorSectionModel> Sections => m_SectionModels;

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

        /// <summary>
        /// Configures the inspector sections based on the <see cref="Model"/>s currently being inspected.
        /// </summary>
        /// <param name="inspectedModels">The inspected models.</param>
        /// <remarks>
        /// 'SetSectionsFromModels' configures the inspector sections based on the <see cref="Model"/> instances currently being inspected.
        /// This method determines which sections should be displayed or updated in the inspector, so the UI reflects the properties
        /// and structure of the inspected models. You can override this method to customize how specific model types affect the inspector
        /// sections. Overriding allows for more precise control over which sections are shown, hidden, or modified based on the different models.
        /// </remarks>
        protected virtual void SetSectionsFromModels(IEnumerable<Model> inspectedModels)
        {
            m_SectionModels.Clear();

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (inspectedModels.Count() == 1)
#pragma warning restore UA2001
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var inspectedModel = inspectedModels.First();
#pragma warning restore UA2001
                if (inspectedModel == null)
                    return;

                switch (inspectedModel)
                {
                    case VariableNodeModel _:
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = k_VariablePropertiesTitle,
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
                    case AbstractNodeModel _:
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = null,
                            Collapsible = false,
                            SectionType = SectionType.Options
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

                        if (inspectedModel is StateModel)
                        {
                            m_SectionModels.Add(new InspectorSectionModel()
                            {
                                Title = k_TransitionsPropertiesTitle,
                                Collapsed = false,
                                SectionType = SectionType.StateTransitions
                            });
                        }
                        break;
                    case VariableDeclarationModelBase _:
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = k_VariablePropertiesTitle,
                            Collapsible = false,
                            SectionType = SectionType.Properties
                        });
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = k_AdvancedVariablePropertiesTitle,
                            Collapsed = false,
                            SectionType = SectionType.Advanced
                        });
                        break;
                    case GroupModel _:
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = null,
                            Collapsible = false,
                            SectionType = SectionType.Properties
                        });
                        break;
                    case StickyNoteModel _:
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = k_StickyNoteInspectorTitle,
                            Collapsible = false,
                            SectionType = SectionType.Properties
                        });
                        break;
                    case WireModel _:
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = null,
                            Collapsible = false,
                            SectionType = SectionType.Options
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
                    case PlacematModel _:
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = k_PlacematPropertiesTitle,
                            Collapsed = false,
                            SectionType = SectionType.Options
                        });
                        break;
                    case GraphModel _:
                        m_SectionModels.Add(new InspectorSectionModel()
                        {
                            Title = k_GraphSettingsTitle,
                            Collapsed = false,
                            SectionType = SectionType.Options
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
                if (typeof(VariableNodeModel).IsAssignableFrom(type))
                {
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = k_VariablePropertiesTitle,
                        Collapsible = false,
                        SectionType = SectionType.Properties
                    });
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = k_AdvancedVariablePropertiesTitle,
                        Collapsed = false,
                        SectionType = SectionType.Advanced
                    });
                }
                else if (typeof(AbstractNodeModel).IsAssignableFrom(type))
                {
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = null,
                        Collapsible = false,
                        SectionType = SectionType.Options
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
                else if (typeof(VariableDeclarationModelBase).IsAssignableFrom(type))
                {
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = k_VariablePropertiesTitle,
                        Collapsible = false,
                        SectionType = SectionType.Properties
                    });
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = k_AdvancedVariablePropertiesTitle,
                        Collapsed = false,
                        SectionType = SectionType.Advanced
                    });
                }
                else if (typeof(GroupModel).IsAssignableFrom(type))
                {
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = null,
                        Collapsible = false,
                        SectionType = SectionType.Properties
                    });
                }
                else if (typeof(WireModel).IsAssignableFrom(type))
                {
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = null,
                        Collapsed = false,
                        SectionType = SectionType.Options
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
                else if (typeof(PlacematModel).IsAssignableFrom(type))
                {
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = k_PlacematPropertiesTitle,
                        Collapsed = false,
                        SectionType = SectionType.Options
                    });
                }
                else if (typeof(GraphModel).IsAssignableFrom(type))
                {
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = k_GraphSettingsTitle,
                        Collapsed = false,
                        SectionType = SectionType.Options
                    });
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = k_AdvancedGraphSettingsTitle,
                        Collapsed = true,
                        SectionType = SectionType.Advanced
                    });
                }
                else if (typeof(Model).IsAssignableFrom(type))
                {
                    m_SectionModels.Add(new InspectorSectionModel()
                    {
                        Title = null,
                        Collapsible = false,
                        SectionType = SectionType.Options
                    });
                }
            }
        }

        bool SetTitleFromModels(IEnumerable<Model> inspectedModels)
        {
            var currentTitle = m_Title;

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (!inspectedModels.Skip(1).HasAny())
#pragma warning restore UA2001
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var inspectedModel = inspectedModels.First();
#pragma warning restore UA2001
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
                        case VariableDeclarationModelBase _:
                            m_Title = k_VariableInspectorTitle;
                            break;
                        case GroupModel _:
                            m_Title = k_GroupInspectorTitle;
                            break;
                        case WireModel _:
                            m_Title = k_WireInspectorTitle;
                            break;
                        case GraphModel _:
                            m_Title = k_GraphInspectorTitle;
                            break;
                        case PlacematModel _:
                            m_Title = k_PlacematInspectorTitle;
                            break;
                        case StickyNoteModel _:
                            m_Title = k_StickyNoteInspectorTitle;
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
                else if (typeof(VariableDeclarationModelBase).IsAssignableFrom(type))
                    m_Title = k_VariablesInspectorTitle;
                else if (typeof(WireModel).IsAssignableFrom(type))
                    m_Title = k_WiresInspectorTitle;
                else if (typeof(PlacematModel).IsAssignableFrom(type))
                    m_Title = k_PlacematsInspectorTitle;
                else if (typeof(GroupModel).IsAssignableFrom(type))
                    m_Title = k_GroupsInspectorTitle;
                else if (typeof(StickyNoteModel).IsAssignableFrom(type))
                    m_Title = k_StickyNotesInspectorTitle;
                else
                    m_Title = k_InspectorTitle;
            }

            return m_Title != currentTitle;
        }
    }
}
