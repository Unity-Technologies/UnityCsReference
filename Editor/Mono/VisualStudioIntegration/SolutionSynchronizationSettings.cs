// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
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
                return string.Join("\r\n", new[] {
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
                    @"{4}",
                    @"EndGlobal",
                    @""
                }).Replace("    ", "\t");
            }
        }

        public virtual string SolutionProjectEntryTemplate
        {
            get
            {
                return string.Join("\r\n", new[] {
                    @"Project(""{{{0}}}"") = ""{1}"", ""{2}"", ""{{{3}}}""",
                    @"EndProject"
                }).Replace("    ", "\t");
            }
        }

        public virtual string SolutionProjectConfigurationTemplate
        {
            get
            {
                return string.Join("\r\n", new[] {
                    @"        {{{0}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU",
                    @"        {{{0}}}.Debug|Any CPU.Build.0 = Debug|Any CPU",
                    @"        {{{0}}}.Release|Any CPU.ActiveCfg = Release|Any CPU",
                    @"        {{{0}}}.Release|Any CPU.Build.0 = Release|Any CPU"
                }).Replace("    ", "\t");
            }
        }

        public virtual string GetProjectHeaderTemplate(ScriptingLanguage language)
        {
            return string.Join("\r\n", new[] {
                @"<?xml version=""1.0"" encoding=""utf-8""?>",
                @"<Project ToolsVersion=""{0}"" DefaultTargets=""Build"" xmlns=""{6}"">",
                @"  <PropertyGroup>",
                @"    <LangVersion>{10}</LangVersion>",
                @"  </PropertyGroup>",
                @"  <PropertyGroup>",
                @"    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>",
                @"    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>",
                @"    <ProductVersion>{1}</ProductVersion>",
                @"    <SchemaVersion>2.0</SchemaVersion>",
                @"    <RootNamespace>{8}</RootNamespace>",
                @"    <ProjectGuid>{{{2}}}</ProjectGuid>",
                @"    <OutputType>Library</OutputType>",
                @"    <AppDesignerFolder>Properties</AppDesignerFolder>",
                @"    <AssemblyName>{7}</AssemblyName>",
                @"    <TargetFrameworkVersion>{9}</TargetFrameworkVersion>",
                @"    <FileAlignment>512</FileAlignment>",
                @"    <BaseDirectory>Assets</BaseDirectory>",
                @"  </PropertyGroup>",
                @"  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">",
                @"    <DebugSymbols>true</DebugSymbols>",
                @"    <DebugType>full</DebugType>",
                @"    <Optimize>false</Optimize>",
                @"    <OutputPath>Temp\bin\Debug\</OutputPath>",
                @"    <DefineConstants>{5}</DefineConstants>",
                @"    <ErrorReport>prompt</ErrorReport>",
                @"    <WarningLevel>4</WarningLevel>",
                @"    <NoWarn>0169</NoWarn>",
                @"    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>",
                @"  </PropertyGroup>",
                @"  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">",
                @"    <DebugType>pdbonly</DebugType>",
                @"    <Optimize>true</Optimize>",
                @"    <OutputPath>Temp\bin\Release\</OutputPath>",
                @"    <ErrorReport>prompt</ErrorReport>",
                @"    <WarningLevel>4</WarningLevel>",
                @"    <NoWarn>0169</NoWarn>",
                @"    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>",
                @"  </PropertyGroup>",
                @"  <ItemGroup>",
                @"    <Reference Include=""System"" />",
                @"    <Reference Include=""System.XML"" />",
                @"    <Reference Include=""System.Core"" />",
                @"    <Reference Include=""System.Runtime.Serialization"" />",
                @"    <Reference Include=""System.Xml.Linq"" />",
                @"    <Reference Include=""UnityEngine"">",
                @"      <HintPath>{3}</HintPath>",
                @"    </Reference>",
                @"    <Reference Include=""UnityEditor"">",
                @"      <HintPath>{4}</HintPath>",
                @"    </Reference>",
                @"  </ItemGroup>",
                @"  <ItemGroup>",
                @""
            });
        }

        public virtual string GetProjectFooterTemplate(ScriptingLanguage language)
        {
            return string.Join("\r\n", new[] {
                @"  </ItemGroup>",
                @"  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />",
                @"  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. ",
                @"       Other similar extension points exist, see Microsoft.Common.targets.",
                @"  <Target Name=""BeforeBuild"">",
                @"  </Target>",
                @"  <Target Name=""AfterBuild"">",
                @"  </Target>",
                @"  -->",
                @"  {0}",
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

    static class VSCodeTemplates
    {
        public static string SettingsJson = @"{
    ""files.exclude"":
    {
        ""**/.DS_Store"":true,
        ""**/.git"":true,
        ""**/.gitignore"":true,
        ""**/.gitmodules"":true,
        ""**/*.booproj"":true,
        ""**/*.pidb"":true,
        ""**/*.suo"":true,
        ""**/*.user"":true,
        ""**/*.userprefs"":true,
        ""**/*.unityproj"":true,
        ""**/*.dll"":true,
        ""**/*.exe"":true,
        ""**/*.pdf"":true,
        ""**/*.mid"":true,
        ""**/*.midi"":true,
        ""**/*.wav"":true,
        ""**/*.gif"":true,
        ""**/*.ico"":true,
        ""**/*.jpg"":true,
        ""**/*.jpeg"":true,
        ""**/*.png"":true,
        ""**/*.psd"":true,
        ""**/*.tga"":true,
        ""**/*.tif"":true,
        ""**/*.tiff"":true,
        ""**/*.3ds"":true,
        ""**/*.3DS"":true,
        ""**/*.fbx"":true,
        ""**/*.FBX"":true,
        ""**/*.lxo"":true,
        ""**/*.LXO"":true,
        ""**/*.ma"":true,
        ""**/*.MA"":true,
        ""**/*.obj"":true,
        ""**/*.OBJ"":true,
        ""**/*.asset"":true,
        ""**/*.cubemap"":true,
        ""**/*.flare"":true,
        ""**/*.mat"":true,
        ""**/*.meta"":true,
        ""**/*.prefab"":true,
        ""**/*.unity"":true,
        ""build/"":true,
        ""Build/"":true,
        ""Library/"":true,
        ""library/"":true,
        ""obj/"":true,
        ""Obj/"":true,
        ""ProjectSettings/"":true,
        ""temp/"":true,
        ""Temp/"":true
    }
}";
    }
}
