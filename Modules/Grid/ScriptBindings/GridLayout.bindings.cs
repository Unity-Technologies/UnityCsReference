// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>An abstract class that defines a grid layout.</summary>
    ///<seealso cref="Grid" />
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Grid/Public/GridMarshalling.h")]
    [NativeHeader("Modules/Grid/Public/Grid.h")]
    [NativeClass("GridLayout", PersistentTypeId = 0x67E12204)]
    public partial class GridLayout : Behaviour
    {
        public GridLayout() {}
        protected internal GridLayout(global::UnityEngine.EntityId id) : base(id) {}
        // Enums.
        ///<summary>The layout of the <see cref="GridLayout" />.</summary>
        ///<remarks>The layout determines the conversion of positions from cell space to local space and vice versa.</remarks>
        public enum CellLayout
        {
            ///<summary>Rectangular layout for cells in the <see cref="GridLayout" />.</summary>
            Rectangle = 0,
            ///<summary>Hexagonal layout for cells in the <see cref="GridLayout" />.</summary>
            Hexagon = 1,
            ///<summary>Isometric layout for cells in the <see cref="GridLayout" />.</summary>
            Isometric = 2,
            ///<summary>Isometric layout for cells in the <see cref="GridLayout" /> where any Z cell value set will be added as a Y value.</summary>
            IsometricZAsY = 3,
        }

        ///<summary>Swizzles cell positions to other positions.</summary>
        public enum CellSwizzle
        {
            ///<summary>Keeps the cell positions at XYZ.</summary>
            ///<remarks>This is the default.</remarks>
            XYZ = 0,
            ///<summary>Swizzles the cell positions from XYZ to XZY.</summary>
            XZY = 1,
            ///<summary>Swizzles the cell positions from XYZ to YXZ.</summary>
            YXZ = 2,
            ///<summary>Swizzles the cell positions from XYZ to YZX.</summary>
            YZX = 3,
            ///<summary>Swizzles the cell positions from XYZ to ZXY.</summary>
            ZXY = 4,
            ///<summary>Swizzles the cell positions from XYZ to ZYX.</summary>
            ZYX = 5
        }

        ///<summary>The size of each cell in the layout.</summary>
        public extern Vector3 cellSize
        {
            [FreeFunction("GridLayoutBindings::GetCellSize", HasExplicitThis = true)]
            get;
        }

        ///<summary>The size of the gap between each cell in the layout.</summary>
        public extern Vector3 cellGap
        {
            [FreeFunction("GridLayoutBindings::GetCellGap", HasExplicitThis = true)]
            get;
        }

        ///<summary>Cell shape and packing that this layout uses when converting between cell and local space.</summary>
        ///<remarks>See <see cref="GridLayout.CellLayout" /> for the available layouts, which include rectangle, hexagon, and isometric shapes.</remarks>
        public extern CellLayout cellLayout
        {
            get;
        }

        ///<summary>Cell swizzle order that this layout applies when converting cell positions to local space.</summary>
        ///<remarks>Swizzling reorders the cell axes. The default <see cref="GridLayout.CellSwizzle.XYZ" /> keeps cell X, Y, and Z mapped to local X, Y, and Z. <see cref="GridLayout.CellSwizzle.XZY" /> swaps the Y and Z axes, which is useful for placing a 2D grid on the XZ ground plane of a 3D scene. See <see cref="GridLayout.CellSwizzle" /> for all available orders.</remarks>
        public extern CellSwizzle cellSwizzle
        {
            get;
        }

        ///<summary>Returns the local bounds for a cell at the location.</summary>
        ///<param name="cellPosition">Location of the cell.</param>
        ///<returns>Local bounds of cell at the location.</returns>
        [FreeFunction("GridLayoutBindings::GetBoundsLocal", HasExplicitThis = true)]
        public extern Bounds GetBoundsLocal(Vector3Int cellPosition);

        ///<summary>Returns the local bounds for the groups of cells at the location.</summary>
        ///<param name="origin">Origin of the group of cells.</param>
        ///<param name="size">Size of the group of cells.</param>
        ///<returns>Local bounds of the group of cells at the location.</returns>
        public Bounds GetBoundsLocal(Vector3 origin, Vector3 size)
        {
            return GetBoundsLocalOriginSize(origin, size);
        }

        [FreeFunction("GridLayoutBindings::GetBoundsLocalOriginSize", HasExplicitThis = true)]
        private extern Bounds GetBoundsLocalOriginSize(Vector3 origin, Vector3 size);

        ///<summary>Converts a cell position to local position space.</summary>
        ///<param name="cellPosition">Cell position to convert.</param>
        ///<returns>Local position of the cell position.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Snap the GameObject to parent GridLayout
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GridLayout gridLayout = transform.parent.GetComponent<GridLayout>();
        ///        Vector3Int cellPosition = gridLayout.LocalToCell(transform.localPosition);
        ///        transform.localPosition = gridLayout.CellToLocal(cellPosition);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [FreeFunction("GridLayoutBindings::CellToLocal", HasExplicitThis = true)]
        public extern Vector3 CellToLocal(Vector3Int cellPosition);

        ///<summary>Converts a local position to cell position.</summary>
        ///<param name="localPosition">Local Position to convert.</param>
        ///<returns>Cell position of the local position.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Snap the GameObject to parent GridLayout
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GridLayout gridLayout = transform.parent.GetComponent<GridLayout>();
        ///        Vector3Int cellPosition = gridLayout.LocalToCell(transform.localPosition);
        ///        transform.localPosition = gridLayout.CellToLocal(cellPosition);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [FreeFunction("GridLayoutBindings::LocalToCell", HasExplicitThis = true)]
        public extern Vector3Int LocalToCell(Vector3 localPosition);

        ///<summary>Converts an interpolated cell position in floats to local position space.</summary>
        ///<remarks>Returns the local position in floats.</remarks>
        ///<param name="cellPosition">Interpolated cell position to convert.</param>
        ///<returns>Local position of the cell position.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Snap the GameObject to parent GridLayout center of cell
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GridLayout gridLayout = transform.parent.GetComponent<GridLayout>();
        ///        Vector3Int cellPosition = gridLayout.LocalToCell(transform.localPosition);
        ///        transform.localPosition = gridLayout.CellToLocalInterpolated(cellPosition + new Vector3(.5f, .5f, .5f));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [FreeFunction("GridLayoutBindings::CellToLocalInterpolated", HasExplicitThis = true)]
        public extern Vector3 CellToLocalInterpolated(Vector3 cellPosition);

        ///<summary>Converts a local position to cell position.</summary>
        ///<remarks>Returns the interpolated cell position in floats, rather than the exact cell position.</remarks>
        ///<param name="localPosition">Local Position to convert.</param>
        ///<returns>Interpolated cell position of the local position.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Move GameObject left by 1/4th of cell width of parent GridLayout
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GridLayout gridLayout = transform.parent.GetComponent<GridLayout>();
        ///        Vector3 cellPosition = gridLayout.LocalToCellInterpolated(transform.localPosition);
        ///        cellPosition += Vector3.left * 0.25f;
        ///        transform.localPosition = gridLayout.CellToLocalInterpolated(cellPosition);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [FreeFunction("GridLayoutBindings::LocalToCellInterpolated", HasExplicitThis = true)]
        public extern Vector3 LocalToCellInterpolated(Vector3 localPosition);

        ///<summary>Converts a cell position to world position space.</summary>
        ///<param name="cellPosition">Cell position to convert.</param>
        ///<returns>World position of the cell position.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Snap the GameObject to parent GridLayout
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GridLayout gridLayout = transform.parent.GetComponentInParent<GridLayout>();
        ///        Vector3Int cellPosition = gridLayout.WorldToCell(transform.position);
        ///        transform.position = gridLayout.CellToWorld(cellPosition);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [FreeFunction("GridLayoutBindings::CellToWorld", HasExplicitThis = true)]
        public extern Vector3 CellToWorld(Vector3Int cellPosition);

        ///<summary>Converts a world position to cell position.</summary>
        ///<remarks>A <see cref="GridLayout" /> has no bounds. Cells extend infinitely in every direction from the origin, so any world position maps to the cell that contains it.</remarks>
        ///<param name="worldPosition">World Position to convert.</param>
        ///<returns>Cell position of the world position.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Snap the GameObject to parent GridLayout
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GridLayout gridLayout = transform.parent.GetComponentInParent<GridLayout>();
        ///        Vector3Int cellPosition = gridLayout.WorldToCell(transform.position);
        ///        transform.position = gridLayout.CellToWorld(cellPosition);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [FreeFunction("GridLayoutBindings::WorldToCell", HasExplicitThis = true)]
        public extern Vector3Int WorldToCell(Vector3 worldPosition);

        ///<summary>Converts a local position to world position.</summary>
        ///<param name="localPosition">Local Position to convert.</param>
        ///<returns>World position of the local position.</returns>
        [FreeFunction("GridLayoutBindings::LocalToWorld", HasExplicitThis = true)]
        public extern Vector3 LocalToWorld(Vector3 localPosition);

        ///<summary>Converts a world position to local position.</summary>
        ///<param name="worldPosition">World Position to convert.</param>
        ///<returns>Local position of the world position.</returns>
        [FreeFunction("GridLayoutBindings::WorldToLocal", HasExplicitThis = true)]
        public extern Vector3 WorldToLocal(Vector3 worldPosition);

        ///<summary>Get the default center coordinate of a cell for the set layout of the <see cref="Grid" />.</summary>
        ///<returns>Cell Center coordinate.</returns>
        [FreeFunction("GridLayoutBindings::GetLayoutCellCenter", HasExplicitThis = true)]
        public extern Vector3 GetLayoutCellCenter();

        ///<summary>Gets the points which outline a cell of the <see cref="GridLayout" />.</summary>
        ///<remarks>The points are in cell space, so they describe a cell before <see cref="cellSize" /> is applied. Scale them by
        ///<see cref="cellSize" /> for the shape of a cell, then <see cref="Grid.Swizzle" /> that by
        ///<see cref="Grid.cellSwizzle" /> and offset it by <see cref="CellToLocal" /> to place it on a given cell.
        ///They trace the cell's outline clockwise, so consecutive points are the cell's edges.
        ///The same points serve every cell of the <see cref="GridLayout" />, so they are read straight from the engine and
        ///nothing is copied or allocated.</remarks>
        ///<returns>The points which outline a cell, in cell space.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // Draw the outline of the cell at the origin
        ///using System;
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnDrawGizmos()
        ///    {
        ///        Grid grid = GetComponent<Grid>();
        ///        ReadOnlySpan<Vector3> points = grid.GetCellPoints();
        ///        Vector3 origin = grid.CellToLocal(Vector3Int.zero);
        ///        for (int i = 0; i < points.Length; ++i)
        ///        {
        ///            Vector3 from = origin + Grid.Swizzle(grid.cellSwizzle, Vector3.Scale(points[i], grid.cellSize));
        ///            Vector3 to = origin + Grid.Swizzle(grid.cellSwizzle, Vector3.Scale(points[(i + 1) % points.Length], grid.cellSize));
        ///            Gizmos.DrawLine(grid.LocalToWorld(from), grid.LocalToWorld(to));
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [FreeFunction("GridLayoutBindings::GetCellPoints", HasExplicitThis = true)]
        public extern ReadOnlySpan<Vector3> GetCellPoints();

        [RequiredByNativeCode]
        private void DoNothing() {}
    }
}
