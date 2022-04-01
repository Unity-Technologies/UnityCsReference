// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Pool;

namespace UnityEditor.Callbacks
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RunAfterClassAttribute : Attribute
    {
        public Type classType { get; }

        public RunAfterClassAttribute(Type type) => classType = type;

        public RunAfterClassAttribute(string assemblyQualifiedName) => classType = Type.GetType(assemblyQualifiedName, false);
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RunBeforeClassAttribute : Attribute
    {
        public Type classType { get; }

        public RunBeforeClassAttribute(Type type) => classType = type;

        public RunBeforeClassAttribute(string assemblyQualifiedName) => classType = Type.GetType(assemblyQualifiedName, false);
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RunAfterAssemblyAttribute : Attribute
    {
        public string assemblyName { get; }

        public RunAfterAssemblyAttribute(string assemblyName) => this.assemblyName = assemblyName;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RunBeforeAssemblyAttribute : Attribute
    {
        public string assemblyName { get; }

        public RunBeforeAssemblyAttribute(string assemblyName) => this.assemblyName = assemblyName;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RunAfterPackageAttribute : Attribute
    {
        public string packageName { get; }

        public RunAfterPackageAttribute(string packageName) => this.packageName = packageName;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RunBeforePackageAttribute : Attribute
    {
        public string packageName { get; }

        public RunBeforePackageAttribute(string packageName) => this.packageName = packageName;
    }

    abstract class OrderedCallbackCollection
    {
        public abstract class Callback : IComparable
        {
            string m_PackageName;

            public abstract Type classType { get; }

            public abstract string name { get; }

            public string packageName
            {
                get
                {
                    if (m_PackageName == null)
                    {
                        var pkg = PackageManager.PackageInfo.FindForAssembly(classType.Assembly);
                        m_PackageName = pkg != null ? pkg.name : string.Empty;
                    }
                    return m_PackageName;
                }
            }

            public HashSet<Callback> outgoing { get; } = new HashSet<Callback>();
            public HashSet<Callback> incoming { get; } = new HashSet<Callback>();

            public abstract IEnumerable<T> GetCustomAttributes<T>() where T : Attribute;

            public void AddIncomingConnection(Callback method)
            {
                incoming.Add(method);
                method.outgoing.Add(this);
            }

            public void AddIncomingConnections(IList<Callback> methods)
            {
                foreach(var m in methods)
                {
                    AddIncomingConnection(m);
                }
            }

            public void AddOutgoingConnection(Callback method)
            {
                outgoing.Add(method);
                method.incoming.Add(this);
            }

            public void AddOutgoingConnections(IList<Callback> methods)
            {
                foreach (var m in methods)
                {
                    AddOutgoingConnection(m);
                }
            }

            public int CompareTo(object obj)
            {
                if (obj is Callback other)
                    return classType.FullName.CompareTo(other.classType.FullName);
                return 0;
            }
        }

        List<Callback> m_SortedCallbacks;

        public abstract string name { get; }

        public List<Callback> sortedCallbacks
        {
            get
            {
                if (m_SortedCallbacks == null)
                    m_SortedCallbacks = GenerateSortedCallbacks();
                return m_SortedCallbacks;
            }
        }

        public abstract List<Callback> GetCallbacks();

        public List<Callback> GenerateDependencyGraph()
        {
            var callbacks = GetCallbacks();
            var packageLookup = DictionaryPool<string, List<Callback>>.Get();
            var assemblyLookup = DictionaryPool<string, List<Callback>>.Get();
            var classLookup = DictionaryPool<Type, Callback>.Get();

            // First generate our lookups for class, assembly and package.
            foreach (var cb in callbacks)
            {
                classLookup[cb.classType] = cb;

                var assemblyName = cb.classType.Assembly.GetName().Name;
                if (!assemblyLookup.TryGetValue(assemblyName, out var assemblies))
                {
                    assemblies = new List<Callback>();
                    assemblyLookup[assemblyName] = assemblies;
                }
                assemblies.Add(cb);

                var package = cb.packageName;
                if(package != null)
                {
                    if (!packageLookup.TryGetValue(package, out var packages))
                    {
                        packages = new List<Callback>();
                        packageLookup[package] = packages;
                    }
                    packages.Add(cb);
                }
            }

            // Sort the methods so that the output order is deterministic.
            callbacks.Sort();

            // Now connect the dependency graph nodes
            foreach (var dependency in callbacks)
            {
                // Dependency by class
                foreach (var runAfter in dependency.GetCustomAttributes<RunAfterClassAttribute>())
                {
                    // Ignore classes that may not exist in the project
                    if (runAfter.classType == null)
                        continue;

                    if (classLookup.TryGetValue(runAfter.classType, out var runAfterMethodInfo))
                    {
                        dependency.AddIncomingConnection(runAfterMethodInfo);
                    }
                }
                foreach (var runBefore in dependency.GetCustomAttributes<RunBeforeClassAttribute>())
                {
                    // Ignore classes that may not exist in the project
                    if (runBefore.classType == null)
                        continue;

                    if (classLookup.TryGetValue(runBefore.classType, out var runBeforeMethodInfo))
                    {
                        dependency.AddOutgoingConnection(runBeforeMethodInfo);
                    }
                }

                // Dependency by package
                foreach (var runAfter in dependency.GetCustomAttributes<RunAfterPackageAttribute>())
                {
                    if (packageLookup.TryGetValue(runAfter.packageName, out var runAfterMethodInfos))
                    {
                        dependency.AddIncomingConnections(runAfterMethodInfos);
                    }
                }
                foreach (var runBefore in dependency.GetCustomAttributes<RunBeforePackageAttribute>())
                {
                    if (packageLookup.TryGetValue(runBefore.packageName, out var runBeforeMethodInfos))
                    {
                        dependency.AddOutgoingConnections(runBeforeMethodInfos);
                    }
                }

                // Dependency by Assembly
                foreach (var runAfter in dependency.GetCustomAttributes<RunAfterAssemblyAttribute>())
                {
                    if (assemblyLookup.TryGetValue(runAfter.assemblyName, out var runAfterMethodInfos))
                    {
                        dependency.AddIncomingConnections(runAfterMethodInfos);
                    }
                }
                foreach (var runBefore in dependency.GetCustomAttributes<RunBeforeAssemblyAttribute>())
                {
                    if (assemblyLookup.TryGetValue(runBefore.assemblyName, out var runBeforeMethodInfos))
                    {
                        dependency.AddOutgoingConnections(runBeforeMethodInfos);
                    }
                }
            }

            DictionaryPool<string, List<Callback>>.Release(packageLookup);
            DictionaryPool<string, List<Callback>>.Release(assemblyLookup);
            DictionaryPool<Type, Callback>.Release(classLookup);

            return callbacks;
        }

        public List<Callback> GenerateSortedCallbacks() => PerformTopologicalSortingKahnAlgorithm(GenerateDependencyGraph());

        List<Callback> PerformTopologicalSortingKahnAlgorithm(List<Callback> dependencyGraph, HashSet<string> cyclicNodes = null)
        {
            int n = dependencyGraph.Count;
            var ordered = new List<Callback>(n);
            var q = new Queue<Callback>();

            // Find nodes which do not need to run after anything(no incoming)
            foreach (var node in dependencyGraph)
            {
                if (node.incoming.Count == 0)
                    q.Enqueue(node);
            }

            while (q.Count != 0)
            {
                var at = q.Dequeue();
                ordered.Add(at);

                foreach (var o in at.outgoing)
                {
                    o.incoming.Remove(at);
                    if (o.incoming.Count == 0)
                        q.Enqueue(o);
                }
                at.outgoing.Clear();
            }

            // Graph contains a cycle
            if (ordered.Count != dependencyGraph.Count)
            {
                var sb = new StringBuilder();
                sb.Append($"Found cycles in callback dependency graph for {name}.\nThe following nodes could not be added:\n");

                var visited = cyclicNodes ?? new HashSet<string>();
                foreach (var node in dependencyGraph)
                {
                    if (node.incoming.Count == 0)
                        continue;

                    PrintChildren(node, sb, visited, 0);
                }
                Debug.LogError(sb.ToString());
            }

            return ordered;
        }

        static void PrintChildren(Callback callback, StringBuilder stringBuilder, HashSet<string> visited, int depth)
        {
            if (visited.Contains(callback.name))
            {
                if (depth != 0)
                {
                    // We have a cycle. Abort here
                    stringBuilder.Append(new string('-', depth));
                    stringBuilder.AppendLine($"<color=red>{callback.name}</color>");
                }

                return;
            }

            visited.Add(callback.name);

            if (depth != 0)
            {
                stringBuilder.Append(new string('-', depth));
            }

            stringBuilder.AppendLine(callback.name);

            foreach (var node in callback.outgoing)
            {
                PrintChildren(node, stringBuilder, visited, depth + 1);
            }
        }

        /// <summary>
        /// This can aid with debugging and understanding the dependencies.
        /// It will generate a Graphviz dot diagram to show the dependencies, cyclic issues and generated callback order.
        /// </summary>
        /// <param name="path">Where to save the generated dot diagram.</param>
        public void GenerateDependencyDiagram(string path = "DependenciesDiagram.dot")
        {
            var dependencyGraph = GenerateDependencyGraph();
            var cyclicNodes = new HashSet<string>();
            var sortedCallbacks = PerformTopologicalSortingKahnAlgorithm(GenerateDependencyGraph(), cyclicNodes);

            var graphvizDiagram = new StringBuilder();
            graphvizDiagram.AppendLine("digraph DependenciesDiagram {");
            graphvizDiagram.AppendLine("\tnode [style=\"filled, rounded\", shape=box, fillcolor=\"#FCE5EC\" color=\"#EB417A\"]");
            foreach(var node in cyclicNodes)
            {
                graphvizDiagram.AppendLine($"\t<{node}>");
            }

            graphvizDiagram.AppendLine();
            graphvizDiagram.AppendLine("\tnode [style=\"filled, rounded\", shape=box, fillcolor=\"#DAEDFD\" color=\"#2196F3\"]");
            graphvizDiagram.AppendLine("\tedge [penwidth=1.5, color=\"#2196F3\"]");
            foreach (var node in dependencyGraph)
            {
                if (node.outgoing.Count > 0)
                {
                    graphvizDiagram.Append($"\t<{node.name}> -> ");

                    var enumerator = node.outgoing.GetEnumerator();
                    enumerator.MoveNext();
                    graphvizDiagram.Append($"<{enumerator.Current.name}>");
                    while (enumerator.MoveNext())
                    {
                        graphvizDiagram.Append($", <{enumerator.Current.name}>");
                    }
                    graphvizDiagram.AppendLine();
                }
            }
            graphvizDiagram.AppendLine();

            // Sorted results
            graphvizDiagram.AppendLine("\tedge [penwidth=1.5, color=\"#67BC6B\"]");
            for (var i = 0; i < sortedCallbacks.Count - 1; ++i)
            {
                graphvizDiagram.AppendLine($"\t<{sortedCallbacks[i].name}> -> <{sortedCallbacks[i + 1].name}>");
            }
            graphvizDiagram.AppendLine("}");

            File.WriteAllText(path, graphvizDiagram.ToString());
            EditorUtility.OpenWithDefaultApp(path);
        }
    }
}
