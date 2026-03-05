// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageReleaseDetailsItem : VisualElement
    {
        public PackageReleaseDetailsItem(string versionString, DateTime? publishedDate, bool isCurrent = false, string releaseNotes = "")
        {
            AddToClassList("release");

            var versionAndReleaseContainer = new VisualElement { name = "versionAndReleaseContainer" };
            Add(versionAndReleaseContainer);

            versionAndReleaseContainer.Add(new SelectableLabel
            {
                name = "version",
                focusable = true,
                text = versionString
            });

            versionAndReleaseContainer.Add(new Label
            {
                name = "current",
                text = isCurrent ? L10n.Tr("Current") : string.Empty
            });

            versionAndReleaseContainer.Add(new SelectableLabel
            {
                name = "releaseDate",
                focusable = true,
                text = publishedDate?.ToString(L10n.Tr("MMMM dd, yyyy"), CultureInfo.CreateSpecificCulture("en-US")) != null ?
                    string.Format(L10n.Tr("released on {0}"), publishedDate?.ToString(L10n.Tr("MMMM dd, yyyy"), CultureInfo.CreateSpecificCulture("en-US"))) :
                    string.Empty
            });

            if (string.IsNullOrEmpty(releaseNotes))
                return;

            Add(new SelectableLabel
            {
                name = "releaseNotes",
                focusable = true,
                text = releaseNotes
            });
        }
    }
}
