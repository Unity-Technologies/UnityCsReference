// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.DeviceSimulation
{
    [ScriptedImporter(1, "device")]
    internal class DeviceInfoImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            SimulatorWindow.MarkAllDeviceListsDirty();

            var asset = ScriptableObject.CreateInstance<DeviceInfoAsset>();

            var deviceJson = File.ReadAllText(ctx.assetPath);
            asset.deviceInfo = ParseDeviceInfo(deviceJson, out var errors, out var systemInfoElement, out var graphicsDataElement);

            if (errors.Length > 0)
            {
                asset.parseErrors = errors;
            }
            else
            {
                FindOptionalFieldAvailability(asset, systemInfoElement, graphicsDataElement);
                AddOptionalFields(asset.deviceInfo);

                // Saving asset path in order to find overlay relatively to it
                asset.directory = Path.GetDirectoryName(ctx.assetPath);
                ctx.DependsOnSourceAsset(ctx.assetPath);
            }

            ctx.AddObjectToAsset("main obj", asset);
            ctx.SetMainObject(asset);
        }

        internal struct GraphicsTypeElement
        {
            public GraphicsDeviceType type;
            public XElement element;
        }

        internal static DeviceInfo ParseDeviceInfo(string deviceJsonText, out string[] errors, out XElement systemInfoElement, out List<GraphicsTypeElement> graphicsTypeElements)
        {
            var errorList = new List<string>();
            graphicsTypeElements = new List<GraphicsTypeElement>();

            XElement root;
            DeviceInfo deviceInfo;
            try
            {
                root = XElement.Load(JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(deviceJsonText), new XmlDictionaryReaderQuotas()));
                deviceInfo = JsonUtility.FromJson<DeviceInfo>(deviceJsonText);
            }
            catch (Exception)
            {
                errorList.Add("Failed parsing JSON. Make sure this is a valid JSON file.");
                errors = errorList.ToArray();
                systemInfoElement = null;
                return null;
            }

            var versionElement = root.Element("version");
            if (versionElement == null)
                errorList.Add("Mandatory field [version] is missing");
            else if (versionElement.Value != "1")
                errorList.Add("[version] field is set to an unknown value. The newest version is 1");

            var friendlyNameElement = root.Element("friendlyName");
            if (friendlyNameElement == null)
                errorList.Add("Mandatory field [friendlyName] is missing");
            else if (string.IsNullOrEmpty(friendlyNameElement.Value))
                errorList.Add("[friendlyName] field is empty, which is not allowed");

            systemInfoElement = root.Element("systemInfo");
            if (systemInfoElement == null)
                errorList.Add("Mandatory field [systemInfo] is missing");
            else
            {
                var operatingSystemElement = systemInfoElement.Element("operatingSystem");
                if (operatingSystemElement == null)
                    errorList.Add("Mandatory field [systemInfo -> operatingSystem] is missing. [operatingSystem] must be set to a string containing either <android> or <ios>");
                else if (!operatingSystemElement.Value.ToLower().Contains("android") && !operatingSystemElement.Value.ToLower().Contains("ios"))
                    errorList.Add("[systemInfo -> operatingSystem] field must be set to a string containing either <android> or <ios>, other platforms are not supported at the moment");

                var graphicsSystemInfoArray = systemInfoElement.Element("graphicsDependentData");
                if (graphicsSystemInfoArray != null)
                {
                    var graphicsSystemInfo = graphicsSystemInfoArray.Elements("item").ToArray();
                    var graphicsTypes = new HashSet<GraphicsDeviceType>();
                    for (int i = 0; i < graphicsSystemInfo.Length; i++)
                    {
                        var graphicsDeviceElement = graphicsSystemInfo[i].Element("graphicsDeviceType");
                        if (graphicsDeviceElement == null)
                            errorList.Add($"Mandatory field [systemInfo -> graphicsDependentData[{i}] -> graphicsDeviceType] is missing. [graphicsDependentData] must contain [graphicsDeviceType]");
                        else if (!int.TryParse(graphicsDeviceElement.Value, out var typeInt) || !Enum.IsDefined(typeof(GraphicsDeviceType), typeInt))
                            errorList.Add($"[systemInfo -> graphicsDependentData[{i}] -> graphicsDeviceType] is set to a value that could not be parsed as GraphicsDeviceType");
                        else
                        {
                            var type = deviceInfo.systemInfo.graphicsDependentData[i].graphicsDeviceType;
                            if (graphicsTypes.Contains(type))
                                errorList.Add($"Multiple [systemInfo -> graphicsDependentData] fields have the same GraphicsDeviceType {type}.");
                            else
                                graphicsTypes.Add(type);
                            graphicsTypeElements.Add(new GraphicsTypeElement {element = graphicsSystemInfo[i], type = type});
                        }
                    }
                }
            }

            var screensElement = root.Element("screens");
            if (screensElement == null)
                errorList.Add("Mandatory field [screens] is missing. [screens] array must contain at least one screen");
            else
            {
                var screenElements = screensElement.Elements("item").ToArray();
                if (!screenElements.Any())
                {
                    errorList.Add("[screens] array must contain at least one screen");
                }
                else
                {
                    for (var i = 0; i < screenElements.Length; i++)
                    {
                        var screen = deviceInfo.screens[i];
                        if (screenElements[i].Element("width") == null)
                            errorList.Add($"Mandatory field [screens[{i}] -> width] is missing");
                        else if (screen.width < 4 || screen.width > 8192)
                            errorList.Add($"[screens[{i}] -> width] field is set to an incorrect value {screen.width}. Screen width must be larger than 4 and smaller than 8192.");
                        if (screenElements[i].Element("height") == null)
                            errorList.Add($"Mandatory field [screens[{i}] -> height] is missing");
                        else if (screen.height < 4 || screen.height > 8192)
                            errorList.Add($"[screens[{i}] -> height] field is set to an incorrect value {screen.height}. Screen height must be larger than 4 and smaller than 8192.");
                        if (screenElements[i].Element("dpi") == null)
                            errorList.Add($"Mandatory field [screens[{i}] -> dpi] is missing");
                        else if (screen.dpi < 0.0001f || screen.dpi > 10000f)
                            errorList.Add($"[screens[{i}] -> dpi] field is set to an incorrect value {screen.dpi}. Screen dpi must be larger than 0 and smaller than 10000.");
                    }
                }
            }

            errors = errorList.ToArray();
            return errors.Length == 0 ? deviceInfo : null;
        }

        internal static void AddOptionalFields(DeviceInfo deviceInfo)
        {
            foreach (var screen in deviceInfo.screens)
            {
                if (screen.orientations == null || screen.orientations.Length == 0)
                {
                    screen.orientations = new[]
                    {
                        new OrientationData {orientation = ScreenOrientation.Portrait},
                        new OrientationData {orientation = ScreenOrientation.PortraitUpsideDown},
                        new OrientationData {orientation = ScreenOrientation.LandscapeLeft},
                        new OrientationData {orientation = ScreenOrientation.LandscapeRight}
                    };
                }
                foreach (var orientation in screen.orientations)
                {
                    if (orientation.safeArea == Rect.zero)
                        orientation.safeArea = SimulatorUtilities.IsLandscape(orientation.orientation) ? new Rect(0, 0, screen.height, screen.width) : new Rect(0, 0, screen.width, screen.height);
                }
            }
        }

        internal static void FindOptionalFieldAvailability(DeviceInfoAsset asset, XElement systemInfoElement, List<GraphicsTypeElement> graphicsDataElements)
        {
            string[] systemInfoFields =
            {
                "deviceModel",
                "deviceType",
                "operatingSystemFamily",
                "processorCount",
                "processorFrequency",
                "processorType",
                "processorModel",
                "processorManufacturer",
                "supportsAccelerometer",
                "supportsAudio",
                "supportsGyroscope",
                "supportsLocationService",
                "supportsVibration",
                "systemMemorySize"
            };

            string[] graphicsSystemInfoFields =
            {
                "graphicsMemorySize",
                "graphicsDeviceName",
                "graphicsDeviceVendor",
                "graphicsDeviceID",
                "graphicsDeviceVendorID",
                "graphicsUVStartsAtTop",
                "graphicsDeviceVersion",
                "graphicsShaderLevel",
                "graphicsMultiThreaded",
                "renderingThreadingMode",
                "foveatedRenderingCaps",
                "hasHiddenSurfaceRemovalOnGPU",
                "hasDynamicUniformArrayIndexingInFragmentShaders",
                "supportsShadows",
                "supportsRawShadowDepthSampling",
                "supportsMotionVectors",
                "supports3DTextures",
                "supports2DArrayTextures",
                "supports3DRenderTextures",
                "supportsCubemapArrayTextures",
                "copyTextureSupport",
                "supportsComputeShaders",
                "supportsGeometryShaders",
                "supportsTessellationShaders",
                "supportsInstancing",
                "supportsHardwareQuadTopology",
                "supports32bitsIndexBuffer",
                "supportsSparseTextures",
                "supportedRenderTargetCount",
                "supportsSeparatedRenderTargetsBlend",
                "supportedRandomWriteTargetCount",
                "supportsMultisampledTextures",
                "supportsMultisampleAutoResolve",
                "supportsTextureWrapMirrorOnce",
                "usesReversedZBuffer",
                "npotSupport",
                "maxTextureSize",
                "maxCubemapSize",
                "maxComputeBufferInputsVertex",
                "maxComputeBufferInputsFragment",
                "maxComputeBufferInputsGeometry",
                "maxComputeBufferInputsDomain",
                "maxComputeBufferInputsHull",
                "maxComputeBufferInputsCompute",
                "maxComputeWorkGroupSize",
                "maxComputeWorkGroupSizeX",
                "maxComputeWorkGroupSizeY",
                "maxComputeWorkGroupSizeZ",
                "supportsAsyncCompute",
                "supportsGraphicsFence",
                "supportsAsyncGPUReadback",
                "supportsParallelPSOCreation",
                "supportsRayTracing",
                "supportsRayTracingShaders",
                "supportsInlineRayTracing",
                "supportsIndirectDispatchRays",
                "supportsSetConstantBuffer",
                "hasMipMaxLevel",
                "supportsMipStreaming",
                "usesLoadStoreActions"
            };

            foreach (var field in systemInfoFields)
            {
                if (systemInfoElement.Element(field) != null)
                    asset.availableSystemInfoFields.Add(field);
            }
            foreach (var graphicsDataElement in graphicsDataElements)
            {
                var availableFields = new HashSet<string>();
                asset.availableGraphicsSystemInfoFields.Add(graphicsDataElement.type, availableFields);
                foreach (var field in graphicsSystemInfoFields)
                {
                    if (graphicsDataElement.element.Element(field) != null)
                        availableFields.Add(field);
                }
            }
        }
    }
}
