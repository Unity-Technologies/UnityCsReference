// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
    [System.Serializable]
    internal class GameViewSizeGroup
    {
        [System.NonSerialized]
        private List<GameViewSize> m_Builtin = new List<GameViewSize>();

        [SerializeField]
        private List<GameViewSize> m_Custom = new List<GameViewSize>();

        // Builtin sizes first then custom sizes
        public GameViewSize GetGameViewSize(int index)
        {
            if (index < m_Builtin.Count)
                return m_Builtin[index];

            index -= m_Builtin.Count;

            if (index >= 0 && index < m_Custom.Count)
                return m_Custom[index];

            Debug.LogError("Invalid index " + (index + m_Builtin.Count) +  " " + m_Builtin.Count + " " + m_Custom.Count);
            return new GameViewSize(GameViewSizeType.AspectRatio, 0, 0, "");
        }

        public string[] GetDisplayTexts()
        {
            List<string> displayList = new List<string>();
            foreach (GameViewSize size in m_Builtin)
                displayList.Add(size.displayText);
            foreach (GameViewSize size in m_Custom)
                displayList.Add(size.displayText);
            return displayList.ToArray();
        }

        public int GetTotalCount()
        {
            return m_Builtin.Count + m_Custom.Count;
        }

        public int GetBuiltinCount()
        {
            return m_Builtin.Count;
        }

        public int GetCustomCount()
        {
            return m_Custom.Count;
        }

        public void AddBuiltinSizes(params GameViewSize[] sizes)
        {
            for (int i = 0; i < sizes.Length; i++)
                AddBuiltinSize(sizes[i]);
        }

        public void AddBuiltinSize(GameViewSize size)
        {
            m_Builtin.Add(size);
            GameViewSizes.instance.Changed();
        }

        public void AddCustomSizes(params GameViewSize[] sizes)
        {
            for (int i = 0; i < sizes.Length; i++)
                AddCustomSize(sizes[i]);
        }

        public void AddCustomSize(GameViewSize size)
        {
            m_Custom.Add(size);
            GameViewSizes.instance.Changed();
        }

        public void RemoveCustomSize(int index)
        {
            int customIndex = TotalIndexToCustomIndex(index);
            if (customIndex >= 0 && customIndex < m_Custom.Count)
            {
                m_Custom.RemoveAt(customIndex);
                GameViewSizes.instance.Changed();
            }
            else
            {
                Debug.LogError("Invalid index " + index + " " + m_Builtin.Count + " " + m_Custom.Count);
            }
        }

        public bool IsCustomSize(int index)
        {
            if (index < m_Builtin.Count)
                return false;
            return true;
        }

        public int TotalIndexToCustomIndex(int index)
        {
            return index - m_Builtin.Count;
        }

        public int IndexOf(GameViewSize view)
        {
            int index = m_Builtin.IndexOf(view);
            if (index >= 0)
                return index;

            return m_Custom.IndexOf(view);
        }
    }
}
