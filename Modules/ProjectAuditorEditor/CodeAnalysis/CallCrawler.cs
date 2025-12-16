// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Mono.Cecil;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    class CallInfo
    {
        public MethodReference Callee { get; }
        public MethodReference Caller { get; }
        public Location Location { get; }
        public bool IsPerfCriticalContext { get; }
        public CallTreeNode Hierarchy { get; set; }

        public CallInfo(
            MethodReference callee,
            MethodReference caller,
            Location location,
            bool isPerfCriticalContext)
        {
            Callee = callee;
            Caller = caller;
            Location = location;
            IsPerfCriticalContext = isPerfCriticalContext;
        }

        public override bool Equals(object obj)
        {
            var other = obj as CallInfo;
            if (other == null)
            {
                return false;
            }

            return other.Callee == Callee &&
                other.Caller == Caller;
        }

        public override int GetHashCode()
        {
            return Callee.GetHashCode()
                + Caller.GetHashCode();
        }
    }

    class CallCrawler
    {
        internal const int k_MaxDepth = 10;

        // key: callee name, value: lists of all callers
        readonly Dictionary<string, List<CallInfo>> m_BucketedCalls =
            new Dictionary<string, List<CallInfo>>(512);

        public void Add(MethodReference callee, MethodReference caller, Location location, bool isPerfCriticalContext)
        {
            var key = callee.FastFullName();
            if (!m_BucketedCalls.TryGetValue(key, out var calls))
            {
                calls = new List<CallInfo>();
                m_BucketedCalls.Add(key, calls);
            }
            calls.Add(new CallInfo(callee, caller, location, isPerfCriticalContext));
        }

        public void BuildCallHierarchies(List<ReportItem> issues, IProgress progress = null)
        {
            if (issues.Count > 0)
            {
                if (progress != null)
                    progress.Start("Analyzing Method calls", string.Empty, issues.Count);

                foreach (var issue in issues)
                {
                    if (progress != null)
                        progress.Advance();

                    const int depth = 0;
                    var root = issue.Dependencies;
                    BuildHierarchy(root as CallTreeNode, depth);

                    // temp fix for null location (code analysis was unable to get sequence point)
                    if (issue.Location == null)
                        issue.Location = root.Location;
                }
                if (progress != null)
                    progress.Clear();
            }
        }

        void BuildHierarchy(CallTreeNode callee, int depth)
        {
            // this check should be removed. Instead, the deep callstacks should be built on-demand
            if (depth++ == k_MaxDepth)
                return;

            // let's find all callers with matching callee
            if (m_BucketedCalls.TryGetValue(callee.MethodFullName, out var callPairs))
            {
                var childrenCount = callPairs.Count;
                var children = new DependencyNode[childrenCount];

                for (var i = 0; i < childrenCount; i++)
                {
                    var call = callPairs[i];
                    if (call.Hierarchy != null)
                    {
                        // use previously built hierarchy
                        children[i] = call.Hierarchy;
                        continue;
                    }

                    var callerName = call.Caller.FastFullName();
                    var hierarchy = new CallTreeNode(call.Caller)
                    {
                        Location = call.Location, PerfCriticalContext = call.IsPerfCriticalContext
                    };

                    // stop recursion, if applicable (note that this only prevents recursion when a method calls itself)
                    if (!callerName.Equals(callee.MethodFullName))
                        BuildHierarchy(hierarchy, depth);

                    children[i] = hierarchy;
                    call.Hierarchy = hierarchy;
                }
                callee.AddChildren(children);
            }
        }
    }
}
