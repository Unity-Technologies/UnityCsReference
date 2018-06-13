// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    public class VisualContainer : VisualElement
    {
        // The VisualContainerFactory creates VisualElements.
        // The <VisualContainer> can substitutes for <VisualElement>
        public new class UxmlFactory : VisualElement.UxmlFactory
        {
            public override string uxmlName
            {
                get { return typeof(VisualContainer).Name; }
            }
            public override string uxmlNamespace
            {
                get { return typeof(VisualContainer).Namespace; }
            }
            public override string uxmlQualifiedName
            {
                get { return typeof(VisualContainer).FullName; }
            }

            public override string substituteForTypeName
            {
                get { return typeof(VisualElement).Name; }
            }

            public override string substituteForTypeNamespace
            {
                get { return typeof(VisualElement).Namespace; }
            }
            public override string substituteForTypeQualifiedName
            {
                get { return typeof(VisualElement).FullName; }
            }
        }

        [Obsolete("VisualContainer.AddChild will be removed. Use VisualElement.Add or VisualElement.shadow.Add instead", false)]
        public void AddChild(VisualElement child)
        {
            shadow.Add(child);
        }

        [Obsolete("VisualContainer.InsertChild will be removed. Use VisualElement.Insert or VisualElement.shadow.Insert instead", false)]
        public void InsertChild(int index, VisualElement child)
        {
            shadow.Insert(index, child);
        }

        [Obsolete("VisualContainer.RemoveChild will be removed. Use VisualElement.Remove or VisualElement.shadow.Remove instead", false)]
        public void RemoveChild(VisualElement child)
        {
            shadow.Remove(child);
        }

        [Obsolete("VisualContainer.RemoveChildAt will be removed. Use VisualElement.RemoveAt or VisualElement.shadow.RemoveAt instead", false)]
        public void RemoveChildAt(int index)
        {
            shadow.RemoveAt(index);
        }

        [Obsolete("VisualContainer.ClearChildren will be removed. Use VisualElement.Clear or VisualElement.shadow.Clear instead", false)]
        public void ClearChildren()
        {
            shadow.Clear();
        }

        [Obsolete("VisualContainer.GetChildAt will be removed. Use VisualElement.ElementAt or VisualElement.shadow.ElementAt instead", false)]
        public VisualElement GetChildAt(int index)
        {
            return shadow[index];
        }
    }
}
