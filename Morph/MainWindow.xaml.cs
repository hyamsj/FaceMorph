using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace Morph
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //string LeftImage = $"C:/Users/joni/Pictures/_Thesis Images/hillary_clinton.jpg";
        //string RightImage = $"C:/Users/joni/Pictures/_Thesis Images/donald_trump.jpg";
        string LeftImage = $"../../hillary_clinton.jpg";
        string RightImage = $"../../ted_cruz.jpg";

        int warpAffinCount = 0;

        Mat leftImageMat = new Mat();
        Mat rightImageMat = new Mat();
        Mat morphImageMat = new Mat();

        VectorOfPointF ffpLeft, ffpRight; // points with landmarks

        private CascadeClassifier haarCascade;
        System.Drawing.Rectangle[] detectedFacesLeft;
        System.Drawing.Rectangle[] detectedFacesRight;
        FacemarkLBFParams facemarkParam;
        FacemarkLBF facemark;
        VectorOfVectorOfPointF landmarksLeft;
        VectorOfVectorOfPointF landmarksRight;

        VectorOfVectorOfInt delaunayTri = new VectorOfVectorOfInt();

        Triangle2DF[] delaunayTrianglesLeft, delaunayTrianglesRight;
        PointF[] ptsLeft, ptsRight;

        public MainWindow()
        {
            InitializeComponent();

            haarCascade = new CascadeClassifier(@"../../data/haarcascade_frontalface_default.xml");
            facemarkParam = new FacemarkLBFParams();
            facemark = new FacemarkLBF(facemarkParam);
            facemark.LoadModel(@"../../data/lbfmodel.yaml");

            leftImageMat = CvInvoke.Imread(LeftImage);
            rightImageMat = CvInvoke.Imread(RightImage);

            leftImage.Source = ToBitmapSource(leftImageMat);
            rightImage.Source = ToBitmapSource(rightImageMat);

            DetectFaceInfo(leftImageMat, rightImageMat);
            CvInvoke.Imshow("test", leftImageMat);

        }


        public void DetectFaceInfo(Mat imgLeft, Mat imgRight)
        {

            // Detect faces
            Image<Bgr, Byte> myImageLeft = imgLeft.ToImage<Bgr, Byte>();
            Image<Bgr, Byte> myImageRight = imgRight.ToImage<Bgr, Byte>();
            Image<Gray, byte> grayFrameLeft = myImageLeft.Convert<Gray, Byte>();
            Image<Gray, byte> grayFrameRight = myImageRight.Convert<Gray, Byte>();


            detectedFacesLeft = haarCascade.DetectMultiScale(grayFrameLeft, 1.1, 10);
            detectedFacesRight = haarCascade.DetectMultiScale(grayFrameRight, 1.1, 10);


            // find facial feature points
            VectorOfRect vrLeft = new VectorOfRect(detectedFacesLeft);
            VectorOfRect vrRight = new VectorOfRect(detectedFacesRight);
            landmarksLeft = new VectorOfVectorOfPointF();
            landmarksRight = new VectorOfVectorOfPointF();
            facemark.Fit(leftImageMat, vrLeft, landmarksLeft);
            facemark.Fit(rightImageMat, vrRight, landmarksRight);

            rightImageMat = imgRight;
            leftImageMat = imgLeft;

            // Delaunay
            using (VectorOfPointF vpfLeft = landmarksLeft[0])
            using (VectorOfPointF vpfRight = landmarksRight[0])
            {
                ptsLeft = vpfLeft.ToArray();
                ptsRight = vpfRight.ToArray();

                using (Subdiv2D subdivisionLeft = new Subdiv2D(ptsLeft))
                using (Subdiv2D subdivisionRight = new Subdiv2D(ptsRight))
                {
                    //Obtain the delaunay's triangulation from the set of points;
                    delaunayTrianglesLeft = subdivisionLeft.GetDelaunayTriangles();
                    delaunayTrianglesRight = subdivisionRight.GetDelaunayTriangles();

                    ffpLeft = landmarksLeft[0];
                    ffpRight = landmarksRight[0];

                }
            }
        }


        private void ShowFaceCB(object sender, RoutedEventArgs e)
        {
            bool newVal = (ShowFace.IsChecked == true);
            Image<Bgr, byte> myLeftImage = leftImageMat.ToImage<Bgr, byte>();
            Image<Bgr, byte> myRightImage = rightImageMat.ToImage<Bgr, byte>();
            if (newVal)
            {
                foreach (var face in detectedFacesLeft)
                {
                    myLeftImage.Draw(face, new Bgr(0, 0, 255), 3);
                }

                foreach (var face in detectedFacesRight)
                {
                    myRightImage.Draw(face, new Bgr(0, 0, 255), 3);
                }

            }
            leftImage.Source = ToBitmapSource(myLeftImage);
            rightImage.Source = ToBitmapSource(myRightImage);
        }

        private void ShowFPCB(object sender, RoutedEventArgs e)
        {
            bool newVal = (ShowFP.IsChecked == true);
            Image<Bgr, byte> myLeftImage = leftImageMat.ToImage<Bgr, byte>();
            Image<Bgr, byte> myRightImage = rightImageMat.ToImage<Bgr, byte>();

            if (newVal)
            {
                FaceInvoke.DrawFacemarks(myLeftImage, ffpLeft, new MCvScalar(255, 0, 0));
                FaceInvoke.DrawFacemarks(myRightImage, ffpRight, new MCvScalar(255, 0, 0));
            }
            leftImage.Source = ToBitmapSource(myLeftImage);
            rightImage.Source = ToBitmapSource(myRightImage);
        }


        public void ShowDelaunayCB(object sender, RoutedEventArgs e)
        {
            Image<Bgr, byte> myLeftImage = leftImageMat.ToImage<Bgr, byte>();
            Image<Bgr, byte> myRightImage = rightImageMat.ToImage<Bgr, byte>();

            bool newVal = (ShowDelaunay.IsChecked == true);
            if (newVal)
            {
                foreach (Triangle2DF triangle in delaunayTrianglesLeft)
                {

                    System.Drawing.Point[] vertices = Array.ConvertAll<PointF, System.Drawing.Point>(triangle.GetVertices(), System.Drawing.Point.Round);
                    using (VectorOfPoint vp = new VectorOfPoint(vertices))
                    {
                        CvInvoke.Polylines(myLeftImage, vp, true, new Bgr(255, 255, 255).MCvScalar);
                    }
                }
                foreach (Triangle2DF triangle in delaunayTrianglesRight)
                {
                    //Console.WriteLine($"triangles: {triangle.V0}");
                    System.Drawing.Point[] vertices = Array.ConvertAll<PointF, System.Drawing.Point>(triangle.GetVertices(), System.Drawing.Point.Round);
                    using (VectorOfPoint vp = new VectorOfPoint(vertices))
                    {
                        CvInvoke.Polylines(myRightImage, vp, true, new Bgr(255, 255, 255).MCvScalar);
                    }
                }
            }

            leftImage.Source = ToBitmapSource(myLeftImage);
            rightImage.Source = ToBitmapSource(myRightImage);
        }

        private void MorphButton_Click(object sender, RoutedEventArgs e)
        {
            // alpha controls degree of morph
            double alpha = 0.5;

            // get input images
            Mat img1 = leftImageMat;
            Mat img2 = rightImageMat;

            // convert Mat to float data type
            img1.ConvertTo(img1, Emgu.CV.CvEnum.DepthType.Cv32F);
            img2.ConvertTo(img2, Emgu.CV.CvEnum.DepthType.Cv32F);

            // empty average image
            morphImageMat = Mat.Zeros(img1.Rows, img1.Cols, Emgu.CV.CvEnum.DepthType.Cv32F, img1.NumberOfChannels);

            // Get Points
            VectorOfPointF points1 = ffpLeft;
            VectorOfPointF points2 = ffpRight;
            VectorOfPointF points = new VectorOfPointF();

            // compute weighted average point coordinates
            for (int i = 0; i < points1.Size; i++)
            {
                float x, y;
                x = (float)(1 - alpha) * points1[i].X + (float)alpha * points2[i].X;
                y = (float)(1 - alpha) * points1[i].Y + (float)alpha * points2[i].Y;

                PointF initial = new PointF { X = x, Y = y };
                PointF[] P = new PointF[1] { initial };
                points.Push(P);
            }
            CalculateDelaunayTriangles();

            int[][] triIndices = delaunayTri.ToArrayOfArray();
            VectorOfPointF t1 = new VectorOfPointF();
            VectorOfPointF t2 = new VectorOfPointF();
            VectorOfPointF t = new VectorOfPointF();


            // TODO
            //PointF[] arrpoints = points.ToArray();
            //PointF[] arrpoints1 = points1.ToArray();
            //PointF[] arrpoints2 = points2.ToArray();

            // nested for loop
            for (int k = 0; k < triIndices.GetLength(0); k++)
            {
                //Console.WriteLine(triIndices[k]);


            }

            foreach (int[] l in triIndices)
            {
                foreach (int s in l)
                {
                    // triangle corner for image 1
                    var triangleCornerX1 = points1[l[0]];
                    var triangleCornerY1 = points1[l[1]];
                    var triangleCornerZ1 = points1[l[2]];

                    PointF[] X1 = { new PointF(triangleCornerX1.X, triangleCornerX1.Y) };
                    PointF[] Y1 = { new PointF(triangleCornerX1.X, triangleCornerX1.Y) };
                    PointF[] Z1 = { new PointF(triangleCornerX1.X, triangleCornerX1.Y) };
                    t1.Push(X1);
                    t2.Push(Y1);
                    t.Push(Z1);


                    // triangle corner for image 2
                    var triangleCornerX2 = points2[l[0]];
                    var triangleCornerY2 = points2[l[1]];
                    var triangleCornerZ2 = points2[l[2]];

                    PointF[] X2 = { new PointF(triangleCornerX2.X, triangleCornerX2.Y) };
                    PointF[] Y2 = { new PointF(triangleCornerX2.X, triangleCornerX2.Y) };
                    PointF[] Z2 = { new PointF(triangleCornerX2.X, triangleCornerX2.Y) };
                    t1.Push(X2);
                    t2.Push(Y2);
                    t.Push(Z2);

                    // triangle corner for image 2
                    var triangleCornerX = points[l[0]];
                    var triangleCornerY = points[l[1]];
                    var triangleCornerZ = points[l[2]];

                    PointF[] X = { new PointF(triangleCornerX2.X, triangleCornerX2.Y) };
                    PointF[] Y = { new PointF(triangleCornerX2.X, triangleCornerX2.Y) };
                    PointF[] Z = { new PointF(triangleCornerX2.X, triangleCornerX2.Y) };
                    t1.Push(X);
                    t2.Push(Y);
                    t.Push(Z);

                    //Console.WriteLine($"current s: {l[0]} {l[1]} {l[2]}");
                    MorphTriangle(ref img1, ref img2, ref morphImageMat, ref t1, ref t2, ref t, alpha);
                }
            }

        }

        public void MorphTriangle(ref Mat img1, ref Mat img2, ref Mat imgMorph, ref VectorOfPointF t1, ref VectorOfPointF t2, ref VectorOfPointF t, double alpha)
        {
            //Console.WriteLine($"t: {t.Size} t1: {t1.Size} t2: {t2.Size}");
            //Console.WriteLine($"img1: {img1.Cols} {img1.Rows} {img1.Width}");
            //Console.WriteLine($"img2: {img2.Cols} {img2.Rows} {img2.Width}");
            //Console.WriteLine($"imgMorp: {imgMorph.Cols} {imgMorph.Rows} {imgMorph.Width}");

            // Find bounding rectangle for each triangle
            System.Drawing.Rectangle r = CvInvoke.BoundingRectangle(t);
            System.Drawing.Rectangle r1 = CvInvoke.BoundingRectangle(t1);
            System.Drawing.Rectangle r2 = CvInvoke.BoundingRectangle(t2);


            //Offset points by left top corner of the respective rectangles
            VectorOfPointF t1Rect = new VectorOfPointF();
            VectorOfPointF t2Rect = new VectorOfPointF();
            VectorOfPointF tRect = new VectorOfPointF();

            VectorOfPoint tRectInt = new VectorOfPoint();
            for (int i = 0; i < 3; i++)
            {
                tRect.Push(new PointF[] { new PointF(t[i].X - r.X, t[i].Y - r.Y) });
                tRectInt.Push(new System.Drawing.Point[] { new System.Drawing.Point((int)t[i].X - r.X, (int)t[i].Y - r.Y) });

                t1Rect.Push(new PointF[] { new PointF(t1[i].X - r1.X, t1[i].Y - r1.Y) });
                t2Rect.Push(new PointF[] { new PointF(t2[i].X - r2.X, t2[i].Y - r2.Y) });
            }

            // Get mask by filling triangle
            Mat mask = Mat.Zeros(r.Height, r.Width, Emgu.CV.CvEnum.DepthType.Cv32F, 3);
            Console.WriteLine($"Rows: {mask.Rows} Cols: {mask.Cols} Type: {Emgu.CV.CvEnum.DepthType.Cv32F} Channels: {mask.NumberOfChannels}");
            CvInvoke.FillConvexPoly(mask, tRectInt, new MCvScalar(1.0, 1.0, 1.0), Emgu.CV.CvEnum.LineType.AntiAlias, 0);

            //Apply warpImage to small rectangular patches

            Mat img1Rect = new Mat(img1, r1);
            Mat img2Rect = new Mat(img2, r2);

            Mat warpImage1 = Mat.Zeros(r.Height, r.Width, Emgu.CV.CvEnum.DepthType.Cv32F, 3); // different
            Mat warpImage2 = Mat.Zeros(r.Height, r.Width, Emgu.CV.CvEnum.DepthType.Cv32F, 3); // different
            Console.WriteLine($"Rows: {warpImage1.Rows} Cols: {warpImage1.Cols} Type: {Emgu.CV.CvEnum.DepthType.Cv32F} Channels: {warpImage1.NumberOfChannels}");

            ApplyAffineTransform(ref warpImage1, ref img1Rect, ref t1Rect, ref tRect);
            ApplyAffineTransform(ref warpImage2, ref img2Rect, ref t2Rect, ref tRect);

            // Alpha blend rectangular patches
            Mat imgRect = (1.0 * alpha) * warpImage1 + alpha * warpImage2;

            // Copy triangular region of the rectangular patch to the output image
            CvInvoke.Multiply(imgRect, mask, imgRect);
            CvInvoke.Multiply(new Mat(imgMorph, r), new MCvScalar(1.0, 1.0, 1.0) - mask, new Mat(imgMorph, r));
            imgMorph = new Mat(imgMorph, r) + imgRect;


            Image<Bgr, byte> myMorph = imgMorph.ToImage<Bgr, byte>();
            morphedImage.Source = ToBitmapSource(myMorph);
            Console.WriteLine(warpAffinCount);
        }

        private void ApplyAffineTransform(ref Mat warpImage, ref Mat src, ref VectorOfPointF srcTri, ref VectorOfPointF dstTri)
        {
            Mat warpMat = CvInvoke.GetAffineTransform(srcTri, dstTri);
            CvInvoke.WarpAffine(src, warpImage, warpMat, warpImage.Size, Emgu.CV.CvEnum.Inter.Linear, borderMode: Emgu.CV.CvEnum.BorderType.Reflect101);
            //warpAffinCount++;

        }

        public void CalculateDelaunayTriangles()
        {

            VectorOfInt ind = new VectorOfInt();
            int[] indArr = new int[3];
            for (int i = 0; i < delaunayTrianglesLeft.Length; i++)
            {
                Triangle2DF t = delaunayTrianglesLeft[i];

                PointF ptzero = new PointF { X = t.V0.X, Y = t.V0.Y };
                PointF[] PZero = new PointF[] { ptzero };

                PointF ptone = new PointF { X = t.V1.X, Y = t.V1.Y };
                PointF[] POne = new PointF[] { ptone };

                PointF pttwo = new PointF { X = t.V2.X, Y = t.V2.Y };
                PointF[] PTwo = new PointF[] { pttwo };

                VectorOfPointF pt = new VectorOfPointF();

                pt.Push(PZero);
                //Console.WriteLine($"Zero X: {PZero[0].X} Y: {PZero[0].Y}");
                pt.Push(POne);
                pt.Push(PTwo);


                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < ffpLeft.Size; k++)
                    {

                        if (Math.Abs(pt[j].X - ffpLeft[k].X) < 1.0 &&
                            Math.Abs(pt[j].Y - ffpLeft[k].Y) < 1)
                        {
                            //Console.WriteLine($"Value {k} added");
                            indArr[j] = k;
                        }
                    }
                }
                ind = new VectorOfInt(indArr);
                delaunayTri.Push(ind);
            }

        }



        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);


        public static BitmapSource ToBitmapSource(IImage image)
        {
            using (System.Drawing.Bitmap source = image.Bitmap)
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap  

                BitmapSource bs = System.Windows.Interop
                  .Imaging.CreateBitmapSourceFromHBitmap(
                  ptr,
                  IntPtr.Zero,
                  Int32Rect.Empty,
                  System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

                DeleteObject(ptr); //release the HBitmap  
                return bs;
            }
        }


    }
}
