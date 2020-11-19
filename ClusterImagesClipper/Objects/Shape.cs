using ClipperLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterImagesClipper.Objects
{
    public class Shape
    {
        public int Index { get; set; }
        public string FileName{ get; set; }
        public string SourcePath{ get; set; }
        public string ProcessedPath{ get; set; }
        public string ExpandedPath{ get; set; }
        public List<Point> BoundingBoxPoints { get; set; }

        public Image ImageRef { get; set; }

        public Point Location { get; set; }
        public Point BestLocation { get; set; }

        public Color DrawColor { get; set; }

        // TODO: Handle 2D rotation
        public float Rotation { get; set; }
        public float BestRotation { get; set; }

        public List<IntPoint> GetDrawingCoordinates()
        {
            var drawCoordinates = new List<IntPoint>();

            Point center = new Point(Location.X + ImageRef.Width/2, Location.Y + ImageRef.Height / 2);
            // Translate and rotate
            foreach (var point in BoundingBoxPoints)
            {
                Point translatedPoint = new Point(point.X + Location.X, point.Y + Location.Y);
                Point rotatedPoint = RotatePoint(translatedPoint, center, Rotation);
                drawCoordinates.Add(new IntPoint(rotatedPoint.X, rotatedPoint.Y));
            }
            return drawCoordinates;
        }

        private Point RotatePoint(Point pointToRotate, Point centerPoint, double angleInDegrees)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);
            return new Point
            {
                X =
                    (int)
                    (cosTheta * (pointToRotate.X - centerPoint.X) -
                    sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                Y =
                    (int)
                    (sinTheta * (pointToRotate.X - centerPoint.X) +
                    cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
            };
        }

        public bool CollidesWith(Shape externalShape)
        {
            var shapeRectacleBoundingBox = new Rectangle(Location.X, Location.Y, ImageRef.Width, ImageRef.Width);
            var externalRectacleBoundingBox = new Rectangle(externalShape.Location.X, externalShape.Location.Y, externalShape.ImageRef.Width, externalShape.ImageRef.Width);
            return shapeRectacleBoundingBox.IntersectsWith(externalRectacleBoundingBox);
        }

        public bool IsNotSelf(Shape externalShape)
        {
            return Index != externalShape.Index;
        }

        public void AddRotation(int rotationAmount)
        {
            Rotation = rotationAmount;
        }

        public Shape()
        {

        }

        public Shape(string filePath, string sourceFolder, string targetFolder, int index, Point startingLocation)
        {
            var fileName = AutoCrop.FilepathToFilename(filePath, sourceFolder);
            var fileAsBitmap = AutoCrop.FilepathToBitmap(filePath);

            this.SourcePath = fileName;
            this.FileName = fileName;

            this.ImageRef = AutoCrop.TrimWhiteSpace(this, fileAsBitmap);
            this.BoundingBoxPoints = AutoCrop.GenerateBoundingBox(this, Config.BoundarBoxAccuracy);

            var processedPath = targetFolder + "//" + this.FileName + "_trimmed.png";
            this.ProcessedPath = processedPath;
            this.ImageRef.Save(processedPath, ImageFormat.Png);

            this.Location = startingLocation;
            this.Rotation = 0;
            this.BestLocation = this.Location;

            this.Index = index;
        }
    }
}
