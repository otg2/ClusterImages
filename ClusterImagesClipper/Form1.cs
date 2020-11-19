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
			shapes = AutoCrop.GenerateShapes(sourceFolder, targetFolder);

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

		// TODO: Add to aggregates
		
		// TODO: Clean up this function and split up into aggregates based on error calculation
		void test()
        {
			var totalError = 0.0;
			var outOfBboundaryError = 0.0;
			var shapeIntersectionError = 0.0;

			if (boundaryShape != null)
			{
				outOfBboundaryError = Aggregation.OutOfBoundaryError.UnoccupiedBoundaryArea(boundaryShape, shapes, this);
			}
			shapeIntersectionError = Aggregation.TotalIntersectionError.ShapeIntersection(boundaryShape, shapes, this);


			var drawableShapes = new Paths(shapes.Count);
			foreach (var shape in shapes)
			{
				drawableShapes.Add(new Path(shape.GetDrawingCoordinates()));
			}
			Utils.DrawPolygons(drawableShapes, Color.Blue, false, this);

			
			Logger.SimpleDebug("Total area out of bounds " + outOfBboundaryError);
			Logger.SimpleDebug("Total area of intersection " + shapeIntersectionError);

			totalError += outOfBboundaryError * Config.BoundaryErrorModifier;
			totalError += shapeIntersectionError * Config.IntersectionErrorModifier;
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
			minError = int.MaxValue;

			for (int i = 0; i < Config.RoughCalculate; i++)
            {
				var rand = new Random();
				// To test error
				foreach (var shape in shapes)
				{
					// This obvious doesn't work. First, simply make random based on image size
					// Should be config
					var xRand = rand.Next(0, 500);
					var yRand = rand.Next(0, 500);
					var rotation = rand.Next(-Config.MaxRotation, Config.MaxRotation);
					shape.Location = new Point(xRand, yRand);
					shape.AddRotation(rotation);
					Logger.SimpleDebug("Shape " + shape.Index + " is " + shape.Location);
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
			var maxWidth = boundaryShape.BestLocation.X + boundaryShape.ImageRef.Width;
			var maxHeight = boundaryShape.BestLocation.Y + boundaryShape.ImageRef.Height;

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
