using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipperLib;
using ClusterImagesClipper.Objects;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;


namespace ClusterImagesClipper.Aggregation
{
    public static class OutOfBoundaryError
    {
        public static double UnoccupiedBoundaryArea(Shape boundaryShape, List<Shape> shapes, Form1 form)
        {
			Paths boundarySolution = new Paths(1);
			Paths boundaryPaths = new Paths(1);
			Paths shapesWithinBoundaries = new Paths(1);
			foreach (var shape in shapes)
			{
				shapesWithinBoundaries.Add(new Path(shape.GetDrawingCoordinates()));
			}

			boundaryPaths.Add(new Path(boundaryShape.GetDrawingCoordinates()));
			Utils.DrawPolygons(boundaryPaths, Color.Green, false, form);

			Clipper c = new Clipper();
			c.AddPolygons(boundaryPaths, PolyType.ptSubject);
			c.AddPolygons(shapesWithinBoundaries, PolyType.ptClip);
			c.Execute(ClipType.ctIntersection, boundarySolution,
			  PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);

			Utils.DrawPolygons(boundarySolution, Color.Orange, true, form);
			var totalBoundaryArea = Utils.PolygonSignedArea(boundaryPaths);
			var boundaryIntersection = Utils.PolygonSignedArea(boundarySolution);
			Logger.SimpleDebug("Area of shapes within boundary is " + boundaryIntersection + " out of " + totalBoundaryArea);
			return totalBoundaryArea - boundaryIntersection;

		}
	}
}
