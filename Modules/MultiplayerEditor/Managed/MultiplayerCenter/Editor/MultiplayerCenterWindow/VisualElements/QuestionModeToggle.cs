// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.Center.Editor
{
    /// <summary>
    /// Visual element that switches between question mode and answer mode.
    /// </summary>
    /// <remarks>
    /// Using a custom visual element to switch the styling allows
    /// showing the <see cref="QuestionMode"/> property in UIBuilder
    /// and makes editing the uxml easier.
    /// </remarks>
    [UxmlElement]
    partial class QuestionModeToggle : VisualElement
    {
        [UxmlAttribute, CreateProperty]
        public bool QuestionMode
        {
            get => ClassListContains(StyleClasses.QuestionMode);
            set => EnableInClassList(StyleClasses.QuestionMode,value);
        }
    }
}
