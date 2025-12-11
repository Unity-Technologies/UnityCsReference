// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.VectorGraphics
{
    /// <summary>An enum describing the viewport options to use when importing the SVG document.</summary>
    public enum ViewportOptions
    {
        /// <summary>Don't preserve the viewport defined in the SVG document.</summary>
        DontPreserve,

        /// <summary>Preserves the viewport defined in the SVG document.</summary>
        PreserveViewport,

        /// <summary>Applies the root view-box defined in the SVG document (if any).</summary>
        /// <remarks>
        /// This option will rescale the SVG asset to a unit size if a view-box is defined in the SVG document.
        /// If no view-box is defined, this option will have the same behavior as `DontPreserve`.
        /// It has limited use and is only available for legacy reasons.
        /// </remarks>
        OnlyApplyRootViewBox
    }

    /// <summary>Reads an SVG document and builds a vector scene.</summary>
    public class SVGParser
    {
        /// <summary>A structure containing the SVG scene data.</summary>
        public struct SceneInfo
        {
            internal SceneInfo(Scene scene, Rect sceneViewport, Dictionary<SceneNode, float> nodeOpacities, Dictionary<string, SceneNode> nodeIDs)
            {
                Scene = scene;
                SceneViewport = sceneViewport;
                NodeOpacity = nodeOpacities;
                NodeIDs = nodeIDs;
            }
        
            /// <summary>The vector scene.</summary>
            public Scene Scene { get; }

            /// <summary>The position and size of the SVG document</summary>
            public Rect SceneViewport { get; }

            /// <summary>A dictionary containing the opacity of the scene nodes.</summary>
            public Dictionary<SceneNode, float> NodeOpacity { get; }

            /// <summary>A dictionary containing the scene node for a given ID</summary>
            public Dictionary<string, SceneNode> NodeIDs { get; }
        }

        /// <summary>Kicks off an SVG file import.</summary>
        /// <param name="textReader">The reader object containing the SVG file data</param>
        /// <param name="dpi">The DPI of the SVG file, or 0 to use the device's DPI</param>
        /// <param name="pixelsPerUnit">How many SVG units fit in a Unity unit</param>
        /// <param name="windowWidth">The default with of the viewport, may be 0</param>
        /// <param name="windowHeight">The default height of the viewport, may be 0</param>
        /// <param name="clipViewport">Whether the vector scene should be clipped by the SVG document's viewport</param>
        /// <returns>A SceneInfo object containing the scene data</returns>
        public static SceneInfo ImportSVG(TextReader textReader, float dpi = 0.0f, float pixelsPerUnit = 1.0f, int windowWidth = 0, int windowHeight = 0, bool clipViewport = false)
        {
            var viewportOptions = clipViewport ? ViewportOptions.PreserveViewport : ViewportOptions.DontPreserve;
            return ImportSVG(textReader, viewportOptions, dpi, pixelsPerUnit, windowWidth, windowHeight);
        }

        /// <summary>Kicks off an SVG file import.</summary>
        /// <param name="textReader">The reader object containing the SVG file data</param>
        /// <param name="viewportOptions">The viewport options to use</param>
        /// <param name="dpi">The DPI of the SVG file, or 0 to use the device's DPI</param>
        /// <param name="pixelsPerUnit">How many SVG units fit in a Unity unit</param>
        /// <param name="windowWidth">The default with of the viewport, may be 0</param>
        /// <param name="windowHeight">The default height of the viewport, may be 0</param>
        /// <returns>A SceneInfo object containing the scene data</returns>
        public static SceneInfo ImportSVG(TextReader textReader, ViewportOptions viewportOptions, float dpi = 0.0f, float pixelsPerUnit = 1.0f, int windowWidth = 0, int windowHeight = 0)
        {
            var scene = new Scene();
            var settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;

            // Validation and resolving can reach through HTTP to fetch and validate against schemas/DTDs, which could take ages
            settings.DtdProcessing = System.Xml.DtdProcessing.Ignore;
            settings.ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.None;
            settings.ValidationType = ValidationType.None;
            settings.XmlResolver = null;

            if (dpi == 0.0f)
                dpi = Screen.dpi;

            Dictionary<SceneNode, float> nodeOpacities;
            Dictionary<string, SceneNode> nodeIDs;

            SVGDocument doc;
            using (var reader = XmlReader.Create(textReader, settings))
            {
                bool applyRootViewBox =
                    (viewportOptions == ViewportOptions.PreserveViewport) ||
                    (viewportOptions == ViewportOptions.OnlyApplyRootViewBox);
                doc = new SVGDocument(reader, dpi, scene, windowWidth, windowHeight, applyRootViewBox);
                doc.Import();
                nodeOpacities = doc.NodeOpacities;
                nodeIDs = doc.NodeIDs;
            }

            float scale = 1.0f / pixelsPerUnit;
            if ((scale != 1.0f) && (scene != null) && (scene.Root != null))
                scene.Root.Transform = scene.Root.Transform * Matrix2D.Scale(new Vector2(scale, scale));

            if ((viewportOptions == ViewportOptions.PreserveViewport) && (scene != null) && (scene.Root != null))
            {
                // Only add clipper if the scene isn't entirely contained in the viewport
                var sceneBounds = VectorUtils.SceneNodeBounds(scene.Root);
                if (!doc.sceneViewport.Contains(sceneBounds.min) || !doc.sceneViewport.Contains(sceneBounds.max))
                {
                    var rectClip = new Shape();
                    VectorUtils.MakeRectangleShape(rectClip, doc.sceneViewport);

                    // We cannot add the clipper directly on scene.Root since it may have a viewbox transform applied.
                    // The simplest is to replace the root node with the new "clipped" one, then the clipping
                    // rectangle can stay in the viewport space (no need to take the viewbox transform into account).
                    scene.Root = new SceneNode()
                    {
                        Children = new List<SceneNode> { scene.Root },
                        Clipper = new SceneNode() { Shapes = new List<Shape>() { rectClip } }
                    };
                }
            }
            
            return new SceneInfo(scene, doc.sceneViewport, nodeOpacities, nodeIDs);
        }
    }

    internal class XmlReaderIterator
    {
        internal class Node
        {
            public Node(XmlReader reader) { this.reader = reader; name = reader.Name; depth = reader.Depth; }
            public string Name { get { return name; } }
            public string this[string attrib] { get { return reader.GetAttribute(attrib); } }
            public SVGPropertySheet GetAttributes()
            {
                var atts = new SVGPropertySheet();
                for (int i = 0; i < reader.AttributeCount; ++i)
                {
                    reader.MoveToAttribute(i);
                    atts[reader.Name] = reader.Value;
                }
                reader.MoveToElement();
                return atts;
            }
            public SVGFormatException GetException(string message) { return new SVGFormatException(reader, message); }
            public SVGFormatException GetUnsupportedAttribValException(string attrib)
            {
                return new SVGFormatException(reader, "Value '" + this[attrib] + "' is invalid for attribute '" + attrib + "'");
            }

            public int Depth { get { return depth; } }
            XmlReader reader;
            int depth;
            string name;
        }

        public XmlReaderIterator(XmlReader reader) { this.reader = reader; }
        public bool GoToRoot(string tagName) { return reader.ReadToFollowing(tagName) && reader.Depth == 0; }
        public Node VisitCurrent() { currentElementVisited = true; return new Node(reader); }
        public bool IsEmptyElement() { return reader.IsEmptyElement; }

        public bool GoToNextChild(Node node)
        {
            if (!currentElementVisited)
                return reader.Depth == node.Depth + 1;

            reader.Read();
            while ((reader.NodeType != XmlNodeType.None) && (reader.NodeType != XmlNodeType.Element))
                reader.Read();
            if (reader.NodeType != XmlNodeType.Element)
                return false;

            currentElementVisited = false;
            return reader.Depth == node.Depth + 1;
        }

        public void SkipCurrentChildTree(Node node)
        {
            while (GoToNextChild(node))
                SkipCurrentChildTree(VisitCurrent());
        }

        public string ReadTextWithinElement()
        {
            if (reader.IsEmptyElement)
                return "";
            
            var text = "";
            while (reader.Read() && reader.NodeType != XmlNodeType.EndElement)
                text += reader.Value;

            return text;
        }

        XmlReader reader;
        bool currentElementVisited;
    }

    internal class SVGFormatException : Exception
    {
        public SVGFormatException() {}
        public SVGFormatException(string message) : base(ComposeMessage(null, message)) {}
        public SVGFormatException(XmlReader reader, string message) : base(ComposeMessage(reader, message)) {}

        public static SVGFormatException StackError { get { return new SVGFormatException("Vector scene construction mismatch"); } }

        static string ComposeMessage(XmlReader reader, string message)
        {
            IXmlLineInfo li = reader as IXmlLineInfo;
            if (li != null)
                return "SVG Error (line " + li.LineNumber + ", character " + li.LinePosition + "): " + message;
            return "SVG Error: " + message;
        }
    }

    internal class SVGDictionary : Dictionary<string, object> {}
    internal class SVGPostponedFills : Dictionary<IFill, string> { }

    internal class SVGDocument
    {
        public SVGDocument(XmlReader docReader, float dpi, Scene scene, int windowWidth, int windowHeight, bool applyRootViewBox)
        {
            allElems = new ElemHandler[]
            { circle, defs, ellipse, g, image, line, linearGradient, path, polygon, polyline, radialGradient, clipPath, pattern, mask, rect, symbol, use, style };

            // These elements excluded below should not be immediatelly part of the hierarchy and can only be referenced
            elemsToAddToHierarchy = new HashSet<ElemHandler>(new ElemHandler[]
                    { circle, /*defs,*/ ellipse, g, image, line, path, polygon, polyline, rect, /*symbol,*/ svg, use });

            this.docReader = new XmlReaderIterator(docReader);
            this.scene = scene;
            this.dpiScale = dpi / 90.0f; // SVG specs assume 90DPI but this machine might use something else
            this.windowWidth = windowWidth;
            this.windowHeight = windowHeight;
            this.applyRootViewBox = applyRootViewBox;
            this.svgObjects[StockBlackNonZeroFillName] = new SolidFill() { Color = new Color(0, 0, 0), Mode = FillMode.NonZero };
            this.svgObjects[StockBlackOddEvenFillName] = new SolidFill() { Color = new Color(0, 0, 0), Mode = FillMode.OddEven };
        }

        public void Import()
        {
            if (scene == null) throw new ArgumentNullException();
            if (!docReader.GoToRoot("svg"))
                throw new SVGFormatException("Document doesn't have 'svg' root");

            currentContainerSize.Push(new Vector2(windowWidth, windowHeight));

            svg();

            currentContainerSize.Pop();
            if (currentContainerSize.Count > 0)
                throw SVGFormatException.StackError;

            PostProcess(scene.Root);
            RemoveInvisibleNodes();
        }

        public Dictionary<SceneNode, float> NodeOpacities { get { return nodeOpacity; } }
        public Dictionary<string, SceneNode> NodeIDs { get { return nodeIDs; } }

        internal const float SVGLengthFactor = 1.41421356f; // Used when calculating relative lengths. See http://www.w3.org/TR/SVG/coords.html#Units
        static internal string StockBlackNonZeroFillName { get { return "unity_internal_black_nz"; } }
        static internal string StockBlackOddEvenFillName { get { return "unity_internal_black_oe"; } }

        void ParseChildren(XmlReaderIterator.Node node, string nodeName)
        {
            var sceneNode = currentSceneNode.Peek();

            var supportedChildren = subTags[nodeName];
            while (docReader.GoToNextChild(node))
            {
                var child = docReader.VisitCurrent();

                ElemHandler handler;
                if (!supportedChildren.TryGetValue(child.Name, out handler))
                {
                    System.Diagnostics.Debug.WriteLine("Skipping over unsupported child (" + child.Name + ") of a (" + node.Name + ")");
                    docReader.SkipCurrentChildTree(child);
                    continue;
                }

                bool addToSceneHierarchy = elemsToAddToHierarchy.Contains(handler);
                SceneNode childVectorNode = null;
                if (addToSceneHierarchy)
                {
                    if (sceneNode.Children == null)
                        sceneNode.Children = new List<SceneNode>();
                    childVectorNode = new SceneNode();
                    nodeGlobalSceneState[childVectorNode] = new NodeGlobalSceneState() { ContainerSize = currentContainerSize.Peek() };
                    sceneNode.Children.Add(childVectorNode);
                    currentSceneNode.Push(childVectorNode);
                }

                styles.PushNode(child);

                if (childVectorNode != null)
                {
                    styles.SaveLayerForSceneNode(childVectorNode);
                    if (styles.Evaluate("display") == "none")
                        invisibleNodes.Add(new NodeWithParent() { node = childVectorNode, parent = sceneNode });
                }

                handler();
                ParseChildren(child, child.Name); // Recurse

                styles.PopNode();

                if (addToSceneHierarchy && currentSceneNode.Pop() != childVectorNode)
                    throw SVGFormatException.StackError;
            }
        }

        #region Tag handling
        void circle()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseID(node, sceneNode);
            ParseOpacity(sceneNode);
            sceneNode.Transform = SVGAttribParser.ParseTransform(node);
            var fill = SVGAttribParser.ParseFill(node, svgObjects, postponedFills, styles);
            PathCorner strokeCorner;
            PathEnding strokeEnding;
            var stroke = ParseStrokeAttributeSet(node, out strokeCorner, out strokeEnding);

            float cx = AttribLengthVal(node, "cx", 0.0f, DimType.Width);
            float cy = AttribLengthVal(node, "cy", 0.0f, DimType.Height);
            float r = AttribLengthVal(node, "r", 0.0f, DimType.Length);

            var circle = new Shape();
            VectorUtils.MakeCircleShape(circle, new Vector2(cx, cy), r);
            circle.PathProps = new PathProperties() { Stroke = stroke, Head = strokeEnding, Tail = strokeEnding, Corners = strokeCorner };
            circle.Fill = fill;

            sceneNode.Shapes = new List<Shape>(1);
            sceneNode.Shapes.Add(circle);

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void defs()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = new SceneNode(); // A new scene node instead of one precreated for us
            ParseOpacity(sceneNode);
            sceneNode.Transform = SVGAttribParser.ParseTransform(node);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, allElems);

            currentSceneNode.Push(sceneNode);
            ParseChildren(node, node.Name);
            if (currentSceneNode.Pop() != sceneNode)
                throw SVGFormatException.StackError;
        }

        void ellipse()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseID(node, sceneNode);
            ParseOpacity(sceneNode);
            sceneNode.Transform = SVGAttribParser.ParseTransform(node);
            var fill = SVGAttribParser.ParseFill(node, svgObjects, postponedFills, styles);
            PathCorner strokeCorner;
            PathEnding strokeEnding;
            var stroke = ParseStrokeAttributeSet(node, out strokeCorner, out strokeEnding);

            float cx = AttribLengthVal(node, "cx", 0.0f, DimType.Width);
            float cy = AttribLengthVal(node, "cy", 0.0f, DimType.Height);
            float rx = AttribLengthVal(node, "rx", 0.0f, DimType.Length);
            float ry = AttribLengthVal(node, "ry", 0.0f, DimType.Length);

            var ellipse = new Shape();
            VectorUtils.MakeEllipseShape(ellipse, new Vector2(cx, cy), rx, ry);
            ellipse.PathProps = new PathProperties() { Stroke = stroke, Corners = strokeCorner, Head = strokeEnding, Tail = strokeEnding };
            ellipse.Fill = fill;

            sceneNode.Shapes = new List<Shape>(1);
            sceneNode.Shapes.Add(ellipse);

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void g()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseID(node, sceneNode);
            ParseOpacity(sceneNode);
            sceneNode.Transform = SVGAttribParser.ParseTransform(node);

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, allElems);
        }

        void image()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            // Try to get the referenced image first, if we fail, we just ignore the whole thing
            var url = node["xlink:href"];
            if (url != null)
            {
                var textureFill = new TextureFill();
                textureFill.Mode = FillMode.NonZero;
                textureFill.Addressing = AddressMode.Clamp;

                var lowercaseURL = url.ToLower();
                if (lowercaseURL.StartsWith("data:"))
                {
                    textureFill.Texture = DecodeTextureData(url);
                }
                else
                {
                    Debug.LogWarning("Unsupported URL scheme for <image>: " + url);
                }

                if (textureFill.Texture != null)
                {
                    // Fills and strokes don't seem to apply to image despite what the specs say
                    // All browsers and editing tools seem to ignore them, so we'll just do as well
                    ParseID(node, sceneNode);
                    ParseOpacity(sceneNode);
                    sceneNode.Transform = SVGAttribParser.ParseTransform(node);

                    var viewPort = ParseViewport(node, sceneNode, currentContainerSize.Peek());
                    sceneNode.Transform = sceneNode.Transform * Matrix2D.Translate(viewPort.position);
                    var viewBoxInfo = new ViewBoxInfo();
                    viewBoxInfo.ViewBox = new Rect(0, 0, textureFill.Texture.width, textureFill.Texture.height);
                    ParseViewBoxAspectRatio(node, ref viewBoxInfo);
                    ApplyViewBox(sceneNode, viewBoxInfo, viewPort);

                    var rect = new Shape();
                    VectorUtils.MakeRectangleShape(rect, new Rect(0, 0, textureFill.Texture.width, textureFill.Texture.height));
                    rect.Fill = textureFill;
                    sceneNode.Shapes = new List<Shape>(1);
                    sceneNode.Shapes.Add(rect);

                    ParseClipAndMask(node, sceneNode);
                }
            }

            // Resolve any previous node that was referencing this image
            string id = node["id"];
            if (!string.IsNullOrEmpty(id))
            {
                List<NodeReferenceData> refList;
                if (postponedSymbolData.TryGetValue(id, out refList))
                {
                    foreach (var refData in refList)
                        ResolveReferencedNode(sceneNode, refData, true);
                }
            }

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void line()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseID(node, sceneNode);
            ParseOpacity(sceneNode);
            sceneNode.Transform = SVGAttribParser.ParseTransform(node);
            PathCorner strokeCorner;
            PathEnding strokeEnding;
            var stroke = ParseStrokeAttributeSet(node, out strokeCorner, out strokeEnding);

            float x1 = AttribLengthVal(node, "x1", 0.0f, DimType.Width);
            float y1 = AttribLengthVal(node, "y1", 0.0f, DimType.Height);
            float x2 = AttribLengthVal(node, "x2", 0.0f, DimType.Width);
            float y2 = AttribLengthVal(node, "y2", 0.0f, DimType.Height);

            var path = new Shape();
            path.PathProps = new PathProperties() { Stroke = stroke, Head = strokeEnding, Tail = strokeEnding };
            path.Contours = new BezierContour[] {
                new BezierContour() { Segments = VectorUtils.BezierSegmentToPath(VectorUtils.MakeLine(new Vector2(x1, y1), new Vector2(x2, y2))) }
            };
            sceneNode.Shapes = new List<Shape>(1);
            sceneNode.Shapes.Add(path);

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void linearGradient()
        {
            var node = docReader.VisitCurrent();

            var link = node["xlink:href"];
            var refFill = SVGAttribParser.ParseRelativeRef(link, svgObjects) as GradientFill;
            var refFillData = refFill != null ? gradientExInfo[refFill] as LinearGradientExData : null;

            bool relativeToWorld = refFillData != null ? refFillData.WorldRelative : false;
            switch (node["gradientUnits"])
            {
                case null:
                    break;

                case "objectBoundingBox":
                    relativeToWorld = false;
                    break;

                case "userSpaceOnUse":
                    relativeToWorld = true;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("gradientUnits");
            }

            AddressMode addressing = refFill != null ? refFill.Addressing : AddressMode.Clamp;
            switch (node["spreadMethod"])
            {
                case null:
                    break;

                case "pad":
                    addressing = AddressMode.Clamp;
                    break;

                case "reflect":
                    addressing = AddressMode.Mirror;
                    break;

                case "repeat":
                    addressing = AddressMode.Wrap;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("spreadMethod");
            }

            var gradientTransform = SVGAttribParser.ParseTransform(node, "gradientTransform");

            GradientFill fill = CloneGradientFill(refFill);
            if (fill == null)
                fill = new GradientFill() { Addressing = addressing, Type = GradientFillType.Linear };

            fill.Type = GradientFillType.Linear;

            LinearGradientExData fillExData = new LinearGradientExData() { WorldRelative = relativeToWorld, FillTransform = gradientTransform };
            gradientExInfo[fill] = fillExData;

            // Fills are defined outside of a shape scope, so we can't resolve relative coordinates here.
            // We defer this entire operation to AdjustFills pass, but we still do value validation here
            // nonetheless to give meaningful error messages to the user if any.
            currentContainerSize.Push(Vector2.one);

            fillExData.X1 = node["x1"];
            fillExData.Y1 = node["y1"];
            fillExData.X2 = node["x2"];
            fillExData.Y2 = node["y2"];

            // The calls below are ineffective but they validate the inputs and throw an error if wrong values are specified, so don't remove them
            AttribLengthVal(fillExData.X1, node, "x1", 0.0f, DimType.Width);
            AttribLengthVal(fillExData.Y1, node, "y1", 0.0f, DimType.Height);
            AttribLengthVal(fillExData.X2, node, "x2", 1.0f, DimType.Width);
            AttribLengthVal(fillExData.Y2, node, "y2", 0.0f, DimType.Height);

            currentContainerSize.Pop();
            currentGradientFill = fill; // Children stops will register to this fill now
            currentGradientId = node["id"];
            currentGradientLink = SVGAttribParser.CleanIri(link);

            if (!string.IsNullOrEmpty(link) && !svgObjects.ContainsKey(link))
            {
                // Reference may be defined later in the file. Save for postponed processing.
                if (!postponedStopData.ContainsKey(currentGradientLink))
                    postponedStopData.Add(currentGradientLink, new List<PostponedStopData>());
                postponedStopData[currentGradientLink].Add(new PostponedStopData() { fill = fill });
            }

            AddToSVGDictionaryIfPossible(node, fill);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, stop);
        }

        void path()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseID(node, sceneNode);
            ParseOpacity(sceneNode);
            sceneNode.Transform = SVGAttribParser.ParseTransform(node);
            var fill = SVGAttribParser.ParseFill(node, svgObjects, postponedFills, styles);
            PathCorner strokeCorner;
            PathEnding strokeEnding;
            var stroke = ParseStrokeAttributeSet(node, out strokeCorner, out strokeEnding);
            var pathProps = new PathProperties() { Stroke = stroke, Corners = strokeCorner, Head = strokeEnding, Tail = strokeEnding };

            // A path may have 1 or more sub paths. Each for us is an individual vector path.
            var contours = SVGAttribParser.ParsePath(node);
            if ((contours != null) && (contours.Count > 0))
            {
                //float pathLength = AttribFloatVal(node, "pathLength"); // This is useful for animation purposes mostly

                sceneNode.Shapes = new List<Shape>(1);
                sceneNode.Shapes.Add(new Shape() { Contours = contours.ToArray(), Fill = fill, PathProps = pathProps });

                AddToSVGDictionaryIfPossible(node, sceneNode);
            }

            ParseClipAndMask(node, sceneNode);

            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void polygon()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseID(node, sceneNode);
            ParseOpacity(sceneNode);
            sceneNode.Transform = SVGAttribParser.ParseTransform(node);
            var fill = SVGAttribParser.ParseFill(node, svgObjects, postponedFills, styles);
            PathCorner strokeCorner;
            PathEnding strokeEnding;
            var stroke = ParseStrokeAttributeSet(node, out strokeCorner, out strokeEnding);

            var pointsAttribVal = node["points"];
            var pointsString = (pointsAttribVal != null) ? pointsAttribVal.Split(whiteSpaceNumberChars, StringSplitOptions.RemoveEmptyEntries) : null;
            if (pointsString != null)
            {
                if ((pointsString.Length & 1) == 1)
                    throw node.GetException("polygon 'points' must specify x,y for each coordinate");
                if (pointsString.Length < 4)
                    throw node.GetException("polygon 'points' do not even specify one triangle");

                var pathProps = new PathProperties() { Stroke = stroke, Corners = strokeCorner, Head = strokeEnding, Tail = strokeEnding };
                var contour = new BezierContour() { Closed = true };
                var lastPoint = new Vector2(
                        AttribLengthVal(pointsString[0], node, "points", 0.0f, DimType.Width),
                        AttribLengthVal(pointsString[1], node, "points", 0.0f, DimType.Height));
                int maxSegments = pointsString.Length / 2;
                var segments = new List<BezierPathSegment>(maxSegments);
                for (int i = 1; i < maxSegments; i++)
                {
                    var newPoint = new Vector2(
                            AttribLengthVal(pointsString[i * 2 + 0], node, "points", 0.0f, DimType.Width),
                            AttribLengthVal(pointsString[i * 2 + 1], node, "points", 0.0f, DimType.Height));
                    if (newPoint == lastPoint)
                        continue;
                    var seg = VectorUtils.MakeLine(lastPoint, newPoint);
                    segments.Add(new BezierPathSegment() { P0 = seg.P0, P1 = seg.P1, P2 = seg.P2 });
                    lastPoint = newPoint;
                }

                if (segments.Count > 0)
                {
                    var connect = VectorUtils.MakeLine(lastPoint, segments[0].P0);
                    segments.Add(new BezierPathSegment() { P0 = connect.P0, P1 = connect.P1, P2 = connect.P2 });
                    contour.Segments = segments.ToArray();

                    var shape = new Shape() { Contours = new BezierContour[] { contour }, PathProps = pathProps, Fill = fill };
                    sceneNode.Shapes = new List<Shape>(1);
                    sceneNode.Shapes.Add(shape);
                }
            }

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void polyline()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseID(node, sceneNode);
            ParseOpacity(sceneNode);
            sceneNode.Transform = SVGAttribParser.ParseTransform(node);
            var fill = SVGAttribParser.ParseFill(node, svgObjects, postponedFills, styles);
            PathCorner strokeCorner;
            PathEnding strokeEnding;
            var stroke = ParseStrokeAttributeSet(node, out strokeCorner, out strokeEnding);

            var pointsAttribVal = node["points"];
            var pointsString = (pointsAttribVal != null) ? pointsAttribVal.Split(whiteSpaceNumberChars, StringSplitOptions.RemoveEmptyEntries) : null;
            if (pointsString != null)
            {
                if ((pointsString.Length & 1) == 1)
                    throw node.GetException("polyline 'points' must specify x,y for each coordinate");
                if (pointsString.Length < 4)
                    throw node.GetException("polyline 'points' do not even specify one line");

                var shape = new Shape() { Fill = fill };
                shape.PathProps = new PathProperties() { Stroke = stroke, Corners = strokeCorner, Head = strokeEnding, Tail = strokeEnding };
                var lastPoint = new Vector2(
                        AttribLengthVal(pointsString[0], node, "points", 0.0f, DimType.Width),
                        AttribLengthVal(pointsString[1], node, "points", 0.0f, DimType.Height));
                int maxSegments = pointsString.Length / 2;
                var segments = new List<BezierPathSegment>(maxSegments);
                for (int i = 1; i < maxSegments; i++)
                {
                    var newPoint = new Vector2(
                            AttribLengthVal(pointsString[i * 2 + 0], node, "points", 0.0f, DimType.Width),
                            AttribLengthVal(pointsString[i * 2 + 1], node, "points", 0.0f, DimType.Height));
                    if (newPoint == lastPoint)
                        continue;
                    var seg = VectorUtils.MakeLine(lastPoint, newPoint);
                    segments.Add(new BezierPathSegment() { P0 = seg.P0, P1 = seg.P1, P2 = seg.P2 });
                    lastPoint = newPoint;
                }
                if (segments.Count > 0 )
                {
                    var connect = VectorUtils.MakeLine(lastPoint, segments[0].P0);
                    segments.Add(new BezierPathSegment() { P0 = connect.P0, P1 = connect.P1, P2 = connect.P2 });
                    shape.Contours = new BezierContour[] {
                         new BezierContour() { Segments = segments.ToArray() }
                    };
                    sceneNode.Shapes = new List<Shape>(1);
                    sceneNode.Shapes.Add(shape);
                }
            }

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void radialGradient()
        {
            var node = docReader.VisitCurrent();

            var link = node["xlink:href"];
            var refFill = SVGAttribParser.ParseRelativeRef(link, svgObjects) as GradientFill;
            var refFillData = refFill != null ? gradientExInfo[refFill] as RadialGradientExData : null;

            bool relativeToWorld = refFillData != null ? refFillData.WorldRelative : false;
            switch (node["gradientUnits"])
            {
                case null:
                    break;

                case "objectBoundingBox":
                    relativeToWorld = false;
                    break;

                case "userSpaceOnUse":
                    relativeToWorld = true;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("gradientUnits");
            }

            AddressMode addressing = refFill != null ? refFill.Addressing : AddressMode.Clamp;
            switch (node["spreadMethod"])
            {
                case null:
                    break;

                case "pad":
                    addressing = AddressMode.Clamp;
                    break;

                case "reflect":
                    addressing = AddressMode.Mirror;
                    break;

                case "repeat":
                    addressing = AddressMode.Wrap;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("spreadMethod");
            }

            var gradientTransform = SVGAttribParser.ParseTransform(node, "gradientTransform");

            GradientFill fill = CloneGradientFill(refFill);
            if (fill == null)
                fill = new GradientFill() { Addressing = addressing, Type = GradientFillType.Radial };

            fill.Type = GradientFillType.Radial;

            RadialGradientExData fillExData = new RadialGradientExData() { WorldRelative = relativeToWorld, FillTransform = gradientTransform };
            gradientExInfo[fill] = fillExData;

            // Fills are defined outside of a shape scope, so we can't resolve relative coordinates here.
            // We defer this entire operation to AdjustFills pass, but we still do value validation here
            // nonetheless to give meaningful error messages to the user if any.
            currentContainerSize.Push(Vector2.one);

            fillExData.Cx = node["cx"];
            fillExData.Cy = node["cy"];
            fillExData.Fx = node["fx"];
            fillExData.Fy = node["fy"];
            fillExData.R = node["r"];

            // The calls below are ineffective but they validate the inputs and throw an error if wrong values are specified, so don't remove them
            AttribLengthVal(fillExData.Cx, node, "cx", 0.5f, DimType.Width);
            AttribLengthVal(fillExData.Cy, node, "cy", 0.5f, DimType.Height);
            AttribLengthVal(fillExData.Fx, node, "fx", 0.5f, DimType.Width);
            AttribLengthVal(fillExData.Fy, node, "fy", 0.5f, DimType.Height);
            AttribLengthVal(fillExData.R, node, "r", 0.5f, DimType.Length);

            currentContainerSize.Pop();
            currentGradientFill = fill; // Children stops will register to this fill now
            currentGradientId = node["id"];
            currentGradientLink = SVGAttribParser.CleanIri(link);

            if (!string.IsNullOrEmpty(link) && !svgObjects.ContainsKey(link))
            {
                // Reference may be defined later in the file. Save for postponed processing.
                if (!postponedStopData.ContainsKey(currentGradientLink))
                    postponedStopData.Add(currentGradientLink, new List<PostponedStopData>());
                postponedStopData[currentGradientLink].Add(new PostponedStopData() { fill = fill });
            }

            AddToSVGDictionaryIfPossible(node, fill);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, stop);
        }

        void clipPath()
        {
            var node = docReader.VisitCurrent();
            string id = node["id"];

             // A new scene node instead of one precreated for us
            var clipRoot = new SceneNode() {
                Transform = SVGAttribParser.ParseTransform(node)
            };

            bool relativeToWorld;
            switch (node["clipPathUnits"])
            {
                case null:
                case "userSpaceOnUse":
                    relativeToWorld = true;
                    break;

                case "objectBoundingBox":
                    relativeToWorld = false;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("clipPathUnits");
            }

            clipData[clipRoot] = new ClipData() { WorldRelative = relativeToWorld };

            AddToSVGDictionaryIfPossible(node, clipRoot);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, allElems);

            currentSceneNode.Push(clipRoot);
            ParseChildren(node, node.Name);
            if (currentSceneNode.Pop() != clipRoot)
                throw SVGFormatException.StackError;
            
            // Resolve any previous node that was referencing this clipping path
            if (!string.IsNullOrEmpty(id))
            {
                List<PostponedClip> clips;
                if (postponedClip.TryGetValue(id, out clips))
                {
                    foreach (var clip in clips)
                        ApplyClipper(clipRoot, clip.node, relativeToWorld);
                }
            }

        }

        void pattern()
        {
            var node = docReader.VisitCurrent();

            // A new scene node instead of one precreated for us
            var patternRoot = new SceneNode() {
                Transform = Matrix2D.identity
            };

            bool relativeToWorld = false;
            switch (node["patternUnits"])
            {
                case null:
                case "objectBoundingBox":
                    relativeToWorld = false;
                    break;

                case "userSpaceOnUse":
                    relativeToWorld = true;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("patternUnits");
            }

            bool contentRelativeToWorld = true;
            switch (node["patternContentUnits"])
            {
                case null:
                case "userSpaceOnUse":
                    contentRelativeToWorld = true;
                    break;

                case "objectBoundingBox":
                    contentRelativeToWorld = false;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("patternContentUnits");
            }

            var x = AttribLengthVal(node["x"], node, "x", 0.0f, DimType.Width);
            var y = AttribLengthVal(node["y"], node, "y", 0.0f, DimType.Height);
            var w = AttribLengthVal(node["width"], node, "width", 0.0f, DimType.Width);
            var h = AttribLengthVal(node["height"], node, "height", 0.0f, DimType.Height);

            var patternTransform = SVGAttribParser.ParseTransform(node, "patternTransform");

            patternData[patternRoot] = new PatternData() {
                WorldRelative = relativeToWorld,
                ContentWorldRelative = contentRelativeToWorld,
                PatternTransform = patternTransform
            };

            var fill = new PatternFill() { 
                Pattern = patternRoot,
                Rect = new Rect(x, y, w, h)
            };

            AddToSVGDictionaryIfPossible(node, fill);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, allElems);

            currentSceneNode.Push(patternRoot);
            ParseChildren(node, node.Name);
            if (currentSceneNode.Pop() != patternRoot)
                throw SVGFormatException.StackError;
        }

        void mask()
        {
            var node = docReader.VisitCurrent();

            // A new scene node instead of one precreated for us
            var maskRoot = new SceneNode() {
                Transform = Matrix2D.identity
            };

            bool relativeToWorld;
            switch (node["maskUnits"])
            {
                case null:
                case "userSpaceOnUse":
                    relativeToWorld = true;
                    break;

                case "objectBoundingBox":
                    relativeToWorld = false;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("maskUnits");
            }

            bool contentRelativeToWorld;
            switch (node["maskContentUnits"])
            {
                case null:
                case "userSpaceOnUse":
                    contentRelativeToWorld = true;
                    break;

                case "objectBoundingBox":
                    contentRelativeToWorld = false;
                    break;

                default:
                    throw node.GetUnsupportedAttribValException("maskContentUnits");
            }

            maskData[maskRoot] = new MaskData() {
                WorldRelative = relativeToWorld,
                ContentWorldRelative = contentRelativeToWorld,
            };

            AddToSVGDictionaryIfPossible(node, maskRoot);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, allElems);

            currentSceneNode.Push(maskRoot);
            ParseChildren(node, node.Name);
            if (currentSceneNode.Pop() != maskRoot)
                throw SVGFormatException.StackError;
        }

        void rect()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseID(node, sceneNode);
            ParseOpacity(sceneNode);
            sceneNode.Transform = SVGAttribParser.ParseTransform(node);
            var fill = SVGAttribParser.ParseFill(node, svgObjects, postponedFills, styles);
            PathCorner strokeCorner;
            PathEnding strokeEnding;
            var stroke = ParseStrokeAttributeSet(node, out strokeCorner, out strokeEnding);

            float x = AttribLengthVal(node, "x", 0.0f, DimType.Width);
            float y = AttribLengthVal(node, "y", 0.0f, DimType.Height);
            float rx = AttribLengthVal(node, "rx", -1.0f, DimType.Length);
            float ry = AttribLengthVal(node, "ry", -1.0f, DimType.Length);
            float width = AttribLengthVal(node, "width", 0.0f, DimType.Length);
            float height = AttribLengthVal(node, "height", 0.0f, DimType.Length);

            if ((rx < 0.0f) && (ry >= 0.0f))
                rx = ry;
            else if ((ry < 0.0f) && (rx >= 0.0f))
                ry = rx;
            else if ((ry < 0.0f) && (rx < 0.0f))
                rx = ry = 0.0f;
            rx = Mathf.Min(rx, width * 0.5f);
            ry = Mathf.Min(ry, height * 0.5f);

            var rad = new Vector2(rx, ry);
            var rect = new Shape();
            VectorUtils.MakeRectangleShape(rect, new Rect(x, y, width, height), rad, rad, rad, rad);
            rect.Fill = fill;
            rect.PathProps = new PathProperties() { Stroke = stroke, Head = strokeEnding, Tail = strokeEnding, Corners = strokeCorner };
            sceneNode.Shapes = new List<Shape>(1);
            sceneNode.Shapes.Add(rect);

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void stop()
        {
            var node = docReader.VisitCurrent();
            System.Diagnostics.Debug.Assert(currentGradientFill != null);

            GradientStop stop = new GradientStop();

            string stopColor = styles.Evaluate("stop-color");
            Color color = stopColor != null ? SVGAttribParser.ParseColor(stopColor) : Color.black;

            color.a = AttribFloatVal("stop-opacity", 1.0f);
            stop.Color = color;

            string offsetString = styles.Evaluate("offset");
            if (!string.IsNullOrEmpty(offsetString))
            {
                bool percentage = offsetString.EndsWith("%");
                if (percentage)
                    offsetString = offsetString.Substring(0, offsetString.Length - 1);
                stop.StopPercentage = SVGAttribParser.ParseFloat(offsetString);
                if (percentage)
                    stop.StopPercentage /= 100.0f;

                stop.StopPercentage = Mathf.Max(0.0f, stop.StopPercentage);
                stop.StopPercentage = Mathf.Min(1.0f, stop.StopPercentage);
            }

            // I don't like this, but hopefully there aren't many stops in a gradient
            GradientStop[] newStops;
            if (currentGradientFill.Stops == null || currentGradientFill.Stops.Length == 0)
                newStops = new GradientStop[1];
            else
            {
                newStops = new GradientStop[currentGradientFill.Stops.Length + 1];
                currentGradientFill.Stops.CopyTo(newStops, 0);
            }
            newStops[newStops.Length - 1] = stop;
            currentGradientFill.Stops = newStops;

            // Apply postponed stops if this was defined later in the file
            if (!string.IsNullOrEmpty(currentGradientId) && postponedStopData.ContainsKey(currentGradientId))
            {
                foreach (var postponedStop in postponedStopData[currentGradientId])
                    postponedStop.fill.Stops = newStops;
            }

            // Local stops overrides referenced ones
            if (!string.IsNullOrEmpty(currentGradientLink) && postponedStopData.ContainsKey(currentGradientLink))
            {
                var stopDataList = postponedStopData[currentGradientLink];
                foreach (var postponedStop in stopDataList)
                {
                    if (postponedStop.fill == currentGradientFill)
                    {
                        stopDataList.Remove(postponedStop);
                        break;
                    }
                }
            }

            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void svg()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = new SceneNode();
            if (scene.Root == null) // If this is the root SVG element, then we set the vector scene root as well
            {
                System.Diagnostics.Debug.Assert(currentSceneNode.Count == 0);
                scene.Root = sceneNode;
            }

            styles.PushNode(node);

            ParseID(node, sceneNode);
            ParseOpacity(sceneNode);

            sceneViewport = ParseViewport(node, sceneNode, new Vector2(windowWidth, windowHeight));
            var viewBoxInfo = ParseViewBox(node, sceneNode, sceneViewport);
            if (applyRootViewBox)
                ApplyViewBox(sceneNode, viewBoxInfo, sceneViewport);

            currentContainerSize.Push(sceneViewport.size);
            if (!viewBoxInfo.IsEmpty)
                currentViewBoxSize.Push(viewBoxInfo.ViewBox.size);

            currentSceneNode.Push(sceneNode);
            nodeGlobalSceneState[sceneNode] = new NodeGlobalSceneState() { ContainerSize = currentContainerSize.Peek() };

            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, allElems);
            ParseChildren(node, "svg");

            if (currentSceneNode.Pop() != sceneNode)
                throw SVGFormatException.StackError;

            if (!viewBoxInfo.IsEmpty)
                currentViewBoxSize.Pop();
            currentContainerSize.Pop();

            styles.PopNode();
        }

        void symbol()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = new SceneNode(); // A new scene node instead of one precreated for us
            string id = node["id"];

            ParseID(node, sceneNode);
            ParseOpacity(sceneNode);
            sceneNode.Transform = Matrix2D.identity;

            Rect viewportRect = new Rect(Vector2.zero, currentContainerSize.Peek());
            var viewBoxInfo = ParseViewBox(node, sceneNode, viewportRect);
            if (!viewBoxInfo.IsEmpty)
                currentViewBoxSize.Push(viewBoxInfo.ViewBox.size);

            symbolViewBoxes[sceneNode] = viewBoxInfo;

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node, allElems);

            currentSceneNode.Push(sceneNode);
            ParseChildren(node, node.Name);
            if (currentSceneNode.Pop() != sceneNode)
                throw SVGFormatException.StackError;

            if (!viewBoxInfo.IsEmpty)
                currentViewBoxSize.Pop();

            ParseClipAndMask(node, sceneNode);

            // Resolve any previous node that was referencing this symbol
            if (!string.IsNullOrEmpty(id))
            {
                List<NodeReferenceData> refList;
                if (postponedSymbolData.TryGetValue(id, out refList))
                {
                    foreach (var refData in refList)
                        ResolveReferencedNode(sceneNode, refData, true);
                }
            }
        }

        void use()
        {
            var node = docReader.VisitCurrent();
            var sceneNode = currentSceneNode.Peek();

            ParseOpacity(sceneNode);

            var sceneViewport = ParseViewport(node, sceneNode, Vector2.zero);
            var refData = new NodeReferenceData() {
                node = sceneNode,
                viewport = sceneViewport,
                id = node["id"]
            };

            var iri = node["xlink:href"];
            var referencedNode = SVGAttribParser.ParseRelativeRef(iri, svgObjects) as SceneNode;
            if (referencedNode == null && !string.IsNullOrEmpty(iri) && iri.StartsWith("#"))
            {
                // The referenced node may be defined later in the file, save it for later
                iri = iri.Substring(1);
                List<NodeReferenceData> refList;
                if (!postponedSymbolData.TryGetValue(iri, out refList))
                {
                    refList = new List<NodeReferenceData>();
                    postponedSymbolData[iri] = refList;
                }
                refList.Add(refData);
            }

            sceneNode.Transform = SVGAttribParser.ParseTransform(node);
            sceneNode.Transform = sceneNode.Transform * Matrix2D.Translate(sceneViewport.position);

            if (referencedNode != null)
                ResolveReferencedNode(referencedNode, refData, false);

            ParseClipAndMask(node, sceneNode);

            AddToSVGDictionaryIfPossible(node, sceneNode);
            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }

        void style()
        {
            var node = docReader.VisitCurrent();
            var text = docReader.ReadTextWithinElement();

            if (text.Length > 0)
                styles.SetGlobalStyleSheet(SVGStyleSheetUtils.Parse(text));

            if (ShouldDeclareSupportedChildren(node))
                SupportElems(node);  // No children supported
        }
        #endregion

        #region Symbol Reference Processing
        private void ResolveReferencedNode(SceneNode referencedNode, NodeReferenceData refData, bool isDeferred)
        {
            // Note we don't use the viewport size because the <use> element doesn't establish a viewport for its referenced elements
            ViewBoxInfo viewBoxInfo;
            if (symbolViewBoxes.TryGetValue(referencedNode, out viewBoxInfo))
                ApplyViewBox(refData.node, viewBoxInfo, refData.viewport); // When using a symbol we need to apply the symbol's view box

            if (refData.node.Children == null)
                refData.node.Children = new List<SceneNode>();

            SVGStyleResolver.StyleLayer rootLayer = null;
            if (isDeferred)
            {
                // If deferred, push back the original <use> tag style layer to be in the same "style environment"
                rootLayer = styles.GetLayerForScenNode(refData.node);
                if (rootLayer != null)
                    styles.PushLayer(rootLayer);
            }

            // Activate the styles of the referenced node
            var styleLayer = nodeStyleLayers[referencedNode];
            if (styleLayer != null)
                styles.PushLayer(styleLayer);

            // Build a map to be able to retrieve the original node's style layer
            var originalNodes = new List<SceneNode>(10);
            foreach (var child in VectorUtils.SceneNodes(referencedNode))
                originalNodes.Add(child);

            var node = CloneSceneNode(referencedNode);

            int originalIndex = 0;
            foreach (var child in VectorUtils.SceneNodes(node))
            {
                var nodeIndex = originalIndex++;
                if (child.Shapes == null)
                    continue;

                var originalNode = originalNodes[nodeIndex];
                var layer = styles.GetLayerForScenNode(originalNode);
                if (layer != null)
                    styles.PushLayer(layer);

                bool isDefaultFill;
                var fill = SVGAttribParser.ParseFill(null, svgObjects, postponedFills, styles, Inheritance.Inherited, out isDefaultFill);
                PathCorner strokeCorner;
                PathEnding strokeEnding;
                var stroke = ParseStrokeAttributeSet(null, out strokeCorner, out strokeEnding);

                foreach (var shape in child.Shapes)
                {
                    var pathProps = shape.PathProps;
                    pathProps.Stroke = stroke;
                    pathProps.Corners = strokeCorner;
                    pathProps.Head = strokeEnding;
                    shape.PathProps = pathProps;
                    shape.Fill = isDefaultFill ? shape.Fill : fill;
                }

                if (layer != null)
                    styles.PopLayer();
            }

            if (styleLayer != null)
                styles.PopLayer();

            if (rootLayer != null)
                styles.PopLayer();

            // We process the node ID here to refer to the proper scene node
            if (!string.IsNullOrEmpty(refData.id))
                nodeIDs[refData.id] = node;

            refData.node.Children.Add(node);
        }
        #endregion

        #region Scene Node Cloning
        // This is a poor man's cloning system, until we have proper serialization in VectorScene.
        private SceneNode CloneSceneNode(SceneNode node)
        {
            if (node == null)
                return null;

            List<SceneNode> children = null;
            if (node.Children != null)
            {
                children = new List<SceneNode>(node.Children.Count);
                foreach (var c in node.Children)
                    children.Add(CloneSceneNode(c));
            }

            List<Shape> shapes = null;
            if (node.Shapes != null)
            {
                shapes = new List<Shape>(node.Shapes.Count);
                foreach (var d in node.Shapes)
                    shapes.Add(CloneShape(d));
            }

            var n = new SceneNode() {
                Children = children,
                Shapes = shapes,
                Transform = node.Transform,
                Clipper = CloneSceneNode(node.Clipper)
            };

            if (nodeGlobalSceneState.ContainsKey(node))
                nodeGlobalSceneState[n] = nodeGlobalSceneState[node];
            if (nodeOpacity.ContainsKey(node))
                nodeOpacity[n] = nodeOpacity[node];

            return n;
        }

        private Shape CloneShape(Shape shape)
        {
            if (shape == null)
                return null;

            BezierContour[] contours = null;
            if (shape.Contours != null)
            {
                contours = new BezierContour[shape.Contours.Length];
                for (int i = 0; i < contours.Length; ++i)
                    contours[i] = CloneContour(shape.Contours[i]);
            }
            return new Shape() {
                Fill = CloneFill(shape.Fill),
                FillTransform = shape.FillTransform,
                PathProps = ClonePathProps(shape.PathProps),
                Contours = contours,
                IsConvex = shape.IsConvex
            };
        }

        private BezierContour CloneContour(BezierContour c)
        {
            BezierPathSegment[] segs = null;
            if (c.Segments != null)
            {
                segs = new BezierPathSegment[c.Segments.Length];
                for (int i = 0; i < segs.Length; ++i)
                {
                    var s = c.Segments[i];
                    segs[i] = new BezierPathSegment() { P0 = s.P0, P1 = s.P1, P2 = s.P2 };
                }
            }
            return new BezierContour() { Segments = segs, Closed = c.Closed };
        }

        private IFill CloneFill(IFill fill)
        {
            if (fill == null)
                return null;

            IFill f = null;
            if (fill is SolidFill)
            {
                var solid = fill as SolidFill;
                f = new SolidFill() {
                    Color = solid.Color,
                    Opacity = solid.Opacity,
                    Mode = solid.Mode
                };
            }
            else if (fill is GradientFill)
            {
                var grad = fill as GradientFill;
                GradientStop[] stops = null;
                if (grad.Stops != null)
                {
                    stops = new GradientStop[grad.Stops.Length];
                    for (int i = 0; i < stops.Length; ++i)
                    {
                        var stop = grad.Stops[i];
                        stops[i] = new GradientStop() { Color = stop.Color, StopPercentage = stop.StopPercentage };
                    }
                }
                var gradientFill = new GradientFill() {
                    Type = grad.Type,
                    Stops = stops,
                    Mode = grad.Mode,
                    Opacity = grad.Opacity,
                    Addressing = grad.Addressing,
                    RadialFocus = grad.RadialFocus
                };
                gradientExInfo[gradientFill] = gradientExInfo[grad];
                f = gradientFill;
            }
            else if (fill is TextureFill)
            {
                var tex = fill as TextureFill;
                f = new TextureFill() {
                    Texture = tex.Texture,
                    Mode = tex.Mode,
                    Opacity = tex.Opacity,
                    Addressing = tex.Addressing
                };
            }
            else if (fill is PatternFill)
            {
                var pat = fill as PatternFill;
                f = new PatternFill() {
                    Mode = pat.Mode,
                    Opacity = pat.Opacity,
                    Pattern = CloneSceneNode(pat.Pattern),
                    Rect = pat.Rect
                };
            }
            return f;
        }

        private PathProperties ClonePathProps(PathProperties props)
        {
            Stroke stroke = null;
            if (props.Stroke != null)
            {
                float[] pattern = null;
                if (props.Stroke.Pattern != null)
                {
                    pattern = new float[props.Stroke.Pattern.Length];
                    for (int i = 0; i < pattern.Length; ++i)
                        pattern[i] = props.Stroke.Pattern[i];
                }
                stroke = new Stroke() {
                    Fill = CloneFill(props.Stroke.Fill),
                    FillTransform = props.Stroke.FillTransform,
                    HalfThickness = props.Stroke.HalfThickness,
                    Pattern = pattern,
                    PatternOffset = props.Stroke.PatternOffset,
                    TippedCornerLimit = props.Stroke.TippedCornerLimit
                };
            }

            return new PathProperties() {
                Stroke = stroke,
                Head = props.Head,
                Tail = props.Tail,
                Corners = props.Corners
            };
        }
        #endregion

        #region Utilities
        private GradientFill CloneGradientFill(GradientFill other)
        {
            if (other == null)
                return null;

            // This is a very fragile gradient fill cloning used since Illustrator
            // will sometimes refer to another fill using a "xlink:href" attribute.
            return new GradientFill() {
                Type = other.Type,
                Stops = other.Stops,
                Mode = other.Mode,
                Opacity = other.Opacity,
                Addressing = other.Addressing,
                RadialFocus = other.RadialFocus
            };
        }
        #endregion

        #region Simple Attribute Handling
        int AttribIntVal(string attribName) { return AttribIntVal(attribName, 0); }
        int AttribIntVal(string attribName, int defaultVal)
        {
            string val = styles.Evaluate(attribName);
            return (val != null) ? int.Parse(val) : defaultVal;
        }

        float AttribFloatVal(string attribName) { return AttribFloatVal(attribName, 0.0f); }
        float AttribFloatVal(string attribName, float defaultVal)
        {
            string val = styles.Evaluate(attribName);
            return (val != null) ? SVGAttribParser.ParseFloat(val) : defaultVal;
        }

        float AttribLengthVal(XmlReaderIterator.Node node, string attribName, DimType dimType) { return AttribLengthVal(node, attribName, 0.0f, dimType); }
        float AttribLengthVal(XmlReaderIterator.Node node, string attribName, float defaultUnitVal, DimType dimType)
        {
            var val = styles.Evaluate(attribName);
            return AttribLengthVal(val, node, attribName, defaultUnitVal, dimType);
        }

        float AttribLengthVal(string val, XmlReaderIterator.Node node, string attribName, float defaultUnitVal, DimType dimType)
        {
            // For reference: http://www.w3.org/TR/SVG/coords.html#Units
            if (val == null) return defaultUnitVal;
            val = val.Trim();
            string unitType = "px";
            char lastChar = val[val.Length - 1];
            if (lastChar == '%')
            {
                float number = SVGAttribParser.ParseFloat(val.Substring(0, val.Length - 1));
                if (number < 0)
                    throw node.GetException("Number in " + attribName + " cannot be negative");
                number /= 100.0f;

                // If there's an active viewbox, this should be used as the reference size for relative coordinates.
                // See https://www.w3.org/TR/SVG/coords.html#Units
                Vector2 vpSize = currentViewBoxSize.Count > 0 ? currentViewBoxSize.Peek() : currentContainerSize.Peek();

                switch (dimType)
                {
                    case DimType.Width: return number * vpSize.x;
                    case DimType.Height: return number * vpSize.y;
                    case DimType.Length: return (number * vpSize.magnitude / SVGLengthFactor); // See http://www.w3.org/TR/SVG/coords.html#Units
                }
            }
            else if (val.Length >= 2)
            {
                unitType = val.Substring(val.Length - 2);
            }

            if (char.IsDigit(lastChar) || (lastChar == '.'))
                return SVGAttribParser.ParseFloat(val); // No unit specified.. assume pixels (one px unit is defined to be equal to one user unit)

            float length = SVGAttribParser.ParseFloat(val.Substring(0, val.Length - 2));
            switch (unitType)
            {
                case "em": throw new NotImplementedException();
                case "ex": throw new NotImplementedException();
                case "px": return length;
                case "in": return 90.0f * length * dpiScale;       // "1in" equals "90px" (and therefore 90 user units)
                case "cm": return 35.43307f * length * dpiScale;   // "1cm" equals "35.43307px" (and therefore 35.43307 user units)
                case "mm": return 3.543307f * length * dpiScale;   // "1mm" would be "3.543307px" (3.543307 user units)
                case "pt": return 1.25f * length * dpiScale;       // "1pt" equals "1.25px" (and therefore 1.25 user units)
                case "pc": return 15.0f * length * dpiScale;       // "1pc" equals "15px" (and therefore 15 user units)
                default:
                    throw new FormatException("Unknown length unit type (" + unitType + ")");
            }
        }

        #endregion

        #region Attribute Set Handling
        void AddToSVGDictionaryIfPossible(XmlReaderIterator.Node node, object vectorElement)
        {
            string id = node["id"];
            if (!string.IsNullOrEmpty(id))
                svgObjects[id] = vectorElement;
        }

        Rect ParseViewport(XmlReaderIterator.Node node, SceneNode sceneNode, Vector2 defaultViewportSize)
        {
            scenePos.x = AttribLengthVal(node, "x", DimType.Width);
            scenePos.y = AttribLengthVal(node, "y", DimType.Height);
            sceneSize.x = AttribLengthVal(node, "width", defaultViewportSize.x, DimType.Width);
            sceneSize.y = AttribLengthVal(node, "height", defaultViewportSize.y, DimType.Height);

            // The size could be all 0, in which case we should ignore the viewport sizing logic altogether
            return new Rect(scenePos, sceneSize);
        }

        enum ViewBoxAlign { Min, Mid, Max }
        enum ViewBoxAspectRatio { DontPreserve, FitLargestDim, FitSmallestDim }
        struct ViewBoxInfo { public Rect ViewBox; public ViewBoxAspectRatio AspectRatio; public ViewBoxAlign AlignX, AlignY; public bool IsEmpty; }
        ViewBoxInfo ParseViewBox(XmlReaderIterator.Node node, SceneNode sceneNode, Rect sceneViewport)
        {
            var viewBoxInfo = new ViewBoxInfo() { IsEmpty = true };
            string viewBoxString = node["viewBox"];
            viewBoxString = viewBoxString != null ? viewBoxString.Trim() : null;
            if (string.IsNullOrEmpty(viewBoxString))
                return viewBoxInfo;

            var viewBoxValues = viewBoxString.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (viewBoxValues.Length != 4)
                throw node.GetException("Invalid viewBox specification");
            Vector2 viewBoxMin = new Vector2(
                    AttribLengthVal(viewBoxValues[0], node, "viewBox", 0.0f, DimType.Width),
                    AttribLengthVal(viewBoxValues[1], node, "viewBox", 0.0f, DimType.Height));
            Vector2 viewBoxSize = new Vector2(
                    AttribLengthVal(viewBoxValues[2], node, "viewBox", sceneViewport.width, DimType.Width),
                    AttribLengthVal(viewBoxValues[3], node, "viewBox", sceneViewport.height, DimType.Height));

            viewBoxInfo.ViewBox = new Rect(viewBoxMin, viewBoxSize);
            ParseViewBoxAspectRatio(node, ref viewBoxInfo);

            viewBoxInfo.IsEmpty = false;
            return viewBoxInfo;
        }

        void ParseViewBoxAspectRatio(XmlReaderIterator.Node node, ref ViewBoxInfo viewBoxInfo)
        {
            viewBoxInfo.AspectRatio = ViewBoxAspectRatio.FitLargestDim;
            viewBoxInfo.AlignX = ViewBoxAlign.Mid;
            viewBoxInfo.AlignY = ViewBoxAlign.Mid;

            string preserveAspectRatioString = node["preserveAspectRatio"];
            preserveAspectRatioString = preserveAspectRatioString != null ? preserveAspectRatioString.Trim() : null;
            bool wantNone = false;
            if (!string.IsNullOrEmpty(preserveAspectRatioString))
            {
                var preserveAspectRatioValues = preserveAspectRatioString.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var value in preserveAspectRatioValues)
                {
                    switch (value)
                    {
                        case "defer": break; // This is only meaningful on <image> that references another SVG, we don't support that
                        case "none": wantNone = true; break;
                        case "xMinYMin": viewBoxInfo.AlignX = ViewBoxAlign.Min; viewBoxInfo.AlignY = ViewBoxAlign.Min; break;
                        case "xMidYMin": viewBoxInfo.AlignX = ViewBoxAlign.Mid; viewBoxInfo.AlignY = ViewBoxAlign.Min; break;
                        case "xMaxYMin": viewBoxInfo.AlignX = ViewBoxAlign.Max; viewBoxInfo.AlignY = ViewBoxAlign.Min; break;
                        case "xMinYMid": viewBoxInfo.AlignX = ViewBoxAlign.Min; viewBoxInfo.AlignY = ViewBoxAlign.Mid; break;
                        case "xMidYMid": viewBoxInfo.AlignX = ViewBoxAlign.Mid; viewBoxInfo.AlignY = ViewBoxAlign.Mid; break;
                        case "xMaxYMid": viewBoxInfo.AlignX = ViewBoxAlign.Max; viewBoxInfo.AlignY = ViewBoxAlign.Mid; break;
                        case "xMinYMax": viewBoxInfo.AlignX = ViewBoxAlign.Min; viewBoxInfo.AlignY = ViewBoxAlign.Max; break;
                        case "xMidYMax": viewBoxInfo.AlignX = ViewBoxAlign.Mid; viewBoxInfo.AlignY = ViewBoxAlign.Max; break;
                        case "xMaxYMax": viewBoxInfo.AlignX = ViewBoxAlign.Max; viewBoxInfo.AlignY = ViewBoxAlign.Max; break;
                        case "meet": viewBoxInfo.AspectRatio = ViewBoxAspectRatio.FitLargestDim; break;
                        case "slice": viewBoxInfo.AspectRatio = ViewBoxAspectRatio.FitSmallestDim; break;
                    }
                }
            }

            if (wantNone) // Override aspect ratio no matter what other modes are chosen (meet/slice)
                viewBoxInfo.AspectRatio = ViewBoxAspectRatio.DontPreserve;
        }

        void ApplyViewBox(SceneNode sceneNode, ViewBoxInfo viewBoxInfo, Rect sceneViewport)
        {
            if ((viewBoxInfo.ViewBox.size == Vector2.zero) || (sceneViewport.size == Vector2.zero))
                return;

            Vector2 scale = Vector2.one, offset = -viewBoxInfo.ViewBox.position;
            if (viewBoxInfo.AspectRatio == ViewBoxAspectRatio.DontPreserve)
            {
                scale = sceneViewport.size / viewBoxInfo.ViewBox.size;
            }
            else
            {
                scale.x = scale.y = sceneViewport.width / viewBoxInfo.ViewBox.width;
                bool fitsOnWidth;
                if (viewBoxInfo.AspectRatio == ViewBoxAspectRatio.FitLargestDim)
                    fitsOnWidth = viewBoxInfo.ViewBox.height * scale.y <= sceneViewport.height;
                else fitsOnWidth = viewBoxInfo.ViewBox.height * scale.y > sceneViewport.height;

                Vector2 alignOffset = Vector2.zero;
                if (fitsOnWidth)
                {
                    // We fit on the width, so apply the vertical alignment rules
                    if (viewBoxInfo.AlignY == ViewBoxAlign.Mid)
                        alignOffset.y = (sceneViewport.height - viewBoxInfo.ViewBox.height * scale.y) * 0.5f;
                    else if (viewBoxInfo.AlignY == ViewBoxAlign.Max)
                        alignOffset.y = sceneViewport.height - viewBoxInfo.ViewBox.height * scale.y;
                }
                else
                {
                    // We didn't fit on width, meaning we should fit on height and use the wiggle room on width
                    scale.x = scale.y = sceneViewport.height / viewBoxInfo.ViewBox.height;

                    // Apply the horizontal alignment rules
                    if (viewBoxInfo.AlignX == ViewBoxAlign.Mid)
                        alignOffset.x = (sceneViewport.width - viewBoxInfo.ViewBox.width * scale.x) * 0.5f;
                    else if (viewBoxInfo.AlignX == ViewBoxAlign.Max)
                        alignOffset.x = sceneViewport.width - viewBoxInfo.ViewBox.width * scale.x;
                }

                offset += alignOffset / scale;
            }

            // Aaaaand finally, the transform
            sceneNode.Transform = sceneNode.Transform * Matrix2D.Scale(scale) * Matrix2D.Translate(offset);
        }

        Stroke ParseStrokeAttributeSet(XmlReaderIterator.Node node, out PathCorner strokeCorner, out PathEnding strokeEnding, Inheritance inheritance = Inheritance.Inherited)
        {
            var stroke = SVGAttribParser.ParseStrokeAndOpacity(node, svgObjects, styles, inheritance);
            strokeCorner = PathCorner.Tipped;
            strokeEnding = PathEnding.Chop;
            if (stroke != null)
            {
                string strokeWidth = styles.Evaluate("stroke-width", inheritance);
                stroke.HalfThickness = AttribLengthVal(strokeWidth, node, "stroke-width", 1.0f, DimType.Length) * 0.5f;
                switch (styles.Evaluate("stroke-linecap", inheritance))
                {
                    case "butt": strokeEnding = PathEnding.Chop; break;
                    case "square": strokeEnding = PathEnding.Square; break;
                    case "round": strokeEnding = PathEnding.Round; break;
                }
                switch (styles.Evaluate("stroke-linejoin", inheritance))
                {
                    case "miter": strokeCorner = PathCorner.Tipped; break;
                    case "round": strokeCorner = PathCorner.Round; break;
                    case "bevel": strokeCorner = PathCorner.Beveled; break;
                }

                string pattern = styles.Evaluate("stroke-dasharray", inheritance);
                if (pattern != null && pattern != "none")
                {
                    string[] entries = pattern.Split(whiteSpaceNumberChars, StringSplitOptions.RemoveEmptyEntries);
                    // If the pattern is odd, then we duplicate it to make it even as per the spec
                    int totalCount = (entries.Length & 1) == 1 ? entries.Length * 2 : entries.Length;
                    stroke.Pattern = new float[totalCount];
                    for (int i = 0; i < entries.Length; i++)
                        stroke.Pattern[i] = AttribLengthVal(entries[i], node, "stroke-dasharray", 0.0f, DimType.Length);

                    // Duplicate the pattern
                    if (totalCount > entries.Length)
                    {
                        for (int i = 0; i < entries.Length; i++)
                            stroke.Pattern[i + entries.Length] = stroke.Pattern[i];
                    }

                    var dashOffset = styles.Evaluate("stroke-dashoffset", inheritance);
                    stroke.PatternOffset = AttribLengthVal(dashOffset, node, "stroke-dashoffset", 0.0f, DimType.Length);
                }

                var strokeMiterLimit = styles.Evaluate("stroke-miterlimit", inheritance);
                stroke.TippedCornerLimit = AttribLengthVal(strokeMiterLimit, node, "stroke-miterlimit", 4.0f, DimType.Length);
                if (stroke.TippedCornerLimit < 1.0f)
                    throw node.GetException("'stroke-miterlimit' should be greater or equal to 1");
            } // If stroke is specified
            return stroke;
        }

        void ParseID(XmlReaderIterator.Node node, SceneNode sceneNode)
        {
            string id = node["id"];
            if (!string.IsNullOrEmpty(id))
            {
                nodeIDs[id] = sceneNode;

                // Store the style layer of this node since it can be referenced later by a <use> tag
                nodeStyleLayers[sceneNode] = styles.PeekLayer();
            }
        }

        float ParseOpacity(SceneNode sceneNode)
        {
            float opacity = AttribFloatVal("opacity", 1.0f);
            if (opacity != 1.0f && sceneNode != null)
                nodeOpacity[sceneNode] = opacity;
            return opacity;
        }

        void ParseClipAndMask(XmlReaderIterator.Node node, SceneNode sceneNode)
        {
            ParseClip(node, sceneNode);
            ParseMask(node, sceneNode);
        }

        void ParseClip(XmlReaderIterator.Node node, SceneNode sceneNode)
        {
            string reference = null;
            string clipPath = styles.Evaluate("clip-path");
            if (clipPath != null)
                reference = SVGAttribParser.ParseURLRef(clipPath);

            if (reference == null)
                return;

            var clipper = SVGAttribParser.ParseRelativeRef(reference, svgObjects) as SceneNode;
            if (clipper == null && reference.Length > 1 && reference.StartsWith("#"))
            {
                // Clipper may be defined later in the file
                List<PostponedClip> clips;
                if (!postponedClip.TryGetValue(reference, out clips))
                    clips = new List<PostponedClip>(1);
                clips.Add(new PostponedClip() { node = sceneNode });
                postponedClip[reference.Substring(1)] = clips;
                return;
            }
            var clipperRoot = clipper;

            bool worldRelative = true;
            ClipData data;
            if (clipData.TryGetValue(clipper, out data))
                worldRelative = data.WorldRelative;

            ApplyClipper(clipper, sceneNode, worldRelative);
        }

        void ApplyClipper(SceneNode clipper, SceneNode target, bool worldRelative)
        {
            SceneNode clipperRoot = clipper;
            if (!worldRelative)
            {
                // If the referenced clip path units is in bounding-box space, we add an intermediate
                // node to scale the content to the correct size.
                var rect = VectorUtils.SceneNodeBounds(target);
                var transform = Matrix2D.Translate(rect.position) * Matrix2D.Scale(rect.size);

                clipperRoot = new SceneNode() {
                    Children = new List<SceneNode> { clipper },
                    Transform = transform
                };
            }
            target.Clipper = clipperRoot;
        }

        void ParseMask(XmlReaderIterator.Node node, SceneNode sceneNode)
        {
            string reference = null;
            string maskRef = node["mask"];
            if (maskRef != null)
                reference = SVGAttribParser.ParseURLRef(maskRef);

            if (reference == null)
                return;

            var maskPath = SVGAttribParser.ParseRelativeRef(reference, svgObjects) as SceneNode;
            var maskRoot = maskPath;

            MaskData data;
            if (maskData.TryGetValue(maskPath, out data) && !data.ContentWorldRelative)
            {
                // If the referenced mask units is in bounding-box space, we add an intermediate
                // node to scale the content to the correct size.
                var rect = VectorUtils.SceneNodeBounds(sceneNode);
                var transform = Matrix2D.Translate(rect.position) * Matrix2D.Scale(rect.size);

                maskRoot = new SceneNode() {
                    Children = new List<SceneNode> { maskPath },
                    Transform = transform
                };
            }

            sceneNode.Clipper = maskRoot;
        }

        #endregion

        #region Textures
        Texture2D DecodeTextureData(string dataURI)
        {
            int pos = 5; // Skip "data:"
            int length = dataURI.Length;

            int startPos = pos;
            while (pos < length && dataURI[pos] != ';' && dataURI[pos] != ',')
                ++pos;

            var mediaType = dataURI.Substring(startPos, pos-startPos).ToLower();
            if (mediaType != "image/png" && mediaType != "image/jpeg")
                return null;

            while (pos < length && dataURI[pos] != ',')
                ++pos;

            ++pos; // Skip ','

            if (pos >= length)
                return null;

            var data = Convert.FromBase64String(dataURI.Substring(pos));

            var tex = new Texture2D(1, 1);
            if (tex.LoadImage(data))
                return tex;

            return null;
        }
        #endregion

        #region Post-processing

        void PostProcess(SceneNode root)
        {
            AdjustFills(root);
        }

        struct HierarchyUpdate
        {
            public SceneNode Parent;
            public SceneNode NewNode;
            public SceneNode ReplaceNode;
        }

        void AdjustFills(SceneNode root)
        {
            var hierarchyUpdates = new List<HierarchyUpdate>();

            // Adjust fills on all objects
            foreach (var nodeInfo in VectorUtils.WorldTransformedSceneNodes(root, nodeOpacity))
            {
                if (nodeInfo.Node.Shapes == null)
                    continue;
                foreach (var shape in nodeInfo.Node.Shapes)
                {
                    if (shape.Fill != null)
                    {
                        // This fill may be a placeholder for postponed reference, try to resolve it here.
                        string reference;
                        if (postponedFills.TryGetValue(shape.Fill, out reference))
                        {
                            var fill = SVGAttribParser.ParseRelativeRef(reference, svgObjects) as IFill;
                            if (fill != null)
                                shape.Fill = fill;
                        }
                    }

                    var stroke = shape.PathProps.Stroke;
                    if (stroke != null && stroke.Fill is GradientFill)
                    {
                        var fillTransform = Matrix2D.identity;
                        AdjustGradientFill(nodeInfo.Node, nodeInfo.WorldTransform, stroke.Fill, shape.Contours, ref fillTransform);
                        stroke.FillTransform = fillTransform;
                    }

                    if (shape.Fill is GradientFill)
                    {
                        var fillTransform = Matrix2D.identity;
                        AdjustGradientFill(nodeInfo.Node, nodeInfo.WorldTransform, shape.Fill, shape.Contours, ref fillTransform);
                        shape.FillTransform = fillTransform;
                    }
                    else if (shape.Fill is PatternFill)
                    {
                        var fillNode = AdjustPatternFill(nodeInfo.Node, nodeInfo.WorldTransform, shape);
                        if (fillNode != null)
                        {
                            hierarchyUpdates.Add(new HierarchyUpdate()
                            {
                                Parent = nodeInfo.Parent,
                                NewNode = fillNode,
                                ReplaceNode = nodeInfo.Node
                            });
                        }
                    }
                }
            }

            foreach (var update in hierarchyUpdates)
            {
                var index = update.Parent.Children.IndexOf(update.ReplaceNode);
                update.Parent.Children.RemoveAt(index);
                update.Parent.Children.Insert(index, update.NewNode);
            }
        }

        void AdjustGradientFill(SceneNode node, Matrix2D worldTransform, IFill fill, BezierContour[] contours, ref Matrix2D computedTransform)
        {
            var gradientFill = fill as GradientFill;
            if (fill == null || contours == null || contours.Length == 0)
                return;

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(-float.MaxValue, -float.MaxValue);
            foreach (var contour in contours)
            {
                var bbox = VectorUtils.Bounds(contour.Segments);
                min = Vector2.Min(min, bbox.min);
                max = Vector2.Max(max, bbox.max);
            }

            Rect bounds = new Rect(min, max - min);

            GradientExData extInfo = (GradientExData)gradientExInfo[gradientFill];
            var containerSize = nodeGlobalSceneState[node].ContainerSize;
            Matrix2D gradTransform = Matrix2D.identity;

            currentContainerSize.Push(extInfo.WorldRelative ? containerSize : Vector2.one);

            // If the fill is object relative, then the dimensions will come to us in
            // a normalized space, we must adjust those to the object's dimensions
            if (extInfo is LinearGradientExData)
            {
                // In SVG, linear gradients are expressed using two vectors. A vector and normal. The vector determines
                // the direction where the gradient increases. The normal determines the slant of the gradient along the vector.
                // Due to transformations, it is possible that those two vectors (the gradient vector and its normal) are not
                // actually perpendicular. That's why a skew transformation is involved here.
                // VectorScene just maps linear gradients from 0 to 1 across the entire bounding box width, so we
                // need to figure out a super transformation that takes those simply-mapped UVs and have them express
                // the linear gradient with its slant and all the fun involved.
                var linGradEx = (LinearGradientExData)extInfo;
                Vector2 lineStart = new Vector2(
                        AttribLengthVal(linGradEx.X1, null, null, 0.0f, DimType.Width),
                        AttribLengthVal(linGradEx.Y1, null, null, 0.0f, DimType.Height));
                Vector2 lineEnd = new Vector2(
                        AttribLengthVal(linGradEx.X2, null, null, currentContainerSize.Peek().x, DimType.Width),
                        AttribLengthVal(linGradEx.Y2, null, null, 0.0f, DimType.Height));

                var gradientVector = lineEnd - lineStart;
                float gradientVectorInvLength = 1.0f / gradientVector.magnitude;
                var scale = Matrix2D.Scale(new Vector2(bounds.width * gradientVectorInvLength, bounds.height * gradientVectorInvLength));
                var rotation = Matrix2D.RotateLH(Mathf.Atan2(gradientVector.y, gradientVector.x));
                var offset = Matrix2D.Translate(-lineStart);
                gradTransform = scale * rotation * offset;
            }
            else if (extInfo is RadialGradientExData)
            {
                // VectorScene positions radial gradiants at the center of the bbox, and picks the radii (not one radius, but two)
                // to fill the space between the center and the two edges (horizontal and vertical). So in the general case
                // the radial is actually an ellipsoid. So we need to do an SRT transformation to position the radial gradient according
                // to the SVG center point and radius
                var radGradEx = (RadialGradientExData)extInfo;
                Vector2 halfCurrentContainerSize = currentContainerSize.Peek() * 0.5f;
                Vector2 center = new Vector2(
                        AttribLengthVal(radGradEx.Cx, null, null, halfCurrentContainerSize.x, DimType.Width),
                        AttribLengthVal(radGradEx.Cy, null, null, halfCurrentContainerSize.y, DimType.Height));
                Vector2 focus = new Vector2(
                        AttribLengthVal(radGradEx.Fx, null, null, center.x, DimType.Width),
                        AttribLengthVal(radGradEx.Fy, null, null, center.y, DimType.Height));
                float radius = AttribLengthVal(radGradEx.R, null, null, halfCurrentContainerSize.magnitude / SVGLengthFactor, DimType.Length);

                // This block below tells that radial focus cannot change per object, but is realized correctly for the first object
                // that requests this gradient. If the gradient is using object-relative coordinates to specify the focus location,
                // then only the first object will look correct, and the rest will potentially not look right. The alternative is
                // to detect if it is necessary and generate a new atlas entry for it
                if (!radGradEx.Parsed)
                {
                    // VectorGradientFill radialFocus is (-1,1) relative to the outer circle
                    gradientFill.RadialFocus = (focus - center) / radius;
                    if (gradientFill.RadialFocus.sqrMagnitude > 1.0f - VectorUtils.Epsilon)
                        gradientFill.RadialFocus = gradientFill.RadialFocus.normalized * (1.0f - VectorUtils.Epsilon); // Stick within the unit circle

                    radGradEx.Parsed = true;
                }

                gradTransform =
                    Matrix2D.Scale(bounds.size * 0.5f / radius) *
                    Matrix2D.Translate(new Vector2(radius, radius) - center);
            }
            else
            {
                Debug.LogError("Unsupported gradient type: " + extInfo);
            }

            currentContainerSize.Pop();

            var uvToWorld = extInfo.WorldRelative ? Matrix2D.Translate(bounds.min) * Matrix2D.Scale(bounds.size) : Matrix2D.identity;
            var boundsInv = new Vector2(1.0f / bounds.width, 1.0f / bounds.height);
            computedTransform = Matrix2D.Scale(boundsInv) * gradTransform * extInfo.FillTransform.Inverse() * uvToWorld;
        }

        SceneNode AdjustPatternFill(SceneNode node, Matrix2D worldTransform, Shape shape)
        {
            PatternFill patternFill = shape.Fill as PatternFill;
            if (patternFill == null ||
                Mathf.Abs(patternFill.Rect.width) < VectorUtils.Epsilon ||
                Mathf.Abs(patternFill.Rect.height) < VectorUtils.Epsilon)
            {
                return null;
            }
            
            var data = patternData[patternFill.Pattern];

            var nodeBounds = VectorUtils.SceneNodeBounds(node);
            var patternRect = patternFill.Rect;
            if (!data.WorldRelative)
            {
                patternRect.position *= nodeBounds.size;
                patternRect.size *= nodeBounds.size;
            }

            // The pattern fill will create a new clipped node containing the repeating pattern
            // as well as a sibling containing the original node. This will replace the original node.
            var replacementNode = new SceneNode() {
                Transform = node.Transform,
                Children = new List<SceneNode>(2)
            };
            node.Transform = Matrix2D.identity;

            // The pattern node will be wrapped in a scaling node if content isn't world relative
            var patternNode = patternFill.Pattern;
            if (!data.ContentWorldRelative)
            {
                patternNode = new SceneNode() {
                    Transform = Matrix2D.Scale(nodeBounds.size),
                    Children = new List<SceneNode> { patternFill.Pattern }
                };
            }

            PostProcess(patternNode); // This will take care of adjusting gradients/inner-patterns

            // Duplicate the filling pattern
            var grid = new SceneNode() {
                Transform = data.PatternTransform,
                Children = new List<SceneNode>(20)
            };

            var fill = new SceneNode() {
                Transform = Matrix2D.identity,
                Children = new List<SceneNode> { grid },
                Clipper = node
            };

            // SVG patterns are clipped in their respective "boxes"
            var clippingBox = new Shape();
            VectorUtils.MakeRectangleShape(clippingBox,  new Rect(0,0,patternRect.width, patternRect.height));

            var box = new SceneNode() {
                Transform = Matrix2D.identity,
                Shapes = new List<Shape> { clippingBox }
            };

            // Compute the bounds of the shape to be filled, taking into account the pattern transform
            var bounds = VectorUtils.SceneNodeBounds(node);
            var invPatternTransform = data.PatternTransform.Inverse();
            var boundVerts = new Vector2[] {
                invPatternTransform * new Vector2(bounds.xMin, bounds.yMin),
                invPatternTransform * new Vector2(bounds.xMax, bounds.yMin),
                invPatternTransform * new Vector2(bounds.xMax, bounds.yMax),
                invPatternTransform * new Vector2(bounds.xMin, bounds.yMax)
            };
            bounds = VectorUtils.Bounds(boundVerts);

            const int kMaxReps = 5000;
            float xCount = bounds.xMax / patternRect.width;
            float yCount = bounds.yMax / patternRect.height;
            if (Mathf.Abs(patternRect.width) < VectorUtils.Epsilon ||
                Mathf.Abs(patternRect.height) < VectorUtils.Epsilon ||
                (xCount*yCount) > kMaxReps)
            {
                Debug.LogWarning("Ignoring pattern which would result in too many repetitions");
                return null;
            }

            // Start the pattern filling process
            var offset = patternRect.position;
            float xStart = (int)(bounds.x / patternRect.width) * patternRect.width - patternRect.width;
            float yStart = (int)(bounds.y / patternRect.height) * patternRect.height - patternRect.height;

            for (float y = yStart; y < bounds.yMax; y += patternRect.height)
            {
                for (float x = xStart; x < bounds.xMax; x += patternRect.width)
                {
                    var pattern = new SceneNode() {
                        Transform = Matrix2D.Translate(new Vector2(x, y) + offset),
                        Children = new List<SceneNode> { patternNode },
                        Clipper = box
                    };
                    grid.Children.Add(pattern);
                }
            }

            replacementNode.Children.Add(fill);
            replacementNode.Children.Add(node);

            return replacementNode;
        }

        void RemoveInvisibleNodes()
        {
            foreach (var n in invisibleNodes)
            {
                if (n.parent.Children != null)
                    n.parent.Children.Remove(n.node);
            }
        }

        #endregion

        delegate void ElemHandler();
        class Handlers : Dictionary<string, ElemHandler>
        {
            public Handlers(int capacity) : base(capacity) {}
        }
        bool ShouldDeclareSupportedChildren(XmlReaderIterator.Node node) { return !subTags.ContainsKey(node.Name); }
        void SupportElems(XmlReaderIterator.Node node, params ElemHandler[] handlers)
        {
            var elems = new Handlers(handlers.Length);
            foreach (var h in handlers)
                elems[h.Method.Name] = h;
            subTags[node.Name] = elems;
        }

        static char[] whiteSpaceNumberChars = " \r\n\t,".ToCharArray();
        enum DimType { Width, Height, Length };
        XmlReaderIterator docReader;
        Scene scene;
        float dpiScale;
        int windowWidth, windowHeight;
        Vector2 scenePos, sceneSize;
        SVGDictionary svgObjects = new SVGDictionary(); // Named elements are looked up in this
        Dictionary<string, Handlers> subTags = new Dictionary<string, Handlers>(); // For each element, the set of elements supported as its children
        Dictionary<GradientFill, GradientExData> gradientExInfo = new Dictionary<GradientFill, GradientExData>();
        Dictionary<SceneNode, ViewBoxInfo> symbolViewBoxes = new Dictionary<SceneNode, ViewBoxInfo>();
        Dictionary<SceneNode, NodeGlobalSceneState> nodeGlobalSceneState = new Dictionary<SceneNode, NodeGlobalSceneState>();
        Dictionary<SceneNode, float> nodeOpacity = new Dictionary<SceneNode, float>();
        Dictionary<string, SceneNode> nodeIDs = new Dictionary<string, SceneNode>();
        Dictionary<SceneNode, SVGStyleResolver.StyleLayer> nodeStyleLayers = new Dictionary<SceneNode, SVGStyleResolver.StyleLayer>();
        Dictionary<SceneNode, ClipData> clipData = new Dictionary<SceneNode, ClipData>();
        Dictionary<SceneNode, PatternData> patternData = new Dictionary<SceneNode, PatternData>();
        Dictionary<SceneNode, MaskData> maskData = new Dictionary<SceneNode, MaskData>();
        Dictionary<string, List<NodeReferenceData>> postponedSymbolData = new Dictionary<string, List<NodeReferenceData>>();
        Dictionary<string, List<PostponedStopData>> postponedStopData = new Dictionary<string, List<PostponedStopData>>();
        Dictionary<string, List<PostponedClip>> postponedClip = new Dictionary<string, List<PostponedClip>>();
        SVGPostponedFills postponedFills = new SVGPostponedFills();
        List<NodeWithParent> invisibleNodes = new List<NodeWithParent>();
        Stack<Vector2> currentContainerSize = new Stack<Vector2>();
        Stack<Vector2> currentViewBoxSize = new Stack<Vector2>();
        Stack<SceneNode> currentSceneNode = new Stack<SceneNode>();
        GradientFill currentGradientFill;
        string currentGradientId;
        string currentGradientLink;
        ElemHandler[] allElems;
        HashSet<ElemHandler> elemsToAddToHierarchy;
        SVGStyleResolver styles = new SVGStyleResolver();
        bool applyRootViewBox;

        internal Rect sceneViewport;

        struct NodeGlobalSceneState
        {
            public Vector2 ContainerSize;
        }

        class GradientExData
        {
            public bool WorldRelative;
            public Matrix2D FillTransform;
        }

        class LinearGradientExData : GradientExData
        {
            public string X1, Y1, X2, Y2;
        }

        class RadialGradientExData : GradientExData
        {
            public bool Parsed;
            public string Cx, Cy, Fx, Fy, R;
        }

        struct ClipData
        {
            public bool WorldRelative;
        }

        struct PatternData
        {
            public bool WorldRelative;
            public bool ContentWorldRelative;
            public Matrix2D PatternTransform;
        }

        struct MaskData
        {
            public bool WorldRelative;
            public bool ContentWorldRelative;
        }

        struct NodeWithParent
        {
            public SceneNode node;
            public SceneNode parent;
        }

        struct NodeReferenceData
        {
            public SceneNode node;
            public Rect viewport;
            public string id;
        }

        struct PostponedStopData
        {
            public GradientFill fill;
        }

        struct PostponedClip
        {
            public SceneNode node;
        }
    }

    internal enum Inheritance
    {
        None,
        Inherited
    }

    internal class SVGStyleResolver
    {
        public void PushNode(XmlReaderIterator.Node node)
        {
            var nodeData = new NodeData();
            nodeData.node = node;
            nodeData.name = node.Name;
            var klass = node["class"];
            if (klass != null)
            {
                nodeData.classes = new List<string>();
                foreach (var c in klass.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = c.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        nodeData.classes.Add(trimmed);
                }
            }
            else
                nodeData.classes = new List<string>();


            // nodeData.classes = SortedClasses(nodeData.classes).ToList();

            var sortedClasses = new List<string>();
            foreach (var c in SortedClasses(nodeData.classes))
                sortedClasses.Add(c);

            nodeData.classes = sortedClasses;
            nodeData.id = node["id"];

            var layer = new StyleLayer();
            layer.nodeData = nodeData;
            layer.attributeSheet = node.GetAttributes();
            layer.styleSheet = new SVGStyleSheet();

            var cssText = node["style"];
            if (cssText != null)
            {
                var props = SVGStyleSheetUtils.ParseInline(cssText);
                layer.styleSheet[node.Name] = props;
            }

            PushLayer(layer);
        }

        public void PopNode()
        {
            PopLayer();
        }

        public void PushLayer(StyleLayer layer)
        {
            layers.Add(layer);
        }

        public void PopLayer()
        {
            if (layers.Count == 0)
                throw SVGFormatException.StackError;

            layers.RemoveAt(layers.Count - 1);
        }

        public StyleLayer PeekLayer()
        {
            if (layers.Count == 0)
                return null;
            return layers[layers.Count-1];
        }

        public void SaveLayerForSceneNode(SceneNode node)
        {
            nodeLayers[node] = PeekLayer();
        }

        public StyleLayer GetLayerForScenNode(SceneNode node)
        {
            if (!nodeLayers.ContainsKey(node))
                return null;
            return nodeLayers[node];
        }

        public void SetGlobalStyleSheet(SVGStyleSheet sheet)
        {
            foreach (var sel in sheet.selectors)
                globalStyleSheet[sel] = sheet[sel];
        }

        public string Evaluate(string attribName, Inheritance inheritance = Inheritance.None)
        {
            for (int i = layers.Count-1; i >= 0; --i)
            {
                string attrib = null;
                if (LookupStyleOrAttribute(layers[i], attribName, inheritance, out attrib))
                    return attrib;
                
                if (inheritance == Inheritance.None)
                    break;
            }
            return null;
        }

        private bool LookupStyleOrAttribute(StyleLayer layer, string attribName, Inheritance inheritance, out string attrib)
        {
            // Try to match a CSS style first
            if (LookupProperty(layer.nodeData, attribName, layer.styleSheet, out attrib))
                return true;

            // Try to match a global CSS style
            if (LookupProperty(layer.nodeData, attribName, globalStyleSheet, out attrib))
                return true;

            // Else, fallback on attribute
            if (layer.attributeSheet.ContainsKey(attribName))
            {
                attrib = layer.attributeSheet[attribName];
                return true;
            }

            return false;
        }

        private bool LookupProperty(NodeData nodeData, string attribName, SVGStyleSheet sheet, out string val)
        {
            var id = string.IsNullOrEmpty(nodeData.id) ? null : "#" + nodeData.id;
            var name = string.IsNullOrEmpty(nodeData.name) ? null : nodeData.name;

            if (LookupPropertyInSheet(sheet, attribName, id, out val))
                return true;

            foreach (var c in nodeData.classes)
            {
                var klass = "." + c;
                if (LookupPropertyInSheet(sheet, attribName, klass, out val))
                    return true;
            }

            if (LookupPropertyInSheet(sheet, attribName, name, out val))
                return true;

            if (LookupPropertyInSheet(sheet, attribName, "*", out val))
                return true;

            val = null;
            return false;
        }

        private bool LookupPropertyInSheet(SVGStyleSheet sheet, string attribName, string selector, out string val)
        {
            if (selector == null)
            {
                val = null;
                return false;
            }

            string matchingSelector = "";

            foreach (var s in sheet.selectors)
            {
                bool partMatches = false;

                var selectorParts = s.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in selectorParts)
                {
                    if (part == selector)
                    {
                        partMatches = true;
                        break;
                    }
                }

                if (partMatches)
                {
                    if (selectorParts.Length == 1)
                    {
                        matchingSelector = selectorParts[0];
                        break;
                    }
                    else if (selectorParts.Length > 1)
                    {
                        if (MatchesDescendants(selectorParts, selectorParts.Length - 1))
                        {
                            matchingSelector = s;
                            break;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(matchingSelector))
            {
                var props = sheet[matchingSelector];
                if (props.ContainsKey(attribName))
                {
                    val = props[attribName];
                    return true;
                }
            }

            val = null;
            return false;
        }

        bool MatchesDescendants(string[] selectorParts, int partIndexToMatch, int layerIndex = -1)
        {
            if (selectorParts.Length == 0)
                return false;

            if (partIndexToMatch < 0)
                return true; // All parts matched

            if (layerIndex < 0)
                layerIndex = layers.Count - 1;

            var partToMatch = selectorParts[partIndexToMatch];
            for (int i = layerIndex; i >= 0; --i)
            {
                var layer = layers[i];
                var nodeData = layer.nodeData;
                bool matchesName = (partToMatch == nodeData.name);
                bool matchesID = (partToMatch == ("#" + nodeData.id));
                bool matchesClass = (nodeData.classes != null && nodeData.classes.Contains(partToMatch.StartsWith(".") ? partToMatch.Substring(1) : partToMatch));
                if (matchesName || matchesID || matchesClass)
                    return MatchesDescendants(selectorParts, partIndexToMatch - 1, i - 1);
            }
            return false;
        }

        private IEnumerable<string> SortedClasses(List<string> classes)
        {
            // We may not have parsed the global sheets yet (happens when setting a class on the root <svg> element).
            int selectorCount = 0;
            foreach (var s in globalStyleSheet.selectors)
                ++selectorCount;

            if (selectorCount == 0)
            {
                foreach (var klass in classes)
                    yield return klass;
            }

            // We match classes in reverse order of their appearance. This isn't conformant to CSS selectors priority,
            // but this works well enough for auto-generated CSS styles.
            var reversedSelectors = new List<string>(globalStyleSheet.selectors);
            reversedSelectors.Reverse();

            foreach (var sel in reversedSelectors)
            {
                var parts = sel.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (part[0] != '.')
                        continue;
                    var klass = part.Substring(1);
                    if (classes.Contains(klass))
                        yield return klass;
                }
            }
        }

        public struct NodeData
        {
            public XmlReaderIterator.Node node;
            public string name;
            public List<string> classes;
            public string id;
        }

        public class StyleLayer
        {
            public SVGStyleSheet styleSheet;
            public SVGPropertySheet attributeSheet;
            public NodeData nodeData;
        }

        private List<StyleLayer> layers = new List<StyleLayer>();
        private SVGStyleSheet globalStyleSheet = new SVGStyleSheet();
        private Dictionary<SceneNode, StyleLayer> nodeLayers = new Dictionary<SceneNode, StyleLayer>();
    }

    internal class SVGAttribParser
    {
        public static List<BezierContour> ParsePath(XmlReaderIterator.Node node)
        {
            string path = node["d"];
            if (string.IsNullOrEmpty(path))
                return null;

            try
            {
                return (new SVGAttribParser(path, AttribPath.Path)).contours;
            }
            catch (Exception e)
            {
                throw node.GetException(e.Message);
            }
        }

        public static Matrix2D ParseTransform(XmlReaderIterator.Node node)
        {
            return ParseTransform(node, "transform");
        }

        public static Matrix2D ParseTransform(XmlReaderIterator.Node node, string attribName)
        {
            // Transforms aren't part of styling and shouldn't be evaluated,
            // they have to be specified as node attributes
            string transform = node[attribName];
            if (string.IsNullOrEmpty(transform))
                return Matrix2D.identity;
            try
            {
                return (new SVGAttribParser(transform, attribName, AttribTransform.Transform)).transform;
            }
            catch (Exception e)
            {
                throw node.GetException(e.Message);
            }
        }

        public static IFill ParseFill(XmlReaderIterator.Node node, SVGDictionary dict, SVGPostponedFills postponedFills, SVGStyleResolver styles, Inheritance inheritance = Inheritance.Inherited)
        {
            bool isDefaultFill;
            return ParseFill(node, dict, postponedFills, styles, inheritance, out isDefaultFill);
        }

        public static IFill ParseFill(XmlReaderIterator.Node node, SVGDictionary dict, SVGPostponedFills postponedFills, SVGStyleResolver styles, Inheritance inheritance, out bool isDefaultFill)
        {
            string opacityAttrib = styles.Evaluate("fill-opacity", inheritance);
            float opacity = (opacityAttrib != null) ? ParseFloat(opacityAttrib) : 1.0f;
            string fillMode = styles.Evaluate("fill-rule", inheritance);
            FillMode mode = FillMode.NonZero;
            if (fillMode != null)
            {
                if (fillMode == "nonzero")
                    mode = FillMode.NonZero;
                else if (fillMode == "evenodd")
                    mode = FillMode.OddEven;
                else throw new Exception("Unknown fill-rule: " + fillMode);
            }

            try
            {
                var fill = styles.Evaluate("fill", inheritance);
                isDefaultFill = (fill == null && opacityAttrib == null);
                return (new SVGAttribParser(fill, "fill", opacity, mode, dict, postponedFills)).fill;
            }
            catch (Exception e)
            {
                throw node.GetException(e.Message);
            }
        }

        public static Stroke ParseStrokeAndOpacity(XmlReaderIterator.Node node, SVGDictionary dict, SVGStyleResolver styles, Inheritance inheritance = Inheritance.Inherited)
        {
            string strokeAttrib = styles.Evaluate("stroke", inheritance);
            if (string.IsNullOrEmpty(strokeAttrib))
                return null; // If stroke is not specified, no other stroke properties matter

            string opacityAttrib = styles.Evaluate("stroke-opacity", inheritance);
            float opacity = (opacityAttrib != null) ? ParseFloat(opacityAttrib) : 1.0f;

            IFill strokeFill = null;
            try
            {
                strokeFill = (new SVGAttribParser(strokeAttrib, "stroke", opacity, FillMode.NonZero, dict, null)).fill;
            }
            catch (Exception e)
            {
                throw node.GetException(e.Message);
            }

            if (strokeFill == null)
                return null;

            return new Stroke() { Fill = strokeFill };
        }

        public static Color ParseColor(string colorString)
        {
            if (colorString[0] == '#')
            {
                // Hex format
                var hexVal = UInt32.Parse(colorString.Substring(1), NumberStyles.HexNumber);
                if (colorString.Length == 4)
                {
                    // #ABC >> #AABBCC
                    return new Color(
                        ((((hexVal >> 8) & 0xF) << 0) | (((hexVal >> 8) & 0xF) << 4)) / 255.0f,
                        ((((hexVal >> 4) & 0xF) << 0) | (((hexVal >> 4) & 0xF) << 4)) / 255.0f,
                        ((((hexVal >> 0) & 0xF) << 0) | (((hexVal >> 0) & 0xF) << 4)) / 255.0f);
                }
                else
                {
                    // #ABCDEF
                    return new Color(
                        ((hexVal >> 16) & 0xFF) / 255.0f,
                        ((hexVal >> 8) & 0xFF) / 255.0f,
                        ((hexVal >> 0) & 0xFF) / 255.0f);
                }
            }
            if (colorString.StartsWith("rgb(") && colorString.EndsWith(")"))
            {
                string numbersString = colorString.Substring(4, colorString.Length-5);
                string[] numbers = numbersString.Split(new char[] { ',', '%' }, StringSplitOptions.RemoveEmptyEntries);
                if (numbers.Length != 3)
                    throw new Exception("Invalid rgb() color specification");
                float divisor = colorString.Contains("%") ? 100.0f : 255.0f;
                return new Color(Byte.Parse(numbers[0]) / divisor, Byte.Parse(numbers[1]) / divisor, Byte.Parse(numbers[2]) / divisor);
            }
            else if (colorString.StartsWith("rgba(") && colorString.EndsWith(")"))
            {
                string numbersString = colorString.Substring(5, colorString.Length-6);
                string[] numbers = numbersString.Split(new char[] { ',', '%' }, StringSplitOptions.RemoveEmptyEntries);
                if (numbers.Length != 4)
                    throw new Exception("Invalid rgba() color specification");
                float divisor = colorString.Contains("%") ? 100.0f : 255.0f;
                return new Color(
                    Byte.Parse(numbers[0]) / divisor,
                    Byte.Parse(numbers[1]) / divisor,
                    Byte.Parse(numbers[2]) / divisor,
                    divisor == 100.0f ? Byte.Parse(numbers[3]) / divisor : ParseFloat(numbers[3]));
            }
            else if(colorString.StartsWith("hsl(") && colorString.EndsWith(")"))
            {
                string numbersString = colorString.Substring(4, colorString.Length-5);
                string[] numbers = numbersString.Split(new char[] { ',', '%' }, StringSplitOptions.RemoveEmptyEntries);
                if (numbers.Length != 3)
                    throw new Exception("Invalid hsl() color specification");

                float hue = ParseFloat(numbers[0]) / 360.0f;
                float saturation = ParseFloat(numbers[1]) / 100.0f;
                float lightness = ParseFloat(numbers[2]) / 100.0f;

                return HSLToRGB(hue, saturation, lightness);
            }

            // Named color
            if (namedColors == null)
                namedColors = new NamedWebColorDictionary();
            return namedColors[colorString.ToLower()];
        }

        private static float HueToValue(float p, float q, float t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < (1.0f/6.0f)) return p + (q - p) * 6 * t;
            if (t < 0.5f) return q;
            if (t < (2.0f/3.0f)) return p + (q - p) * (2.0f/3.0f - t) * 6.0f;
            return p;
        }

        private static Color HSLToRGB(float hue, float saturation, float lightness)
        {
            float q = (lightness < 0.5f) ? lightness * (1 + saturation) : lightness + saturation - lightness * saturation;
            float p = 2.0f * lightness - q;
            float r = HueToValue(p, q, hue + 1.0f/3.0f);
            float g = HueToValue(p, q, hue);
            float b = HueToValue(p, q, hue - 1.0f/3.0f);
            return new Color(r, g, b);
        }

        public static string ParseURLRef(string url)
        {
            if (url.StartsWith("url(") && url.EndsWith(")"))
                return url.Substring(4, url.Length - 5);
            return null;
        }

        public static object ParseRelativeRef(string iri, SVGDictionary dict)
        {
            if (iri == null)
                return null;

            if (!iri.StartsWith("#"))
                throw new Exception("Unsupported reference type (" + iri + ")");
            iri = iri.Substring(1);
            object obj;
            dict.TryGetValue(iri, out obj);
            return obj;
        }

        public static string CleanIri(string iri)
        {
            if (iri == null)
                return null;
            if (!iri.StartsWith("#"))
                throw new Exception("Unsupported reference type (" + iri + ")");
            iri = iri.Substring(1);
            return iri;
        }

        SVGAttribParser(string attrib, AttribPath attribPath)
        {
            attribName = "path";
            attribString = attrib;
            NextPathCommand(true);
            if (pathCommand != 'm' && pathCommand != 'M')
                throw new Exception("Path must start with a MoveTo pathCommand");

            char lastCmdNoCase = '\0';
            Vector2 lastQCtrlPoint = Vector2.zero;

            while (NextPathCommand() != (char)0)
            {
                bool relative = (pathCommand >= 'a') && (pathCommand <= 'z');
                char cmdNoCase = char.ToLower(pathCommand);
                if (cmdNoCase == 'm') // Move-to
                {
                    penPos = NextVector2(relative);
                    pathCommand = relative ? 'l' : 'L'; // After a move-to, we automatically switch to a line-to of the same relativity
                    ConcludePath(false);
                }
                else if (cmdNoCase == 'z') // ClosePath
                {
                    if (currentContour.First != null)
                        penPos = currentContour.First.Value.P0;
                    ConcludePath(true);
                }
                else if (cmdNoCase == 'l') // Line-to
                {
                    var to = NextVector2(relative);
                    if ((to - penPos).magnitude > VectorUtils.Epsilon)
                        currentContour.AddLast(VectorUtils.MakeLine(penPos, to));
                    penPos = to;
                }
                else if (cmdNoCase == 'h') // Horizontal-line-to
                {
                    float x = relative ? penPos.x + NextFloat() : NextFloat();
                    var to = new Vector2(x, penPos.y);
                    if ((to - penPos).magnitude > VectorUtils.Epsilon)
                        currentContour.AddLast(VectorUtils.MakeLine(penPos, to));
                    penPos = to;
                }
                else if (cmdNoCase == 'v') // Vertical-line-to
                {
                    float y = relative ? penPos.y + NextFloat() : NextFloat();
                    var to = new Vector2(penPos.x, y);
                    if ((to - penPos).magnitude > VectorUtils.Epsilon)
                        currentContour.AddLast(VectorUtils.MakeLine(penPos, to));
                    penPos = to;
                }
                else if (cmdNoCase == 'c' || cmdNoCase == 'q') // Cubic-bezier-curve or quadratic-bezier-curve
                {
                    // If relative, the pen position is on P0 and is only moved to P3
                    BezierSegment bs = new BezierSegment();
                    bs.P0 = penPos;
                    bs.P1 = NextVector2(relative);
                    if (cmdNoCase == 'c')
                        bs.P2 = NextVector2(relative);
                    bs.P3 = NextVector2(relative);

                    if (cmdNoCase == 'q')
                    {
                        lastQCtrlPoint = bs.P1;
                        var t = 2.0f/3.0f;
                        bs.P1 = bs.P0 + t * (lastQCtrlPoint - bs.P0);
                        bs.P2 = bs.P3 + t * (lastQCtrlPoint - bs.P3);
                    }

                    penPos = bs.P3;

                    if (!VectorUtils.IsEmptySegment(bs))
                        currentContour.AddLast(bs);
                }
                else if (cmdNoCase == 's' || cmdNoCase == 't') // Smooth cubic-bezier-curve or smooth quadratic-bezier-curve
                {
                    Vector2 reflectedP1 = penPos;
                    if (currentContour.Count > 0 && (lastCmdNoCase == 'c' || lastCmdNoCase == 'q' || lastCmdNoCase == 's' || lastCmdNoCase == 't'))
                        reflectedP1 += currentContour.Last.Value.P3 - ((lastCmdNoCase == 'q' || lastCmdNoCase == 't') ? lastQCtrlPoint : currentContour.Last.Value.P2);

                    // If relative, the pen position is on P0 and is only moved to P3
                    BezierSegment bs = new BezierSegment();
                    bs.P0 = penPos;
                    bs.P1 = reflectedP1;
                    if (cmdNoCase == 's')
                        bs.P2 = NextVector2(relative);
                    bs.P3 = NextVector2(relative);

                    if (cmdNoCase == 't')
                    {
                        lastQCtrlPoint = bs.P1;
                        var t = 2.0f / 3.0f;
                        bs.P1 = bs.P0 + t * (lastQCtrlPoint - bs.P0);
                        bs.P2 = bs.P3 + t * (lastQCtrlPoint - bs.P3);
                    }

                    penPos = bs.P3;

                    if (!VectorUtils.IsEmptySegment(bs))
                        currentContour.AddLast(bs);
                }
                else if (cmdNoCase == 'a') // Elliptical-arc-to
                {
                    Vector2 radii = NextVector2();
                    float xAxisRotation = NextFloat();
                    bool largeArcFlag = NextBool();
                    bool sweepFlag = NextBool();
                    Vector2 to = NextVector2(relative);

                    if (radii.magnitude <= VectorUtils.Epsilon)
                    {
                        if ((to - penPos).magnitude > VectorUtils.Epsilon)
                            currentContour.AddLast(VectorUtils.MakeLine(penPos, to));
                    }
                    else
                    {
                        var ellipsePath = VectorUtils.BuildEllipsePath(penPos, to, -xAxisRotation * Mathf.Deg2Rad, radii.x, radii.y, largeArcFlag, sweepFlag);
                        foreach (var seg in VectorUtils.SegmentsInPath(ellipsePath))
                            currentContour.AddLast(seg);
                    }

                    penPos = to;
                }

                lastCmdNoCase = cmdNoCase;

            } // While commands exist in the string

            ConcludePath(false);
        }

        SVGAttribParser(string attrib, string attribNameVal, AttribTransform attribTransform)
        {
            attribString = attrib;
            attribName = attribNameVal;
            transform = Matrix2D.identity;
            while (stringPos < attribString.Length)
            {
                int cmdPos = stringPos;
                var trasformCommand = NextStringCommand();
                if (string.IsNullOrEmpty(trasformCommand))
                    return;
                SkipSymbol('(');

                if (trasformCommand == "matrix")
                {
                    Matrix2D mat = new Matrix2D();
                    mat.m00 = NextFloat();
                    mat.m10 = NextFloat();
                    mat.m01 = NextFloat();
                    mat.m11 = NextFloat();
                    mat.m02 = NextFloat();
                    mat.m12 = NextFloat();
                    transform *= mat;
                }
                else if (trasformCommand == "translate")
                {
                    float x = NextFloat();
                    float y = 0;
                    if (!PeekSymbol(')'))
                        y = NextFloat();
                    transform *= Matrix2D.Translate(new Vector2(x, y));
                }
                else if (trasformCommand == "scale")
                {
                    float x = NextFloat();
                    float y = x;
                    if (!PeekSymbol(')'))
                        y = NextFloat();
                    transform *= Matrix2D.Scale(new Vector2(x, y));
                }
                else if (trasformCommand == "rotate")
                {
                    float a = NextFloat() * Mathf.Deg2Rad;
                    float cx = 0, cy = 0;
                    if (!PeekSymbol(')'))
                    {
                        cx = NextFloat();
                        cy = NextFloat();
                    }
                    transform *= Matrix2D.Translate(new Vector2(cx, cy)) * Matrix2D.RotateLH(-a) * Matrix2D.Translate(new Vector2(-cx, -cy));
                }
                else if ((trasformCommand == "skewX") || (trasformCommand == "skewY"))
                {
                    float a = Mathf.Tan(NextFloat() * Mathf.Deg2Rad);
                    Matrix2D mat = Matrix2D.identity;
                    if (trasformCommand == "skewY")
                        mat.m10 = a;
                    else mat.m01 = a;
                    transform *= mat;
                }
                else throw new Exception("Unknown transform command at " + cmdPos + " in trasform specification");

                SkipSymbol(')');
            }
        }

        SVGAttribParser(string attrib, string attribName, float opacity, FillMode mode, SVGDictionary dict, SVGPostponedFills postponedFills, bool allowReference = true)
        {
            this.attribName = attribName;
            if (string.IsNullOrEmpty(attrib))
            {
                if (opacity < 1.0f)
                    fill = new SolidFill() { Color = new Color(0, 0, 0, opacity) };
                else
                    fill = dict[mode == FillMode.NonZero ?
                                SVGDocument.StockBlackNonZeroFillName :
                                SVGDocument.StockBlackOddEvenFillName] as IFill;
                return;
            }

            if (attrib == "none" || attrib == "transparent")
                return;

            if (attrib == "currentColor")
            {
                Debug.LogError("currentColor is not supported as a " + attribName + " value");
                return;
            }

            string[] paintParts = attrib.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (allowReference)
            {
                string reference = ParseURLRef(paintParts[0]);
                if (reference != null)
                {
                    fill = ParseRelativeRef(reference, dict) as IFill;
                    if (fill == null)
                    {
                        if (paintParts.Length > 1)
                        {
                            fill = (new SVGAttribParser(paintParts[1], attribName, opacity, mode, dict, postponedFills, false)).fill;
                        }
                        else if (postponedFills != null)
                        {
                            // The reference doesn't exist, but may be defined later in the file.
                            // Make a dummy fill to be replaced later.
                            fill = new SolidFill() { Color = Color.clear };
                            postponedFills[fill] = reference;
                        }
                    }

                    if (fill != null)
                        fill.Opacity = opacity;
    
                    return;
                }
            }

            var clr = ParseColor(string.Join("", paintParts));
            clr.a *= opacity;
            if (paintParts.Length > 1)
            {
                // TODO: Support ICC-Color
            }
            fill = new SolidFill() { Color = clr, Mode = mode };
        }

        void ConcludePath(bool joinEnds)
        {
            // No need to manually close the path with the last line. It is implied.
            //if (joinEnds && currentPath.Count >= 2)
            //{
            //    BezierSegment bs = new BezierSegment();
            //    bs.MakeLine(currentPath.Last.Value.P3, currentPath.First.Value.P0);
            //    currentPath.AddLast(bs);
            //}
            if (currentContour.Count > 0)
            {
                BezierContour contour = new BezierContour();
                contour.Closed = joinEnds && (currentContour.Count >= 1);
                contour.Segments = new BezierPathSegment[currentContour.Count + 1];
                int index = 0;
                foreach (var bs in currentContour)
                    contour.Segments[index++] = new BezierPathSegment() { P0 = bs.P0, P1 = bs.P1, P2 = bs.P2  };
                var connect = VectorUtils.MakeLine(currentContour.Last.Value.P3, contour.Segments[0].P0);
                contour.Segments[index] = new BezierPathSegment() { P0 = connect.P0, P1 = connect.P1, P2 = connect.P2 };
                contours.Add(contour);
            }
            currentContour.Clear(); // Restart a new path
        }

        Vector2 NextVector2(bool relative = false)
        {
            var v = new Vector2(NextFloat(), NextFloat());
            return relative ? v + penPos : v;
        }

        float NextFloat()
        {
            SkipWhitespaces();
            if (stringPos >= attribString.Length)
                throw new Exception(attribName + " specification ended before sufficing numbers required by the last pathCommand");

            int startPos = stringPos;
            if (attribString[stringPos] == '-' || attribString[stringPos] == '+')
                stringPos++; // Skip over the negative sign if it exists

            bool gotPeriod = false;
            bool gotE = false;
            while (stringPos < attribString.Length)
            {
                char c = attribString[stringPos];
                if (!gotPeriod && (c == '.'))
                {
                    gotPeriod = true;
                    stringPos++;
                    continue;
                }
                if (!gotE && ((c == 'e') || (c == 'E')))
                {
                    gotE = true;
                    stringPos++;
                    if ((stringPos < attribString.Length) && (attribString[stringPos] == '-'))
                        stringPos++; // Skip over the negative sign if it exists for the e
                    continue;
                }
                if (!char.IsDigit(c))
                    break;
                stringPos++;
            }

            if ((stringPos - startPos == 0) ||
                ((stringPos - startPos == 1) && attribString[startPos] == '-'))
                throw new Exception("Missing number at " + startPos + " in " + attribName + " specification");

            return ParseFloat(attribString.Substring(startPos, stringPos - startPos));
        }

        internal static float ParseFloat(string s)
        {
            return float.Parse(s, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
        }

        bool NextBool()
        {
            bool result = false;
            bool error = false;

            SkipWhitespaces();

            if (stringPos < attribString.Length)
            {
                var c = attribString[stringPos];
                ++stringPos;

                if (c != '0' && c != '1')
                    error = true;
                else
                    result = c == '1';
            }
            else
            {
                error = true;
            }
            
            if (error)
                throw new Exception("Expected bool at " + stringPos + " of " + attribName + " specification");

            return result;
        }

        char NextPathCommand(bool noCommandInheritance = false)
        {
            SkipWhitespaces();
            if (stringPos >= attribString.Length)
                return (char)0;

            char newCmd = attribString[stringPos];
            if ((newCmd >= 'a' && newCmd <= 'z') || (newCmd >= 'A' && newCmd <= 'Z'))
            {
                pathCommand = newCmd;
                stringPos++;
                return newCmd;
            }

            if (!noCommandInheritance && (char.IsDigit(newCmd) || (newCmd == '.') || (newCmd == '-')))
                return pathCommand; // Stepped onto a number, which means we keep the last pathCommand
            throw new Exception("Unexpected character at " + stringPos + " in path specification");
        }

        string NextStringCommand()
        {
            SkipWhitespaces();
            if (stringPos >= attribString.Length)
                return null;

            int startPos = stringPos;
            while (stringPos < attribString.Length)
            {
                char c = attribString[stringPos];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                    stringPos++;
                else break;
            }

            if (stringPos - startPos == 0)
                throw new Exception("Unexpected character at " + stringPos + " in " + attribName + " specification");

            return attribString.Substring(startPos, stringPos - startPos);
        }

        void SkipSymbol(char s)
        {
            SkipWhitespaces();
            if (stringPos >= attribString.Length || (attribString[stringPos] != s))
                throw new Exception("Expected " + s + " at " + stringPos + " of " + attribName + " specification");
            stringPos++;
        }

        bool PeekSymbol(char s)
        {
            SkipWhitespaces();
            return (stringPos < attribString.Length) && (attribString[stringPos] == s);
        }

        void SkipWhitespaces()
        {
            while (stringPos < attribString.Length)
            {
                switch (attribString[stringPos])
                {
                    case ' ':
                    case '\r':
                    case '\n':
                    case '\t':
                    case ',':
                        stringPos++;
                        break;
                    default:
                        return;
                }
            }
        }

        enum AttribPath { Path };
        enum AttribTransform { Transform };
        enum AttribStroke { Stroke };

        // Path data
        LinkedList<BezierSegment> currentContour = new LinkedList<BezierSegment>();
        List<BezierContour> contours = new List<BezierContour>();
        Vector2 penPos;
        string attribString;
        char pathCommand;

        // Transform data
        Matrix2D transform;

        // Fill data
        IFill fill;

        // Parsing data
        string attribName;
        int stringPos;

        static NamedWebColorDictionary namedColors;
    }

    class NamedWebColorDictionary : Dictionary<string, Color>
    {
        public NamedWebColorDictionary()
        {
            this["aliceblue"] = new Color(240.0f / 255.0f, 248.0f / 255.0f, 255.0f / 255.0f);
            this["antiquewhite"] = new Color(250.0f / 255.0f, 235.0f / 255.0f, 215.0f / 255.0f);
            this["aqua"] = new Color(0.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
            this["aquamarine"] = new Color(127.0f / 255.0f, 255.0f / 255.0f, 212.0f / 255.0f);
            this["azure"] = new Color(240.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
            this["beige"] = new Color(245.0f / 255.0f, 245.0f / 255.0f, 220.0f / 255.0f);
            this["bisque"] = new Color(255.0f / 255.0f, 228.0f / 255.0f, 196.0f / 255.0f);
            this["black"] = new Color(0.0f / 255.0f, 0.0f / 255.0f, 0.0f / 255.0f);
            this["blanchedalmond"] = new Color(255.0f / 255.0f, 235.0f / 255.0f, 205.0f / 255.0f);
            this["blue"] = new Color(0.0f / 255.0f, 0.0f / 255.0f, 255.0f / 255.0f);
            this["blueviolet"] = new Color(138.0f / 255.0f, 43.0f / 255.0f, 226.0f / 255.0f);
            this["brown"] = new Color(165.0f / 255.0f, 42.0f / 255.0f, 42.0f / 255.0f);
            this["burlywood"] = new Color(222.0f / 255.0f, 184.0f / 255.0f, 135.0f / 255.0f);
            this["cadetblue"] = new Color(95.0f / 255.0f, 158.0f / 255.0f, 160.0f / 255.0f);
            this["chartreuse"] = new Color(127.0f / 255.0f, 255.0f / 255.0f, 0.0f / 255.0f);
            this["chocolate"] = new Color(210.0f / 255.0f, 105.0f / 255.0f, 30.0f / 255.0f);
            this["coral"] = new Color(255.0f / 255.0f, 127.0f / 255.0f, 80.0f / 255.0f);
            this["cornflowerblue"] = new Color(100.0f / 255.0f, 149.0f / 255.0f, 237.0f / 255.0f);
            this["cornsilk"] = new Color(255.0f / 255.0f, 248.0f / 255.0f, 220.0f / 255.0f);
            this["crimson"] = new Color(220.0f / 255.0f, 20.0f / 255.0f, 60.0f / 255.0f);
            this["cyan"] = new Color(0.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
            this["darkblue"] = new Color(0.0f / 255.0f, 0.0f / 255.0f, 139.0f / 255.0f);
            this["darkcyan"] = new Color(0.0f / 255.0f, 139.0f / 255.0f, 139.0f / 255.0f);
            this["darkgoldenrod"] = new Color(184.0f / 255.0f, 134.0f / 255.0f, 11.0f / 255.0f);
            this["darkgray"] = new Color(169.0f / 255.0f, 169.0f / 255.0f, 169.0f / 255.0f);
            this["darkgrey"] = new Color(169.0f / 255.0f, 169.0f / 255.0f, 169.0f / 255.0f);
            this["darkgreen"] = new Color(0.0f / 255.0f, 100.0f / 255.0f, 0.0f / 255.0f);
            this["darkkhaki"] = new Color(189.0f / 255.0f, 183.0f / 255.0f, 107.0f / 255.0f);
            this["darkmagenta"] = new Color(139.0f / 255.0f, 0.0f / 255.0f, 139.0f / 255.0f);
            this["darkolivegreen"] = new Color(85.0f / 255.0f, 107.0f / 255.0f, 47.0f / 255.0f);
            this["darkorange"] = new Color(255.0f / 255.0f, 140.0f / 255.0f, 0.0f / 255.0f);
            this["darkorchid"] = new Color(153.0f / 255.0f, 50.0f / 255.0f, 204.0f / 255.0f);
            this["darkred"] = new Color(139.0f / 255.0f, 0.0f / 255.0f, 0.0f / 255.0f);
            this["darksalmon"] = new Color(233.0f / 255.0f, 150.0f / 255.0f, 122.0f / 255.0f);
            this["darkseagreen"] = new Color(143.0f / 255.0f, 188.0f / 255.0f, 143.0f / 255.0f);
            this["darkslateblue"] = new Color(72.0f / 255.0f, 61.0f / 255.0f, 139.0f / 255.0f);
            this["darkslategray"] = new Color(47.0f / 255.0f, 79.0f / 255.0f, 79.0f / 255.0f);
            this["darkslategrey"] = new Color(47.0f / 255.0f, 79.0f / 255.0f, 79.0f / 255.0f);
            this["darkturquoise"] = new Color(0.0f / 255.0f, 206.0f / 255.0f, 209.0f / 255.0f);
            this["darkviolet"] = new Color(148.0f / 255.0f, 0.0f / 255.0f, 211.0f / 255.0f);
            this["deeppink"] = new Color(255.0f / 255.0f, 20.0f / 255.0f, 147.0f / 255.0f);
            this["deepskyblue"] = new Color(0.0f / 255.0f, 191.0f / 255.0f, 255.0f / 255.0f);
            this["dimgray"] = new Color(105.0f / 255.0f, 105.0f / 255.0f, 105.0f / 255.0f);
            this["dimgrey"] = new Color(105.0f / 255.0f, 105.0f / 255.0f, 105.0f / 255.0f);
            this["dodgerblue"] = new Color(30.0f / 255.0f, 144.0f / 255.0f, 255.0f / 255.0f);
            this["firebrick"] = new Color(178.0f / 255.0f, 34.0f / 255.0f, 34.0f / 255.0f);
            this["floralwhite"] = new Color(255.0f / 255.0f, 250.0f / 255.0f, 240.0f / 255.0f);
            this["forestgreen"] = new Color(34.0f / 255.0f, 139.0f / 255.0f, 34.0f / 255.0f);
            this["fuchsia"] = new Color(255.0f / 255.0f, 0.0f / 255.0f, 255.0f / 255.0f);
            this["gainsboro"] = new Color(220.0f / 255.0f, 220.0f / 255.0f, 220.0f / 255.0f);
            this["ghostwhite"] = new Color(248.0f / 255.0f, 248.0f / 255.0f, 255.0f / 255.0f);
            this["gold"] = new Color(255.0f / 255.0f, 215.0f / 255.0f, 0.0f / 255.0f);
            this["goldenrod"] = new Color(218.0f / 255.0f, 165.0f / 255.0f, 32.0f / 255.0f);
            this["gray"] = new Color(128.0f / 255.0f, 128.0f / 255.0f, 128.0f / 255.0f);
            this["grey"] = new Color(128.0f / 255.0f, 128.0f / 255.0f, 128.0f / 255.0f);
            this["green"] = new Color(0.0f / 255.0f, 128.0f / 255.0f, 0.0f / 255.0f);
            this["greenyellow"] = new Color(173.0f / 255.0f, 255.0f / 255.0f, 47.0f / 255.0f);
            this["honeydew"] = new Color(240.0f / 255.0f, 255.0f / 255.0f, 240.0f / 255.0f);
            this["hotpink"] = new Color(255.0f / 255.0f, 105.0f / 255.0f, 180.0f / 255.0f);
            this["indianred"] = new Color(205.0f / 255.0f, 92.0f / 255.0f, 92.0f / 255.0f);
            this["indigo"] = new Color(75.0f / 255.0f, 0.0f / 255.0f, 130.0f / 255.0f);
            this["ivory"] = new Color(255.0f / 255.0f, 255.0f / 255.0f, 240.0f / 255.0f);
            this["khaki"] = new Color(240.0f / 255.0f, 230.0f / 255.0f, 140.0f / 255.0f);
            this["lavender"] = new Color(230.0f / 255.0f, 230.0f / 255.0f, 250.0f / 255.0f);
            this["lavenderblush"] = new Color(255.0f / 255.0f, 240.0f / 255.0f, 245.0f / 255.0f);
            this["lawngreen"] = new Color(124.0f / 255.0f, 252.0f / 255.0f, 0.0f / 255.0f);
            this["lemonchiffon"] = new Color(255.0f / 255.0f, 250.0f / 255.0f, 205.0f / 255.0f);
            this["lightblue"] = new Color(173.0f / 255.0f, 216.0f / 255.0f, 230.0f / 255.0f);
            this["lightcoral"] = new Color(240.0f / 255.0f, 128.0f / 255.0f, 128.0f / 255.0f);
            this["lightcyan"] = new Color(224.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
            this["lightgoldenrodyellow"] = new Color(250.0f / 255.0f, 250.0f / 255.0f, 210.0f / 255.0f);
            this["lightgray"] = new Color(211.0f / 255.0f, 211.0f / 255.0f, 211.0f / 255.0f);
            this["lightgrey"] = new Color(211.0f / 255.0f, 211.0f / 255.0f, 211.0f / 255.0f);
            this["lightgreen"] = new Color(144.0f / 255.0f, 238.0f / 255.0f, 144.0f / 255.0f);
            this["lightpink"] = new Color(255.0f / 255.0f, 182.0f / 255.0f, 193.0f / 255.0f);
            this["lightsalmon"] = new Color(255.0f / 255.0f, 160.0f / 255.0f, 122.0f / 255.0f);
            this["lightseagreen"] = new Color(32.0f / 255.0f, 178.0f / 255.0f, 170.0f / 255.0f);
            this["lightskyblue"] = new Color(135.0f / 255.0f, 206.0f / 255.0f, 250.0f / 255.0f);
            this["lightslategray"] = new Color(119.0f / 255.0f, 136.0f / 255.0f, 153.0f / 255.0f);
            this["lightslategrey"] = new Color(119.0f / 255.0f, 136.0f / 255.0f, 153.0f / 255.0f);
            this["lightsteelblue"] = new Color(176.0f / 255.0f, 196.0f / 255.0f, 222.0f / 255.0f);
            this["lightyellow"] = new Color(255.0f / 255.0f, 255.0f / 255.0f, 224.0f / 255.0f);
            this["lime"] = new Color(0.0f / 255.0f, 255.0f / 255.0f, 0.0f / 255.0f);
            this["limegreen"] = new Color(50.0f / 255.0f, 205.0f / 255.0f, 50.0f / 255.0f);
            this["linen"] = new Color(250.0f / 255.0f, 240.0f / 255.0f, 230.0f / 255.0f);
            this["magenta"] = new Color(255.0f / 255.0f, 0.0f / 255.0f, 255.0f / 255.0f);
            this["maroon"] = new Color(128.0f / 255.0f, 0.0f / 255.0f, 0.0f / 255.0f);
            this["mediumaquamarine"] = new Color(102.0f / 255.0f, 205.0f / 255.0f, 170.0f / 255.0f);
            this["mediumblue"] = new Color(0.0f / 255.0f, 0.0f / 255.0f, 205.0f / 255.0f);
            this["mediumorchid"] = new Color(186.0f / 255.0f, 85.0f / 255.0f, 211.0f / 255.0f);
            this["mediumpurple"] = new Color(147.0f / 255.0f, 112.0f / 255.0f, 219.0f / 255.0f);
            this["mediumseagreen"] = new Color(60.0f / 255.0f, 179.0f / 255.0f, 113.0f / 255.0f);
            this["mediumslateblue"] = new Color(123.0f / 255.0f, 104.0f / 255.0f, 238.0f / 255.0f);
            this["mediumspringgreen"] = new Color(0.0f / 255.0f, 250.0f / 255.0f, 154.0f / 255.0f);
            this["mediumturquoise"] = new Color(72.0f / 255.0f, 209.0f / 255.0f, 204.0f / 255.0f);
            this["mediumvioletred"] = new Color(199.0f / 255.0f, 21.0f / 255.0f, 133.0f / 255.0f);
            this["midnightblue"] = new Color(25.0f / 255.0f, 25.0f / 255.0f, 112.0f / 255.0f);
            this["mintcream"] = new Color(245.0f / 255.0f, 255.0f / 255.0f, 250.0f / 255.0f);
            this["mistyrose"] = new Color(255.0f / 255.0f, 228.0f / 255.0f, 225.0f / 255.0f);
            this["moccasin"] = new Color(255.0f / 255.0f, 228.0f / 255.0f, 181.0f / 255.0f);
            this["navajowhite"] = new Color(255.0f / 255.0f, 222.0f / 255.0f, 173.0f / 255.0f);
            this["navy"] = new Color(0.0f / 255.0f, 0.0f / 255.0f, 128.0f / 255.0f);
            this["oldlace"] = new Color(253.0f / 255.0f, 245.0f / 255.0f, 230.0f / 255.0f);
            this["olive"] = new Color(128.0f / 255.0f, 128.0f / 255.0f, 0.0f / 255.0f);
            this["olivedrab"] = new Color(107.0f / 255.0f, 142.0f / 255.0f, 35.0f / 255.0f);
            this["orange"] = new Color(255.0f / 255.0f, 165.0f / 255.0f, 0.0f / 255.0f);
            this["orangered"] = new Color(255.0f / 255.0f, 69.0f / 255.0f, 0.0f / 255.0f);
            this["orchid"] = new Color(218.0f / 255.0f, 112.0f / 255.0f, 214.0f / 255.0f);
            this["palegoldenrod"] = new Color(238.0f / 255.0f, 232.0f / 255.0f, 170.0f / 255.0f);
            this["palegreen"] = new Color(152.0f / 255.0f, 251.0f / 255.0f, 152.0f / 255.0f);
            this["paleturquoise"] = new Color(175.0f / 255.0f, 238.0f / 255.0f, 238.0f / 255.0f);
            this["palevioletred"] = new Color(219.0f / 255.0f, 112.0f / 255.0f, 147.0f / 255.0f);
            this["papayawhip"] = new Color(255.0f / 255.0f, 239.0f / 255.0f, 213.0f / 255.0f);
            this["peachpuff"] = new Color(255.0f / 255.0f, 218.0f / 255.0f, 185.0f / 255.0f);
            this["peru"] = new Color(205.0f / 255.0f, 133.0f / 255.0f, 63.0f / 255.0f);
            this["pink"] = new Color(255.0f / 255.0f, 192.0f / 255.0f, 203.0f / 255.0f);
            this["plum"] = new Color(221.0f / 255.0f, 160.0f / 255.0f, 221.0f / 255.0f);
            this["powderblue"] = new Color(176.0f / 255.0f, 224.0f / 255.0f, 230.0f / 255.0f);
            this["purple"] = new Color(128.0f / 255.0f, 0.0f / 255.0f, 128.0f / 255.0f);
            this["rebeccapurple"] = new Color(102.0f / 255.0f, 51.0f / 255.0f, 153.0f / 255.0f);
            this["red"] = new Color(255.0f / 255.0f, 0.0f / 255.0f, 0.0f / 255.0f);
            this["rosybrown"] = new Color(188.0f / 255.0f, 143.0f / 255.0f, 143.0f / 255.0f);
            this["royalblue"] = new Color(65.0f / 255.0f, 105.0f / 255.0f, 225.0f / 255.0f);
            this["saddlebrown"] = new Color(139.0f / 255.0f, 69.0f / 255.0f, 19.0f / 255.0f);
            this["salmon"] = new Color(250.0f / 255.0f, 128.0f / 255.0f, 114.0f / 255.0f);
            this["sandybrown"] = new Color(244.0f / 255.0f, 164.0f / 255.0f, 96.0f / 255.0f);
            this["seagreen"] = new Color(46.0f / 255.0f, 139.0f / 255.0f, 87.0f / 255.0f);
            this["seashell"] = new Color(255.0f / 255.0f, 245.0f / 255.0f, 238.0f / 255.0f);
            this["sienna"] = new Color(160.0f / 255.0f, 82.0f / 255.0f, 45.0f / 255.0f);
            this["silver"] = new Color(192.0f / 255.0f, 192.0f / 255.0f, 192.0f / 255.0f);
            this["skyblue"] = new Color(135.0f / 255.0f, 206.0f / 255.0f, 235.0f / 255.0f);
            this["slateblue"] = new Color(106.0f / 255.0f, 90.0f / 255.0f, 205.0f / 255.0f);
            this["slategray"] = new Color(112.0f / 255.0f, 128.0f / 255.0f, 144.0f / 255.0f);
            this["slategrey"] = new Color(112.0f / 255.0f, 128.0f / 255.0f, 144.0f / 255.0f);
            this["snow"] = new Color(255.0f / 255.0f, 250.0f / 255.0f, 250.0f / 255.0f);
            this["springgreen"] = new Color(0.0f / 255.0f, 255.0f / 255.0f, 127.0f / 255.0f);
            this["steelblue"] = new Color(70.0f / 255.0f, 130.0f / 255.0f, 180.0f / 255.0f);
            this["tan"] = new Color(210.0f / 255.0f, 180.0f / 255.0f, 140.0f / 255.0f);
            this["teal"] = new Color(0.0f / 255.0f, 128.0f / 255.0f, 128.0f / 255.0f);
            this["thistle"] = new Color(216.0f / 255.0f, 191.0f / 255.0f, 216.0f / 255.0f);
            this["tomato"] = new Color(255.0f / 255.0f, 99.0f / 255.0f, 71.0f / 255.0f);
            this["turquoise"] = new Color(64.0f / 255.0f, 224.0f / 255.0f, 208.0f / 255.0f);
            this["violet"] = new Color(238.0f / 255.0f, 130.0f / 255.0f, 238.0f / 255.0f);
            this["wheat"] = new Color(245.0f / 255.0f, 222.0f / 255.0f, 179.0f / 255.0f);
            this["white"] = new Color(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
            this["whitesmoke"] = new Color(245.0f / 255.0f, 245.0f / 255.0f, 245.0f / 255.0f);
            this["yellow"] = new Color(255.0f / 255.0f, 255.0f / 255.0f, 0.0f / 255.0f);
            this["yellowgreen"] = new Color(154.0f / 255.0f, 205.0f / 255.0f, 50.0f / 255.0f);
        }
    } // The boring NamedWebColorDictionary class
} // namespace
