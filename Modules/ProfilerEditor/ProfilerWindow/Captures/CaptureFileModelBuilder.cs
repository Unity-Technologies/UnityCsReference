// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;

namespace Unity.Profiling.Editor.UI
{
    internal class CaptureFileModelBuilder
    {
        string m_FileName;

        public CaptureFileModelBuilder(string fileName)
        {
            m_FileName = fileName;
        }

        public CaptureFileModel Build()
        {
            var creationTime = File.GetLastWriteTime(m_FileName);
            var fakeSessionID = creationTime.Year * 10000 + creationTime.Month * 100 + creationTime.Day;

            // TODO: Proper metadata support.
            return new CaptureFileModel(
                Path.GetFileNameWithoutExtension(m_FileName),
                m_FileName,
                "", //CaptureMetadata.ProductName,
                "", //CaptureMetadata.Description,
                (uint)fakeSessionID, //CaptureMetadata.SessionGUID,
                creationTime, //timestamp,
                RuntimePlatform.OSXPlayer, //runtimePlatform,
                false, //editorPlatform,
                ""); //CaptureMetadata.UnityVersion,
        }
    }
}
