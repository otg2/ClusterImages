using ClusterImagesClipper.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterImagesClipper
{
    public static class AutoCrop
    {
        public static Shape GenerateBoundaryShape(string sourceFolder, string targetFolder)
        {
            // TODO: scale up boundary shape based on average size of ther shapes or smth
            var files = GetImagePaths(sourceFolder);
            foreach (var file in files)
            {
                if (file.Contains("boundary"))
                {
                    var boundaryShape = new Shape(file, sourceFolder, targetFolder, 0, new Point(150, 150));
                    return boundaryShape;
                }
            }
            return null;
        }

        public static List<Shape> GenerateShapes(string sourceFolder, string targetFolder, Shape boundaryShapeRef)
        {
            int assignIndex = 1;
            int assignedWidth = 0;
            int assignedHeight = 0;

            List<Shape> shapes = new List<Shape>();

            var files = GetImagePaths(sourceFolder);
            foreach (var file in files)
            {
                if (file.Contains("boundary")) continue; // Skip adding boundary in shapes, that comes later
                var shape = new Shape(file, sourceFolder, targetFolder, assignIndex, new Point(assignedWidth, assignedHeight));
                shape.BoundaryShapeRef = boundaryShapeRef;
                shapes.Add(shape);

                assignIndex++;
                // TODO: Handle how initial coordinates are given
                assignedWidth += Convert.ToInt32(shape.ImageRef.Width / shapes.Count);
                assignedHeight += Convert.ToInt32(shape.ImageRef.Height / shapes.Count);
            }

            return shapes;
        }

       

        public static string FilepathToFilename(string path, string sourceFolder) => path.Replace(sourceFolder, "").Replace("\\", "").Replace(".png", "");

        private static List<string> GetImagePaths(string sourceFolder)
        {
            var filepaths = new List<string>();
            var ext = new List<string> { Config.ExtensionType };

            var dir = Directory.GetCurrentDirectory();
            var files = Directory
                .EnumerateFiles(sourceFolder, "*.*", SearchOption.AllDirectories)
                .Where(s => ext.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant()));

            foreach (var file in files)
                filepaths.Add(file);
            return filepaths;
        }

        public static Bitmap FilepathToBitmap(string filePath)
        {
            Bitmap bitmap;
            using (Stream bmpStream = File.Open(filePath, FileMode.Open))
            {
                Image image = Image.FromStream(bmpStream);
                bitmap = new Bitmap(image);
            }
            // THINK: Do we want to scale down the boundary shape?
            if (Config.ScaleImages && !filePath.Contains("boundary"))
            {
                if(Config.ScaleUseFactoring)
                    bitmap = new Bitmap(bitmap, new Size(bitmap.Width / Config.ScaleFactor, bitmap.Height / Config.ScaleFactor));
                else
                {
                    var targetFactor = (double)bitmap.Width / Config.ScaleTargetWidth;
                    var width = Convert.ToInt32(bitmap.Width / targetFactor);
                    var height = Convert.ToInt32(bitmap.Height / targetFactor);
                    bitmap = new Bitmap(bitmap,  new Size(width, height));
                }
            }
            return bitmap;
        }

        // TODO: Use optimized interpolation for resizing?
        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        /*public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }*/

        // TODO: Fix bounding bound of shape 2 (curved forms)
        // TODO: When that is done, fix ordering of point. Can be done via clipper and no path intersection
        public static Image TrimWhiteSpace(Shape shape, Bitmap bmp)
        {
            if (Image.GetPixelFormatSize(bmp.PixelFormat) != 32)
                throw new InvalidOperationException("Autocrop currently only supports 32 bits per pixel images.");

            // Initialize variables
            var cropColor = Color.Transparent;

            var bottom = 0;
            var left = bmp.Width; // Set the left crop point to the width so that the logic below will set the left value to the first non crop color pixel it comes across.
            var right = 0;
            var top = bmp.Height; // Set the top crop point to the height so that the logic below will set the top value to the first non crop color pixel it comes across.

            var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);

            // Trim image and store bounding box
            unsafe
            {
                var dataPtr = (byte*)bmpData.Scan0;

                for (var y = 0; y < bmp.Height; y++)
                {
                    for (var x = 0; x < bmp.Width; x++)
                    {
                        var rgbPtr = dataPtr + (x * 4);

                        var b = rgbPtr[0];
                        var g = rgbPtr[1];
                        var r = rgbPtr[2];
                        var a = rgbPtr[3];

                        // If any of the pixel RGBA values don't match and the crop color is not transparent, or if the crop color is transparent and the pixel A value is not transparent
                        if ((cropColor.A > 0 && (b != cropColor.B || g != cropColor.G || r != cropColor.R || a != cropColor.A)) || (cropColor.A == 0 && a != 0))
                        {
                            if (x < left)
                                left = x;

                            if (x >= right)
                                right = x + 1;

                            if (y < top)
                                top = y;

                            if (y >= bottom)
                                bottom = y + 1;
                        }

                        /*
                         if( y % heightInterval ==0)
                        {
                            boundingLeft = bitmap.widht;
                            boundingRight = 0;
                        }
                         */
                    }

                    dataPtr += bmpData.Stride;
                }
            }

            bmp.UnlockBits(bmpData);

            if (left < right && top < bottom)
                return bmp.Clone(new Rectangle(left, top, right - left, bottom - top), bmp.PixelFormat);

            return null; // Entire image should be cropped, so just return null
        }

        // TODO: Combine this with upper function to skip scanning image twice
        public static List<Point> GenerateBoundingBox(Shape shape, int n)
        {
            var bitmap = new Bitmap(shape.ImageRef);

            // density - split image into NxN frames
            float densityPercentage = 1.0f / n;
            var heightInterval = Convert.ToInt32((bitmap.Height * densityPercentage) / 2);

            var boundingBoxPoints = new List<Point>();

            if (Image.GetPixelFormatSize(bitmap.PixelFormat) != 32)
                throw new InvalidOperationException("Autocrop currently only supports 32 bits per pixel images.");

            // Initialize variables
            var cropColor = Color.Transparent;

            var left = bitmap.Width;
            var right = 0;

            var bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);

            // Trim image and store bounding box
            unsafe
            {
                var dataPtr = (byte*)bmpData.Scan0;

                for (var y = 0; y < bitmap.Height; y += heightInterval)
                {
                    for (var x = 0; x < bitmap.Width; x++)
                    {
                        var rgbPtr = dataPtr + (x * 4);

                        var b = rgbPtr[0];
                        var g = rgbPtr[1];
                        var r = rgbPtr[2];
                        var a = rgbPtr[3];

                        // If any of the pixel RGBA values don't match and the crop color is not transparent, or if the crop color is transparent and the pixel A value is not transparent
                        if ((cropColor.A > 0 && (b != cropColor.B || g != cropColor.G || r != cropColor.R || a != cropColor.A)) || (cropColor.A == 0 && a != 0))
                        {
                            if (x < left)
                                left = x;

                            if (x >= right)
                                right = x + 1;
                        }
                    }

                    var pointLeft = new Point(left, y);
                    var pointRight = new Point(right, y);
                    boundingBoxPoints.Add(new Point(left, y));
                    boundingBoxPoints.Add(new Point(right, y));

                    left = bitmap.Width;
                    right = 0;

                    dataPtr += bmpData.Stride * heightInterval;
                }
            }
            bitmap.UnlockBits(bmpData);

            var indexedNums = boundingBoxPoints.Select((num, idx) => new { num, idx });
            var evens = indexedNums.Where(x => x.idx % 2 == 0).OrderByDescending(x => x.idx);
            var odds = indexedNums.Where(x => x.idx % 2 == 1);
            var endSequence = odds.Concat(evens).Select(x => x.num).ToList();

            return endSequence;
        }

        // TODO: Add things below to another Storing class of some sort
        /*------------------------------------------------------------------------------------------*/


        public static void GenerateExpandedImages(string targetFolder, List<Shape> shapes, Shape boundaryShape, int width, int height)
        {
            ClearDirectory(targetFolder);
            // Have their location correct and name them by the order of their respective layers. 
            // Draw index, draw by descending order
            var shapesDrawingOrder = DrawingOrder(shapes, ShapeDrawingOrder.DownToUp);
            var orderIndex = shapesDrawingOrder.Count;
            foreach (var shape in shapesDrawingOrder)
            {
                SaveImage(targetFolder, width, height, shape, orderIndex);
                orderIndex--;
            }
            if(boundaryShape != null)
                SaveImage(targetFolder, width, height, boundaryShape, 0);
        }

        private static void ClearDirectory(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        private static List<Shape> DrawingOrder(List<Shape> shapes, ShapeDrawingOrder orderMethod)
        {
            switch (orderMethod)
            {
                case ShapeDrawingOrder.UpToDown: return shapes.OrderBy(x => x.BestLocation.Y).ToList();
                case ShapeDrawingOrder.DownToUp: return shapes.OrderByDescending(x => x.BestLocation.Y).ToList();
                case ShapeDrawingOrder.LeftToRight: return shapes.OrderBy(x => x.BestLocation.X).ToList();
                case ShapeDrawingOrder.RightToLeft: return shapes.OrderByDescending(x => x.BestLocation.X).ToList();
            }
            return shapes;
        }

        private static void SaveImage(string targetFolder, int width, int height, Shape shape, int orderIndex)
        {
            var bitmap = MergeShapeToExpandedImage(targetFolder, width, height, shape.ImageRef, shape.BestLocation, shape.BestRotation);
            var expandedPath = targetFolder + "//order" + orderIndex.ToString()+ "_" + shape.FileName + "_expanded.png";
            shape.ExpandedPath = expandedPath;
            bitmap.Save(expandedPath, ImageFormat.Png);
        }

        private static Bitmap MergeShapeToExpandedImage(string targetFolder, int outputImageWidth, int outputImageHeight, Image shapeImage, Point bestLocation, float bestRotation)
        {
            if (shapeImage == null)
            {
                Debug.WriteLine("No shapeimage detected for " + targetFolder);
                return null;
            }

            Bitmap outputImage = new Bitmap(outputImageWidth, outputImageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(outputImage))
            {
                graphics.TranslateTransform(bestLocation.X + (float)shapeImage.Width / 2, bestLocation.Y +(float)shapeImage.Height / 2);
                graphics.RotateTransform(bestRotation);
                graphics.TranslateTransform(-bestLocation.X + -(float)shapeImage.Width / 2,-bestLocation.Y + -(float)shapeImage.Height / 2);
                graphics.DrawImage(shapeImage, new Rectangle(bestLocation, shapeImage.Size));
            }

            return outputImage;
        }

        private static Bitmap GenerateEmptyBitmap(int width, int height)
        {
            Bitmap expandedBitmap = new Bitmap(width, height);
            return expandedBitmap;
        }
    }
}
