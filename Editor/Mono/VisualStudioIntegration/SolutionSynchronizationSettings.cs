// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

namespace UnityEditor.VisualStudioIntegration
{
    interface ISolutionSynchronizationSettings
    {
        int VisualStudioVersion { get; }
        string SolutionTemplate { get; }
        string SolutionProjectEntryTemplate { get; }
        string SolutionProjectConfigurationTemplate { get; }
        string EditorAssemblyPath { get; }
        string EngineAssemblyPath { get; }
        string MonoLibFolder { get; }
        string[] Defines { get; }
        string GetProjectHeaderTemplate(ScriptingLanguage language);
        string GetProjectFooterTemplate(ScriptingLanguage language);
    }

    internal class DefaultSolutionSynchronizationSettings : ISolutionSynchronizationSettings
    {
        public virtual int VisualStudioVersion
        {
            get { return 9; }
        }

        public virtual string SolutionTemplate
        {
            get
            {
                return string.Join(Environment.NewLine, new[]
                {
                    @"",
                    @"Microsoft Visual Studio Solution File, Format Version {0}",
                    @"# Visual Studio {1}",
                    @"{2}",
                    @"Global",
                    @"    GlobalSection(SolutionConfigurationPlatforms) = preSolution",
                    @"        Debug|Any CPU = Debug|Any CPU",
                    @"        Release|Any CPU = Release|Any CPU",
                    @"    EndGlobalSection",
                    @"    GlobalSection(ProjectConfigurationPlatforms) = postSolution",
                    @"{3}",
                    @"    EndGlobalSection",
                    @"    GlobalSection(SolutionProperties) = preSolution",
                    @"        HideSolutionNode = FALSE",
                    @"    EndGlobalSection",
                    @"EndGlobal",
                    @""
                }).Replace("    ", "\t");
            }
        }

        public virtual string SolutionProjectEntryTemplate
        {
            get
            {
                return string.Join(Environment.NewLine, new[]
                {
                    @"Project(""{{{0}}}"") = ""{1}"", ""{2}"", ""{{{3}}}""",
                    @"EndProject"
                }).Replace("    ", "\t");
            }
        }

        public virtual string SolutionProjectConfigurationTemplate
        {
            get
            {
                return string.Join(Environment.NewLine, new[]
                {
                    @"        {{{0}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU",
                    @"        {{{0}}}.Debug|Any CPU.Build.0 = Debug|Any CPU",
                    @"        {{{0}}}.Release|Any CPU.ActiveCfg = Release|Any CPU",
                    @"        {{{0}}}.Release|Any CPU.Build.0 = Release|Any CPU"
                }).Replace("    ", "\t");
            }
        }

        public virtual string GetProjectHeaderTemplate(ScriptingLanguage language)
        {
            var header = new[]
            {
                @"<?xml version=""1.0"" encoding=""utf-8""?>",
                @"<Project ToolsVersion=""{0}"" DefaultTargets=""Build"" xmlns=""{6}"">",
                @"  <PropertyGroup>",
                @"    <LangVersion>{11}</LangVersion>",
                @"    <CscToolPath>{14}</CscToolPath>",
                @"    <CscToolExe>{15}</CscToolExe>",
                @"  </PropertyGroup>",
                @"  <PropertyGroup>",
                @"    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>",
                @"    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>",
                @"    <ProductVersion>{1}</ProductVersion>",
                @"    <SchemaVersion>2.0</SchemaVersion>",
                @"    <RootNamespace>{9}</RootNamespace>",
                @"    <ProjectGuid>{{{2}}}</ProjectGuid>",
                @"    <OutputType>Library</OutputType>",
                @"    <AppDesignerFolder>Properties</AppDesignerFolder>",
                @"    <AssemblyName>{7}</AssemblyName>",
                @"    <TargetFrameworkVersion>{10}</TargetFrameworkVersion>",
                @"    <FileAlignment>512</FileAlignment>",
                @"    <BaseDirectory>{12}</BaseDirectory>",
                @"  </PropertyGroup>",
                @"  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">",
                @"    <DebugSymbols>true</DebugSymbols>",
                @"    <DebugType>full</DebugType>",
                @"    <Optimize>false</Optimize>",
                @"    <OutputPath>{8}</OutputPath>",
                @"    <DefineConstants>{5}</DefineConstants>",
                @"    <ErrorReport>prompt</ErrorReport>",
                @"    <WarningLevel>4</WarningLevel>",
                @"    <NoWarn>0169</NoWarn>",
                @"    <AllowUnsafeBlocks>{13}</AllowUnsafeBlocks>",
                @"  </PropertyGroup>",
                @"  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">",
                @"    <DebugType>pdbonly</DebugType>",
                @"    <Optimize>true</Optimize>",
                @"    <OutputPath>Temp\bin\Release\</OutputPath>",
                @"    <ErrorReport>prompt</ErrorReport>",
                @"    <WarningLevel>4</WarningLevel>",
                @"    <NoWarn>0169</NoWarn>",
                @"    <AllowUnsafeBlocks>{13}</AllowUnsafeBlocks>",
                @"  </PropertyGroup>",
            };

            var forceExplicitReferences = new string[]
            {
                @"  <PropertyGroup>",
                @"    <NoConfig>true</NoConfig>",
                @"    <NoStdLib>true</NoStdLib>",
                @"    <AddAdditionalExplicitAssemblyReferences>false</AddAdditionalExplicitAssemblyReferences>",
                @"    <ImplicitlyExpandNETStandardFacades>false</ImplicitlyExpandNETStandardFacades>",
                @"    <ImplicitlyExpandDesignTimeFacades>false</ImplicitlyExpandDesignTimeFacades>",
                @"  </PropertyGroup>",
            };

            var systemReferences = new string[]
            {
                @"  <ItemGroup>",
                @"    <Reference Include=""System"" />",
                @"    <Reference Include=""System.Xml"" />",
                @"    <Reference Include=""System.Core"" />",
                @"    <Reference Include=""System.Runtime.Serialization"" />",
                @"    <Reference Include=""System.Xml.Linq"" />",
                @"  </ItemGroup>"
            };

            var footer = new string[]
            {
                @"  <ItemGroup>",
                @""
            };

            string[] text;

            if (language == ScriptingLanguage.CSharp)
                text = header.Concat(forceExplicitReferences).Concat(footer).ToArray();
            else
                text = header.Concat(systemReferences).Concat(footer).ToArray();

            return string.Join(Environment.NewLine, text);
        }

        public virtual string GetProjectFooterTemplate(ScriptingLanguage language)
        {
            return string.Join(Environment.NewLine, new[]
            {
                @"  </ItemGroup>",
                @"  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />",
                @"  <!-- To modify your build process, add your task inside one of the targets below and uncomment it.",
                @"       Other similar extension points exist, see Microsoft.Common.targets.",
                @"  <Target Name=""BeforeBuild"">",
                @"  </Target>",
                @"  <Target Name=""AfterBuild"">",
                @"  </Target>",
                @"  -->",
                @"</Project>",
                @""
            });
        }

        public virtual string EditorAssemblyPath
        {
            get { return "/Managed/UnityEditor.dll"; }
        }

        public virtual string EngineAssemblyPath
        {
            get { return "/Managed/UnityEngine.dll"; }
        }

        public virtual string MonoLibFolder
        {
            get { return FrameworksPath() + "/Mono/lib/mono/unity/"; }
        }

        public virtual string[] Defines
        {
            get { return new string[0]; }
        }

        protected virtual string FrameworksPath()
        {
            return string.Empty;
        }
    }
}
