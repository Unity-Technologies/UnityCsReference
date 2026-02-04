// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Unity.UI.Builder
{
    internal static class VisualTreeAssetUtilities
    {
        public struct ElementsInfo
        {
            public int elements { get; set; }
            public int editorElements { get; set; }
            public int customElements { get; set; }
            public int hierarchyDepth { get; set; }
        }

        public static VisualTreeAsset CreateInstanceWithHideFlags()
        {
            var vta = ScriptableObject.CreateInstance<VisualTreeAsset>();

            vta.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;

            var uxmlTagElement = new VisualElementAsset(BuilderConstants.UxmlTagTypeName);
            var engineNamespaceDefinition = new UxmlNamespaceDefinition {prefix = UXMLConstants.UxmlEngineNamespaceDefaultPrefix, resolvedNamespace = UXMLConstants.UxmlEngineNamespace };
            uxmlTagElement.namespaceDefinitions.Add(engineNamespaceDefinition);
            uxmlTagElement.namespaceDefinitions.Add(new UxmlNamespaceDefinition{ prefix = UXMLConstants.UxmlEditorNamespaceDefaultPrefix, resolvedNamespace = UXMLConstants.UxmlEditorNamespace });
            uxmlTagElement.xmlNamespace = engineNamespaceDefinition;
            vta.SetRootAsset(uxmlTagElement);
            return vta;
        }

        public static void InitializeObject(UxmlObjectAsset oba)
        {
            oba.orderInDocument = -1;
        }

        static void GetElementsInfoRecursively(UxmlAsset asset, ref ElementsInfo elementsInfo)
        {
            Assert.IsNotNull(asset);

            // Editor elements
            if (asset.fullTypeName.Contains("UnityEditor")
                || BuilderLibraryContent.IsEditorOnlyControl(asset.fullTypeName))
            {
                elementsInfo.editorElements++;
            }
            // Custom controls
            // i.e. everything that is not a template and is not in the Standard library tab
            else if (asset.fullTypeName != TemplateAsset.UxmlInstanceTypeName
                     && !BuilderLibraryContent.IsStandardControl(asset.fullTypeName))
            {
                elementsInfo.customElements++;
            }

            // All elements
            elementsInfo.elements++;

            for (var i = 0; i < asset.childCount; ++i)
            {
                GetElementsInfoRecursively(asset[i], ref elementsInfo);
            }
        }

        /// <summary>
        /// It gathers the data needed to fill <see cref="ElementsInfo"/>
        /// </summary>
        internal static ElementsInfo GetElementsInfo(VisualTreeAsset vta)
        {
            var elementsInfo = new ElementsInfo();

            var rootAsset = vta.visualTree;
            for(var i = 0; i < rootAsset.childCount; ++i)
                GetElementsInfoRecursively(rootAsset[i], ref elementsInfo);

            elementsInfo.hierarchyDepth = ComputeDepth(vta);

            return elementsInfo;
        }

        private static int ComputeDepth(VisualTreeAsset vta)
        {
            return ComputeDepthRecursive(vta.visualTree);
        }

        private static int ComputeDepthRecursive(UxmlAsset asset)
        {
            if (asset.childCount == 0)
                return 0;

            var maxSubDepth = 0;

            for (var i = 0; i < asset.childCount; ++i)
            {
                var depth = ComputeDepthRecursive(asset[i]);
                if (depth > maxSubDepth)
                    maxSubDepth = depth;
            }

            return maxSubDepth + 1;
        }
    }
}
