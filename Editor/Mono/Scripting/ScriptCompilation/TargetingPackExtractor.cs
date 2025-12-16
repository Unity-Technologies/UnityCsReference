// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System.Collections.Generic;
using System.Xml.Linq;
using NiceIO;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal class TargetingPackExtractor
    {
        internal static (NPath[] referenceAssemblies, NPath[] analyzers) GetAssembliesFromFrameworkList(NPath rootDirectory)
        {
            var frameworkList = XDocument.Load(rootDirectory.Combine("data/FrameworkList.xml").ToString());
            List<NPath> referenceAssemblies = new();
            List<NPath> analyzers = new();
            foreach (var fileElem in frameworkList.Root.Elements("File"))
            {
                var type = fileElem.Attribute("Type")?.Value;
                var path = fileElem.Attribute("Path")?.Value;
                if (type == null || path == null)
                {
                    continue;
                }
                var asm = rootDirectory.Combine(path);
                if (type == "Managed")
                {
                    referenceAssemblies.Add(asm);
                }
                if (type == "Analyzer")
                {
                    analyzers.Add(asm);
                }
            }
            return (referenceAssemblies.ToArray(), analyzers.ToArray());
        }
    }
}
