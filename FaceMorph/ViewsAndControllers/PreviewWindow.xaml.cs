using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FaceMorph.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace FaceMorph.ViewsAndControllers
{
    /// <summary>
    /// Interaction logic for PreviewWindow.xaml
    /// </summary>
    public partial class PreviewWindow : Window
    {
        ImageDetails curr;
        ImageDetails next;
        ObservableCollection<ImageDetails> Images;

        Image<Bgr, byte> currImageI;
        Image<Bgr, byte> nextImageI;

        Mat currImageMat = new Mat();
        Mat nextImageMat = new Mat();
        Mat morphImageMat = new Mat();

        VectorOfPointF ffpCurr, ffpNext; // points with landmarks

        Rectangle[] facesArrCurr;
        Rectangle[] facesArrNext;
        List<Rectangle> facesListCurr;
        List<Rectangle> facesListNext;

        FacemarkLBFParams facemarkParam;
        FacemarkLBF facemark;
        VectorOfVectorOfPointF landmarksCurr;
        VectorOfVectorOfPointF landmarksNext;

        VectorOfVectorOfInt delaunayTri = new VectorOfVectorOfInt();

        Triangle2DF[] delaunayTrianglesCurr, delaunayTrianglesNext;
        PointF[] ptsCurr, ptsNext;



        public const int RECT_WIDTH = 5;
        public const double HAAR_SCALE_FACTOR = 1.05;
        public const int HAAR_SCALE_MIN_NEIGHBOURS = 4;
        System.Drawing.Size HAAR_MIN_SIZE = new System.Drawing.Size(30, 30); // todo: should be % of image pixel height and width
        System.Drawing.Size HAAR_MAX_SIZE = new System.Drawing.Size(3000, 3000);


        private bool facesDetected = false;
        private int currentFaceCurr = 0;
        private int currentFaceNext = 0;

        public PreviewWindow(ImageDetails imageDetails, ObservableCollection<ImageDetails> images)
        {
            this.Images = images;
            this.curr = imageDetails;
            DisplayImages();

            currImageI = new Image<Bgr, byte>(curr.Title);
            nextImageI = new Image<Bgr, byte>(next.Title);

            InitializeComponent();

        }


        public void DisplayImages()
        {
            // checks if only one image in list, i.e. nothing to morph
            if (Images.Count == 1)
            {
                this.DataContext = new PreviewImageHolder()
                {
                    CurrImage = new BitmapImage(new Uri(curr.Title))
                };
                MessageBox.Show("Can't morph a single image");
            }

            // checks if current picture is the first 
            else if (curr.Id == 0)
            {

                next = Images.Where(x => x.Id == curr.Id + 1).FirstOrDefault();
                this.DataContext = new PreviewImageHolder()
                {
                    CurrImage = new BitmapImage(new Uri(curr.Title)),
                    NextImage = new BitmapImage(new Uri(next.Title)),
                };
            }

            // checks if current picture is the last
            else if (curr.Id == Images.Count - 1)
            {
                this.DataContext = new PreviewImageHolder()
                {
                    CurrImage = new BitmapImage(new Uri(curr.Title)),
                };
            }
            else
            {
                //prev = Images.Where(x => x.Id == curr.Id - 1).FirstOrDefault();
                next = Images.Where(x => x.Id == curr.Id + 1).FirstOrDefault();
                this.DataContext = new PreviewImageHolder()
                {
                    CurrImage = new BitmapImage(new Uri(curr.Title)),
                    NextImage = new BitmapImage(new Uri(next.Title)),
                };

            }

        }

        private void DisplayFaceClicked(object sender, RoutedEventArgs e)
        {
            if (!facesDetected)
            {
                DetectFaceInfo(); // todo: should be called earlier (not the drawing, but the detecting)
                DrawFaceRectsCurr(facesListCurr);
                DrawFaceRectsNext(facesListNext);
                facesDetected = true;
                facesCount.Content = $"{facesListCurr.Count}";
            }

        }

        public void DetectFaceInfo() // todo: call earlier
        {

            try
            {
                // Detect Faces
                string facePath = Path.GetFullPath(@"../../data/haarcascade_frontalface_default.xml");
                CascadeClassifier classifierFace = new CascadeClassifier(facePath);
                Image<Gray, byte> imgGrayCurr = currImageI.Convert<Gray, byte>().Clone();
                Image<Gray, byte> imgGrayNext = nextImageI.Convert<Gray, byte>().Clone();

                facesArrCurr = classifierFace.DetectMultiScale(imgGrayCurr, HAAR_SCALE_FACTOR, HAAR_SCALE_MIN_NEIGHBOURS, HAAR_MIN_SIZE, HAAR_MAX_SIZE);
                facesArrNext = classifierFace.DetectMultiScale(imgGrayNext, HAAR_SCALE_FACTOR, HAAR_SCALE_MIN_NEIGHBOURS, HAAR_MIN_SIZE, HAAR_MAX_SIZE);

                facesListCurr = facesArrCurr.OfType<Rectangle>().ToList();
                facesListNext = facesArrNext.OfType<Rectangle>().ToList();

                //// Find facial feature points
                VectorOfRect vrLeft = new VectorOfRect(facesArrCurr);
                VectorOfRect vrRight = new VectorOfRect(facesArrNext);
                landmarksNext = new VectorOfVectorOfPointF();
                landmarksCurr = new VectorOfVectorOfPointF();

                //// fill mat
                currImageMat = CvInvoke.Imread(curr.Title);
                nextImageMat = CvInvoke.Imread(next.Title);
                facemarkParam = new FacemarkLBFParams();
                facemark = new FacemarkLBF(facemarkParam);
                facemark.LoadModel(@"../../data/lbfmodel.yaml");
                facemark.Fit(currImageMat, vrLeft, landmarksCurr);
                facemark.Fit(nextImageMat, vrRight, landmarksNext);

                ffpCurr = landmarksCurr[0];
                ffpNext = landmarksNext[1]; // todo: user needs to be able to choose face

                // Delaunay
                using (VectorOfPointF vpfCurr = ffpCurr)
                using (VectorOfPointF vpfNext = ffpNext)
                {
                    ptsCurr = vpfCurr.ToArray();
                    ptsNext = vpfNext.ToArray();

                    using (Subdiv2D subdivisionLeft = new Subdiv2D(ptsCurr))
                    using (Subdiv2D subdivisionRight = new Subdiv2D(ptsNext))
                    {
                        //Obtain the delaunay's triangulation from the set of points;
                        delaunayTrianglesCurr = subdivisionLeft.GetDelaunayTriangles();
                        delaunayTrianglesNext = subdivisionRight.GetDelaunayTriangles();
                    }
                }

                //DrawFFPCurr();
                //DrawFFPNext();
                DrawDelaunayCurr();
                DrawDelaunayNext();

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public void DrawFaceRectsCurr(List<Rectangle> facesList)
        {
            if (facesList.Count > 0)
            {
                for (int i = 0; i < facesList.Count; i++)
                {
                    Console.WriteLine($"Width: {facesList[i].Width}, Height: {facesList[i].Height}");
                    if (i == 0)
                    {
                        currImageI.Draw(facesList[i], new Bgr(0, 255, 0), RECT_WIDTH);
                        currentFaceCurr = i;
                    }
                    else if (i > 0)
                    {
                        currImageI.Draw(facesList[i], new Bgr(0, 0, 255), RECT_WIDTH);
                    }

                }

            }
            PreviewImageHolder updatedImage = new PreviewImageHolder
            {
                CurrImage = BitmapSourceConvert.ToBitmapSource(currImageI),
            };

            currImage.DataContext = updatedImage;

        }

        public void DrawFaceRectsNext(List<Rectangle> facesList)
        {
            if (facesList.Count > 0)
            {
                for (int i = 0; i < facesList.Count; i++)
                {
                    Console.WriteLine($"Width: {facesList[i].Width}, Height: {facesList[i].Height}");
                    if (i == 0)
                    {
                        nextImageI.Draw(facesList[i], new Bgr(0, 255, 0), RECT_WIDTH);
                        currentFaceNext = i;
                    }
                    else if (i > 0)
                    {
                        nextImageI.Draw(facesList[i], new Bgr(0, 0, 255), RECT_WIDTH);
                    }

                }

            }
            PreviewImageHolder updatedImage = new PreviewImageHolder
            {
                NextImage = BitmapSourceConvert.ToBitmapSource(nextImageI),
            };

            nextImage.DataContext = updatedImage;

            //CvInvoke.Imwrite("test.jpg",currImageI);
        }

        public void DrawFFPCurr()
        {
            Image<Bgr, byte> myImage = currImageMat.ToImage<Bgr, byte>();
            FaceInvoke.DrawFacemarks(myImage, ffpCurr, new MCvScalar(255, 0, 0));
            CvInvoke.Imwrite("testffp.jpg", myImage);
            currImage.Source = BitmapSourceConvert.ToBitmapSource(myImage);
        }

        public void DrawFFPNext()
        {
            Image<Bgr, byte> myImage = nextImageMat.ToImage<Bgr, byte>();
            FaceInvoke.DrawFacemarks(myImage, ffpNext, new MCvScalar(255, 0, 0));
            CvInvoke.Imwrite("testffp.jpg", myImage);
            nextImage.Source = BitmapSourceConvert.ToBitmapSource(myImage);
        }

        public void DrawDelaunayCurr()
        {
            Image<Bgr, byte> myImage = currImageMat.ToImage<Bgr, byte>();

            foreach (Triangle2DF triangle in delaunayTrianglesCurr)
            {

                System.Drawing.Point[] vertices = Array.ConvertAll<PointF, System.Drawing.Point>(triangle.GetVertices(), System.Drawing.Point.Round);
                using (VectorOfPoint vp = new VectorOfPoint(vertices))
                {
                    CvInvoke.Polylines(myImage, vp, true, new Bgr(255, 255, 255).MCvScalar);
                }
            }
            currImage.Source = BitmapSourceConvert.ToBitmapSource(myImage);
        }

        public void DrawDelaunayNext()
        {
            Image<Bgr, byte> myImage = nextImageMat.ToImage<Bgr, byte>();

            foreach (Triangle2DF triangle in delaunayTrianglesNext)
            {

                System.Drawing.Point[] vertices = Array.ConvertAll<PointF, System.Drawing.Point>(triangle.GetVertices(), System.Drawing.Point.Round);
                using (VectorOfPoint vp = new VectorOfPoint(vertices))
                {
                    CvInvoke.Polylines(myImage, vp, true, new Bgr(255, 255, 255).MCvScalar);
                }
            }
            nextImage.Source = BitmapSourceConvert.ToBitmapSource(myImage);
        }

        private void ChangeFaceLeftButton_Click(object sender, RoutedEventArgs e)
        {
            if (facesDetected)
            {
                if (facesListCurr.Count > 1)
                {
                    if (currentFaceCurr == 0)
                    {
                        currentFaceCurr = facesListCurr.Count - 1;
                        RedrawFaces();
                    }
                    else
                    {
                        currentFaceCurr--;
                        RedrawFaces();
                    }
                }
            }

        }

        private void ChangeFaceRightButton_Click(object sender, RoutedEventArgs e)
        {
            if (facesDetected)
            {
                if (facesListCurr.Count > 1)
                {
                    if (currentFaceCurr == facesListCurr.Count - 1)
                    {
                        currentFaceCurr = 0;
                        RedrawFaces();
                    }
                    else
                    {
                        currentFaceCurr++;
                        RedrawFaces();
                    }
                }
            }
        }

        public void RedrawFaces()
        {

            currImageI = new Image<Bgr, byte>(curr.Title);

            if (facesListCurr.Count > 0)
            {
                for (int i = 0; i < facesListCurr.Count; i++)
                {
                    if (i == currentFaceCurr)
                    {
                        currImageI.Draw(facesListCurr[i], new Bgr(0, 255, 0), RECT_WIDTH);
                    }
                    else if (i != currentFaceCurr)
                    {
                        currImageI.Draw(facesListCurr[i], new Bgr(0, 0, 255), RECT_WIDTH);
                    }

                }

            }
            PreviewImageHolder updatedImage = new PreviewImageHolder
            {
                CurrImage = BitmapSourceConvert.ToBitmapSource(currImageI),
            };
            currImage.DataContext = updatedImage;
            curr.FaceLocation = facesListCurr[currentFaceCurr];

        }

    }
}
