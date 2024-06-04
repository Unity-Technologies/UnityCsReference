// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Overlays
{
    [FilePath("Overlays/CanvasesSaveData.asset", FilePathAttribute.Location.PreferencesFolder)]
    class OverlayCanvasesData : ScriptableSingleton<OverlayCanvasesData>, ISerializationCallbackReceiver
    {
        [Serializable]
        struct WindowToCanvasDataPair
        {
            public WindowToCanvasDataPair(string windowType, List<SaveData> canvasSaveData)
            {
                m_WindowType = windowType;
                m_CanvasSaveData = canvasSaveData;
            }

            public string m_WindowType;
            public List<SaveData> m_CanvasSaveData = new();
        }
        
        [SerializeField]
        List<WindowToCanvasDataPair> m_CanvasesSaveData = new();
        Dictionary<string, List<SaveData>> m_WindowToCanvasSaveData = new();
        
        [Serializable]
        struct WindowToCanvasPair
        {
            public WindowToCanvasPair(string windowType, OverlayCanvas canvas)
            {
                m_WindowType = windowType;
                m_Canvas = canvas;
            }

            public string m_WindowType;
            public OverlayCanvas m_Canvas;
        }
        
        // [SerializeField] for the list below is intentionally omitted.
        // Since List is serializable, it will survive domain reload as a ScriptableSingleton field but not serialize to asset.
        // This allows to track last active canvas throughout a session and NOT have this data restored on next session.
        List<WindowToCanvasPair> m_WindowToLastActiveCanvasList = new();
        
        Dictionary<string, OverlayCanvas> m_WindowToLastActiveCanvasMap = new();

        public void OnBeforeSerialize()
        {
            // Output window to canvas SaveData map to a list
            if (m_CanvasesSaveData == null)
                m_CanvasesSaveData = new();
            else 
                m_CanvasesSaveData.Clear();

            foreach (var dataPair in m_WindowToCanvasSaveData)
                m_CanvasesSaveData.Add(new WindowToCanvasDataPair(dataPair.Key, dataPair.Value));
            
            // Output window to last active canvas map to a list
            if (m_WindowToLastActiveCanvasList == null)
                m_WindowToLastActiveCanvasList = new();
            else 
                m_WindowToLastActiveCanvasList.Clear();
            
            foreach (var dataPair in m_WindowToLastActiveCanvasMap)
                m_WindowToLastActiveCanvasList.Add(new WindowToCanvasPair(dataPair.Key, dataPair.Value));
        }
        
        public void OnAfterDeserialize()
        {
            // Restore window to canvas save datas map
            if (m_WindowToCanvasSaveData == null)
                m_WindowToCanvasSaveData = new();
            else 
                m_WindowToCanvasSaveData.Clear();

            if (m_CanvasesSaveData == null)
            {
                m_CanvasesSaveData = new();
                return;
            }

            for (int i = 0; i < m_CanvasesSaveData.Count; ++i)
            {
                var dataPair = m_CanvasesSaveData[i];
                m_WindowToCanvasSaveData.Add(dataPair.m_WindowType, dataPair.m_CanvasSaveData);
            }
            
            // Restore window to last active canvas map
            if (m_WindowToLastActiveCanvasMap == null)
                m_WindowToLastActiveCanvasMap = new();
            else 
                m_WindowToLastActiveCanvasMap.Clear();

            if (m_WindowToLastActiveCanvasList == null)
            {
                m_WindowToLastActiveCanvasList = new();
                return;
            }
            
            for (int i = 0; i < m_WindowToLastActiveCanvasList.Count; ++i)
            {
                var dataPair = m_WindowToLastActiveCanvasList[i];
                m_WindowToLastActiveCanvasMap.Add(dataPair.m_WindowType, dataPair.m_Canvas);
            }
        }
        
        void OnDisable()
        {
            Save();
        }

        public void AddAndSaveCanvasData(EditorWindow containerWindow, List<SaveData> canvasSaveData)
        {
            var windowType = containerWindow.GetType().AssemblyQualifiedName;
            if (!m_WindowToCanvasSaveData.TryAdd(windowType, canvasSaveData))
                m_WindowToCanvasSaveData[windowType] = canvasSaveData;

            Save();
        }

        public bool GetCanvasSaveData(EditorWindow containerWindow, out List<SaveData> canvasSaveData)
        {
            if (m_WindowToCanvasSaveData.TryGetValue(containerWindow.GetType().AssemblyQualifiedName, out canvasSaveData) &&
                canvasSaveData != null)
                return true;

            return false;
        }
        
        void Save()
        {
            Save(true);
        }

        public bool TryGetLastActiveCanvasForWindowType(EditorWindow window, out OverlayCanvas lastActiveCanvas)
        {
            return m_WindowToLastActiveCanvasMap.TryGetValue(window.GetType().AssemblyQualifiedName, out lastActiveCanvas);
        }

        public void SetLastActiveCanvasForWindowType(OverlayCanvas canvas)
        {
            var windowType = canvas.containerWindow.GetType().AssemblyQualifiedName;
            if (!m_WindowToLastActiveCanvasMap.TryAdd(windowType, canvas))
                m_WindowToLastActiveCanvasMap[windowType] = canvas;
        }
    }
}
