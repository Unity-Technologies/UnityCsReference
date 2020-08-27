// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Compilation;
using UnityEditor.Utils;
using UnityEngine;

namespace UnityEditor.Scripting.Compilers
{
    internal static class MicrosoftResponseFileParser
    {
        static readonly char[] CompilerOptionArgumentSeperators = {';', ','};

        public static List<ScriptCompilerBase.CompilerOption> GetCompilerOptions(string responseFileContent)
        {
            var compilerOptions = new List<ScriptCompilerBase.CompilerOption>();

            var responseFileStrings = ResponseFileTextToStrings(responseFileContent);

            foreach (var line in responseFileStrings)
            {
                int idx = line.IndexOf(':');
                string arg, value;

                if (idx == -1)
                {
                    arg = line;
                    value = "";
                }
                else
                {
                    arg = line.Substring(0, idx);
                    value = line.Substring(idx + 1);
                }

                if (!string.IsNullOrEmpty(arg) && arg[0] == '-')
                    arg = '/' + arg.Substring(1);

                compilerOptions.Add(new ScriptCompilerBase.CompilerOption {Arg = arg, Value = value});
            }

            return compilerOptions;
        }

        public static List<string> GetDefines(List<ScriptCompilerBase.CompilerOption> compilerOptions)
        {
            List<string> defines = new List<string>(compilerOptions.Count);
            foreach (var compilerOption in compilerOptions)
            {
                if (IsDefine(compilerOption))
                {
                    defines.AddRange(GetOptionDefines(compilerOption));
                }
            }

            return defines;
        }

        private static string[] GetOptionDefines(ScriptCompilerBase.CompilerOption compilerOption)
        {
            return compilerOption.Value.Split(CompilerOptionArgumentSeperators);
        }

        public static string GetResponseFileContent(string projectDirectory, string path)
        {
            var responseFilePath = Paths.ConvertSeparatorsToUnity(path);
            var projectDirectoryUnitySeperators = Paths.ConvertSeparatorsToUnity(projectDirectory);

            var relativeResponseFilePath = Paths.GetPathRelativeToProjectDirectory(responseFilePath);
            var responseFile = AssetDatabase.LoadAssetAtPath<TextAsset>(relativeResponseFilePath);

            if (responseFile)
            {
                return responseFile.text;
            }

            if (File.Exists(responseFilePath))
            {
                return File.ReadAllText(responseFilePath);
            }

            return string.Empty;
        }

        public static ResponseFileData ParseResponseFileFromFile(
            string responseFilePath,
            string projectDirectory,
            string[] systemReferenceDirectories)
        {
            responseFilePath = Paths.ConvertSeparatorsToUnity(responseFilePath);
            projectDirectory = Paths.ConvertSeparatorsToUnity(projectDirectory);

            var relativeResponseFilePath = GetRelativePath(responseFilePath, projectDirectory);
            var responseFile = AssetDatabase.LoadAssetAtPath<TextAsset>(relativeResponseFilePath);

            if (!responseFile && File.Exists(responseFilePath))
            {
                var responseFileText = File.ReadAllText(responseFilePath);
                return ParseResponseFileText(
                    responseFileText,
                    responseFilePath,
                    projectDirectory,
                    systemReferenceDirectories);
            }

            if (!responseFile)
            {
                var empty = new ResponseFileData
                {
                    Defines = new string[0],
                    FullPathReferences = new string[0],
                    Unsafe = false,
                    Errors = new string[0],
                    OtherArguments = new string[0],
                };

                return empty;
            }

            return ParseResponseFileText(
                responseFile.text,
                responseFile.name,
                projectDirectory,
                systemReferenceDirectories);
        }

        static string GetRelativePath(string responseFilePath, string projectDirectory)
        {
            if (Path.IsPathRooted(responseFilePath) && responseFilePath.Contains(projectDirectory))
            {
                responseFilePath = responseFilePath.Substring(projectDirectory.Length + 1);
            }

            return responseFilePath;
        }

        static ResponseFileData ParseResponseFileText(
            string fileContent,
            string fileName,
            string projectDirectory,
            string[] systemReferenceDirectories)
        {
            List<ScriptCompilerBase.CompilerOption> compilerOptions = GetCompilerOptions(fileContent);

            var responseArguments = new List<string>();
            var defines = new List<string>();
            var references = new List<string>();
            bool unsafeDefined = false;
            var errors = new List<string>();

            foreach (var option in compilerOptions)
            {
                var arg = option.Arg;
                var value = option.Value;

                if (IsDefine(option))
                {
                    defines.AddRange(GetOptionDefines(option));
                }
                else if (IsReference(option))
                {
                    if (TryGetReference(option, fileName, projectDirectory, systemReferenceDirectories, ref errors,
                        out var result))
                    {
                        references.Add(result);
                    }
                }
                else if (IsSetUnsafe(option))
                {
                    unsafeDefined = true;
                }
                else if (IsUnsetUnsafe(option))
                {
                    unsafeDefined = false;
                }
                else
                {
                    var valueWithColon = value.Length == 0 ? "" : ":" + value;
                    responseArguments.Add(arg + valueWithColon);
                }
            }

            var responseFileData = new ResponseFileData
            {
                Defines = defines.ToArray(),
                FullPathReferences = references.ToArray(),
                Unsafe = unsafeDefined,
                Errors = errors.ToArray(),
                OtherArguments = responseArguments.ToArray(),
            };

            return responseFileData;
        }

        private static bool IsDefine(ScriptCompilerBase.CompilerOption compilerOption)
        {
            if (string.IsNullOrEmpty(compilerOption?.Arg))
            {
                return false;
            }

            return compilerOption.Arg.Equals("/d", StringComparison.Ordinal)
                || compilerOption.Arg.Equals("/define", StringComparison.Ordinal);
        }

        private static bool IsReference(ScriptCompilerBase.CompilerOption compilerOption)
        {
            if (string.IsNullOrEmpty(compilerOption?.Arg))
            {
                return false;
            }

            return compilerOption.Arg.Equals("/r", StringComparison.Ordinal)
                || compilerOption.Arg.Equals("/reference", StringComparison.Ordinal);
        }

        private static bool IsSetUnsafe(ScriptCompilerBase.CompilerOption compilerOption)
        {
            if (string.IsNullOrEmpty(compilerOption?.Arg))
            {
                return false;
            }

            return compilerOption.Arg.Equals("/unsafe", StringComparison.Ordinal)
                || compilerOption.Arg.Equals("/unsafe+", StringComparison.Ordinal);
        }

        private static bool IsUnsetUnsafe(ScriptCompilerBase.CompilerOption compilerOption)
        {
            if (string.IsNullOrEmpty(compilerOption?.Arg))
            {
                return false;
            }

            return compilerOption.Arg.Equals("/unsafe", StringComparison.Ordinal)
                || compilerOption.Arg.Equals("/unsafe+", StringComparison.Ordinal);
        }

        private static bool TryGetReference(ScriptCompilerBase.CompilerOption option, string fileName,
            string projectDirectory, string[] systemReferenceDirectories,
            ref List<string> errors, out string result)
        {
            result = null;
            var value = option.Value;
            if (value.Length == 0)
            {
                errors.Add("No value set for reference");
                return false; // break;
            }

            string[] refs = value.Split(CompilerOptionArgumentSeperators);

            if (refs.Length != 1)
            {
                errors.Add("Cannot specify multiple aliases using single /reference option");
                return false; // break;
            }

            var reference = refs[0];
            if (reference.Length == 0)
            {
                return false; // continue;
            }

            int index = reference.IndexOf('=');
            var responseReference = index > -1 ? reference.Substring(index + 1) : reference;

            var fullPathReference = responseReference;
            bool isRooted = Path.IsPathRooted(responseReference);
            if (!isRooted)
            {
                foreach (var directory in systemReferenceDirectories)
                {
                    var systemReferencePath = Paths.Combine(directory, responseReference);
                    if (File.Exists(systemReferencePath))
                    {
                        fullPathReference = systemReferencePath;
                        isRooted = true;
                        break;
                    }
                }

                var userPath = Paths.Combine(projectDirectory, responseReference);
                if (File.Exists(userPath))
                {
                    fullPathReference = userPath;
                    isRooted = true;
                }
            }

            if (!isRooted)
            {
                errors.Add(
                    $"{fileName}: not parsed correctly: {responseReference} could not be found as a system library.\n" +
                    "If this was meant as a user reference please provide the relative path from project root (parent of the Assets folder) in the response file.");
                return false; // continue;
            }

            responseReference = fullPathReference.Replace('\\', '/');
            result = responseReference;
            return true;
        }

        // From:
        // https://github.com/mono/mono/blob/c106cdc775792ceedda6da58de7471f9f5c0b86c/mcs/mcs/settings.cs
        //
        // settings.cs: All compiler settings
        //
        // Author: Miguel de Icaza (miguel@ximian.com)
        //            Ravi Pratap  (ravi@ximian.com)
        //            Marek Safar  (marek.safar@gmail.com)
        //
        //
        // Dual licensed under the terms of the MIT X11 or GNU GPL
        //
        // Copyright 2001 Ximian, Inc (http://www.ximian.com)
        // Copyright 2004-2008 Novell, Inc
        // Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
        static string[] ResponseFileTextToStrings(string responseFileText)
        {
            var args = new List<string>();

            var sb = new System.Text.StringBuilder();

            var textLines = responseFileText.Split('\n', '\r');

            foreach (var line in textLines)
            {
                int t = line.Length;

                for (int i = 0; i < t; i++)
                {
                    char c = line[i];

                    if (c == '"' || c == '\'')
                    {
                        char end = c;

                        for (i++; i < t; i++)
                        {
                            c = line[i];

                            if (c == end)
                                break;
                            sb.Append(c);
                        }
                    }
                    else if (c == ' ')
                    {
                        if (sb.Length > 0)
                        {
                            args.Add(sb.ToString());
                            sb.Length = 0;
                        }
                    }
                    else
                        sb.Append(c);
                }

                if (sb.Length > 0)
                {
                    args.Add(sb.ToString());
                    sb.Length = 0;
                }
            }

            return args.ToArray();
        }
    }
}
