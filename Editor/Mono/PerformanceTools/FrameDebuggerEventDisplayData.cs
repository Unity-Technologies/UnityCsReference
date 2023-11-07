// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Experimental.Rendering;

using UnityEditor;

namespace UnityEditorInternal.FrameDebuggerInternal
{
    internal struct ShaderPropertyCollection
    {
        internal string m_TypeName;
        internal string m_Header;
        internal string m_HeaderFormat;
        internal string m_Format;
        internal ShaderPropertyDisplayInfo[] m_Data;

        internal void GetCopyText(ref StringBuilder sb)
        {
            sb.AppendLine(FrameDebuggerStyles.EventDetails.s_DashesString);
            sb.AppendLine(m_TypeName);
            sb.AppendLine(FrameDebuggerStyles.EventDetails.s_DashesString);

            if (m_Data.Length == 0)
            {
                sb.AppendLine("<Empty>");
                sb.AppendLine();
                return;
            }

            sb.AppendLine(m_Header);
            for (int i = 0; i < m_Data.Length; i++)
                m_Data[i].GetCopyText(ref sb);
            sb.AppendLine();
        }

        internal string copyString
        {
            get
            {
                StringBuilder sb = new StringBuilder(4096);
                GetCopyText(ref sb);
                return sb.ToString();
            }
        }

        internal void Clear()
        {
            for (int i = 0; i < m_Data.Length; i++)
                m_Data[i].Clear();
            m_Data = null;
        }
    }

    internal struct ShaderPropertyDisplayInfo
    {
        internal bool m_IsArray;
        internal bool m_IsFoldoutOpen;
        internal string m_FoldoutString;
        internal string m_PropertyString;
        internal Texture m_Texture;
        internal GUIStyle m_ArrayGUIStyle;
        internal RenderTexture m_TextureCopy;

        internal string copyString
        {
            get
            {
                StringBuilder sb = new StringBuilder(4096);
                GetCopyText(ref sb);
                return sb.ToString();
            }
        }

        internal void Clear()
        {
            // We only need to destroy the copy texture as the other one is
            // directly from source so it could be an asset in the project, etc.
            if (m_TextureCopy != null)
            {
                FrameDebuggerHelper.DestroyTexture(ref m_TextureCopy);
                m_TextureCopy = null;
            }
        }

        internal void GetCopyText(ref StringBuilder sb)
        {
            if (!m_IsArray)
                sb.AppendLine(m_PropertyString);
            else
            {
                sb.AppendLine(m_FoldoutString);
                sb.AppendLine(m_PropertyString);
            }
        }
    }

    internal enum ShaderPropertyType
    {
        Keyword = 0,
        Texture = 1,
        Int = 2,
        Float = 3,
        Vector = 4,
        Matrix = 5,
        Buffer = 6,
        CBuffer = 7,
    }

    // Cached data built from FrameDebuggerEventData.
    // Only need to rebuild them when event data actually changes.
    internal class CachedEventDisplayData
    {
        internal int m_Index;
        internal int m_RTDisplayIndex;
        internal int m_RenderTargetWidth;
        internal int m_RenderTargetHeight;
        internal int m_RenderTargetShowableRTCount;
        internal uint m_Hash;
        internal bool m_IsValid;
        internal bool m_IsClearEvent;
        internal bool m_IsResolveEvent;
        internal bool m_IsComputeEvent;
        internal bool m_IsRayTracingEvent;
        internal bool m_IsConfigureFoveatedEvent;
        internal bool m_ShouldDisplayRealAndOriginalShaders;
        internal bool m_RenderTargetIsBackBuffer;
        internal bool m_RenderTargetIsDepthOnlyRT;
        internal bool m_RenderTargetHasShowableDepth;
        internal bool m_RenderTargetHasStencilBits;
        internal float m_DetailsGUIWidth;
        internal float m_DetailsGUIHeight;
        internal string m_Title;
        internal string m_RealShaderName;
        internal string m_OriginalShaderName;
        internal string m_RayTracingShaderName;
        internal string m_RayTracingGenerationShaderName;
        internal Mesh[] m_Meshes;
        internal GUIContent[] m_MeshNames;
        internal RenderTexture m_RenderTargetRenderTexture;
        internal FrameEventType m_Type;
        internal GraphicsFormat m_RenderTargetFormat;
        internal UnityEngine.Object m_RealShader;
        internal UnityEngine.Object m_OriginalShader;

        internal string copyString
        {
            get
            {
                string detailsString = detailsCopyString;
                m_StringBuilder.Clear();
                m_StringBuilder.AppendLine(FrameDebuggerStyles.EventDetails.s_EqualsString);
                m_StringBuilder.AppendLine(m_Title);
                m_StringBuilder.AppendLine(FrameDebuggerStyles.EventDetails.s_EqualsString);
                m_StringBuilder.AppendLine();
                m_StringBuilder.AppendLine(detailsString);

                for (int i = 0; i < m_ShaderProperties.Length; i++)
                    m_ShaderProperties[i].GetCopyText(ref m_StringBuilder);

                return m_StringBuilder.ToString();
            }
        }

        internal string detailsCopyString
        {
            get
            {
                StringBuilder sb = new StringBuilder(4096);
                sb.AppendLine(FrameDebuggerStyles.EventDetails.s_DashesString);
                sb.AppendLine("Details");
                sb.AppendLine(FrameDebuggerStyles.EventDetails.s_DashesString);
                sb.AppendLine(details);

                if (m_ShouldDisplayRealAndOriginalShaders)
                {
                    sb.AppendFormat(k_TwoColumnFormat, FrameDebuggerStyles.EventDetails.s_RealShaderText, m_RealShaderName);
                    sb.AppendLine();
                    sb.AppendFormat(k_TwoColumnFormat, FrameDebuggerStyles.EventDetails.s_OriginalShaderText, m_OriginalShaderName);
                    sb.AppendLine();
                }
                return sb.ToString();
            }
        }

        private string m_Details;
        internal string details
        {
            get
            {
                if (string.IsNullOrEmpty(m_Details))
                    m_Details = m_DetailsStringBuilder.ToString();

                return m_Details;
            }
        }

        private string m_MeshNamesString;
        internal string meshNames
        {
            get
            {
                if (string.IsNullOrEmpty(m_MeshNamesString))
                    m_MeshNamesString = m_MeshStringBuilder.ToString();

                return m_MeshNamesString;
            }
        }
        internal ShaderPropertyCollection[] m_ShaderProperties;
        internal ShaderPropertyCollection m_Keywords => m_ShaderProperties[(int)ShaderPropertyType.Keyword];
        internal ShaderPropertyCollection m_Textures => m_ShaderProperties[(int)ShaderPropertyType.Texture];
        internal ShaderPropertyCollection m_Ints     => m_ShaderProperties[(int)ShaderPropertyType.Int];
        internal ShaderPropertyCollection m_Floats   => m_ShaderProperties[(int)ShaderPropertyType.Float];
        internal ShaderPropertyCollection m_Vectors  => m_ShaderProperties[(int)ShaderPropertyType.Vector];
        internal ShaderPropertyCollection m_Matrices => m_ShaderProperties[(int)ShaderPropertyType.Matrix];
        internal ShaderPropertyCollection m_Buffers  => m_ShaderProperties[(int)ShaderPropertyType.Buffer];
        internal ShaderPropertyCollection m_CBuffers => m_ShaderProperties[(int)ShaderPropertyType.CBuffer];

        // Private
        private int m_MaxNameLength;
        private int m_MaxStageLength;
        private int m_MaxTexSizeLength;
        private int m_MaxSampleTypeLength;
        private int m_MaxColorFormatLength;
        private int m_MaxDepthFormatLength;
        private int dataTypeEnumLength => Enum.GetNames(typeof(ShaderPropertyType)).Length;
        private StringBuilder m_StringBuilder = new StringBuilder(32768);
        private StringBuilder m_DetailsStringBuilder = new StringBuilder(4096);
        private StringBuilder m_MeshStringBuilder = new StringBuilder(64);

        // Constants
        private const int k_MinNameLength = 38;
        private const int k_MinStageLength = 5; // "Stage".Length
        private const int k_MinTexSizeLength = 11; // "65536x65536".Length
        private const int k_MinSampleTypeLength = 12; // "Sampler Type".Length
        private const int k_MinColorFormatLength = 12; // "Color Format".Length
        private const int k_MinDepthFormatLength = 19; // "DepthStencil Format".Length
        private const int k_ColumnSpace = 2;
        private const int k_MaxDecimalNumbers = 7;
        private const int k_MaxNaturalNumber = 10 + k_MaxDecimalNumbers + 1 + 1; // + decimal point + sign
        private const string k_TwoColumnFormat  = "{0, -22}{1, -35}";
        private const string k_FourColumnFormat = "{0, -22}{1, -35}{2, -22}{3, -10}";
        private static string s_IntPrecision = k_MaxNaturalNumber + ":F0";
        private static string s_FloatPrecision = k_MaxNaturalNumber + ":F" + k_MaxDecimalNumbers;
        private const string k_NotAvailableString = FrameDebuggerStyles.EventDetails.k_NotAvailable;

        // Structs
        private struct ShaderPropertySortingInfo : IComparable<ShaderPropertySortingInfo>
        {
            internal string m_Name;
            internal int m_ArrayIndex;

            public int CompareTo(ShaderPropertySortingInfo other)
            {
                return string.Compare(m_Name, other.m_Name);
            }
        }

        internal void Initialize(FrameDebuggerEvent curEvent, FrameDebuggerEventData curEventData)
        {
            Clear();

            BuildCurEventDataStrings(curEvent, curEventData);
            BuildShaderPropertyData(curEventData);

            if (curEventData.m_RenderTargetRenderTexture != null)
            {
                m_RenderTargetRenderTexture = curEventData.m_RenderTargetRenderTexture;

                RenderTextureDescriptor desc = curEventData.m_RenderTargetRenderTexture.descriptor;
                if (desc.width > 0 && desc.height > 0)
                {
                    m_RenderTargetWidth = desc.width;
                    m_RenderTargetHeight = desc.height;
                }
                else
                {
                    m_RenderTargetWidth = curEventData.m_RenderTargetWidth;
                    m_RenderTargetHeight = curEventData.m_RenderTargetHeight;
                }
                m_IsValid = true;
            }
            else
            {
                m_RenderTargetWidth = curEventData.m_RenderTargetWidth;
                m_RenderTargetHeight = curEventData.m_RenderTargetHeight;
                m_IsValid = m_IsComputeEvent || m_IsRayTracingEvent;
            }
        }

        private void BuildShaderPropertyData(FrameDebuggerEventData curEventData)
        {
            ShaderInfo shaderInfo = curEventData.m_ShaderInfo;
            m_ShaderProperties= new ShaderPropertyCollection[dataTypeEnumLength];
            m_ShaderProperties[(int)ShaderPropertyType.Keyword] = GetShaderData(ShaderPropertyType.Keyword, "Keywords", ref shaderInfo, shaderInfo.m_Keywords.Length);
            m_ShaderProperties[(int)ShaderPropertyType.Float]   = GetShaderData(ShaderPropertyType.Float, "Floats", ref shaderInfo, shaderInfo.m_Floats.Length);
            m_ShaderProperties[(int)ShaderPropertyType.Int]     = GetShaderData(ShaderPropertyType.Int, "Ints", ref shaderInfo, shaderInfo.m_Ints.Length);
            m_ShaderProperties[(int)ShaderPropertyType.Vector]  = GetShaderData(ShaderPropertyType.Vector, "Vectors", ref shaderInfo, shaderInfo.m_Vectors.Length);
            m_ShaderProperties[(int)ShaderPropertyType.Matrix]  = GetShaderData(ShaderPropertyType.Matrix, "Matrices", ref shaderInfo, shaderInfo.m_Matrices.Length);
            m_ShaderProperties[(int)ShaderPropertyType.Texture] = GetShaderData(ShaderPropertyType.Texture, "Textures", ref shaderInfo, shaderInfo.m_Textures.Length);
            m_ShaderProperties[(int)ShaderPropertyType.Buffer]  = GetShaderData(ShaderPropertyType.Buffer, "Buffers", ref shaderInfo, shaderInfo.m_Buffers.Length);
            m_ShaderProperties[(int)ShaderPropertyType.CBuffer] = GetShaderData(ShaderPropertyType.CBuffer, "Constant Buffers", ref shaderInfo, shaderInfo.m_CBuffers.Length);
        }

        private ShaderPropertyCollection GetShaderData(ShaderPropertyType propType, string typeName, ref ShaderInfo shaderInfo, int arrayLength)
        {
            ShaderPropertyCollection displayInfo = new ShaderPropertyCollection();
            displayInfo.m_TypeName = typeName;
            GetFormatAndHeader(propType, ref shaderInfo, ref displayInfo);

            // Clear and Resolve events often have properties from the previous events
            // tied to them. We therefore force it to skip them.
            // TODO: Fix this properly in C++ land.
            if (m_IsClearEvent || m_IsResolveEvent)
                arrayLength = 0;

            // Check the data and create structs for valid ones...
            List<ShaderPropertySortingInfo> myList = new List<ShaderPropertySortingInfo>();
            for (int index = 0; index < arrayLength; index++)
                if (CreateShaderPropertyDisplayInfo(propType, index, ref shaderInfo, out ShaderPropertySortingInfo propDisplayInfo))
                    myList.Add(propDisplayInfo);

            // Sort it...
            myList.Sort();

            // Fill the ordered structs with data...
            ShaderPropertyDisplayInfo[] myArr = new ShaderPropertyDisplayInfo[myList.Count];
            for (int index = 0; index < myList.Count; index++)
                GetShaderPropertyData(propType, myList[index].m_ArrayIndex, myList[index].m_Name, ref displayInfo, ref shaderInfo, ref myArr[index]);

            // Assign the array and return
            displayInfo.m_Data = myArr;
            return displayInfo;
        }

        private void GetFormatAndHeader(ShaderPropertyType dataType, ref ShaderInfo shaderInfo, ref ShaderPropertyCollection displayData)
        {
            // To keep the lines dynamic that scales with the various properties, we
            // count the characters for various fields so we can adjust the text formatting
            CountCharacters(dataType, shaderInfo);

            // Every property starts with the name + scope
            displayData.m_Format = "{0, " + -m_MaxNameLength + "}{1, " + -m_MaxStageLength + "}";
            displayData.m_HeaderFormat = string.Empty;

            switch (dataType)
            {
                case ShaderPropertyType.Keyword:
                    displayData.m_Format += "{2, -12}{3, -9}";
                    displayData.m_Header = String.Format(displayData.m_Format, "Name", "Stage", "Scope", "Dynamic");
                    break;
                case ShaderPropertyType.Float:
                    displayData.m_Format += "{2, " + s_FloatPrecision + "}";
                    displayData.m_Header = String.Format(displayData.m_Format, "Name", "Stage", "Value");
                    break;
                case ShaderPropertyType.Int:
                    displayData.m_Format += "{2, " + s_IntPrecision + "}";
                    displayData.m_Header = String.Format(displayData.m_Format, "Name", "Stage", "Value");
                    break;
                case ShaderPropertyType.Vector:
                    displayData.m_Format += "{2, " + s_FloatPrecision + "}{3, " + s_FloatPrecision + "}{4, " + s_FloatPrecision + "}{5, " + s_FloatPrecision + "}";
                    displayData.m_Header = String.Format(displayData.m_Format, "Name", "Stage", "Value(R)", "Value(G)", "Value(B)", "Value(A)");
                    break;
                case ShaderPropertyType.Matrix:
                    displayData.m_HeaderFormat = "{0, " + -m_MaxNameLength + "}{1, " + -m_MaxStageLength + "}{2, " + s_FloatPrecision + "}{3, " + s_FloatPrecision + "}{4, " + s_FloatPrecision + "}{5, " + s_FloatPrecision + "}";
                    displayData.m_Format = "{0, " + -m_MaxNameLength + "}{1, " + -m_MaxStageLength + "}{2, " + s_FloatPrecision + "}{3, " + s_FloatPrecision + "}{4, " + s_FloatPrecision + "}{5, " + s_FloatPrecision + "}\n"
                                       + "{6, " + -m_MaxNameLength + "}{7, " + -m_MaxStageLength + "}{8, " + s_FloatPrecision + "}{9, " + s_FloatPrecision + "}{10, " + s_FloatPrecision + "}{11, " + s_FloatPrecision + "}\n"
                                       + "{12, " + -m_MaxNameLength + "}{13, " + -m_MaxStageLength + "}{14, " + s_FloatPrecision + "}{15, " + s_FloatPrecision + "}{16, " + s_FloatPrecision + "}{17, " + s_FloatPrecision + "}\n"
                                       + "{18, " + -m_MaxNameLength + "}{19, " + -m_MaxStageLength + "}{20, " + s_FloatPrecision + "}{21, " + s_FloatPrecision + "}{22, " + s_FloatPrecision + "}{23, " + s_FloatPrecision + "}";
                    displayData.m_Header = String.Format(displayData.m_HeaderFormat, "Name", "Stage", "Column 0", "Column 1", "Column 2", "Column 3");
                    break;
                case ShaderPropertyType.Texture:
                    displayData.m_Format += "{2, " + -m_MaxTexSizeLength + "}{3, " + -m_MaxSampleTypeLength + "}{4, " + -m_MaxColorFormatLength + "}{5, " + -m_MaxDepthFormatLength + "}{6}";
                    displayData.m_HeaderFormat = displayData.m_Format;
                    displayData.m_Header = String.Format(displayData.m_HeaderFormat, "Name", "Stage", "Size", "Sampler Type", "Color Format", "DepthStencil Format", "Texture");
                    break;
                case ShaderPropertyType.Buffer:
                    displayData.m_Format = "{0, " + -m_MaxNameLength + "}";
                    displayData.m_Header = String.Format(displayData.m_Format, "Name");
                    break;
                case ShaderPropertyType.CBuffer:
                    displayData.m_Format = "{0, " + -m_MaxNameLength + "}";
                    displayData.m_Header = String.Format(displayData.m_Format, "Name");
                    break;
                default:
                    return;
            }
        }

        private bool CreateShaderPropertyDisplayInfo(ShaderPropertyType dataType, int arrayIndex, ref ShaderInfo shaderInfo, out ShaderPropertySortingInfo data)
        {
            int flags;
            string name;
            data = new ShaderPropertySortingInfo();
            switch (dataType)
            {
                case ShaderPropertyType.Keyword: flags = shaderInfo.m_Keywords[arrayIndex].m_Flags; name = shaderInfo.m_Keywords[arrayIndex].m_Name; break;
                case ShaderPropertyType.Float:   flags = shaderInfo.m_Floats[arrayIndex].m_Flags;   name = shaderInfo.m_Floats[arrayIndex].m_Name; break;
                case ShaderPropertyType.Int:     flags = shaderInfo.m_Ints[arrayIndex].m_Flags;     name = shaderInfo.m_Ints[arrayIndex].m_Name; break;
                case ShaderPropertyType.Vector:  flags = shaderInfo.m_Vectors[arrayIndex].m_Flags;  name = shaderInfo.m_Vectors[arrayIndex].m_Name; break;
                case ShaderPropertyType.Matrix:  flags = shaderInfo.m_Matrices[arrayIndex].m_Flags; name = shaderInfo.m_Matrices[arrayIndex].m_Name; break;
                case ShaderPropertyType.Texture: flags = shaderInfo.m_Textures[arrayIndex].m_Flags; name = shaderInfo.m_Textures[arrayIndex].m_Name; break;
                case ShaderPropertyType.Buffer:  flags = 1;                                         name = shaderInfo.m_Buffers[arrayIndex].m_Name; break;
                case ShaderPropertyType.CBuffer: flags = 1;                                         name = shaderInfo.m_CBuffers[arrayIndex].m_Name; break;
                default: return false;
            }

            // We get a lot of rubbish data sent in. Needs investigation to why...
            if (dataType != ShaderPropertyType.Keyword && dataType != ShaderPropertyType.Texture && dataType != ShaderPropertyType.Buffer && dataType != ShaderPropertyType.CBuffer)
                if (FrameDebuggerHelper.GetNumberOfValuesFromFlags(flags) <= 0)
                    return false;

            data.m_Name = name;
            data.m_ArrayIndex = arrayIndex;
            return true;
        }

        private string GetArrayIndexString(int nameLength, int numOfValues, int currentIndex)
        {
            int numOfSpaces = nameLength + (FrameDebuggerHelper.CountDigits(numOfValues) - FrameDebuggerHelper.CountDigits(currentIndex));
            return $"{new string(' ', numOfSpaces)}[{currentIndex}]";
        }

        private bool GetShaderPropertyData(ShaderPropertyType dataType, int arrayIndex, string name, ref ShaderPropertyCollection propertyTypeDisplayData, ref ShaderInfo shaderInfo, ref ShaderPropertyDisplayInfo data)
        {
            m_StringBuilder.Clear();
            data = new ShaderPropertyDisplayInfo();

            // Create and return the data...
            string stage;
            int numOfValues;
            switch (dataType)
            {
                case ShaderPropertyType.Keyword:
                    Profiler.BeginSample("Keyword");
                    ShaderKeywordInfo keywordInfo = shaderInfo.m_Keywords[arrayIndex];
                    data.m_PropertyString = m_StringBuilder.AppendFormat(propertyTypeDisplayData.m_Format,
                        name,
                        FrameDebuggerHelper.GetShaderStageString(keywordInfo.m_Flags),
                        keywordInfo.m_IsGlobal ? "Global" : "Local",
                        keywordInfo.m_IsDynamic ? "Yes" : "No").ToString();
                    Profiler.EndSample();
                    break;

                case ShaderPropertyType.Texture:
                    Profiler.BeginSample("Texture");
                    ShaderTextureInfo textureInfo = shaderInfo.m_Textures[arrayIndex];
                    stage = FrameDebuggerHelper.GetShaderStageString(textureInfo.m_Flags);

                    Texture texture = textureInfo.m_Value;
                    string samplerType;
                    string size;
                    string colorFormat;
                    string depthStencilFormat;
                    string textureName = textureInfo.m_TextureName;
                    if (texture != null)
                    {
                        if (texture.dimension == TextureDimension.Tex2DArray)
                            samplerType = "Tex2D-Array";
                        else if (texture.dimension == TextureDimension.CubeArray)
                            samplerType = "Cube-Array";
                        else
                            samplerType =  $"{texture.dimension}";

                        size = $"{texture.width}x{texture.height}";
                        colorFormat = FrameDebuggerHelper.GetColorFormat(ref texture);
                        depthStencilFormat = FrameDebuggerHelper.GetDepthStencilFormat(ref texture);

                        // We need to do a blit for when MSAA is enabled or when trying to show depth.
                        // The ObjectPreview doesn't currently support RenderTextures with only depth...
                        int msaaVal = FrameDebuggerHelper.GetMSAAValue(ref texture);
                        bool isDepthTex = FrameDebuggerHelper.IsADepthTexture(ref texture);
                        if (msaaVal > 1 || isDepthTex)
                        {
                            data.m_TextureCopy = new RenderTexture(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                            FrameDebuggerHelper.BlitToRenderTexture(
                                ref texture,
                                ref data.m_TextureCopy,
                                texture.width,
                                texture.height,
                                Vector4.one,
                                new Vector4(0f, 1f, 0f, 0f),
                                false,
                                false
                            );
                        }
                        else
                            data.m_Texture = texture;
                    }
                    else
                    {
                        samplerType = FrameDebuggerStyles.EventDetails.k_NotAvailable;
                        size = FrameDebuggerStyles.EventDetails.k_NotAvailable;
                        colorFormat = FrameDebuggerStyles.EventDetails.k_NotAvailable;
                        depthStencilFormat = FrameDebuggerStyles.EventDetails.k_NotAvailable;
                    }

                    data.m_PropertyString = m_StringBuilder.AppendFormat(propertyTypeDisplayData.m_HeaderFormat, name, stage, size, samplerType, colorFormat, depthStencilFormat, textureName).ToString();
                    Profiler.EndSample();
                    break;

                case ShaderPropertyType.Int:
                    Profiler.BeginSample("Int");
                    ShaderIntInfo intInfo = shaderInfo.m_Ints[arrayIndex];
                    numOfValues = FrameDebuggerHelper.GetNumberOfValuesFromFlags(intInfo.m_Flags);
                    stage = FrameDebuggerHelper.GetShaderStageString(intInfo.m_Flags);
                    data.m_IsArray = numOfValues > 1;
                    data.m_ArrayGUIStyle = CreateArrayGUIStyle(numOfValues);

                    if (!data.m_IsArray)
                        data.m_PropertyString = String.Format(propertyTypeDisplayData.m_Format, name, stage, intInfo.m_Value);
                    else
                    {
                        m_StringBuilder.Clear();
                        data.m_FoldoutString = String.Format(propertyTypeDisplayData.m_Format, $"{name}[{numOfValues}]", stage, string.Empty, string.Empty, string.Empty, string.Empty);
                        for (int k = arrayIndex; k < arrayIndex + numOfValues; k++)
                        {
                            int value = shaderInfo.m_Ints[k].m_Value;
                            m_StringBuilder.AppendFormat(propertyTypeDisplayData.m_Format,
                                                         GetArrayIndexString(name.Length, numOfValues, k - arrayIndex),
                                                         string.Empty,
                                                         value).AppendLine();
                        }

                        // Remove last linebreak
                        if (m_StringBuilder.Length > 2)
                            m_StringBuilder.Length--;

                        data.m_PropertyString = m_StringBuilder.ToString();
                    }
                    Profiler.EndSample();
                    break;

                case ShaderPropertyType.Float:
                    Profiler.BeginSample("Float");
                    ShaderFloatInfo floatInfo = shaderInfo.m_Floats[arrayIndex];
                    numOfValues = FrameDebuggerHelper.GetNumberOfValuesFromFlags(floatInfo.m_Flags);
                    stage = FrameDebuggerHelper.GetShaderStageString(floatInfo.m_Flags);
                    data.m_IsArray = numOfValues > 1;
                    data.m_ArrayGUIStyle = CreateArrayGUIStyle(numOfValues);

                    if (!data.m_IsArray)
                        data.m_PropertyString = String.Format(propertyTypeDisplayData.m_Format, name, stage, floatInfo.m_Value);
                    else
                    {
                        m_StringBuilder.Clear();

                        data.m_FoldoutString = m_StringBuilder.AppendFormat(propertyTypeDisplayData.m_Format, $"{name}[{numOfValues}]", stage, string.Empty, string.Empty, string.Empty, string.Empty).ToString();
                        m_StringBuilder.Clear();
                        for (int k = arrayIndex; k < arrayIndex + numOfValues; k++)
                        {
                            float value = shaderInfo.m_Floats[k].m_Value;
                            m_StringBuilder.AppendFormat(propertyTypeDisplayData.m_Format,
                                                         GetArrayIndexString(name.Length, numOfValues, k - arrayIndex),
                                                         string.Empty,
                                                         value).AppendLine();
                        }

                        // Remove last linebreak
                        if (numOfValues > 1 && m_StringBuilder.Length > 2)
                            m_StringBuilder.Length--;

                        data.m_PropertyString = m_StringBuilder.ToString();
                    }
                    Profiler.EndSample();
                    break;

                case ShaderPropertyType.Vector:
                    Profiler.BeginSample("Vector");
                    ShaderVectorInfo vectorInfo = shaderInfo.m_Vectors[arrayIndex];
                    numOfValues = FrameDebuggerHelper.GetNumberOfValuesFromFlags(vectorInfo.m_Flags);
                    stage = FrameDebuggerHelper.GetShaderStageString(vectorInfo.m_Flags);
                    data.m_IsArray = numOfValues > 1;
                    data.m_ArrayGUIStyle = CreateArrayGUIStyle(numOfValues);

                    if (!data.m_IsArray)
                        data.m_PropertyString = String.Format(propertyTypeDisplayData.m_Format, name, stage, vectorInfo.m_Value.x, vectorInfo.m_Value.y, vectorInfo.m_Value.z, vectorInfo.m_Value.w);
                    else
                    {
                        m_StringBuilder.Clear();
                        data.m_FoldoutString = String.Format(propertyTypeDisplayData.m_Format, $"{name}[{numOfValues}]", stage, string.Empty, string.Empty, string.Empty, string.Empty);
                        for (int k = arrayIndex; k < arrayIndex + numOfValues; k++)
                        {
                            Vector4 value = shaderInfo.m_Vectors[k].m_Value;
                            m_StringBuilder.AppendFormat(propertyTypeDisplayData.m_Format,
                                                         GetArrayIndexString(name.Length, numOfValues, k - arrayIndex),
                                                         string.Empty,
                                                         value.x,
                                                         value.y,
                                                         value.z,
                                                         value.w).AppendLine();
                        }

                        // Remove last linebreak
                        if (numOfValues > 1 && m_StringBuilder.Length > 2)
                            m_StringBuilder.Length--;

                        data.m_PropertyString = m_StringBuilder.ToString();
                    }

                    Profiler.EndSample();
                    break;

                case ShaderPropertyType.Matrix:
                    Profiler.BeginSample("Matrix");
                    ShaderMatrixInfo matrixInfo = shaderInfo.m_Matrices[arrayIndex];
                    numOfValues = FrameDebuggerHelper.GetNumberOfValuesFromFlags(matrixInfo.m_Flags);
                    stage = FrameDebuggerHelper.GetShaderStageString(matrixInfo.m_Flags);
                    data.m_IsArray = numOfValues > 1;
                    data.m_ArrayGUIStyle = CreateArrayGUIStyle(numOfValues * 4);

                    if (!data.m_IsArray)
                    {
                        Matrix4x4 value = matrixInfo.m_Value;
                        data.m_PropertyString = m_StringBuilder.AppendFormat(propertyTypeDisplayData.m_Format,
                                         name, stage, value.m00, value.m01, value.m02, value.m03,
                                         string.Empty, string.Empty, value.m10, value.m11, value.m12, value.m13,
                                         string.Empty, string.Empty, value.m20, value.m21, value.m22, value.m23,
                                         string.Empty, string.Empty, value.m30, value.m31, value.m32, value.m33).ToString();
                    }
                    else
                    {
                        m_StringBuilder.Clear();
                        data.m_FoldoutString = m_StringBuilder.AppendFormat(propertyTypeDisplayData.m_HeaderFormat, $"{name}[{numOfValues}]", stage, string.Empty, string.Empty, string.Empty, string.Empty).ToString();
                        m_StringBuilder.Clear();
                        for (int k = arrayIndex; k < arrayIndex + numOfValues; k++)
                        {
                            Matrix4x4 value = shaderInfo.m_Matrices[k].m_Value;
                            m_StringBuilder.AppendFormat(propertyTypeDisplayData.m_Format,
                                                         GetArrayIndexString(name.Length, numOfValues, k - arrayIndex),
                                                                       string.Empty, value.m00, value.m01, value.m02, value.m03,
                                                         string.Empty, string.Empty, value.m10, value.m11, value.m12, value.m13,
                                                         string.Empty, string.Empty, value.m20, value.m21, value.m22, value.m23,
                                                         string.Empty, string.Empty, value.m30, value.m31, value.m32, value.m33).AppendLine();
                        }

                        // Remove last linebreak
                        if (numOfValues > 1 && m_StringBuilder.Length > 2)
                            m_StringBuilder.Length--;

                        data.m_PropertyString = m_StringBuilder.ToString();
                    }
                    Profiler.EndSample();
                    break;

                case ShaderPropertyType.Buffer:
                    Profiler.BeginSample("Buffer");
                    ShaderBufferInfo bufferInfo = shaderInfo.m_Buffers[arrayIndex];
                    data.m_PropertyString = String.Format(propertyTypeDisplayData.m_Format, name);
                    Profiler.EndSample();
                    break;

                case ShaderPropertyType.CBuffer:
                    Profiler.BeginSample("Constant Buffer");
                    ShaderConstantBufferInfo constantBufferInfo = shaderInfo.m_CBuffers[arrayIndex];
                    data.m_PropertyString = String.Format(propertyTypeDisplayData.m_Format, name);
                    Profiler.EndSample();
                    break;

                default:
                    return false;
            }

            return true;
        }

        private GUIStyle CreateArrayGUIStyle(int numOfValues)
        {
            // GUIStyle.CalcSizeWithConstraints() is super expensive so to avoid that we set a fixed
            // height and width so that function can early out. The width doesn't matter due as the
            // Vertical layout above makes sure it doesn't go too far but needs to be set for the early out.
            GUIStyle style = new GUIStyle(FrameDebuggerStyles.EventDetails.s_MonoLabelNoWrapStyle);
            style.fixedWidth = 1000f;
            style.fixedHeight = 16 * numOfValues;
            return style;
        }

        internal void OnDisable()
        {
            Clear();
        }

        private void Clear()
        {
            FrameDebuggerHelper.DestroyTexture(ref m_RenderTargetRenderTexture);
            m_StringBuilder.Clear();
            m_DetailsStringBuilder.Clear();
            m_MeshStringBuilder.Clear();
            m_Details = string.Empty;
            m_MeshNamesString = string.Empty;
            m_RealShaderName = string.Empty;
            m_RealShader = null;
            m_OriginalShaderName = string.Empty;
            m_OriginalShader = null;
            m_ShouldDisplayRealAndOriginalShaders = false;

            if (m_ShaderProperties != null)
            {
                for (int i = 0; i < m_ShaderProperties.Length; i++)
                {
                    if (!m_ShaderProperties[i].Equals(default(ShaderPropertyCollection)))
                        m_ShaderProperties[i].Clear();
                }
                m_ShaderProperties = null;
            }
        }

        private void BuildCurEventDataStrings(FrameDebuggerEvent curEvent, FrameDebuggerEventData curEventData)
        {
            // Initialize some key settings
            m_Index = FrameDebuggerUtility.limit - 1;
            m_Type = curEvent.m_Type;
            m_RTDisplayIndex = curEventData.m_RTDisplayIndex;

            m_IsClearEvent = FrameDebuggerHelper.IsAClearEvent(m_Type);
            m_IsResolveEvent = FrameDebuggerHelper.IsAResolveEvent(m_Type);
            m_IsComputeEvent = FrameDebuggerHelper.IsAComputeEvent(m_Type);
            m_IsRayTracingEvent = FrameDebuggerHelper.IsARayTracingEvent(m_Type);
            m_IsConfigureFoveatedEvent = FrameDebuggerHelper.IsAConfigureFoveatedRenderingEvent(m_Type);

            m_RenderTargetIsBackBuffer = curEventData.m_RenderTargetIsBackBuffer;
            m_RenderTargetFormat = (GraphicsFormat)curEventData.m_RenderTargetFormat;
            m_RenderTargetIsDepthOnlyRT = GraphicsFormatUtility.IsDepthFormat(m_RenderTargetFormat);
            m_RenderTargetHasShowableDepth = (curEventData.m_RenderTargetHasDepthTexture != 0);
            m_RenderTargetHasStencilBits = (curEventData.m_RenderTargetHasStencilBits != 0);
            m_RenderTargetShowableRTCount = curEventData.m_RenderTargetCount;

            // Event title
            int eventTypeInt = (int)m_Type;
            var eventObj = FrameDebuggerUtility.GetFrameEventObject(m_Index);
            if (eventObj)
                m_Title = $"Event #{m_Index + 1} {FrameDebuggerStyles.s_FrameEventTypeNames[eventTypeInt]} {eventObj.name}";
            else
                m_Title = $"Event #{m_Index + 1} {FrameDebuggerStyles.s_FrameEventTypeNames[eventTypeInt]}";

            // Collect data into a string builder based on the event type...
            if (m_IsComputeEvent)
                BuildComputeEventDataStrings(curEvent, curEventData);
            else if (m_IsRayTracingEvent)
                BuildRayTracingEventDataStrings(curEvent, curEventData);
            else if (m_IsClearEvent)
                BuildClearEventDataStrings(curEvent, curEventData);
            else
                BuildDrawCallEventDataStrings(curEvent, curEventData);

            // Create a string out of the string builder
            m_Details = m_DetailsStringBuilder.ToString();

            // Calculate the width and height so the scrollbar functions correctly for the details
            GUIContent content = new GUIContent(m_Details);
            Vector2 guiContentSize = FrameDebuggerStyles.EventDetails.s_MonoLabelStyle.CalcSize(content);
            m_DetailsGUIWidth = guiContentSize.x + 12; // Add small margin to the width
            m_DetailsGUIHeight = guiContentSize.y;
        }

        private void BuildRayTracingEventDataStrings(FrameDebuggerEvent curEvent, FrameDebuggerEventData curEventData)
        {
            bool hasAccelerationName = curEventData.m_RayTracingShaderAccelerationStructureName.Length > 0;
            string rayTracingMaxRecursionDepth     = $"{curEventData.m_RayTracingShaderMaxRecursionDepth}";
            string rayTracingDispatchSize          = $"{curEventData.m_RayTracingShaderWidth} x {curEventData.m_RayTracingShaderHeight} x {curEventData.m_RayTracingShaderDepth}";
            string rayTracingAccelerationStructure = hasAccelerationName ? $"{curEventData.m_RayTracingShaderAccelerationStructureName} ({curEventData.m_RayTracingShaderAccelerationStructureSize} KB)" : k_NotAvailableString;
            string rayTracingMissShaderCount       = $"{curEventData.m_RayTracingShaderMissShaderCount}";
            string rayTracingCallableShaderCount   = $"{curEventData.m_RayTracingShaderCallableShaderCount}";
            string rayTracingPassName              = $"{curEventData.m_RayTracingShaderPassName}";
            m_RayTracingShaderName                 = $"{curEventData.m_RayTracingShaderName}";
            m_RayTracingGenerationShaderName       = $"{curEventData.m_RayTracingShaderRayGenShaderName}";

            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Max. Recursion Depth", rayTracingMaxRecursionDepth).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Dispatch Size", rayTracingDispatchSize).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Accel. Structure", rayTracingAccelerationStructure).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Miss Shader Count", rayTracingMissShaderCount).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Callable Shader Count", rayTracingCallableShaderCount).AppendLine();
            m_DetailsStringBuilder.AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Pass", rayTracingPassName).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Ray Tracing Shader", m_RayTracingShaderName).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Ray Generation Shader", m_RayTracingGenerationShaderName).AppendLine();
        }

        private void BuildComputeEventDataStrings(FrameDebuggerEvent curEvent, FrameDebuggerEventData curEventData)
        {
            bool hasGroups = curEventData.m_ComputeShaderThreadGroupsX != 0 || curEventData.m_ComputeShaderThreadGroupsY != 0 || curEventData.m_ComputeShaderThreadGroupsZ != 0;
            string computeThreadGroups    = hasGroups ? $"{curEventData.m_ComputeShaderThreadGroupsX}x{curEventData.m_ComputeShaderThreadGroupsY}x{curEventData.m_ComputeShaderThreadGroupsZ}" : "Indirect dispatch";
            string computeThreadGroupSize = curEventData.m_ComputeShaderGroupSizeX > 0 ? $"{curEventData.m_ComputeShaderGroupSizeX}x{curEventData.m_ComputeShaderGroupSizeY}x{curEventData.m_ComputeShaderGroupSizeZ}" : k_NotAvailableString;
            string computeShaderKernel    = curEventData.m_ComputeShaderKernelName;
            string computeShaderName      = curEventData.m_ComputeShaderName;

            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Thread Groups", computeThreadGroups).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Thread Group Size", computeThreadGroupSize).AppendLine();
            m_DetailsStringBuilder.AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Kernel", computeShaderKernel).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Compute Shader", computeShaderName).AppendLine();
        }

        private void BuildClearEventDataStrings(FrameDebuggerEvent curEvent, FrameDebuggerEventData curEventData)
        {
            string target = curEventData.m_RenderTargetName;

            int typeInt         = (int)curEvent.m_Type;
            string clearColor   = (typeInt & 1) != 0 ? $"({curEventData.m_RenderTargetClearColorR:F2}, {curEventData.m_RenderTargetClearColorG:F2}, {curEventData.m_RenderTargetClearColorB:F2}, {curEventData.m_RenderTargetClearColorA:F2})" : k_NotAvailableString;
            string clearDepth   = (typeInt & 2) != 0 ? curEventData.m_ClearDepth.ToString("f3") : k_NotAvailableString;
            string clearStencil = (typeInt & 4) != 0 ? FrameDebuggerHelper.GetStencilString((int)curEventData.m_ClearStencil) : k_NotAvailableString;

            string size         = $"{curEventData.m_RenderTargetWidth}x{curEventData.m_RenderTargetHeight}";
            string format       = $"{m_RenderTargetFormat}";

            bool hasColorActions = curEventData.m_RenderTargetLoadAction != -1;
            bool hasDepthActions = curEventData.m_RenderTargetDepthLoadAction != -1;
            string colorActions = hasColorActions ? $"{(RenderBufferLoadAction)curEventData.m_RenderTargetLoadAction} / {(RenderBufferStoreAction)curEventData.m_RenderTargetStoreAction}" : k_NotAvailableString;
            string depthActions = hasDepthActions ? $"{(RenderBufferLoadAction)curEventData.m_RenderTargetDepthLoadAction} / {(RenderBufferStoreAction)curEventData.m_RenderTargetDepthStoreAction}" : k_NotAvailableString;

            m_DetailsStringBuilder.Append(FrameDebuggerStyles.EventDetails.s_RenderTargetText).AppendLine();
            m_DetailsStringBuilder.Append(target).AppendLine();
            m_DetailsStringBuilder.AppendLine();

            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Size", size).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Format", format).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Color Actions", colorActions).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Depth Actions", depthActions).AppendLine();
            m_DetailsStringBuilder.AppendLine();

            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Clear Color",  clearColor).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Clear Depth",  clearDepth).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Clear Stencil", clearStencil).AppendLine();
        }

        private void BuildDrawCallEventDataStrings(FrameDebuggerEvent curEvent, FrameDebuggerEventData curEventData)
        {
            m_ShouldDisplayRealAndOriginalShaders = true;

            //
            FrameDebuggerRasterState rasterState = curEventData.m_RasterState;
            FrameDebuggerDepthState depthState = curEventData.m_DepthState;
            FrameDebuggerBlendState blendState = curEventData.m_BlendState;

            // Shader names
            m_RealShaderName = curEventData.m_RealShaderName;
            m_RealShader = Shader.Find(m_RealShaderName);
            m_OriginalShaderName = curEventData.m_OriginalShaderName;
            m_OriginalShader = Shader.Find(m_OriginalShaderName);

            //
            bool isResolveOrConfigureFov = m_IsResolveEvent || m_IsConfigureFoveatedEvent;
            bool hasColorActions = curEventData.m_RenderTargetLoadAction != -1;
            bool hasDepthActions = curEventData.m_RenderTargetDepthLoadAction != -1;
            bool isStencilEnabled = curEventData.m_StencilState.m_StencilEnable;

            // Gather the necessary strings...
            string target = curEventData.m_RenderTargetName;
            string batchbreakCause = FrameDebuggerStyles.EventDetails.s_BatchBreakCauses[curEventData.m_BatchBreakCause];
            FrameDebuggerHelper.SpliceText(ref batchbreakCause, 85);

            string size         = $"{curEventData.m_RenderTargetWidth}x{curEventData.m_RenderTargetHeight}";
            string format       = $"{m_RenderTargetFormat}";
            string colorActions = hasColorActions ? $"{(RenderBufferLoadAction)curEventData.m_RenderTargetLoadAction} / {(RenderBufferStoreAction)curEventData.m_RenderTargetStoreAction}" : k_NotAvailableString;
            string depthActions = hasDepthActions ? $"{(RenderBufferLoadAction)curEventData.m_RenderTargetDepthLoadAction} / {(RenderBufferStoreAction)curEventData.m_RenderTargetDepthStoreAction}" : k_NotAvailableString;

            string zClip        = !isResolveOrConfigureFov ? rasterState.m_DepthClip.ToString() : k_NotAvailableString;
            string zTest        = !isResolveOrConfigureFov ? depthState.m_DepthFunc.ToString() : k_NotAvailableString;
            string zWrite       = !isResolveOrConfigureFov ? (depthState.m_DepthWrite == 0 ? "Off" : "On") : k_NotAvailableString;
            string cull         = !isResolveOrConfigureFov ? rasterState.m_CullMode.ToString() : k_NotAvailableString;
            string conservative = !isResolveOrConfigureFov ? rasterState.m_Conservative.ToString() : k_NotAvailableString;
            string offset       = !isResolveOrConfigureFov ? $"{rasterState.m_SlopeScaledDepthBias}, {rasterState.m_DepthBias}" : k_NotAvailableString;
            string memoryless   = !isResolveOrConfigureFov ? (curEventData.m_RenderTargetMemoryless != 0 ? "Yes" : "No") : k_NotAvailableString;
            string foveatedRendering = FrameDebuggerHelper.GetFoveatedRenderingModeString(curEventData.m_RenderTargetFoveatedRenderingMode);

            bool hasGroups = curEventData.m_ComputeShaderThreadGroupsX != 0 || curEventData.m_ComputeShaderThreadGroupsY != 0 || curEventData.m_ComputeShaderThreadGroupsZ != 0;
            string computeShaderKernel    = m_IsComputeEvent ? curEventData.m_ComputeShaderKernelName : k_NotAvailableString;
            string computeThreadGroups    = m_IsComputeEvent && hasGroups ? $"{curEventData.m_ComputeShaderThreadGroupsX}x{curEventData.m_ComputeShaderThreadGroupsY}x{curEventData.m_ComputeShaderThreadGroupsZ}" : m_IsComputeEvent ? "Indirect dispatch" : k_NotAvailableString;
            string computeThreadGroupSize = m_IsComputeEvent && curEventData.m_ComputeShaderGroupSizeX > 0 ? $"{curEventData.m_ComputeShaderGroupSizeX}x{curEventData.m_ComputeShaderGroupSizeY}x{curEventData.m_ComputeShaderGroupSizeZ}" : k_NotAvailableString;

            string passName = $"{(string.IsNullOrEmpty(curEventData.m_PassName) ? k_NotAvailableString : curEventData.m_PassName)} ({curEventData.m_ShaderPassIndex})";
            string lightModeName = string.IsNullOrEmpty(curEventData.m_PassLightMode) ? k_NotAvailableString : curEventData.m_PassLightMode;

            // Format them all together...
            m_DetailsStringBuilder.Append(FrameDebuggerStyles.EventDetails.s_RenderTargetText).AppendLine();
            m_DetailsStringBuilder.Append(target).AppendLine();
            m_DetailsStringBuilder.AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Size", size, "ZClip", zClip).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Format", format, "ZTest", zTest).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Color Actions", colorActions, "ZWrite", zWrite).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Depth Actions", depthActions, "Cull", cull).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Memoryless", memoryless, "Conservative", conservative).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Foveated Rendering", foveatedRendering, "Offset", offset).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, string.Empty, string.Empty, string.Empty, string.Empty).AppendLine();

            if (isResolveOrConfigureFov)
            {
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "ColorMask", k_NotAvailableString, "Stencil", string.Empty).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Blend Color", k_NotAvailableString, "Stencil Ref", k_NotAvailableString).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Blend Alpha", k_NotAvailableString, "Stencil ReadMask", k_NotAvailableString).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "BlendOp Color", k_NotAvailableString, "Stencil WriteMask", k_NotAvailableString).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "BlendOp Alpha", k_NotAvailableString, "Stencil Comp", k_NotAvailableString).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, string.Empty, string.Empty, "Stencil Pass", k_NotAvailableString).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "DrawInstanced Calls", k_NotAvailableString, "Stencil Fail", k_NotAvailableString).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Instances", k_NotAvailableString, "Stencil ZFail", k_NotAvailableString).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Draw Calls", "1", string.Empty, string.Empty).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Vertices", k_NotAvailableString, string.Empty, string.Empty).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Indices", k_NotAvailableString, string.Empty, string.Empty).AppendLine();
            }
            else
            {
                string colorMask = FrameDebuggerHelper.GetColorMask(blendState.m_WriteMask);
                string blendColor = $"{blendState.m_SrcBlend} / {blendState.m_DstBlend}";
                string blendAlpha = $"{blendState.m_SrcBlendAlpha} / {blendState.m_DstBlendAlpha}";
                string blendOpColor = blendState.m_BlendOp.ToString();
                string blendOpAlpha = blendState.m_BlendOpAlpha.ToString();

                string stencilRef = isStencilEnabled ? FrameDebuggerHelper.GetStencilString(curEventData.m_StencilRef) : k_NotAvailableString;
                string stencilReadMask = isStencilEnabled && curEventData.m_StencilState.m_ReadMask != 255 ? FrameDebuggerHelper.GetStencilString(curEventData.m_StencilState.m_ReadMask) : k_NotAvailableString;
                string stencilWriteMask = isStencilEnabled && curEventData.m_StencilState.m_WriteMask != 255 ? FrameDebuggerHelper.GetStencilString(curEventData.m_StencilState.m_WriteMask) : k_NotAvailableString;
                string stencilComp = k_NotAvailableString;
                string stencilPass = k_NotAvailableString;
                string stencilFail = k_NotAvailableString;
                string stencilZFail = k_NotAvailableString;

                if (isStencilEnabled)
                {
                    CullMode cullMode = curEventData.m_RasterState.m_CullMode;

                    // Only show *Front states when CullMode is set to Back.
                    if (cullMode == CullMode.Back)
                    {
                        stencilComp  = $"{curEventData.m_StencilState.m_StencilFuncFront}";
                        stencilPass  = $"{curEventData.m_StencilState.m_StencilPassOpFront}";
                        stencilFail  = $"{curEventData.m_StencilState.m_StencilFailOpFront}";
                        stencilZFail = $"{curEventData.m_StencilState.m_StencilZFailOpFront}";
                    }
                    // Only show *Back states when CullMode is set to Front.
                    else if (cullMode == CullMode.Front)
                    {
                        stencilComp  = $"{curEventData.m_StencilState.m_StencilFuncBack}";
                        stencilPass  = $"{curEventData.m_StencilState.m_StencilPassOpBack}";
                        stencilFail  = $"{curEventData.m_StencilState.m_StencilFailOpBack}";
                        stencilZFail = $"{curEventData.m_StencilState.m_StencilZFailOpBack}";
                    }
                    // Show both *Front and *Back states for two-sided geometry.
                    else
                    {
                        stencilComp  = $"{curEventData.m_StencilState.m_StencilFuncFront} / {curEventData.m_StencilState.m_StencilFuncBack}";
                        stencilPass  = $"{curEventData.m_StencilState.m_StencilPassOpFront} / {curEventData.m_StencilState.m_StencilPassOpBack}";
                        stencilFail  = $"{curEventData.m_StencilState.m_StencilFailOpFront} / {curEventData.m_StencilState.m_StencilFailOpBack}";
                        stencilZFail = $"{curEventData.m_StencilState.m_StencilZFailOpFront} / {curEventData.m_StencilState.m_StencilZFailOpBack}";
                    }
                }

                string drawInstancedCalls = curEventData.m_InstanceCount > 1 ? $"{curEventData.m_DrawCallCount}" : k_NotAvailableString;
                string drawInstances      = curEventData.m_InstanceCount > 1 ? $"{curEventData.m_InstanceCount}" : k_NotAvailableString;
                string drawCalls          = curEventData.m_InstanceCount > 1 ? k_NotAvailableString                : $"{curEventData.m_DrawCallCount}";

                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "ColorMask", colorMask, "Stencil", string.Empty).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Blend Color", blendColor, "Stencil Ref", stencilRef).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Blend Alpha", blendAlpha, "Stencil ReadMask", stencilReadMask).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "BlendOp Color", blendOpColor, "Stencil WriteMask", stencilWriteMask).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "BlendOp Alpha", blendOpAlpha, "Stencil Comp", stencilComp).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, string.Empty, string.Empty, "Stencil Pass", stencilPass).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "DrawInstanced Calls", drawInstancedCalls, "Stencil Fail", stencilFail).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Instances", drawInstances, "Stencil ZFail", stencilZFail).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Draw Calls", drawCalls, string.Empty, string.Empty).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Vertices", curEventData.m_VertexCount.ToString(), string.Empty, string.Empty).AppendLine();
                m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, "Indices", curEventData.m_IndexCount.ToString(), string.Empty, string.Empty).AppendLine();
            }

            m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, string.Empty, string.Empty, string.Empty, string.Empty).AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_FourColumnFormat, FrameDebuggerStyles.EventDetails.s_BatchCauseText, string.Empty, string.Empty, string.Empty).AppendLine();
            m_DetailsStringBuilder.Append(batchbreakCause).AppendLine();
            m_DetailsStringBuilder.AppendLine();

            if (curEventData.m_MeshInstanceIDs != null && curEventData.m_MeshInstanceIDs.Length > 0)
            {
                HashSet<int> meshIDs = new HashSet<int>();
                for (int i = 0; i < curEventData.m_MeshInstanceIDs.Length; i++)
                {
                    int id = curEventData.m_MeshInstanceIDs[i];
                    Mesh mesh = EditorUtility.InstanceIDToObject(id) as Mesh;
                    if (mesh != null)
                        meshIDs.Add(id);
                }

                List<Mesh> meshes = new List<Mesh>();
                foreach (int id in meshIDs)
                {
                    Mesh mesh = EditorUtility.InstanceIDToObject(id) as Mesh;
                    meshes.Add(mesh);
                }

                m_Meshes = meshes.ToArray();
                m_MeshNames = new GUIContent[meshes.Count];
                for (var i = 0; i < m_Meshes.Length; ++i)
                {
                    m_MeshNames[i] = EditorGUIUtility.TrTextContent(m_Meshes[i].name, string.Empty);

                    if (i == 0)
                        m_MeshStringBuilder.AppendFormat(k_TwoColumnFormat, "Meshes", m_MeshNames[i].text);
                    else
                        m_MeshStringBuilder.AppendFormat(k_TwoColumnFormat, string.Empty, m_MeshNames[i].text);

                    m_MeshStringBuilder.AppendLine();
                }
            }
            else
            {
                if (curEventData.m_Mesh == null || curEventData.m_Mesh.name == string.Empty)
                {
                    m_MeshStringBuilder.AppendFormat(k_TwoColumnFormat, "Mesh", k_NotAvailableString);
                    m_Meshes = null;
                    m_MeshNames = null;
                }
                else
                {
                    m_Meshes = new Mesh[] { curEventData.m_Mesh };
                    m_MeshNames = new GUIContent[1];
                    m_MeshNames[0] = EditorGUIUtility.TrTextContent(curEventData.m_Mesh.name, string.Empty);
                    m_MeshStringBuilder.AppendFormat(k_TwoColumnFormat, "Mesh", m_MeshNames[0].text);
                }
                m_MeshStringBuilder.AppendLine();
            }

            m_DetailsStringBuilder.Append(m_MeshStringBuilder);

            m_DetailsStringBuilder.AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "LightMode", lightModeName);
            m_DetailsStringBuilder.AppendLine();
            m_DetailsStringBuilder.AppendFormat(k_TwoColumnFormat, "Pass", passName);
        }

        private void CountCharacters(ShaderPropertyType dataType, ShaderInfo shaderInfo)
        {
            m_MaxNameLength = k_MinNameLength;
            m_MaxStageLength = k_MinStageLength;
            m_MaxTexSizeLength = k_MinTexSizeLength;
            m_MaxSampleTypeLength = k_MinSampleTypeLength;
            m_MaxColorFormatLength = k_MinColorFormatLength;
            m_MaxDepthFormatLength = k_MinDepthFormatLength;

            switch (dataType)
            {
                case ShaderPropertyType.Keyword:
                    for (int i = 0; i < shaderInfo.m_Keywords.Length; i++)
                    {
                        m_MaxNameLength = Mathf.Max(m_MaxNameLength, shaderInfo.m_Keywords[i].m_Name.Length);
                        m_MaxStageLength = Mathf.Max(m_MaxStageLength, FrameDebuggerHelper.GetShaderStageString(shaderInfo.m_Keywords[i].m_Flags).Length);
                    }
                    break;
                case ShaderPropertyType.Texture:
                    for (int i = 0; i < shaderInfo.m_Textures.Length; i++)
                    {
                        m_MaxNameLength = Mathf.Max(m_MaxNameLength, shaderInfo.m_Textures[i].m_Name.Length);
                        m_MaxStageLength = Mathf.Max(m_MaxStageLength, FrameDebuggerHelper.GetShaderStageString(shaderInfo.m_Textures[i].m_Flags).Length);

                        Texture texture = shaderInfo.m_Textures[i].m_Value;
                        if (texture != null)
                        {
                            m_MaxColorFormatLength = Mathf.Max(m_MaxColorFormatLength, FrameDebuggerHelper.GetColorFormat(ref texture).Length);
                            m_MaxDepthFormatLength = Mathf.Max(m_MaxDepthFormatLength, FrameDebuggerHelper.GetDepthStencilFormat(ref texture).Length);
                            m_MaxTexSizeLength = Mathf.Max(m_MaxTexSizeLength, $"{texture.width}x{texture.height}".Length);
                            m_MaxSampleTypeLength = Mathf.Max(m_MaxSampleTypeLength, $"{texture.dimension}".Length);
                        }
                    }
                    break;
                case ShaderPropertyType.Int:
                    for (int i = 0; i < shaderInfo.m_Ints.Length; i++)
                    {
                        int numOfValues = FrameDebuggerHelper.GetNumberOfValuesFromFlags(shaderInfo.m_Ints[i].m_Flags);
                        int arrayChars = numOfValues > 1 ? FrameDebuggerHelper.CountDigits(numOfValues) + 2 : 0;
                        m_MaxNameLength = Mathf.Max(m_MaxNameLength, shaderInfo.m_Ints[i].m_Name.Length + arrayChars);
                        m_MaxStageLength = Mathf.Max(m_MaxStageLength, FrameDebuggerHelper.GetShaderStageString(shaderInfo.m_Ints[i].m_Flags).Length);
                    }
                    break;
                case ShaderPropertyType.Float:
                    for (int i = 0; i < shaderInfo.m_Floats.Length; i++)
                    {
                        int numOfValues = FrameDebuggerHelper.GetNumberOfValuesFromFlags(shaderInfo.m_Floats[i].m_Flags);
                        int arrayChars = numOfValues > 1 ? FrameDebuggerHelper.CountDigits(numOfValues) + 2 : 0;
                        m_MaxNameLength = Mathf.Max(m_MaxNameLength, shaderInfo.m_Floats[i].m_Name.Length + arrayChars);
                        m_MaxStageLength = Mathf.Max(m_MaxStageLength, FrameDebuggerHelper.GetShaderStageString(shaderInfo.m_Floats[i].m_Flags).Length);
                    }
                    break;
                case ShaderPropertyType.Vector:
                    for (int i = 0; i < shaderInfo.m_Vectors.Length; i++)
                    {
                        int numOfValues = FrameDebuggerHelper.GetNumberOfValuesFromFlags(shaderInfo.m_Vectors[i].m_Flags);
                        int arrayChars = numOfValues > 1 ? FrameDebuggerHelper.CountDigits(numOfValues) + 2 : 0;
                        m_MaxNameLength = Mathf.Max(m_MaxNameLength, shaderInfo.m_Vectors[i].m_Name.Length + arrayChars);
                        m_MaxStageLength = Mathf.Max(m_MaxStageLength, FrameDebuggerHelper.GetShaderStageString(shaderInfo.m_Vectors[i].m_Flags).Length);
                    }
                    break;
                case ShaderPropertyType.Matrix:
                    for (int i = 0; i < shaderInfo.m_Matrices.Length; i++)
                    {
                        int numOfValues = FrameDebuggerHelper.GetNumberOfValuesFromFlags(shaderInfo.m_Matrices[i].m_Flags);
                        int arrayChars = numOfValues > 1 ? FrameDebuggerHelper.CountDigits(numOfValues) + 2 : 0;
                        m_MaxNameLength = Mathf.Max(m_MaxNameLength, shaderInfo.m_Matrices[i].m_Name.Length + arrayChars);
                        m_MaxStageLength = Mathf.Max(m_MaxStageLength, FrameDebuggerHelper.GetShaderStageString(shaderInfo.m_Matrices[i].m_Flags).Length);
                    }
                    break;
                case ShaderPropertyType.Buffer:
                    for (int i = 0; i < shaderInfo.m_Buffers.Length; i++)
                    {
                        m_MaxNameLength = Mathf.Max(m_MaxNameLength, shaderInfo.m_Buffers[i].m_Name.Length);
                        m_MaxStageLength = Mathf.Max(m_MaxStageLength, FrameDebuggerHelper.GetShaderStageString(shaderInfo.m_Buffers[i].m_Flags).Length);
                    }
                    break;
                case ShaderPropertyType.CBuffer:
                    for (int i = 0; i < shaderInfo.m_CBuffers.Length; i++)
                    {
                        m_MaxNameLength = Mathf.Max(m_MaxNameLength, shaderInfo.m_CBuffers[i].m_Name.Length);
                        m_MaxStageLength = Mathf.Max(m_MaxStageLength, FrameDebuggerHelper.GetShaderStageString(shaderInfo.m_CBuffers[i].m_Flags).Length);
                    }
                    break;
                default:
                    break;
            }

            m_MaxNameLength += k_ColumnSpace;
            m_MaxStageLength += k_ColumnSpace;
            m_MaxTexSizeLength += k_ColumnSpace;
            m_MaxSampleTypeLength += k_ColumnSpace;
            m_MaxColorFormatLength += k_ColumnSpace;
            m_MaxDepthFormatLength += k_ColumnSpace;
        }
    }
}
