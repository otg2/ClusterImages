using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClipperLib;

using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;

using System.Diagnostics;
using ClusterImagesClipper.Objects;

namespace ClusterImagesClipper
{
    public partial class Form1 : Form
    {
		public List<Shape> shapes;
		public Shape boundaryShape;

		private int minError = int.MaxValue;
		private string sourceFolder, targetFolder, expandedTargetFolder;
        

        public Form1()
        {
			InitializeTargetFolders(Config.Prefix);
           
			// If we want to fit clustering within a specific shape, make the error function based on fitness within said space
			boundaryShape = AutoCrop.GenerateBoundaryShape(sourceFolder, targetFolder);
			shapes = AutoCrop.GenerateShapes(sourceFolder, targetFolder, boundaryShape);

			InitializeComponent();
		}

		private void InitializeTargetFolders(string prefix)
        {
			var checkedPrefix = String.IsNullOrEmpty(prefix)
				? "default"
				: prefix;
			sourceFolder = checkedPrefix + "/" + Config.SourceFolder;
			targetFolder = checkedPrefix + "/" + Config.TargetFolder;
			expandedTargetFolder = checkedPrefix + "/" + Config.ExpandedTargetFolder;

			System.IO.Directory.CreateDirectory(sourceFolder);
			System.IO.Directory.CreateDirectory(targetFolder);
			System.IO.Directory.CreateDirectory(expandedTargetFolder);
		}

		void test()
        {
			var totalError = 0.0;
			var outOfBboundaryError = 0.0;
			var shapeIntersectionError = 0.0;

			if (boundaryShape != null)
			{
				outOfBboundaryError = Aggregation.OutOfBoundaryError.UnoccupiedBoundaryArea(boundaryShape, shapes, this);
			}
			shapeIntersectionError = Aggregation.IntersectionError.TotalShapeIntersection(shapes, this);


			var drawableShapes = new Paths(shapes.Count);
			foreach (var shape in shapes)
			{
				drawableShapes.Add(new Path(shape.GetDrawingCoordinates()));
			}
			Utils.DrawPolygons(drawableShapes, Color.Blue, false, this);

			
			Logger.SimpleDebug("Total area out of bounds " + outOfBboundaryError);
			Logger.SimpleDebug("Total area of intersection " + shapeIntersectionError);

			totalError += outOfBboundaryError;
			totalError += shapeIntersectionError;
			Logger.SimpleDebug("Total error " + totalError);

			// TODO: Start look at converge/ml formula

			if (totalError < minError)
            {
				minError = Convert.ToInt32(totalError);
				Logger.SimpleDebug("BEST ACHIEVED VIA "  + minError);
				foreach (var shape in shapes)
				{
					shape.BestLocation = shape.Location;
					shape.BestRotation = shape.Rotation;
				}
            }
		}

		private void button1_Click(object sender, EventArgs e)
        {
			Debug.WriteLine("Starting...");
			Graphics g = this.CreateGraphics();
			g.FillRectangle(Brushes.White, 0, 0, Width, Height);

			//if(Config.NumberOfRuns == 1)
			// TODO: Add a reset button?
			minError = int.MaxValue;

            var rand = new Random();

			for (int i = 0; i < Config.NumberOfRuns; i++)
            {
				// Always draw the best possible outcome when we are not drawing lines
				if (i == Config.NumberOfRuns - 1 && !Config.DrawLinesOnInterface )
                {
					Config.DrawLinesOnInterface = true;
					Utils.DrawBestPositionsInterface(boundaryShape, shapes, this);
					// TODO: Draw min error on interface
					Config.DrawLinesOnInterface = false;
				}

				//var collidingShapes = Utils.GetCollidingShapes(shapes);

				// Shuffle them to change order of new position method
				shapes.Shuffle();

				// To test error
				foreach (var shape in shapes)
				{
					//var collisions = collidingShapes.Where(x => x.Item1 == shape.Index || x.Item2 == shape.Index).ToList();
					if(shape.FileName=="15")
                    {
						var amm = 43;
                    }
					shape.FindNewPosition(rand, shapes);
					//collidingShapes = collidingShapes.Where(x => x.Item1 != shape.Index && x.Item2 != shape.Index).ToList();
				}
				test();
			}
			Debug.WriteLine("Generating");
			ExportImages();
			Debug.WriteLine("Done");
		}
		// When done, export images with best location and rotation as png
		private void ExportImages() // object sender, EventArgs e
		{
			// On complete, expand all shapes with dimension of max width and height.
			var maxWidth = boundaryShape != null 
				? boundaryShape.BestLocation.X + boundaryShape.ImageRef.Width
				: 0;
			var maxHeight = boundaryShape != null
				? boundaryShape.BestLocation.Y + boundaryShape.ImageRef.Height
				: 0;
			foreach (var shape in shapes)
            {
				var shapeTotalWidth = shape.BestLocation.X + shape.ImageRef.Width;
				var shapeTotalHeight = shape.BestLocation.Y + shape.ImageRef.Height;

				if (shapeTotalWidth > maxWidth) maxWidth = shapeTotalWidth;
				if (shapeTotalHeight > maxHeight) maxHeight = shapeTotalHeight;
			}

			AutoCrop.GenerateExpandedImages(expandedTargetFolder, shapes, boundaryShape, maxWidth, maxHeight);
		}
	}
}
