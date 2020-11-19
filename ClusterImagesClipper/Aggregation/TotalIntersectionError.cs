using ClipperLib;
using ClusterImagesClipper.Objects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;


namespace ClusterImagesClipper.Aggregation
{
    public static class TotalIntersectionError
    {
        public static double ShapeIntersection(Shape boundaryShape, List<Shape> shapes, Form1 form)
        {
			var collidedShapes = new List<(int, int)>();
			Paths subject = new Paths(1);
			Paths collidingClips = new Paths(1);
			var totalError = 0.0;

			foreach (var shape in shapes)
			{
				// Add shape to subject
				subject = new Paths(1);
				subject.Add(new Path(shape.GetDrawingCoordinates()));

				// Find all shapes with collision distance and add to temporary clib
				collidingClips = new Paths(2);
				foreach (var externalShape in shapes)
				{
					if (shape.IsNotSelf(externalShape) && shape.CollidesWith(externalShape))
					{
						var rankedIndexTuple =
							(Math.Min(shape.Index, externalShape.Index), Math.Max(shape.Index, externalShape.Index));

						if (!collidedShapes.Contains(rankedIndexTuple))
						{
							collidedShapes.Add(rankedIndexTuple);
							collidingClips.Add(new Path(externalShape.GetDrawingCoordinates()));
						}
					}
				}

				Paths solution = new Paths(1);

				Clipper c = new Clipper();
				c.AddPolygons(subject, PolyType.ptSubject);
				c.AddPolygons(collidingClips, PolyType.ptClip);
				c.Execute(ClipType.ctIntersection, solution,
				  PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);

				Utils.DrawPolygons(solution, Color.Red, true, form);
				var intersectionError = Utils.PolygonSignedArea(solution);
				Logger.SimpleDebug("Area of intersection for shape " + shape.Index + " is " + intersectionError);
				totalError += intersectionError;
			}
			return totalError;
		}
    }
}
