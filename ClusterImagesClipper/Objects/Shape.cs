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
        public Shape BoundaryShapeRef { get; set; }

        public Image ImageRef { get; set; }

        public Point Location { get; set; }
        public Point BestLocation { get; set; }

        public Color DrawColor { get; set; }

        public float Rotation { get; set; }
        public float BestRotation { get; set; }
        public double ErrorContribution { get; set; }

        // Real shape
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

        // Replica - TODO: Create this as a separate class?
        public Shape(Point location, float rotation, List<Point> boundingBoxPoints, Image imageRef)
        {
            this.Location = location;
            this.Rotation = rotation;
            this.BoundingBoxPoints = boundingBoxPoints;
            this.ImageRef = imageRef;
        }

        // TODO: Remove the get from functions and simply name CurrentDrawingCoordinates and BestDrawingCoordinates
        public List<IntPoint> GetDrawingCoordinates()
        {
            return GetBoundingCoordinates(Location, Rotation);
        }

        public List<IntPoint> GetBestDrawingCoordinates()
        {
            return GetBoundingCoordinates(BestLocation, BestRotation);
        }

        private List<IntPoint> GetBoundingCoordinates(Point referencePointLocation, double referenceRotation)
        {
            var drawCoordinates = new List<IntPoint>();

            Point center = new Point(referencePointLocation.X + ImageRef.Width / 2, referencePointLocation.Y + ImageRef.Height / 2);
            // Translate and rotate
            foreach (var point in BoundingBoxPoints)
            {
                Point translatedPoint = new Point(point.X + referencePointLocation.X, point.Y + referencePointLocation.Y);
                Point rotatedPoint = RotatePoint(translatedPoint, center, referenceRotation);
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
            if (externalShape == null) return false;
            var shapeRectacleBoundingBox = new Rectangle(Location.X, Location.Y, ImageRef.Width, ImageRef.Width);
            var externalRectacleBoundingBox = new Rectangle(externalShape.Location.X, externalShape.Location.Y, externalShape.ImageRef.Width, externalShape.ImageRef.Width);
            return shapeRectacleBoundingBox.IntersectsWith(externalRectacleBoundingBox);
        }

        public bool IsNotSelf(Shape externalShape)
        {
            return Index != externalShape.Index;
        }

        public void AddRotation(float rotationAmount)
        {
            Rotation = rotationAmount;
        }

        public void FindNewPosition(Random rand, List<Shape> shapes)
        {
            // Are we colliding with boundary shape?
            if (Aggregation.IntersectionError.SingleShapeIntersection(this, BoundaryShapeRef) <= 0)
            {
                var withinBoundaryShapeX = rand.Next(BoundaryShapeRef.Location.X, BoundaryShapeRef.Location.X + BoundaryShapeRef.ImageRef.Width);
                var withinBoundaryShapeY = rand.Next(BoundaryShapeRef.Location.Y, BoundaryShapeRef.Location.Y + BoundaryShapeRef.ImageRef.Height);
                this.Location = new Point(withinBoundaryShapeX, withinBoundaryShapeY);
                this.BestLocation = this.Location;
                return;
            }
            
            var collisions = GetCollidingShapes(shapes);
            foreach (var shapeIndex in collisions)
            {
                var externalShape = Utils.GetShapeByIndex(shapes, shapeIndex);
                var minimalCollisionShape = FindMinimalCollision(externalShape);
                externalShape.Location = minimalCollisionShape.Location;

                var rotation = FindMinimalRotation(externalShape);
                externalShape.AddRotation(rotation);
            }
            Logger.SimpleDebug("Shape " + this.Index + " is " + this.Location);
        }

        private float FindMinimalRotation(Shape externalShape)
        {
            // Make this the error that would mount up if they were in the same location
            var maxErrorShape = new Shape(externalShape.Location, externalShape.Rotation, externalShape.BoundingBoxPoints, externalShape.ImageRef);
            var actualError = BoundaryAndCollisionError(externalShape);

            var selectedShape = maxErrorShape;
            selectedShape.ErrorContribution = actualError;

            int steps = 3;
            var rotationThreshold = (float)Config.MaxRotation / steps;
            for(int i = 0; i < steps*2+1; i++)
            {
                var rotationAmount = -1 * Config.MaxRotation + i * rotationThreshold;
                var rotationShape = new Shape(externalShape.Location, rotationAmount, externalShape.BoundingBoxPoints, externalShape.ImageRef);
                selectedShape = CompareErrorAndSelectShape(rotationShape.Location, rotationShape, selectedShape);
            }
            return selectedShape.Rotation;
        }

        private Shape FindMinimalCollision(Shape externalShape)
        {
            // Make this the error that would mount up if they were in the same location
            var maxErrorShape = new Shape(this.Location, this.Rotation, externalShape.BoundingBoxPoints, externalShape.ImageRef);
            var maxError = BoundaryAndCollisionError(maxErrorShape);
            var actualError = BoundaryAndCollisionError(externalShape);
            var distanceRatio = actualError / maxError;

            var selectedShape = maxErrorShape;
            selectedShape.ErrorContribution = maxError;

            // Create 8 replicas and move the external in any of the direction to a new location, pick the one that yields the lowest error
            var points = new List<Point>()
            {
                DirectionalPoint(externalShape, distanceRatio,0.0,1.0), // N
                DirectionalPoint(externalShape, distanceRatio,0.5,0.5), // NE
                DirectionalPoint(externalShape, distanceRatio,1.0,0.0), // E
                DirectionalPoint(externalShape, distanceRatio,0.5,-0.5), // SE
                DirectionalPoint(externalShape, distanceRatio,0.0,-1.0), // S
                DirectionalPoint(externalShape, distanceRatio,-0.5,-0.5), // SW
                DirectionalPoint(externalShape, distanceRatio,-1.0,0.0), // W
                DirectionalPoint(externalShape, distanceRatio,-0.5,0.5)  // NW
            };
            foreach (var point in points)
            {
                selectedShape = CompareErrorAndSelectShape(point, externalShape, selectedShape);
            }
            return selectedShape;
        }
        private Point DirectionalPoint(Shape externalShape, double ratio, double xMultiplier, double yMultiplier)
        {
            return new Point(
                Convert.ToInt32(this.Location.X + externalShape.ImageRef.Width * ratio * xMultiplier),
                Convert.ToInt32(this.Location.Y + externalShape.ImageRef.Height * ratio * yMultiplier)
                );
        }
        private double BoundaryAndCollisionError(Shape replicaShape)
        {
            return Aggregation.IntersectionError.SingleShapeIntersection(this, replicaShape)
                + Aggregation.OutOfBoundaryError.SingleUnoccupiedBoundaryArea(BoundaryShapeRef, replicaShape);
        }
        private Shape CompareErrorAndSelectShape(Point destinationPoint, Shape externalShape, Shape selectedShape)
        {
            var compareShape = new Shape(destinationPoint, externalShape.Rotation, externalShape.BoundingBoxPoints, externalShape.ImageRef);
            var compareError = BoundaryAndCollisionError(compareShape);
            if (compareError < selectedShape.ErrorContribution)
            {
                compareShape.ErrorContribution = compareError;
                return compareShape;
            }
            return selectedShape;
        }

        private List<int> GetCollidingShapes(List<Shape> shapes)
        {
            var collidedShapes = new List<int>();
            foreach (var externalShape in shapes)
            {
                if (this.IsNotSelf(externalShape) && this.CollidesWith(externalShape))
                {
                    collidedShapes.Add(externalShape.Index);
                }
            }
            return collidedShapes;
        }

    }
}
