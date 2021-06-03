// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Context menu apply/revert extension hook for object in prefab editing mode.
    /// </summary>
    public interface IApplyRevertPropertyContextMenuItemProvider
    {
        /// <summary>
        /// Called as the context menu is being built to retrieve the method to be called (if any) when the user select the 'revert' menu item.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="revertMethod"></param>
        /// <returns>false if a revert menu item is not meant to be shown</returns>
        bool TryGetRevertMethodForFieldName(SerializedProperty property, out Action<SerializedProperty> revertMethod);

        /// <summary>
        /// Called as the context menu is being built to retrieve the method to be called (if any) when the user select the 'apply' menu item.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="applyMethod"></param>
        /// <returns>false if an apply menu item is not meant to be shown</returns>
        bool TryGetApplyMethodForFieldName(SerializedProperty property, out Action<SerializedProperty> applyMethod);

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        string GetSourceTerm();

        /// <summary>
        ///
        /// </summary>
        /// <param name="comp"></param>
        /// <returns></returns>
        string GetSourceName(Component comp);
    }
}
