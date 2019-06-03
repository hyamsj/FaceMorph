using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Runtime.InteropServices;
using System.IO;
using Emgu.CV.Face;
using Emgu.CV.Util;
using System.Drawing;

namespace FaceDetectWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        bool drawRects = true;
        bool drawFaceMarks = true;
        bool drawDelauney = true;

        string facePath = Path.GetFullPath(@"../../data/haarcascade_frontalface_default.xml");
        private VideoCapture capture;
        private CascadeClassifier haarCascade;
        DispatcherTimer timer;
        FacemarkLBFParams facemarkParam;
        FacemarkLBF facemark;



        public MainWindow()
        {
            InitializeComponent();

            capture = new VideoCapture();
            haarCascade = new CascadeClassifier(@"../../data/haarcascade_frontalface_default.xml");
            facemarkParam = new FacemarkLBFParams();
            facemark = new FacemarkLBF(facemarkParam);
            facemark.LoadModel(@"../../data/lbfmodel.yaml");

            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            Image<Bgr, Byte> currentFrame = capture.QueryFrame().ToImage<Bgr, Byte>();

            if (currentFrame != null)
            {
                Image<Gray, byte> grayFrame = currentFrame.Convert<Gray, Byte>();

                var detectedFaces = haarCascade.DetectMultiScale(grayFrame, 1.1, 10, new System.Drawing.Size(150, 150), new System.Drawing.Size(300, 300));

                VectorOfRect vr = new VectorOfRect(detectedFaces);
                VectorOfVectorOfPointF landmarks = new VectorOfVectorOfPointF();
                facemark.Fit(currentFrame, vr, landmarks);




                if (drawRects)
                {
                    foreach (var face in detectedFaces)
                    {
                        currentFrame.Draw(face, new Bgr(0, double.MaxValue, 0), 3);
                        //Console.WriteLine($"Width: {face.Width}, Height: {face.Height}");
                    }

                }


                if (drawFaceMarks)
                {
                    for (int i = 0; i < landmarks.Size; i++)
                    {
                        using (VectorOfPointF vpf = landmarks[i])
                        {
                            FaceInvoke.DrawFacemarks(currentFrame, vpf, new MCvScalar(255, 0, 0));

                        }
                    }

                }

                if (drawDelauney)
                {
                    //System.Drawing.Size size = new System.Drawing.Size(currentFrame.Width, currentFrame.Height);
                    //System.Drawing.Rectangle rect = new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), size);

                    Triangle2DF[] delaunayTriangles;
                    PointF[] pts;
                    

                    for (int i = 0; i < landmarks.Size; i++)
                    {
                        using (VectorOfPointF vpf = landmarks[i])
                        {
                            PointF[] pts2 = vpf.ToArray();

                            using (Subdiv2D subdivision = new Subdiv2D(pts2))
                            {
                                //Obtain the delaunay's triangulation from the set of points;
                                delaunayTriangles = subdivision.GetDelaunayTriangles();
                                foreach (Triangle2DF triangle in delaunayTriangles)
                                {
                                    System.Drawing.Point[] vertices = Array.ConvertAll<PointF, System.Drawing.Point>(triangle.GetVertices(), System.Drawing.Point.Round);
                                    using (VectorOfPoint vp = new VectorOfPoint(vertices))
                                    {
                                        CvInvoke.Polylines(currentFrame, vp, true, new Bgr(255, 255, 255).MCvScalar);
                                    }
                                }
                            }
                        }
                    }
                }
                image1.Source = ToBitmapSource(currentFrame);
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
