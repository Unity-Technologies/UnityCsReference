// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine.Rendering;

namespace UnityEditor
{
    internal class LookDevEnvironmentLibrary
        : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private List<CubemapInfo>      m_HDRIList = new List<CubemapInfo>();
        [SerializeField] private List<CubemapInfo>      m_SerialShadowMapHDRIList = new List<CubemapInfo>(); // Dedicated to save shadow cubemap info for serialization purpose

        private LookDevView m_LookDevView = null;
        private bool m_Dirty = false;

        public bool dirty
        {
            get { return m_Dirty; }
            set { m_Dirty = value; }
        }

        public List<CubemapInfo> hdriList
        {
            get { return m_HDRIList; }
        }

        public int hdriCount
        {
            get { return hdriList.Count; }
        }

        public void InsertHDRI(Cubemap cubemap)
        {
            InsertHDRI(cubemap, -1);
        }

        // If insertionIndex is -1 it mean we insert at the end of the list
        public void InsertHDRI(Cubemap cubemap, int insertionIndex)
        {
            Undo.RecordObject(m_LookDevView.envLibrary, "Insert HDRI");
            Undo.RecordObject(m_LookDevView.config, "Insert HDRI");

            // Handle cubemap index remapping for both context. Simply do it brute force in all cases.
            // Save the cubemap info before any modification to m_HDRIList.
            // Also if we are inserting m_DefaultHDRI, it mean we have an empty m_HDRIList
            Cubemap cubemap0 = null;
            Cubemap cubemap1 = null;

            if (cubemap == LookDevResources.m_DefaultHDRI)
            {
                cubemap0 = LookDevResources.m_DefaultHDRI;
                cubemap1 = LookDevResources.m_DefaultHDRI;
            }
            else
            {
                cubemap0 = m_HDRIList[m_LookDevView.config.lookDevContexts[0].currentHDRIIndex].cubemap;
                cubemap1 = m_HDRIList[m_LookDevView.config.lookDevContexts[1].currentHDRIIndex].cubemap;
            }

            // Check if the cubemap already exist
            int iIndex = m_HDRIList.FindIndex(x => x.cubemap == cubemap);

            // Create cubemap if it doesn't exist
            if (iIndex == -1)
            {
                m_Dirty = true;

                CubemapInfo newInfo = null;

                // Check if the cubemap exist but as a shadow cubemap only
                // in this case we don't recreate the CubemapInfo, but we still insert it as a new one.
                for (int i = 0; i < m_HDRIList.Count; ++i)
                {
                    if (m_HDRIList[i].cubemapShadowInfo.cubemap == cubemap)
                    {
                        newInfo = m_HDRIList[i].cubemapShadowInfo;
                        // Prevent recursion with shadow cubemap info
                        newInfo.SetCubemapShadowInfo(newInfo);
                        break;
                    }
                }

                if (newInfo == null)
                {
                    newInfo = new CubemapInfo();
                    newInfo.cubemap = cubemap;
                    newInfo.ambientProbe.Clear();
                    newInfo.alreadyComputed = false;
                    newInfo.SetCubemapShadowInfo(newInfo); // By default we use the same cubemap for the version without sun.
                }

                int newCubemapIndex = m_HDRIList.Count;
                // Add the cubemap to the specified location or last if no location provide
                m_HDRIList.Insert(insertionIndex == -1 ? newCubemapIndex : insertionIndex, newInfo);

                // When inserting the default HDRI the first time, the lookdev env is not yet ready.
                // But as we default the latlong light position of ShadowInfo to brightest location of default HDRI this is not a problem to not call the function.
                if (newInfo.cubemap != LookDevResources.m_DefaultHDRI)
                    LookDevResources.UpdateShadowInfoWithBrightestSpot(newInfo);
            }

            // If we haven't inserted at end of the list, if it is not a new cubemap and if we do not insert at the same place, we need to shift current cubemap position in the list
            if (iIndex != insertionIndex && iIndex != -1 && insertionIndex != -1)
            {
                // Get cubemap info before modifying m_LookDevSetup.m_HDRIList;
                CubemapInfo infos = m_HDRIList[iIndex];

                m_HDRIList.RemoveAt(iIndex);
                // If we insert after the removed cubemap we need to increase the index
                m_HDRIList.Insert(iIndex > insertionIndex ? insertionIndex : insertionIndex - 1, infos);
            }

            m_LookDevView.config.lookDevContexts[0].UpdateProperty(LookDevProperty.HDRI, m_HDRIList.FindIndex(x => x.cubemap == cubemap0));
            m_LookDevView.config.lookDevContexts[1].UpdateProperty(LookDevProperty.HDRI, m_HDRIList.FindIndex(x => x.cubemap == cubemap1));

            m_LookDevView.Repaint();
        }

        public bool RemoveHDRI(Cubemap cubemap)
        {
            if (cubemap != null)
            {
                Undo.RecordObject(m_LookDevView.envLibrary, "Remove HDRI");
                Undo.RecordObject(m_LookDevView.config, "Remove HDRI");
            }

            if (cubemap == LookDevResources.m_DefaultHDRI)
            {
                Debug.LogWarning("Cannot remove default HDRI from the library");
                return false;
            }

            int iIndex = m_HDRIList.FindIndex(x => x.cubemap == cubemap);
            if (iIndex != -1)
            {
                Cubemap cubemap0 = m_HDRIList[m_LookDevView.config.lookDevContexts[0].currentHDRIIndex].cubemap;
                Cubemap cubemap1 = m_HDRIList[m_LookDevView.config.lookDevContexts[1].currentHDRIIndex].cubemap;

                m_HDRIList.RemoveAt(iIndex);

                int defaultIndex = m_HDRIList.Count == 0 ? -1 : 0;

                // If not the one removed, restore the right indices for both views
                m_LookDevView.config.lookDevContexts[0].UpdateProperty(LookDevProperty.HDRI, cubemap0 == cubemap ? defaultIndex : m_HDRIList.FindIndex(x => x.cubemap == cubemap0));
                m_LookDevView.config.lookDevContexts[1].UpdateProperty(LookDevProperty.HDRI, cubemap1 == cubemap ? defaultIndex : m_HDRIList.FindIndex(x => x.cubemap == cubemap1));

                m_LookDevView.Repaint();
                m_Dirty = true;
                return true;
            }

            return false;
        }

        public void CleanupDeletedHDRI()
        {
            while (RemoveHDRI(null))
            {
                // When we suppress HDRI we will have null reference, so keep the list clean and delete in the list
            }
        }

        ShadowInfo GetCurrentShadowInfo()
        {
            return m_HDRIList[m_LookDevView.config.lookDevContexts[(int)m_LookDevView.config.currentEditionContext].currentHDRIIndex].shadowInfo;
        }

        public void SetLookDevView(LookDevView lookDevView)
        {
            m_LookDevView = lookDevView;
        }

        public void OnBeforeSerialize()
        {
            m_SerialShadowMapHDRIList.Clear();

            // We need to 'convert' all shadow cubemap to index before saving, any shadow cubemap without matching HDRI in the main list
            // will be added to the HDRI list for serialization.
            for (int i = 0; i < m_HDRIList.Count; ++i)
            {
                CubemapInfo shadowCubemapInfo = m_HDRIList[i].cubemapShadowInfo;

                // Check if we have already added it to shadow cubemap list
                m_HDRIList[i].serialIndexMain = m_HDRIList.FindIndex(x => x == shadowCubemapInfo);
                if (m_HDRIList[i].serialIndexMain == -1)
                {
                    m_HDRIList[i].serialIndexShadow = m_SerialShadowMapHDRIList.FindIndex(x => x == shadowCubemapInfo);
                    if (m_HDRIList[i].serialIndexShadow == -1)
                    {
                        m_SerialShadowMapHDRIList.Add(shadowCubemapInfo);
                        m_HDRIList[i].serialIndexShadow = m_SerialShadowMapHDRIList.Count - 1;
                    }
                }
            }
        }

        public void OnAfterDeserialize()
        {
            for (int i = 0; i < m_HDRIList.Count; ++i)
            {
                if (m_HDRIList[i].serialIndexMain != -1)
                {
                    m_HDRIList[i].cubemapShadowInfo = m_HDRIList[hdriList[i].serialIndexMain];
                }
                else
                {
                    m_HDRIList[i].cubemapShadowInfo = m_SerialShadowMapHDRIList[m_HDRIList[i].serialIndexShadow];
                }
            }
        }
    }

    [CustomEditor(typeof(LookDevEnvironmentLibrary))]
    internal class LookDevEnvironmentLibraryInspector : AssetImporterEditor
    {
        // We don't want users to edit these in the inspector
        public override void OnInspectorGUI()
        {
        }
    }
}
