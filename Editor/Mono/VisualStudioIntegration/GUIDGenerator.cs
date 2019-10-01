// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.VisualStudioIntegration
{
    interface IGUIDGenerator
    {
        string ProjectGuid(string projectName, string assemblyName);
        string SolutionGuid(string projectName, string extension);
    }

    class GUIDProvider : IGUIDGenerator
    {
        public string ProjectGuid(string projectName, string assemblyName)
        {
            return SolutionGuidGenerator.GuidForProject(projectName + assemblyName);
        }

        public string SolutionGuid(string projectName, string extension)
        {
            return SolutionGuidGenerator.GuidForSolution(projectName, extension); // GetExtensionOfSourceFiles(assembly.sourceFiles)
        }
    }
}
