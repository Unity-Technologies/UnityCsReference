// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    abstract class GraphView : DataWatchContainer, ISelection
    {
        // Layer class. Used for queries below.
        class Layer : VisualElement {}

        private GraphViewPresenter m_Presenter;

        public T GetPresenter<T>() where T : GraphViewPresenter
        {
            return presenter as T;
        }

        public GraphViewPresenter presenter
        {
            get { return m_Presenter; }
            set
            {
                if (m_Presenter == value)
                    return;

                RemoveWatch();
                m_Presenter = value;
                OnDataChanged();
                AddWatch();
            }
        }

        class ContentViewContainer : VisualElement
        {
            public override bool Overlaps(Rect r)
            {
                return true;
            }
        }

        protected GraphViewTypeFactory typeFactory { get; set; }

        public VisualElement contentViewContainer { get; private set; }

        public VisualElement viewport
        {
            get { return this; }
        }

        public ITransform viewTransform
        {
            get { return contentViewContainer.transform; }
        }

        public void UpdateViewTransform(Vector3 newPosition, Vector3 newScale)
        {
            contentViewContainer.transform.position = newPosition;
            contentViewContainer.transform.scale = newScale;

            if (m_Presenter != null)
            {
                m_Presenter.position = newPosition;
                m_Presenter.scale = newScale;
            }

            UpdatePersistedViewTransform();
        }

        bool m_FrameAnimate = false;

        public enum FrameType
        {
            All = 0,
            Selection = 1,
            Origin = 2
        }

        readonly Dictionary<int, Layer> m_ContainerLayers = new Dictionary<int, Layer>();

        protected GraphView()
        {
            selection = new List<ISelectable>();
            clippingOptions = ClippingOptions.ClipContents;
            contentViewContainer = new ContentViewContainer
            {
                name = "contentViewContainer",
                clippingOptions = ClippingOptions.NoClipping,
                pickingMode = PickingMode.Ignore
            };

            // make it absolute and 0 sized so it acts as a transform to move children to and fro
            Add(contentViewContainer);

            typeFactory = new GraphViewTypeFactory();
            typeFactory[typeof(EdgePresenter)] = typeof(Edge);

            AddStyleSheetPath("StyleSheets/GraphView/GraphView.uss");
            graphElements = contentViewContainer.Query().Children<Layer>().Children<GraphElement>().Build();
            nodes = this.Query<Layer>().Children<Node>().Build();
        }

        void AddLayer(int index)
        {
            m_ContainerLayers.Add(index, new Layer { clippingOptions = ClippingOptions.NoClipping, pickingMode = PickingMode.Ignore });

            foreach (var layer in m_ContainerLayers.OrderBy(t => t.Key).Select(t => t.Value))
            {
                if (layer.parent != null)
                    contentViewContainer.Remove(layer);
                contentViewContainer.Add(layer);
            }
        }

        VisualElement GetLayer(int index)
        {
            return m_ContainerLayers[index];
        }

        public UQuery.QueryState<GraphElement> graphElements { get; private set; }
        public UQuery.QueryState<Node> nodes { get; private set; }

        [Serializable]
        class PersistedViewTransform
        {
            public Vector3 position = Vector3.zero;
            public Vector3 scale = Vector3.one;
        }
        PersistedViewTransform m_PersistedViewTransform;

        ContentZoomer m_Zoomer;
        int m_ZoomerMaxElementCountWithPixelCacheRegen = 100;
        Vector3 m_MinScale = ContentZoomer.DefaultMinScale;
        Vector3 m_MaxScale = ContentZoomer.DefaultMaxScale;

        public Vector3 minScale
        {
            get { return m_MinScale; }
        }

        public Vector3 maxScale
        {
            get { return m_MaxScale; }
        }

        public float scale
        {
            get { return viewTransform.scale.x; }
        }

        public int zoomerMaxElementCountWithPixelCacheRegen
        {
            get { return m_ZoomerMaxElementCountWithPixelCacheRegen; }
            set
            {
                if (m_ZoomerMaxElementCountWithPixelCacheRegen == value)
                    return;

                m_ZoomerMaxElementCountWithPixelCacheRegen = value;
                if (m_Presenter != null)
                    m_Zoomer.keepPixelCacheOnZoom = m_Presenter.elements.Count() > m_ZoomerMaxElementCountWithPixelCacheRegen;
            }
        }

        public void SetupZoom(Vector3 minScaleSetup, Vector3 maxScaleSetup)
        {
            m_MinScale = minScaleSetup;
            m_MaxScale = maxScaleSetup;
            UpdateContentZoomer();
        }

        private void UpdatePersistedViewTransform()
        {
            if (m_PersistedViewTransform == null)
                return;

            m_PersistedViewTransform.position = contentViewContainer.transform.position;
            m_PersistedViewTransform.scale = contentViewContainer.transform.scale;

            SavePersistentData();
        }

        public override void OnPersistentDataReady()
        {
            base.OnPersistentDataReady();

            string key = GetFullHierarchicalPersistenceKey();

            m_PersistedViewTransform = GetOrCreatePersistentData<PersistedViewTransform>(m_PersistedViewTransform, key);

            UpdateViewTransform(m_PersistedViewTransform.position, m_PersistedViewTransform.scale);
        }

        void UpdateContentZoomer()
        {
            if (m_MinScale != m_MaxScale)
            {
                if (m_Zoomer == null)
                {
                    m_Zoomer = new ContentZoomer(m_MinScale, m_MaxScale);
                    this.AddManipulator(m_Zoomer);
                }
                else
                {
                    m_Zoomer.minScale = m_MinScale;
                    m_Zoomer.maxScale = m_MaxScale;
                }
            }
            else
            {
                if (m_Zoomer != null)
                    this.RemoveManipulator(m_Zoomer);
            }

            ValidateTransform();
        }

        void ValidateTransform()
        {
            if (contentViewContainer == null)
                return;
            Vector3 transformScale = viewTransform.scale;

            transformScale.x = Mathf.Max(Mathf.Min(maxScale.x, transformScale.x), minScale.x);
            transformScale.y = Mathf.Max(Mathf.Min(maxScale.y, transformScale.y), minScale.y);

            viewTransform.scale = transformScale;
        }

        public override void OnDataChanged()
        {
            if (m_Presenter == null)
                return;

            contentViewContainer.transform.position = m_Presenter.position;
            contentViewContainer.transform.scale = m_Presenter.scale != Vector3.zero ? m_Presenter.scale : Vector3.one;
            ValidateTransform();
            UpdatePersistedViewTransform();

            // process removals
            List<GraphElement> current = graphElements.ToList();

            foreach (var c in current)
            {
                // been removed?
                if (!m_Presenter.elements.Contains(c.presenter))
                {
                    c.parent.Remove(c);
                    selection.Remove(c);
                }
            }

            // process additions
            int elementCount = 0;
            foreach (GraphElementPresenter elementPresenter in m_Presenter.elements)
            {
                elementCount++;

                // been added?
                var found = false;

                // For regular presenters we check inside the contentViewContainer
                // for their VisualElements.
                if ((elementPresenter.capabilities & Capabilities.Floating) == 0)
                {
                    foreach (var dc in current)
                    {
                        if (dc != null && dc.presenter == elementPresenter)
                        {
                            found = true;
                            break;
                        }
                    }
                }
                // For floating presenters, like the MiniMap, we need to check the
                // directly children of the GraphView for their VisualElements,
                // excluding the contentViewContainer. That's where floating
                // presenters are added.
                else
                {
                    foreach (var dc in Children())
                    {
                        if (dc == contentViewContainer)
                            continue;

                        var graphElement = dc as GraphElement;
                        if (graphElement == null)
                            continue;

                        if (graphElement.presenter == elementPresenter)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                    InstantiateElement(elementPresenter);
            }

            // Change Zoomer pixel caching setting based on number of GraphElements.
            m_Zoomer.keepPixelCacheOnZoom = elementCount > m_ZoomerMaxElementCountWithPixelCacheRegen;
        }

        protected override UnityEngine.Object[] toWatch
        {
            get { return new UnityEngine.Object[] { presenter }; }
        }

        // ISelection implementation
        public List<ISelectable> selection { get; protected set; }

        // functions to ISelection extensions
        public virtual void AddToSelection(ISelectable selectable)
        {
            var graphElement = selectable as GraphElement;
            if (graphElement == null)
                return;
            graphElement.OnSelected();
            if (graphElement.presenter != null)
                graphElement.presenter.selected = true;
            selection.Add(selectable);
            contentViewContainer.Dirty(ChangeType.Repaint);
        }

        public virtual void RemoveFromSelection(ISelectable selectable)
        {
            var graphElement = selectable as GraphElement;
            if (graphElement == null)
                return;
            if (graphElement.presenter != null)
                graphElement.presenter.selected = false;
            selection.Remove(selectable);
            contentViewContainer.Dirty(ChangeType.Repaint);
        }

        public virtual void ClearSelection()
        {
            foreach (var graphElement in selection.OfType<GraphElement>())
            {
                if (graphElement.presenter != null)
                    graphElement.presenter.selected = false;
            }

            selection.Clear();
            contentViewContainer.Dirty(ChangeType.Repaint);
        }

        private void InstantiateElement(GraphElementPresenter elementPresenter)
        {
            // call factory
            GraphElement newElem = typeFactory.Create(elementPresenter);

            if (newElem == null)
            {
                return;
            }

            newElem.SetPosition(elementPresenter.position);
            newElem.presenter = elementPresenter;

            if ((elementPresenter.capabilities & Capabilities.Resizable) != 0)
            {
                newElem.Add(new Resizer());
                newElem.style.borderBottom = 6;
            }

            bool attachToContainer = (elementPresenter.capabilities & Capabilities.Floating) == 0;
            if (attachToContainer)
            {
                int newLayer = newElem.layer;
                if (!m_ContainerLayers.ContainsKey(newLayer))
                {
                    AddLayer(newLayer);
                }
                GetLayer(newLayer).Add(newElem);
            }
            else
            {
                Add(newElem);
            }
        }

        public EventPropagation DeleteSelection()
        {
            // and DeleteSelection would call that method.
            if (presenter == null)
                return EventPropagation.Stop;

            var elementsToRemove = new HashSet<GraphElementPresenter>();
            foreach (var selectedElement in selection.Cast<GraphElement>()
                     .Where(e => e != null && e.presenter != null))
            {
                if ((selectedElement.presenter.capabilities & Capabilities.Deletable) == 0)
                    continue;

                elementsToRemove.Add(selectedElement.presenter);

                var connectorColl = selectedElement.GetPresenter<NodePresenter>();
                if (connectorColl == null)
                    continue;

                elementsToRemove.UnionWith(connectorColl.inputAnchors.SelectMany(c => c.connections)
                    .Where(d => (d.capabilities & Capabilities.Deletable) != 0)
                    .Cast<GraphElementPresenter>());
                elementsToRemove.UnionWith(connectorColl.outputAnchors.SelectMany(c => c.connections)
                    .Where(d => (d.capabilities & Capabilities.Deletable) != 0)
                    .Cast<GraphElementPresenter>());
            }

            foreach (var b in elementsToRemove)
                presenter.RemoveElement(b);

            // Notify the ends of connections that the connection is going way.
            foreach (var connection in elementsToRemove.OfType<EdgePresenter>())
            {
                connection.output = null;
                connection.input = null;

                if (connection.output != null)
                {
                    connection.output.Disconnect(connection);
                }

                if (connection.input != null)
                {
                    connection.input.Disconnect(connection);
                }
            }

            return (elementsToRemove.Count > 0) ? EventPropagation.Stop : EventPropagation.Continue;
        }

        public EventPropagation FrameAll()
        {
            return Frame(FrameType.All);
        }

        public EventPropagation FrameSelection()
        {
            return Frame(FrameType.Selection);
        }

        public EventPropagation FrameOrigin()
        {
            return Frame(FrameType.Origin);
        }

        public EventPropagation FramePrev()
        {
            if (contentViewContainer.childCount == 0)
                return EventPropagation.Continue;

            List<GraphElement> childrenList = graphElements.ToList();
            childrenList.Reverse();
            return FramePrevNext(childrenList);
        }

        public EventPropagation FrameNext()
        {
            if (contentViewContainer.childCount == 0)
                return EventPropagation.Continue;
            return FramePrevNext(graphElements.ToList());
        }

        // TODO: Do we limit to GraphElements or can we tab through ISelectable's?
        EventPropagation FramePrevNext(List<GraphElement> childrenEnum)
        {
            GraphElement graphElement = null;

            // Start from current selection, if any
            if (selection.Count != 0)
                graphElement = selection[0] as GraphElement;

            for (int i = 0; i < childrenEnum.Count; i++)
            {
                if (childrenEnum[i] == graphElement)
                {
                    if (i < childrenEnum.Count - 1)
                    {
                        graphElement = childrenEnum[i + 1];
                    }
                    else
                    {
                        graphElement = childrenEnum[0];
                    }
                    break;
                }
            }

            if (graphElement == null)
                return EventPropagation.Continue;

            // New selection...
            ClearSelection();
            AddToSelection(graphElement);

            // ...and frame this new selection
            return Frame(FrameType.Selection);
        }

        EventPropagation Frame(FrameType frameType)
        {
            // Reset container translation, scale and position
            contentViewContainer.transform.position = Vector3.zero;
            contentViewContainer.transform.scale = Vector3.one;
            // TODO remove once we clarify Touch()
            contentViewContainer.Dirty(ChangeType.Repaint);

            if (frameType == FrameType.Origin)
            {
                return EventPropagation.Stop;
            }

            Rect rectToFit = contentViewContainer.layout;
            if (frameType == FrameType.Selection)
            {
                // Now calculate rectangle to fit all selected elements
                if (selection.Count == 0)
                {
                    return EventPropagation.Continue;
                }

                var graphElement = selection[0] as GraphElement;
                if (graphElement != null)
                {
                    rectToFit = graphElement.localBound;
                }

                rectToFit = selection.OfType<GraphElement>()
                    .Aggregate(rectToFit, (current, e) => RectUtils.Encompass(current, e.localBound));
            }
            else /*if (frameType == FrameType.All)*/
            {
                rectToFit = CalculateRectToFitAll();
            }

            Vector3 frameTranslation;
            Vector3 frameScaling;
            int frameBorder = 30;

            CalculateFrameTransform(rectToFit, layout, frameBorder, out frameTranslation, out frameScaling);

            if (m_FrameAnimate)
            {
                // TODO Animate framing
                // RMAnimation animation = new RMAnimation();
                // parent.Animate(parent)
                //       .Lerp(new string[] {"m_Scale", "m_Translation"},
                //             new object[] {parent.scale, parent.translation},
                //             new object[] {frameScaling, frameTranslation}, 0.08f);
            }
            else
            {
                Matrix4x4.TRS(frameTranslation, Quaternion.identity, frameScaling);

                UpdateViewTransform(frameTranslation, frameScaling);
            }

            contentViewContainer.Dirty(ChangeType.Repaint);

            UpdatePersistedViewTransform();

            return EventPropagation.Stop;
        }

        public Rect CalculateRectToFitAll()
        {
            Rect rectToFit = contentViewContainer.layout;
            bool reachedFirstChild = false;

            graphElements.ForEach(ge =>
                {
                    var elementPresenter = (ge != null)
                        ? ge.GetPresenter<GraphElementPresenter>()
                        : null;
                    if (elementPresenter == null ||
                        (elementPresenter.capabilities & Capabilities.Floating) != 0 ||
                        (elementPresenter is EdgePresenter))
                    {
                        return;
                    }

                    if (!reachedFirstChild)
                    {
                        rectToFit = ge.localBound;
                        reachedFirstChild = true;
                    }
                    else
                    {
                        rectToFit = RectUtils.Encompass(rectToFit, ge.localBound);
                    }
                });

            return rectToFit;
        }

        public static void CalculateFrameTransform(Rect rectToFit, Rect clientRect, int border, out Vector3 frameTranslation, out Vector3 frameScaling)
        {
            // bring slightly smaller screen rect into GUI space
            var screenRect = new Rect
            {
                xMin = border,
                xMax = clientRect.width - border,
                yMin = border,
                yMax = clientRect.height - border
            };

            Matrix4x4 m = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            Rect identity = GUIUtility.ScreenToGUIRect(screenRect);

            // measure zoom level necessary to fit the canvas rect into the screen rect
            float zoomLevel = Math.Min(identity.width / rectToFit.width, identity.height / rectToFit.height);

            // clamp
            zoomLevel = Mathf.Clamp(zoomLevel, ContentZoomer.DefaultMinScale.y, 1.0f);

            var transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(zoomLevel, zoomLevel, 1.0f));

            var edge = new Vector2(clientRect.width, clientRect.height);
            var origin = new Vector2(0, 0);

            var r = new Rect
            {
                min = origin,
                max = edge
            };

            var parentScale = new Vector3(transform.GetColumn(0).magnitude,
                    transform.GetColumn(1).magnitude,
                    transform.GetColumn(2).magnitude);
            Vector2 offset = r.center - (rectToFit.center * parentScale.x);

            // Update output values before leaving
            frameTranslation = new Vector3(offset.x, offset.y, 0.0f);
            frameScaling = parentScale;

            GUI.matrix = m;
        }
    }
}
