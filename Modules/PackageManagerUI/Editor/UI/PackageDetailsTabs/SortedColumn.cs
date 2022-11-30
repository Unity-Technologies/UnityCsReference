// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal;

// This class is being used to replace the SortColumnDescription class that is owned by UIToolkit. We need it to be serialized
// to keep the sorting in the imported assets tab through domain reload
[Serializable]
internal class SortedColumn
{
    public int columnIndex;
    public SortDirection sortDirection;

    public SortedColumn(int columnIndex, SortDirection sortDirection)
    {
        this.columnIndex = columnIndex;
        this.sortDirection = sortDirection;
    }

    public SortedColumn(SortColumnDescription sortColumnDescription)
    {
        columnIndex = sortColumnDescription.column.index;
        sortDirection = sortColumnDescription.direction;
    }
}
