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
using System.Diagnostics;

namespace LibTessDotNet
{
    internal class Mesh : MeshUtils.Pooled<Mesh>
    {
        internal MeshUtils.Vertex _vHead;
        internal MeshUtils.Face _fHead;
        internal MeshUtils.Edge _eHead, _eHeadSym;

        public Mesh()
        {
            var v = _vHead = MeshUtils.Vertex.Create();
            var f = _fHead = MeshUtils.Face.Create();

            var pair = MeshUtils.EdgePair.Create();
            var e = _eHead = pair._e;
            var eSym = _eHeadSym = pair._eSym;

            v._next = v._prev = v;
            v._anEdge = null;

            f._next = f._prev = f;
            f._anEdge = null;
            f._trail = null;
            f._marked = false;
            f._inside = false;

            e._next = e;
            e._Sym = eSym;
            e._Onext = null;
            e._Lnext = null;
            e._Org = null;
            e._Lface = null;
            e._winding = 0;
            e._activeRegion = null;

            eSym._next = eSym;
            eSym._Sym = e;
            eSym._Onext = null;
            eSym._Lnext = null;
            eSym._Org = null;
            eSym._Lface = null;
            eSym._winding = 0;
            eSym._activeRegion = null;
        }

        public override void Reset()
        {
            _vHead = null;
            _fHead = null;
            _eHead = _eHeadSym = null;
        }

        public override void OnFree()
        {
            for (MeshUtils.Face f = _fHead._next, fNext = _fHead; f != _fHead; f = fNext)
            {
                fNext = f._next;
                f.Free();
            }
            for (MeshUtils.Vertex v = _vHead._next, vNext = _vHead; v != _vHead; v = vNext)
            {
                vNext = v._next;
                v.Free();
            }
            for (MeshUtils.Edge e = _eHead._next, eNext = _eHead; e != _eHead; e = eNext)
            {
                eNext = e._next;
                e.Free();
            }
        }

        /// <summary>
        /// Creates one edge, two vertices and a loop (face).
        /// The loop consists of the two new half-edges.
        /// </summary>
        public MeshUtils.Edge MakeEdge()
        {
            var e = MeshUtils.MakeEdge(_eHead);

            MeshUtils.MakeVertex(e, _vHead);
            MeshUtils.MakeVertex(e._Sym, _vHead);
            MeshUtils.MakeFace(e, _fHead);

            return e;
        }

        /// <summary>
        /// Splice is the basic operation for changing the
        /// mesh connectivity and topology.  It changes the mesh so that
        ///     eOrg->Onext = OLD( eDst->Onext )
        ///     eDst->Onext = OLD( eOrg->Onext )
        /// where OLD(...) means the value before the meshSplice operation.
        /// 
        /// This can have two effects on the vertex structure:
        ///  - if eOrg->Org != eDst->Org, the two vertices are merged together
        ///  - if eOrg->Org == eDst->Org, the origin is split into two vertices
        /// In both cases, eDst->Org is changed and eOrg->Org is untouched.
        /// 
        /// Similarly (and independently) for the face structure,
        ///  - if eOrg->Lface == eDst->Lface, one loop is split into two
        ///  - if eOrg->Lface != eDst->Lface, two distinct loops are joined into one
        /// In both cases, eDst->Lface is changed and eOrg->Lface is unaffected.
        /// 
        /// Some special cases:
        /// If eDst == eOrg, the operation has no effect.
        /// If eDst == eOrg->Lnext, the new face will have a single edge.
        /// If eDst == eOrg->Lprev, the old face will have a single edge.
        /// If eDst == eOrg->Onext, the new vertex will have a single edge.
        /// If eDst == eOrg->Oprev, the old vertex will have a single edge.
        /// </summary>
        public void Splice(MeshUtils.Edge eOrg, MeshUtils.Edge eDst)
        {
            if (eOrg == eDst)
            {
                return;
            }

            bool joiningVertices = false;
            if (eDst._Org != eOrg._Org)
            {
                // We are merging two disjoint vertices -- destroy eDst->Org
                joiningVertices = true;
                MeshUtils.KillVertex(eDst._Org, eOrg._Org);
            }
            bool joiningLoops = false;
            if (eDst._Lface != eOrg._Lface)
            {
                // We are connecting two disjoint loops -- destroy eDst->Lface
                joiningLoops = true;
                MeshUtils.KillFace(eDst._Lface, eOrg._Lface);
            }

            // Change the edge structure
            MeshUtils.Splice(eDst, eOrg);

            if (!joiningVertices)
            {
                // We split one vertex into two -- the new vertex is eDst->Org.
                // Make sure the old vertex points to a valid half-edge.
                MeshUtils.MakeVertex(eDst, eOrg._Org);
                eOrg._Org._anEdge = eOrg;
            }
            if (!joiningLoops)
            {
                // We split one loop into two -- the new loop is eDst->Lface.
                // Make sure the old face points to a valid half-edge.
                MeshUtils.MakeFace(eDst, eOrg._Lface);
                eOrg._Lface._anEdge = eOrg;
            }
        }

        /// <summary>
        /// Removes the edge eDel. There are several cases:
        /// if (eDel->Lface != eDel->Rface), we join two loops into one; the loop
        /// eDel->Lface is deleted. Otherwise, we are splitting one loop into two;
        /// the newly created loop will contain eDel->Dst. If the deletion of eDel
        /// would create isolated vertices, those are deleted as well.
        /// </summary>
        public void Delete(MeshUtils.Edge eDel)
        {
            var eDelSym = eDel._Sym;

            // First step: disconnect the origin vertex eDel->Org.  We make all
            // changes to get a consistent mesh in this "intermediate" state.

            bool joiningLoops = false;
            if (eDel._Lface != eDel._Rface)
            {
                // We are joining two loops into one -- remove the left face
                joiningLoops = true;
                MeshUtils.KillFace(eDel._Lface, eDel._Rface);
            }

            if (eDel._Onext == eDel)
            {
                MeshUtils.KillVertex(eDel._Org, null);
            }
            else
            {
                // Make sure that eDel->Org and eDel->Rface point to valid half-edges
                eDel._Rface._anEdge = eDel._Oprev;
                eDel._Org._anEdge = eDel._Onext;

                MeshUtils.Splice(eDel, eDel._Oprev);

                if (!joiningLoops)
                {
                    // We are splitting one loop into two -- create a new loop for eDel.
                    MeshUtils.MakeFace(eDel, eDel._Lface);
                }
            }

            // Claim: the mesh is now in a consistent state, except that eDel->Org
            // may have been deleted.  Now we disconnect eDel->Dst.

            if (eDelSym._Onext == eDelSym)
            {
                MeshUtils.KillVertex(eDelSym._Org, null);
                MeshUtils.KillFace(eDelSym._Lface, null);
            }
            else
            {
                // Make sure that eDel->Dst and eDel->Lface point to valid half-edges
                eDel._Lface._anEdge = eDelSym._Oprev;
                eDelSym._Org._anEdge = eDelSym._Onext;
                MeshUtils.Splice(eDelSym, eDelSym._Oprev);
            }

            // Any isolated vertices or faces have already been freed.
            MeshUtils.KillEdge(eDel);
        }

        /// <summary>
        /// Creates a new edge such that eNew == eOrg.Lnext and eNew.Dst is a newly created vertex.
        /// eOrg and eNew will have the same left face.
        /// </summary>
        public MeshUtils.Edge AddEdgeVertex(MeshUtils.Edge eOrg)
        {
            var eNew = MeshUtils.MakeEdge(eOrg);
            var eNewSym = eNew._Sym;

            // Connect the new edge appropriately
            MeshUtils.Splice(eNew, eOrg._Lnext);

            // Set vertex and face information
            eNew._Org = eOrg._Dst;
            MeshUtils.MakeVertex(eNewSym, eNew._Org);
            eNew._Lface = eNewSym._Lface = eOrg._Lface;

            return eNew;
        }

        /// <summary>
        /// Splits eOrg into two edges eOrg and eNew such that eNew == eOrg.Lnext.
        /// The new vertex is eOrg.Dst == eNew.Org.
        /// eOrg and eNew will have the same left face.
        /// </summary>
        public MeshUtils.Edge SplitEdge(MeshUtils.Edge eOrg)
        {
            var eTmp = AddEdgeVertex(eOrg);
            var eNew = eTmp._Sym;

            // Disconnect eOrg from eOrg->Dst and connect it to eNew->Org
            MeshUtils.Splice(eOrg._Sym, eOrg._Sym._Oprev);
            MeshUtils.Splice(eOrg._Sym, eNew);

            // Set the vertex and face information
            eOrg._Dst = eNew._Org;
            eNew._Dst._anEdge = eNew._Sym; // may have pointed to eOrg->Sym
            eNew._Rface = eOrg._Rface;
            eNew._winding = eOrg._winding; // copy old winding information
            eNew._Sym._winding = eOrg._Sym._winding;

            return eNew;
        }

        /// <summary>
        /// Creates a new edge from eOrg->Dst to eDst->Org, and returns the corresponding half-edge eNew.
        /// If eOrg->Lface == eDst->Lface, this splits one loop into two,
        /// and the newly created loop is eNew->Lface.  Otherwise, two disjoint
        /// loops are merged into one, and the loop eDst->Lface is destroyed.
        /// 
        /// If (eOrg == eDst), the new face will have only two edges.
        /// If (eOrg->Lnext == eDst), the old face is reduced to a single edge.
        /// If (eOrg->Lnext->Lnext == eDst), the old face is reduced to two edges.
        /// </summary>
        public MeshUtils.Edge Connect(MeshUtils.Edge eOrg, MeshUtils.Edge eDst)
        {
            var eNew = MeshUtils.MakeEdge(eOrg);
            var eNewSym = eNew._Sym;

            bool joiningLoops = false;
            if (eDst._Lface != eOrg._Lface)
            {
                // We are connecting two disjoint loops -- destroy eDst->Lface
                joiningLoops = true;
                MeshUtils.KillFace(eDst._Lface, eOrg._Lface);
            }

            // Connect the new edge appropriately
            MeshUtils.Splice(eNew, eOrg._Lnext);
            MeshUtils.Splice(eNewSym, eDst);

            // Set the vertex and face information
            eNew._Org = eOrg._Dst;
            eNewSym._Org = eDst._Org;
            eNew._Lface = eNewSym._Lface = eOrg._Lface;

            // Make sure the old face points to a valid half-edge
            eOrg._Lface._anEdge = eNewSym;

            if (!joiningLoops)
            {
                MeshUtils.MakeFace(eNew, eOrg._Lface);
            }

            return eNew;
        }

        /// <summary>
        /// Destroys a face and removes it from the global face list. All edges of
        /// fZap will have a NULL pointer as their left face. Any edges which
        /// also have a NULL pointer as their right face are deleted entirely
        /// (along with any isolated vertices this produces).
        /// An entire mesh can be deleted by zapping its faces, one at a time,
        /// in any order. Zapped faces cannot be used in further mesh operations!
        /// </summary>
        public void ZapFace(MeshUtils.Face fZap)
        {
            var eStart = fZap._anEdge;

            // walk around face, deleting edges whose right face is also NULL
            var eNext = eStart._Lnext;
            MeshUtils.Edge e, eSym;
            do {
                e = eNext;
                eNext = e._Lnext;

                e._Lface = null;
                if (e._Rface == null)
                {
                    // delete the edge -- see TESSmeshDelete above

                    if (e._Onext == e)
                    {
                        MeshUtils.KillVertex(e._Org, null);
                    }
                    else
                    {
                        // Make sure that e._Org points to a valid half-edge
                        e._Org._anEdge = e._Onext;
                        MeshUtils.Splice(e, e._Oprev);
                    }
                    eSym = e._Sym;
                    if (eSym._Onext == eSym)
                    {
                        MeshUtils.KillVertex(eSym._Org, null);
                    }
                    else
                    {
                        // Make sure that eSym._Org points to a valid half-edge
                        eSym._Org._anEdge = eSym._Onext;
                        MeshUtils.Splice(eSym, eSym._Oprev);
                    }
                    MeshUtils.KillEdge(e);
                }
            } while (e != eStart);

            /* delete from circular doubly-linked list */
            var fPrev = fZap._prev;
            var fNext = fZap._next;
            fNext._prev = fPrev;
            fPrev._next = fNext;

            fZap.Free();
        }

        public void MergeConvexFaces(int maxVertsPerFace)
        {
            for (var f = _fHead._next; f != _fHead; f = f._next)
            {
                // Skip faces which are outside the result
                if (!f._inside)
                {
                    continue;
                }

                var eCur = f._anEdge;
                var vStart = eCur._Org;

                while (true)
                {
                    var eNext = eCur._Lnext;
                    var eSym = eCur._Sym;

                    if (eSym != null && eSym._Lface != null && eSym._Lface._inside)
                    {
                        // Try to merge the neighbour faces if the resulting polygons
                        // does not exceed maximum number of vertices.
                        int curNv = f.VertsCount;
                        int symNv = eSym._Lface.VertsCount;
                        if ((curNv + symNv - 2) <= maxVertsPerFace)
                        {
                            // Merge if the resulting poly is convex.
                            if (Geom.VertCCW(eCur._Lprev._Org, eCur._Org, eSym._Lnext._Lnext._Org) &&
                                Geom.VertCCW(eSym._Lprev._Org, eSym._Org, eCur._Lnext._Lnext._Org))
                            {
                                eNext = eSym._Lnext;
                                Delete(eSym);
                                eCur = null;
                            }
                        }
                    }

                    if (eCur != null && eCur._Lnext._Org == vStart)
                        break;

                    // Continue to next edge.
                    eCur = eNext;
                }
            }
        }

        [Conditional("DEBUG")]
        public void Check()
        {
            MeshUtils.Edge e;

            MeshUtils.Face fPrev = _fHead, f;
            for (fPrev = _fHead; (f = fPrev._next) != _fHead; fPrev = f)
            {
                e = f._anEdge;
                do {
                    Debug.Assert(e._Sym != e);
                    Debug.Assert(e._Sym._Sym == e);
                    Debug.Assert(e._Lnext._Onext._Sym == e);
                    Debug.Assert(e._Onext._Sym._Lnext == e);
                    Debug.Assert(e._Lface == f);
                    e = e._Lnext;
                } while (e != f._anEdge);
            }
            Debug.Assert(f._prev == fPrev && f._anEdge == null);

            MeshUtils.Vertex vPrev = _vHead, v;
            for (vPrev = _vHead; (v = vPrev._next) != _vHead; vPrev = v)
            {
                Debug.Assert(v._prev == vPrev);
                e = v._anEdge;
                do
                {
                    Debug.Assert(e._Sym != e);
                    Debug.Assert(e._Sym._Sym == e);
                    Debug.Assert(e._Lnext._Onext._Sym == e);
                    Debug.Assert(e._Onext._Sym._Lnext == e);
                    Debug.Assert(e._Org == v);
                    e = e._Onext;
                } while (e != v._anEdge);
            }
            Debug.Assert(v._prev == vPrev && v._anEdge == null);

            MeshUtils.Edge ePrev = _eHead;
            for (ePrev = _eHead; (e = ePrev._next) != _eHead; ePrev = e)
            {
                Debug.Assert(e._Sym._next == ePrev._Sym);
                Debug.Assert(e._Sym != e);
                Debug.Assert(e._Sym._Sym == e);
                Debug.Assert(e._Org != null);
                Debug.Assert(e._Dst != null);
                Debug.Assert(e._Lnext._Onext._Sym == e);
                Debug.Assert(e._Onext._Sym._Lnext == e);
            }
            Debug.Assert(e._Sym._next == ePrev._Sym
                && e._Sym == _eHeadSym
                && e._Sym._Sym == e
                && e._Org == null && e._Dst == null
                && e._Lface == null && e._Rface == null);
        }
    }
}
