// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    [Flags]
    enum IterationJumps
    {
        None = 0,
        Break = 1,
        Continue = 1 << 1
    }

    class ExecutionState
    {
        public IterationJumps iterationJumps = IterationJumps.None;

        public void Break()
        {
            iterationJumps |= IterationJumps.Break;
        }

        public bool IsBreaking()
        {
            return (iterationJumps & IterationJumps.Break) == IterationJumps.Break;
        }

        public void Continue()
        {
            iterationJumps |= IterationJumps.Continue;
        }

        public bool IsContinuing()
        {
            return (iterationJumps & IterationJumps.Continue) == IterationJumps.Continue;
        }

        public void ResetIterationControl()
        {
            iterationJumps = IterationJumps.None;
        }
    }

    readonly struct SearchExpressionRuntime
    {
        public readonly SearchContext search;
        internal readonly ExecutionState state;
        public readonly Stack<SearchItem> items;
        public readonly Stack<SearchExpressionContext> frames;

        public bool valid => frames != null && frames.Count > 0;
        public SearchExpressionContext current => frames.Peek();

        internal SearchExpressionRuntime(SearchContext searchContext, SearchExpressionExecutionFlags flags)
        {
            search = searchContext;
            frames = new Stack<SearchExpressionContext>();
            state = new ExecutionState();
            items = new Stack<SearchItem>();

            frames.Push(new SearchExpressionContext(this, null, null, SearchExpressionExecutionFlags.Root | flags));
        }

        public override string ToString()
        {
            if (!valid)
                return "-";
            var f = frames.Peek();
            return $"#{frames.Count} {f.flags} | {f.expression} | args:{f.args.Length}";
        }

        public IDisposable Push(SearchExpression searchExpression, IEnumerable<SearchExpression> args)
        {
            frames.Push(new SearchExpressionContext(this, searchExpression, args.ToArray(), SearchExpressionExecutionFlags.None));
            return new PushPopScope<SearchExpressionContext>(frames);
        }

        internal IDisposable Push(SearchExpression searchExpression, IEnumerable<SearchExpression> args, SearchExpressionExecutionFlags flags)
        {
            flags |= frames.Peek().flags & SearchExpressionExecutionFlags.TransferedFlags;
            frames.Push(new SearchExpressionContext(this, searchExpression, args.ToArray(), flags));
            return new PushPopScope<SearchExpressionContext>(frames);
        }

        public IDisposable Push(SearchItem item)
        {
            items.Push(item);
            return new PushPopScope<SearchItem>(items);
        }

        struct PushPopScope<T> : IDisposable
        {
            readonly Stack<T> s;
            volatile bool disposed;

            public PushPopScope(Stack<T> s)
            {
                this.s = s;
                disposed = false;
            }

            public void Dispose()
            {
                if (disposed)
                    return;
                s.Pop();
                disposed = true;
            }
        }
    }
}
