// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;

namespace Unity.UNetWeaver
{
    class Helpers
    {
        // This code is taken from SerializationWeaver

        class AddSearchDirectoryHelper
        {
            delegate void AddSearchDirectoryDelegate(string directory);
            readonly AddSearchDirectoryDelegate _addSearchDirectory;

            public AddSearchDirectoryHelper(IAssemblyResolver assemblyResolver)
            {
                // reflection is used because IAssemblyResolver doesn't implement AddSearchDirectory but both DefaultAssemblyResolver and NuGetAssemblyResolver do
                var addSearchDirectory = assemblyResolver.GetType().GetMethod("AddSearchDirectory", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { typeof(string) }, null);
                if (addSearchDirectory == null)
                    throw new Exception("Assembly resolver doesn't implement AddSearchDirectory method.");
                _addSearchDirectory = (AddSearchDirectoryDelegate)Delegate.CreateDelegate(typeof(AddSearchDirectoryDelegate), assemblyResolver, addSearchDirectory);
            }

            public void AddSearchDirectory(string directory)
            {
                _addSearchDirectory(directory);
            }
        }

        public static string UnityEngineDLLDirectoryName()
        {
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            return directoryName != null ? directoryName.Replace(@"file:\", "") : null;
        }

        public static ISymbolReaderProvider GetSymbolReaderProvider(string inputFile)
        {
            string nakedFileName = inputFile.Substring(0, inputFile.Length - 4);
            if (File.Exists(nakedFileName + ".pdb"))
            {
                Console.WriteLine("Symbols will be read from " + nakedFileName + ".pdb");
                return new PdbReaderProvider();
            }
            if (File.Exists(nakedFileName + ".dll.mdb"))
            {
                Console.WriteLine("Symbols will be read from " + nakedFileName + ".dll.mdb");
                return new MdbReaderProvider();
            }
            Console.WriteLine("No symbols for " + inputFile);
            return null;
        }

        public static string DestinationFileFor(string outputDir, string assemblyPath)
        {
            var fileName = Path.GetFileName(assemblyPath);
            Debug.Assert(fileName != null, "fileName != null");

            return Path.Combine(outputDir, fileName);
        }

        public static ReaderParameters ReaderParameters(string assemblyPath, IEnumerable<string> extraPaths, IAssemblyResolver assemblyResolver, string unityEngineDLLPath, string unityUNetDLLPath)
        {
            var parameters = new ReaderParameters();
            if (assemblyResolver == null)
                assemblyResolver = new DefaultAssemblyResolver();
            var helper = new AddSearchDirectoryHelper(assemblyResolver);
            helper.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));
            helper.AddSearchDirectory(Helpers.UnityEngineDLLDirectoryName());
            helper.AddSearchDirectory(Path.GetDirectoryName(unityEngineDLLPath));
            helper.AddSearchDirectory(Path.GetDirectoryName(unityUNetDLLPath));
            if (extraPaths != null)
            {
                foreach (var path in extraPaths)
                    helper.AddSearchDirectory(path);
            }
            parameters.AssemblyResolver = assemblyResolver;
            parameters.SymbolReaderProvider = GetSymbolReaderProvider(assemblyPath);
            return parameters;
        }

        public static WriterParameters GetWriterParameters(ReaderParameters readParams)
        {
            var writeParams = new WriterParameters();
            if (readParams.SymbolReaderProvider is PdbReaderProvider)
            {
                //Log("Will export symbols of pdb format");
                writeParams.SymbolWriterProvider = new PdbWriterProvider();
            }
            else if (readParams.SymbolReaderProvider is MdbReaderProvider)
            {
                //Log("Will export symbols of mdb format");
                writeParams.SymbolWriterProvider = new MdbWriterProvider();
            }
            return writeParams;
        }

        public static TypeReference MakeGenericType(TypeReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
                throw new ArgumentException();

            var instance = new GenericInstanceType(self);
            foreach (var argument in arguments)
                instance.GenericArguments.Add(argument);

            return instance;
        }

        // used to get a specialized method on a generic class, such as SyncList<T>::HandleMsg()
        public static MethodReference MakeHostInstanceGeneric(MethodReference self, params TypeReference[] arguments)
        {
            var reference = new MethodReference(self.Name, self.ReturnType, MakeGenericType(self.DeclaringType, arguments))
            {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                CallingConvention = self.CallingConvention
            };

            foreach (var parameter in self.Parameters)
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));

            foreach (var genericParameter in self.GenericParameters)
                reference.GenericParameters.Add(new GenericParameter(genericParameter.Name, reference));

            return reference;
        }
    }
}
