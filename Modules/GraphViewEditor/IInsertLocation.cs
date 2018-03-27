// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal struct InsertInfo
    {
        public static readonly InsertInfo nil = new InsertInfo { target = null, index = -1, localPosition = Vector2.zero };
        public VisualElement target;
        public int index;
        public Vector2 localPosition;
    }

    internal interface IInsertLocation
    {
        void GetInsertInfo(Vector2 worldPosition, out InsertInfo insertInfo);
    }
}
