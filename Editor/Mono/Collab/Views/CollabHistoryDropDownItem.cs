// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Collaboration
{
    internal class CollabHistoryDropDownItem : VisualElement
    {
        public CollabHistoryDropDownItem(string path, string action)
        {
            var fileName = Path.GetFileName(path);
            var folderName = Path.GetDirectoryName(path);
            var fileIcon = GetIconElement(action, fileName);
            var metaContainer = new VisualElement();
            var fileNameLabel = new Label
            {
                name = "FileName",
                text = String.IsNullOrEmpty(fileName) ? folderName : fileName
            };
            var filePathLabel = new Label
            {
                name = "FilePath",
                text = path
            };
            metaContainer.Add(fileNameLabel);
            metaContainer.Add(filePathLabel);
            Add(fileIcon);
            Add(metaContainer);
        }

        private Image GetIconElement(string action, string fileName)
        {
            var prefix = String.IsNullOrEmpty(fileName) ? "Folder" : "File";
            var actionName = action.First().ToString().ToUpper() + action.Substring(1);
            var iconElement = new Image
            {
                name = "FileIcon",
                image = EditorGUIUtility.LoadIcon("Icons/Collab." + prefix + actionName + ".png")
            };
            return iconElement;
        }
    }
}
