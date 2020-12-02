using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface used to implement group management for <see cref="IGroupBox"/>.
    /// </summary>
    internal interface IGroupManager
    {
        IGroupBoxOption GetSelectedOption();
        void OnOptionSelectionChanged(IGroupBoxOption selectedOption);
        void RegisterOption(IGroupBoxOption option);
        void UnregisterOption(IGroupBoxOption option);
    }

    /// <summary>
    /// This is the default implementation of group management for <see cref="IGroupBox"/>.
    /// It is by default used on <see cref="IPanel"/> and <see cref="GroupBox"/>.
    /// A different implementation can be provided by using <see cref="IGroupBox{T}"/> instead.
    /// </summary>
    internal class DefaultGroupManager : IGroupManager
    {
        List<IGroupBoxOption> m_GroupOptions = new List<IGroupBoxOption>();
        IGroupBoxOption m_SelectedOption;

        public IGroupBoxOption GetSelectedOption()
        {
            return m_SelectedOption;
        }

        public void OnOptionSelectionChanged(IGroupBoxOption selectedOption)
        {
            if (m_SelectedOption == selectedOption)
                return;

            m_SelectedOption = selectedOption;

            foreach (var option in m_GroupOptions)
            {
                option.SetSelected(option == m_SelectedOption);
            }
        }

        public void RegisterOption(IGroupBoxOption option)
        {
            if (!m_GroupOptions.Contains(option))
            {
                m_GroupOptions.Add(option);
            }
        }

        public void UnregisterOption(IGroupBoxOption option)
        {
            m_GroupOptions.Remove(option);
        }
    }
}
