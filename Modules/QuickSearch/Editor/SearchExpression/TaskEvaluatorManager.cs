// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditorInternal;

namespace UnityEditor.Search
{
    static class TaskEvaluatorManager
    {
        class MainThreadIEnumerableHandler<T> : BaseAsyncIEnumerableHandler<T>
        {
            List<T> m_Results = new List<T>();

            public IEnumerable<T> results => m_Results;
            public EventWaitHandle startEvent { get; private set; } = new EventWaitHandle(false, EventResetMode.AutoReset);
            public EventWaitHandle stopEvent { get; private set; } = new EventWaitHandle(false, EventResetMode.AutoReset);

            public override void SendItems(IEnumerable<T> items)
            {
                m_Results.AddRange(items);
            }

            internal override void Start()
            {
                base.Start();
                stopEvent.Reset();
                startEvent.Set();
            }

            public override void Stop()
            {
                base.Stop();
                stopEvent.Set();
            }
        }

        public static IEnumerable<SearchItem> Evaluate(SearchExpressionContext c, SearchExpression expression)
        {
            var concurrentList = new ConcurrentBag<SearchItem>();
            var yieldSignal = new EventWaitHandle(false, EventResetMode.AutoReset);
            var task = Task.Run(() =>
            {
                Thread.CurrentThread.Name = expression.name;
                var enumerable = expression.Execute(c, SearchExpressionExecutionFlags.ThreadedEvaluation);
                foreach (var searchItem in enumerable)
                {
                    if (searchItem != null)
                    {
                        concurrentList.Add(searchItem);
                        yieldSignal.Set();
                    }
                }
            });

            while (!concurrentList.IsEmpty || !TaskHelper.IsTaskFinished(task))
            {
                if (!yieldSignal.WaitOne(0))
                {
                    if (concurrentList.IsEmpty)
                        Dispatcher.ProcessOne();
                    yield return null;
                }
                while (concurrentList.TryTake(out var item))
                    yield return item;
            }

            if (task.IsFaulted && task.Exception?.InnerException != null)
                throw new SearchExpressionEvaluatorException(c, task.Exception.InnerException);
        }

        public static T EvaluateMainThread<T>(Func<T> callback)
        {
            if (InternalEditorUtility.CurrentThreadIsMainThread())
                return callback();

            var waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            T result = default;
            Dispatcher.Enqueue(() =>
            {
                result = callback();
                waitHandle.Set();
            });

            waitHandle.WaitOne();

            return result;
        }

        public static IEnumerable<T> EvaluateMainThread<T>(Action<Action<T>> callback)
        {
            var concurrentList = new ConcurrentBag<T>();
            var yielderHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

            void ItemReceived(T item)
            {
                concurrentList.Add(item);
                yielderHandle.Set();
            }

            if (!InternalEditorUtility.CurrentThreadIsMainThread())
            {
                var finishedHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
                Dispatcher.Enqueue(() =>
                {
                    callback(ItemReceived);
                    finishedHandle.Set();
                });

                while (!finishedHandle.WaitOne(0))
                {
                    if (yielderHandle.WaitOne(0))
                        while (concurrentList.TryTake(out var item))
                            yield return item;
                }
            }
            else
                callback(ItemReceived);

            while (concurrentList.Count > 0)
            {
                if (concurrentList.TryTake(out var item))
                    yield return item;
            }
        }

        public static IEnumerable<T> EvaluateMainThread<T>(IEnumerable<T> set, Func<T, T> callback, int minBatchSize = 10) where T : class
        {
            if (InternalEditorUtility.CurrentThreadIsMainThread())
            {
                foreach (var r in set)
                {
                    if (r == null)
                    {
                        yield return null;
                        continue;
                    }

                    yield return callback(r);
                }
                yield break;
            }

            var items = new ConcurrentBag<T>();
            var results = new ConcurrentBag<T>();
            var resultSignal = new EventWaitHandle(false, EventResetMode.AutoReset);

            var batchFinishedSignal = new EventWaitHandle(true, EventResetMode.AutoReset);

            void ProcessBatch()
            {
                while (!items.IsEmpty)
                {
                    if (!items.TryTake(out var item))
                        break;

                    var result = callback(item);
                    if (result != null)
                    {
                        results.Add(result);
                        resultSignal.Set();
                    }
                }

                batchFinishedSignal.Set();
            }

            int initialBatchSize = minBatchSize;
            foreach (var r in set)
            {
                if (r == null)
                {
                    yield return null;
                    continue;
                }

                items.Add(r);
                if (--initialBatchSize < 0 && batchFinishedSignal.WaitOne(0))
                {
                    Dispatcher.Enqueue(ProcessBatch);
                    initialBatchSize = minBatchSize;
                }

                if (resultSignal.WaitOne(0))
                    while (results.TryTake(out var item))
                        yield return item;
            }

            batchFinishedSignal.Reset();
            Dispatcher.Enqueue(ProcessBatch);
            while (!batchFinishedSignal.WaitOne(0))
            {
                if (resultSignal.WaitOne(0))
                    while (results.TryTake(out var item))
                        yield return item;
            }

            while (results.TryTake(out var item))
                yield return item;
        }

        public static IEnumerable<T> EvaluateMainThreadUnroll<T>(Func<IEnumerable<T>> callback)
        {
            if (InternalEditorUtility.CurrentThreadIsMainThread())
                return callback();

            MainThreadIEnumerableHandler<T> enumerableHandler = new MainThreadIEnumerableHandler<T>();
            Dispatcher.Enqueue(() =>
            {
                var enumerable = callback();
                enumerableHandler.Reset(enumerable);
                enumerableHandler.Start();
            });

            enumerableHandler.startEvent.WaitOne();
            enumerableHandler.stopEvent.WaitOne();

            return enumerableHandler.results;
        }
    }
}
