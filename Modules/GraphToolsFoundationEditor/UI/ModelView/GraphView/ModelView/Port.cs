// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using System.Collections;

// ReSharper disable InconsistentNaming

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// UI for a <see cref="PortModel"/>.
    /// Allows connection of <see cref="Wire"/>s.
    /// Handles dropping of elements on top of them to create a wire.
    /// </summary>
    class Port : ModelView, ISelectionDraggerTarget
    {
        public static readonly string ussClassName = "ge-port";
        public static readonly string willConnectModifierUssClassName = ussClassName.WithUssModifier("will-connect");
        public static readonly string connectedModifierUssClassName = ussClassName.WithUssModifier("connected");
        public static readonly string notConnectedModifierUssClassName = ussClassName.WithUssModifier("not-connected");
        public static readonly string inputModifierUssClassName = ussClassName.WithUssModifier("direction-input");
        public static readonly string outputModifierUssClassName = ussClassName.WithUssModifier("direction-output");
        public static readonly string capacityNoneModifierUssClassName = ussClassName.WithUssModifier("capacity-none");
        public static readonly string hiddenModifierUssClassName = ussClassName.WithUssModifier("hidden");
        public static readonly string dropHighlightAcceptedClass = ussClassName.WithUssModifier("drop-highlighted");
        public static readonly string dropHighlightDeniedClass = dropHighlightAcceptedClass.WithUssModifier("denied");
        public static readonly string portDataTypeClassNamePrefix = ussClassName.WithUssModifier("data-type-");
        public static readonly string portTypeModifierClassNamePrefix = ussClassName.WithUssModifier("type-");
        public static readonly string iconClass = "ge-icon";

        /// <summary>
        /// The prefix for the data type uss class name
        /// </summary>
        public static readonly string dataTypeClassPrefix = iconClass.WithUssModifier("data-type-");

        /// <summary>
        /// The USS class name used for vertical ports.
        /// </summary>
        public static readonly string verticalModifierUssClassName = ussClassName.WithUssModifier("vertical");

        public static readonly string connectorPartName = "connector-container";
        public static readonly string constantEditorPartName = "constant-editor";

        static readonly CustomStyleProperty<Color> k_PortColorProperty = new CustomStyleProperty<Color>("--port-color");
        static readonly Vector2 k_HitBoxSize = new Vector2(40, 20);
        int m_ConnectedWiresCount = Int32.MinValue;

        protected string m_CurrentDropHighlightClass = dropHighlightAcceptedClass;

        string m_CurrentDataClassName;
        string m_CurrentTypeClassName;

        bool m_Hovering;
        bool m_WillConnect;

        WireConnector m_WireConnector;

        VisualElement m_ConnectorCache;

        static Color DefaultPortColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(193 / 255f, 193 / 255f, 193 / 255f);
                }

                return new Color(90 / 255f, 90 / 255f, 90 / 255f);
            }
        }

        /// <summary>
        /// The <see cref="GraphView"/> this port belongs to.
        /// </summary>
        public GraphView GraphView => RootView as GraphView;

        public PortModel PortModel => Model as PortModel;

        public WireConnector WireConnector
        {
            get => m_WireConnector;
            protected set
            {
                ConnectorElement.ReplaceManipulator(ref m_WireConnector, value);
            }
        }

        public VisualElement ConnectorElement
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether the port will be connected during an edge drag if the mouse is released where it is.
        /// </summary>
        public bool WillConnect
        {
            get => m_WillConnect;
            set
        {
                m_WillConnect = value;
                EnableInClassList(willConnectModifierUssClassName, value);
                GetConnector()?.MarkDirtyRepaint();
            }
        }

        public Color PortColor { get; protected set; } = DefaultPortColor;

        /// <inheritdoc />
        public virtual bool CanAcceptDrop(IReadOnlyList<GraphElementModel> droppedElements)
        {
            // Only one element can be dropped at once.
            if (droppedElements.Count != 1)
                return false;

            // The elements that can be dropped: a variable declaration from the Blackboard and any node with a single input or output (eg.: variable and constant nodes).
            switch (droppedElements[0])
            {
                case VariableDeclarationModel variableDeclaration:
                    return CanAcceptDroppedVariable(variableDeclaration);
                case ISingleInputPortNodeModel:
                case ISingleOutputPortNodeModel:
                    return GetPortToConnect(droppedElements[0]) != null;
                default:
                    return false;
            }
        }

        bool CanAcceptDroppedVariable(VariableDeclarationModel variableDeclaration)
        {
            if (variableDeclaration is IPlaceholder)
                return false;

            if (PortModel.Capacity == PortCapacity.None)
                return false;

            if (!variableDeclaration.IsInputOrOutput())
                return PortModel.Direction == PortDirection.Input && variableDeclaration.DataType == PortModel.DataTypeHandle;

            var isTrigger = variableDeclaration.IsInputOrOutputTrigger();
            if (isTrigger && PortModel.PortType != PortType.Execution || !isTrigger && PortModel.DataTypeHandle != variableDeclaration.DataType)
                return false;

            var isInput = variableDeclaration.Modifiers == ModifierFlags.Read;
            if (isInput && PortModel.Direction != PortDirection.Input || !isInput && PortModel.Direction != PortDirection.Output)
                return false;

            return true;
        }

        /// <inheritdoc />
        public virtual void ClearDropHighlightStatus()
        {
            RemoveFromClassList(m_CurrentDropHighlightClass);
        }

        /// <inheritdoc />
        public virtual void SetDropHighlightStatus(IReadOnlyList<GraphElementModel> dropCandidates)
        {
            m_CurrentDropHighlightClass = CanAcceptDrop(dropCandidates) ? dropHighlightAcceptedClass : dropHighlightDeniedClass;
            AddToClassList(m_CurrentDropHighlightClass);
        }

        /// <inheritdoc />
        public virtual void PerformDrop(IReadOnlyList<GraphElementModel> dropCandidates)
        {
            if (GraphView == null)
                return;

            var selectable = dropCandidates.Single();
            var portToConnect = GetPortToConnect(selectable);
            Assert.IsNotNull(portToConnect);

            var toPort = portToConnect.Direction == PortDirection.Input ? portToConnect : PortModel;
            var fromPort = portToConnect.Direction == PortDirection.Input ? PortModel : portToConnect;

            GraphView.Dispatch(new CreateWireCommand(toPort, fromPort, true));
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(PortConnectorWithIconPart.Create(connectorPartName, Model, this, ussClassName));
            PartList.AppendPart(PortConstantEditorPart.Create(constantEditorPartName, Model, this, ussClassName));
        }

        public Label Label
        {
            get;
            private set;
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            var constantPart = (PortConstantEditorPart)PartList.GetPart(constantEditorPartName);
            var connectorPart = (PortConnectorPart)PartList.GetPart(connectorPartName);

            ConnectorElement = connectorPart.Root ?? this;
            WireConnector = new WireConnector(GraphView);

            Label = ConnectorElement.Q<Label>(PortConnectorPart.labelName);

            if (constantPart.Dragger != null)
            {
                constantPart.Dragger.SetDragZone(Label);

                Label.EnableInClassList(FloatField.labelDraggerVariantUssClassName, true);
            }

            AddToClassList(ussClassName);
            this.AddStylesheet_Internal("Port.uss");
        }

        /// <inheritdoc />
        public override bool HasModelDependenciesChanged()
        {
            return true;
        }

        /// <inheritdoc/>
        public override void AddModelDependencies()
        {
            foreach (var wireModel in PortModel.GetConnectedWires())
            {
                AddDependencyToWireModel(wireModel);
            }
        }

        /// <inheritdoc />
        public override bool HasForwardsDependenciesChanged()
        {
            return m_ConnectedWiresCount != PortModel.GetConnectedWires().Count;
        }

        /// <inheritdoc />
        public override void AddForwardDependencies()
        {
            base.AddForwardDependencies();

            var wires = PortModel.GetConnectedWires();
            m_ConnectedWiresCount = wires.Count;
            foreach (var wireModel in wires)
            {
                var ui = wireModel.GetView_Internal(RootView);
                if (ui != null)
                    Dependencies.AddForwardDependency(ui, DependencyTypes.Geometry);
            }
        }

        /// <summary>
        /// Add <paramref name="wireModel"/> as a model dependency to this element.
        /// </summary>
        /// <param name="wireModel">The model to add as a dependency.</param>
        public void AddDependencyToWireModel(WireModel wireModel)
        {
            // When wire is created/deleted, port connector needs to be updated (filled/unfilled).
            Dependencies.AddModelDependency(wireModel);
        }

        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            base.OnCustomStyleResolved(evt);
            var currentColor = PortColor;

            if (evt.customStyle.TryGetValue(k_PortColorProperty, out var portColorValue))
                PortColor = portColorValue;

            if (currentColor != PortColor && PartList.GetPart(connectorPartName) is PortConnectorWithIconPart portConnector)
            {
                portConnector.UpdateFromModel();
            }
        }

        static List<(int id, string className)> s_PortTypeClassNameCache = new List<(int, string)>(4)
        {
            (PortType.Execution.Id, GenerateClassNameForPortType(PortType.Execution)),
            (PortType.Data.Id, GenerateClassNameForPortType(PortType.Data)),
            (PortType.MissingPort.Id, GenerateClassNameForPortType(PortType.MissingPort)),
        };

        static string GenerateClassNameForPortType(PortType t)
        {
            return portTypeModifierClassNamePrefix + GetClassNameSuffixForType(t);
        }

        protected static string GetClassNameSuffixForType(PortType t)
        {
            return t.ToString().ToLower();
        }

        static string GetCachedClassNameForPortType(PortType t)
        {
            var cacheIndex = -1;
            for (int i = 0; i < s_PortTypeClassNameCache.Count; i++)
            {
                if (s_PortTypeClassNameCache[i].id == t.Id)
                {
                    cacheIndex = i;
                    break;
                }
            }

            if (cacheIndex == -1)
            {
                cacheIndex = s_PortTypeClassNameCache.Count;
                s_PortTypeClassNameCache.Add((t.Id, GenerateClassNameForPortType(t)));
            }

            return s_PortTypeClassNameCache[cacheIndex].className;
        }

        static bool PortHasOption(PortModelOptions portOptions, PortModelOptions optionToCheck)
        {
            return (portOptions & optionToCheck) != 0;
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            var wasUpdatedAtLeastOnce = m_CurrentDataClassName != null;
            // Speed-up when creating many ports:
            // The first time the port is updated, avoid trying to remove classes we know aren't there
            void EnableClass(string className, bool condition)
            {
                if (wasUpdatedAtLeastOnce)
                    EnableInClassList(className, condition);
                else if (condition)
                {
                    AddToClassList(className);
                }
            }

            this.PreallocForMoreClasses(6); // hidden, connected, direction, capacity, datatype, type
            var hidden = PortModel != null && PortHasOption(PortModel.Options, PortModelOptions.Hidden);
            EnableClass(hiddenModifierUssClassName, hidden);

            var portIsConnected = PortModel.IsConnected();
            EnableClass(connectedModifierUssClassName, portIsConnected);
            EnableClass(notConnectedModifierUssClassName, !portIsConnected);

            EnableClass(inputModifierUssClassName, PortModel.Direction == PortDirection.Input);
            EnableClass(outputModifierUssClassName, PortModel.Direction == PortDirection.Output);
            EnableClass(capacityNoneModifierUssClassName, PortModel.Capacity == PortCapacity.None);
            EnableClass(verticalModifierUssClassName, PortModel.Orientation == PortOrientation.Vertical);

            this.ReplaceAndCacheClassName(portDataTypeClassNamePrefix + GetClassNameSuffixForDataType_Internal(PortModel.PortDataType), ref m_CurrentDataClassName);
            this.ReplaceAndCacheClassName(GetCachedClassNameForPortType(PortModel.PortType), ref m_CurrentTypeClassName);

            tooltip = PortModel.Orientation == PortOrientation.Horizontal ? PortModel.ToolTip :
                string.IsNullOrEmpty(PortModel.ToolTip) ? PortModel.UniqueName :
                PortModel.UniqueName + "\n" + PortModel.ToolTip;

            var connector = GetConnector();
            if (connector != null)
            {
                connector.generateVisualContent = PortModel.PortType == PortType.Execution ? OnGenerateExecutionConnectorVisualContent : OnGenerateDataConnectorVisualContent;
                connector.MarkDirtyRepaint();
            }

        }

        static readonly Dictionary<Type, string> k_TypeClassNameSuffix = new Dictionary<Type, string>();

        internal static string GetClassNameSuffixForDataType_Internal(Type thisPortType)
        {
            if (thisPortType == null)
                return String.Empty;

            if (k_TypeClassNameSuffix.TryGetValue(thisPortType, out var kebabCaseName))
                return kebabCaseName;

            if (thisPortType.IsSubclassOf(typeof(GameObject)))
                return "game-object";
            if (thisPortType.IsSubclassOf(typeof(Transform)))
                return "transform";
            if (thisPortType.IsSubclassOf(typeof(Material)))
                return "material";
            if (thisPortType.IsSubclassOf(typeof(Cubemap)) || thisPortType == typeof(Cubemap))
                return "cubemap";
            if (thisPortType.IsSubclassOf(typeof(Texture2DArray)))
                return "texture2darray";
            if (thisPortType.IsSubclassOf(typeof(Texture2D)) || thisPortType.IsSubclassOf(typeof(Texture)))
                return "texture2d";
            if (thisPortType.IsSubclassOf(typeof(Material)))
                return "material";
            if (thisPortType == typeof(MissingPort))
                return "missing-port";
            if (thisPortType == typeof(IList))
                return "list";
            if (thisPortType == typeof(IDictionary))
                return "dictionary";
            if (thisPortType.IsSubclassOf(typeof(Enum)))
                return "enum";
            if (thisPortType == typeof(Matrix4x4))
                return "matrix";

            kebabCaseName = thisPortType.Name.ToKebabCase_Internal();
            k_TypeClassNameSuffix.Add(thisPortType, kebabCaseName);
            return kebabCaseName;
        }

        public Vector3 GetGlobalCenter()
        {
            var connector = GetConnector();
            var localCenter = new Vector2(connector.layout.width * .5f, connector.layout.height * .5f);
            return connector.LocalToWorld(localCenter);
        }

        public VisualElement GetConnector()
        {
            if (m_ConnectorCache == null)
            {
                var portConnector = PartList.GetPart(connectorPartName) as PortConnectorPart;
                m_ConnectorCache = portConnector?.Connector ?? portConnector?.Root ?? this;
            }

            return m_ConnectorCache;
        }

        /// <summary>
        /// Whether the mouse is Hovering the port.
        /// </summary>
        public bool Hovering
        {
            get => m_Hovering;
            set
            {
                m_Hovering = value;
                GetConnector()?.MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Whether the port cap should actually be visible.
        /// </summary>
        public bool IsCapVisible => Hovering || WillConnect || PortModel.IsConnected();

        /// <summary>
        /// Gets the port hit box.
        /// </summary>
        /// <param name="port">The <see cref="Port"/> to get the hit box for.</param>
        /// <param name="isCreatingFrom">Whether the user is creating a wire from the port. Else, the user is plugging a wire into the port. </param>
        public static Rect GetPortHitBoxBounds(Port port, bool isCreatingFrom = false)
        {
            if (port.worldBound.height <= 0 || port.worldBound.width <= 0)
                return Rect.zero;

            var node = port.PortModel.NodeModel.GetView<Node>(port.RootView);
            if (node == null)
                return Rect.zero;

            var worldBoundHitBoxSize = MultiplyVector2(ref port.worldTransformRef, k_HitBoxSize);
            var connector = port.GetConnector();

            var direction = port.PortModel.Direction;
            var orientation = port.PortModel.Orientation;

            float hitBoxPosX;
            float hitBoxPosY;

            if (isCreatingFrom)
            {
                // The user is creating a wire from the port.
                if (orientation == PortOrientation.Horizontal)
                {
                    hitBoxPosX = direction == PortDirection.Input ? node.worldBound.xMin : node.worldBound.xMax - worldBoundHitBoxSize.x;
                    hitBoxPosY = connector.worldBound.center.y - worldBoundHitBoxSize.y * 0.5f;
                }
                else
                {
                    hitBoxPosX = connector.worldBound.center.x - worldBoundHitBoxSize.x * 0.5f;
                    hitBoxPosY = direction == PortDirection.Input ? connector.worldBound.yMin : connector.worldBound.yMax - worldBoundHitBoxSize.y;
                }
            }
            else
            {
                // The user is plugging a wire into the port.
                if (orientation == PortOrientation.Horizontal)
                {
                    hitBoxPosX = direction == PortDirection.Input ? connector.worldBound.xMax - worldBoundHitBoxSize.x : connector.worldBound.xMin;
                    hitBoxPosY = connector.worldBound.center.y - worldBoundHitBoxSize.y * 0.5f;
                }
                else
                {
                    worldBoundHitBoxSize.x = k_HitBoxSize.y;
                    worldBoundHitBoxSize.y = k_HitBoxSize.x;
                    worldBoundHitBoxSize = MultiplyVector2(ref port.worldTransformRef, worldBoundHitBoxSize);

                    hitBoxPosX = connector.worldBound.center.x - worldBoundHitBoxSize.x * 0.5f;
                    hitBoxPosY = direction == PortDirection.Input ? connector.worldBound.yMin - (worldBoundHitBoxSize.y - connector.layout.height) : connector.worldBound.yMin;
                }
            }

            return new Rect(new Vector2(hitBoxPosX, hitBoxPosY), worldBoundHitBoxSize);
        }

        PortModel GetPortToConnect(GraphElementModel selectable)
        {
            return (selectable as PortNodeModel)?.GetPortFitToConnectTo(PortModel);
        }

        void OnGenerateExecutionConnectorVisualContent(MeshGenerationContext mgc)
        {
            mgc.painter2D.strokeColor = PortColor;
            mgc.painter2D.lineJoin = LineJoin.Round;

            var paintRect = GetConnector().localBound;
            paintRect.position = Vector2.zero;

            MakeTriangle(mgc.painter2D, paintRect);
            mgc.painter2D.lineWidth = 1.0f;
            mgc.painter2D.Stroke();

            if (IsCapVisible)
            {
                paintRect.position += PortModel?.Orientation == PortOrientation.Horizontal ? new Vector2(1.33f, 2) : new Vector2(2, 1.33f);
                paintRect.size -= Vector2.one * 4;
                mgc.painter2D.fillColor = PortColor;
                MakeTriangle(mgc.painter2D, paintRect);
                mgc.painter2D.Fill();
            }
        }

        void OnGenerateDataConnectorVisualContent(MeshGenerationContext mgc)
        {
            mgc.painter2D.strokeColor = PortColor;
            mgc.painter2D.lineJoin = LineJoin.Round;

            var paintRect = GetConnector().localBound;
            paintRect.position = Vector2.zero;

            MakeCircle(mgc.painter2D, paintRect);
            mgc.painter2D.lineWidth = 1.0f;
            mgc.painter2D.Stroke();

            if (IsCapVisible)
            {
                paintRect.position += Vector2.one * 2;
                paintRect.size -= Vector2.one * 4;
                mgc.painter2D.fillColor = PortColor;
                MakeCircle(mgc.painter2D, paintRect);
                mgc.painter2D.Fill();
            }
        }

        void MakeTriangle(Painter2D painter2D, Rect paintRect)
        {
            painter2D.BeginPath();
            if (PortModel?.Orientation == PortOrientation.Horizontal)
            {
                painter2D.MoveTo(new Vector2(paintRect.xMin, paintRect.yMin));
                painter2D.LineTo(new Vector2(paintRect.xMax, paintRect.center.y));
                painter2D.LineTo(new Vector2(paintRect.xMin, paintRect.yMax));
                painter2D.LineTo(new Vector2(paintRect.xMin, paintRect.yMin));
            }
            else
            {
                painter2D.MoveTo(new Vector2(paintRect.xMin, paintRect.yMin));
                painter2D.LineTo(new Vector2(paintRect.xMax, paintRect.yMin));
                painter2D.LineTo(new Vector2(paintRect.center.x, paintRect.yMax));
                painter2D.LineTo(new Vector2(paintRect.xMin, paintRect.yMin));
            }
            painter2D.ClosePath();
        }

        void MakeCircle(Painter2D painter2D, Rect paintRect)
        {
            painter2D.BeginPath();
            painter2D.Arc(paintRect.center, paintRect.width * 0.5f, 0, Angle.Turns(1));
            painter2D.ClosePath();
        }
    }
}
