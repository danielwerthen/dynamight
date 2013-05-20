using AForge;
using AForge.Imaging;
using AForge.Math.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CameraDrawing
{
    public class ImageProc
    {
        public static void Rectangle(WriteableBitmap bitmap, DrawingContext dc)
        {

            // locating objects
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.FilterBlobs = true;
            blobCounter.MaxHeight = 375;
            blobCounter.MaxWidth = 375;
            System.Drawing.Bitmap image;
            using (var stream = new MemoryStream())
            {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                image = new System.Drawing.Bitmap(stream);
            }
            blobCounter.ProcessImage(image);
            Blob[] blobs = blobCounter.GetObjectsInformation();

            // check for rectangles
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            foreach (var blob in blobs)
            {
                List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blob);
                List<IntPoint> cornerPoints;

                // use the shape checker to extract the corner points
                if (shapeChecker.IsQuadrilateral(edgePoints, out cornerPoints))
                {
                    // only do things if the corners form a rectangle
                    if (shapeChecker.CheckPolygonSubType(cornerPoints) == PolygonSubType.Rectangle)
                    {
                        // here i use the graphics class to draw an overlay, but you
                        // could also just use the cornerPoints list to calculate your
                        // x, y, width, height values.
                        List<AForge.Point> Points = new List<AForge.Point>();
                        foreach (var point in cornerPoints)
                        {
                            Points.Add(new AForge.Point(point.X, point.Y));
                        }
                        var path = new PathFigure(new System.Windows.Point(Points.First().X, Points.First().Y), Points.Select(row => new System.Windows.Media.LineSegment(new System.Windows.Point(row.X, row.Y), false)), true);

                        dc.DrawGeometry(Brushes.Red, null, new PathGeometry(new PathFigure[] { path }));
                    }
                }
            }
        }
    }
}
