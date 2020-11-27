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

namespace ClusterImagesClipper
{
	public static class Utils
	{
		// https://forums.autodesk.com/t5/net/sorting-a-list-point-of-a-polygon-clockwise/td-p/7366469
		public static double PolygonSignedArea(List<List<IntPoint>> paths)
		{
			double area = 0.0;
			if (paths.Count == 0) return area;

			var path = paths[0];
			var startingPoint = ClipperPointToPoint(path[0]);
			for (int i = 1; i < path.Count - 1; i++)
			{
				area += DoubleSignedArea(startingPoint, ClipperPointToPoint(path[i]), ClipperPointToPoint(path[i + 1]));
			}
			return area / 2.0;
		}

		private static Point ClipperPointToPoint(IntPoint source) => new Point(Convert.ToInt32(source.X), Convert.ToInt32(source.Y));

		private static double DoubleSignedArea(Point p1, Point p2, Point p3) =>
			(p2.X - p1.X) * (p3.Y - p1.Y) - (p2.Y - p1.Y) * (p3.X - p1.X);


		public static void DrawPolygons(List<List<IntPoint>> paths, Color mainColor, bool debugMode, Form1 form)
		{
			if (!Config.DrawLinesOnInterface) return;

			Graphics g = form.CreateGraphics();
			Pen p;
			Font drawFont = new Font("Arial", 10);
			Brush drawBrush = new SolidBrush(Color.Black);
			StringFormat drawFormat = new StringFormat();

			string drawString;

			for (int i = 0; i < paths.Count; i++)
			{
				var path = paths[i];
				var p1 = ClipperPointToPoint(path[0]);
				if (debugMode)
				{
					drawString = "0";
					g.DrawString(drawString, drawFont, drawBrush, p1.X, p1.Y, drawFormat);
				}

				for (int j = 1; j < path.Count; j++)
				{
					if (mainColor != null)
						p = new Pen(mainColor, 2);
					else
						p = new Pen(Color.FromArgb(255 * j / path.Count, 255 - (255 * j / path.Count), 0), 2);

					var p2 = ClipperPointToPoint(path[j]);
					g.DrawLine(p, p1, p2);
					if (debugMode)
					{
						drawString = j.ToString();
						g.DrawString(drawString, drawFont, drawBrush, p2.X, p2.Y, drawFormat);
					}
					p1 = p2;
				}
				var startingPoint = ClipperPointToPoint(path[0]);
				p = new Pen(Color.FromArgb(255, 0, 0), 2);
				g.DrawLine(p, p1, startingPoint);
			}
		}

		public static void DrawBestPositionsInterface(Shape boundaryShape, List<Shape> shapes, Form1 form)
		{
			if(boundaryShape != null)
            {
				var boundaryShapePath = new Paths(1);
				boundaryShapePath.Add(new Path(boundaryShape.GetBestDrawingCoordinates()));
				DrawPolygons(boundaryShapePath, Color.Green, false, form);
			}
			

			var drawableShapes = new Paths(shapes.Count);
			foreach (var shape in shapes)
			{
				drawableShapes.Add(new Path(shape.GetBestDrawingCoordinates()));
			}
			DrawPolygons(drawableShapes, Color.Blue, false, form);
		}

		public static Shape GetShapeByIndex(List<Shape> shapes, int index)
		{
			return shapes.Find(x => x.Index == index);
		}

		// Can be optimized by removing possible set from N future runs, however n^2 aint an issue with 100 shapes or so
		public static List<(int, int)> GetCollidingShapes(List<Shape> shapes)
		{
			var collidedShapes = new List<(int, int)>();
			foreach (var shape in shapes)
			{ 
				foreach (var externalShape in shapes)
				{
					if (shape.IsNotSelf(externalShape) && shape.CollidesWith(externalShape))
					{
						var rankedIndexTuple =
							(Math.Min(shape.Index, externalShape.Index), Math.Max(shape.Index, externalShape.Index));

						if (!collidedShapes.Contains(rankedIndexTuple))
						{
							collidedShapes.Add(rankedIndexTuple);
						}
					}
				}
			}
			return collidedShapes;
		}

		private static Random rng = new Random();

		public static void Shuffle<T>(this IList<T> list)
		{
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = rng.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}
	}
}
