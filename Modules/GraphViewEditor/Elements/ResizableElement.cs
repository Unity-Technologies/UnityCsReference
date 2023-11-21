// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    [Flags]
    public enum ResizerDirection
    {
        Top = 1 << 0,
        Bottom = 1 << 1,
        Left = 1 << 2,
        Right = 1 << 3,
    }

    public class ResizableElement : VisualElement
    {
        [ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new ResizableElement();
        }

        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<ResizableElement> {}

        public ResizableElement() : this("UXML/GraphView/Resizable.uxml")
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("resizableElement");
        }

        public ResizableElement(string uiFile)
        {
            var tpl = Resources.Load<VisualTreeAsset>(uiFile);
            if (tpl == null)
                tpl = EditorGUIUtility.Load(uiFile) as VisualTreeAsset;

            var sheet = EditorGUIUtility.Load("StyleSheets/GraphView/Resizable.uss") as StyleSheet;
            styleSheets.Add(sheet);

            tpl.CloneTree(this);

            foreach (ResizerDirection value in System.Enum.GetValues(typeof(ResizerDirection)))
            {
                VisualElement resizer = this.Q(value.ToString().ToLowerInvariant() + "-resize");
                if (resizer != null)
                    resizer.AddManipulator(new ElementResizer(this, value));
            }

            foreach (ResizerDirection vertical in new[] { ResizerDirection.Top, ResizerDirection.Bottom })
                foreach (ResizerDirection horizontal in new[] { ResizerDirection.Left, ResizerDirection.Right })
                {
                    VisualElement resizer = this.Q(vertical.ToString().ToLowerInvariant() + "-" + horizontal.ToString().ToLowerInvariant() + "-resize");
                    if (resizer != null)
                        resizer.AddManipulator(new ElementResizer(this, vertical | horizontal));
                }
        }
    }
}
