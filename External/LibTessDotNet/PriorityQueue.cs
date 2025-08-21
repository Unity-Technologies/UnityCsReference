/*
** SGI FREE SOFTWARE LICENSE B (Version 2.0, Sept. 18, 2008) 
** Copyright (C) 2011 Silicon Graphics, Inc.
** All Rights Reserved.
**
** Permission is hereby granted, free of charge, to any person obtaining a copy
** of this software and associated documentation files (the "Software"), to deal
** in the Software without restriction, including without limitation the rights
** to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
** of the Software, and to permit persons to whom the Software is furnished to do so,
** subject to the following conditions:
** 
** The above copyright notice including the dates of first publication and either this
** permission notice or a reference to http://oss.sgi.com/projects/FreeB/ shall be
** included in all copies or substantial portions of the Software. 
**
** THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
** INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
** PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL SILICON GRAPHICS, INC.
** BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
** TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE
** OR OTHER DEALINGS IN THE SOFTWARE.
** 
** Except as contained in this notice, the name of Silicon Graphics, Inc. shall not
** be used in advertising or otherwise to promote the sale, use or other dealings in
** this Software without prior written authorization from Silicon Graphics, Inc.
*/
/*
** Original Author: Eric Veach, July 1994.
** libtess2: Mikko Mononen, http://code.google.com/p/libtess2/.
** LibTessDotNet: Remi Gillig, https://github.com/speps/LibTessDotNet
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LibTessDotNet
{
    internal class PriorityQueue<TValue> where TValue : class
    {
        private PriorityHeap<TValue>.LessOrEqual _leq;
        private PriorityHeap<TValue> _heap;
        private TValue[] _keys;
        private int[] _order;

        private int _size, _max;
        private bool _initialized;

        public bool Empty { get { return _size == 0 && _heap.Empty; } }

        public PriorityQueue(int initialSize, PriorityHeap<TValue>.LessOrEqual leq)
        {
            _leq = leq;
            _heap = new PriorityHeap<TValue>(initialSize, leq);

            _keys = new TValue[initialSize];

            _size = 0;
            _max = initialSize;
            _initialized = false;
        }

        class StackItem
        {
            internal int p, r;
        };

        static void Swap(ref int a, ref int b)
        {
            int tmp = a;
            a = b;
            b = tmp;
        }

        public void Init()
        {
            var stack = new Stack<StackItem>();
            int p, r, i, j, piv;
            uint seed = 2016473283;

            p = 0;
            r = _size - 1;
            _order = new int[_size + 1];
            for (piv = 0, i = p; i <= r; ++piv, ++i)
            {
                _order[i] = piv;
            }

            stack.Push(new StackItem { p = p, r = r });
            while (stack.Count > 0)
            {
                var top = stack.Pop();
                p = top.p;
                r = top.r;

                while (r > p + 10)
                {
                    seed = seed * 1539415821 + 1;
                    i = p + (int)(seed % (r - p + 1));
                    piv = _order[i];
                    _order[i] = _order[p];
                    _order[p] = piv;
                    i = p - 1;
                    j = r + 1;
                    do {
                        do { ++i; } while (!_leq(_keys[_order[i]], _keys[piv]));
                        do { --j; } while (!_leq(_keys[piv], _keys[_order[j]]));
                        Swap(ref _order[i], ref _order[j]);
                    } while (i < j);
                    Swap(ref _order[i], ref _order[j]);
                    if (i - p < r - j)
                    {
                        stack.Push(new StackItem { p = j + 1, r = r });
                        r = i - 1;
                    }
                    else
                    {
                        stack.Push(new StackItem { p = p, r = i - 1 });
                        p = j + 1;
                    }
                }
                for (i = p + 1; i <= r; ++i)
                {
                    piv = _order[i];
                    for (j = i; j > p && !_leq(_keys[piv], _keys[_order[j - 1]]); --j)
                    {
                        _order[j] = _order[j - 1];
                    }
                    _order[j] = piv;
                }
            }


            _max = _size;
            _initialized = true;
            _heap.Init();
        }

        public PQHandle Insert(TValue value)
        {
            if (_initialized)
            {
                return _heap.Insert(value);
            }

            int curr = _size;
            if (++_size >= _max)
            {
                _max <<= 1;
                Array.Resize(ref _keys, _max);
            }

            _keys[curr] = value;
            return new PQHandle { _handle = -(curr + 1) };
        }

        public TValue ExtractMin()
        {
            Debug.Assert(_initialized);

            if (_size == 0)
            {
                return _heap.ExtractMin();
            }
            TValue sortMin = _keys[_order[_size - 1]];
            if (!_heap.Empty)
            {
                TValue heapMin = _heap.Minimum();
                if (_leq(heapMin, sortMin))
                    return _heap.ExtractMin();
            }
            do {
                --_size;
            } while (_size > 0 && _keys[_order[_size - 1]] == null);

            return sortMin;
        }

        public TValue Minimum()
        {
            Debug.Assert(_initialized);

            if (_size == 0)
            {
                return _heap.Minimum();
            }
            TValue sortMin = _keys[_order[_size - 1]];
            if (!_heap.Empty)
            {
                TValue heapMin = _heap.Minimum();
                if (_leq(heapMin, sortMin))
                    return heapMin;
            }
            return sortMin;
        }

        public void Remove(PQHandle handle)
        {
            Debug.Assert(_initialized);

            int curr = handle._handle;
            if (curr >= 0)
            {
                _heap.Remove(handle);
                return;
            }
            curr = -(curr + 1);
            Debug.Assert(curr < _max && _keys[curr] != null);

            _keys[curr] = null;
            while (_size > 0 && _keys[_order[_size - 1]] == null)
            {
                --_size;
            }
        }
    }
}
