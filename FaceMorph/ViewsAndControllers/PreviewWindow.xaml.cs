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

        float defaultAlpha = 0;

        public const int RECT_WIDTH = 5;
        public const double HAAR_SCALE_FACTOR = 1.05;
        public const int HAAR_SCALE_MIN_NEIGHBOURS = 4;
        public const double HAAR_MIN_FACE_FACTOR = 0.3;
        public const double HAAR_MAX_FACE_FACTOR = 0.8;


        private bool facesDetected = false;
        private int currentFaceCurr = 0;
        private int currentFaceNext = 0;

        public PreviewWindow(ImageDetails imageDetails, ObservableCollection<ImageDetails> images)
        {
            this.Images = images;
            this.curr = imageDetails;
            DisplayImages();


            if (next != null)
            {
                currImageI = new Image<Bgr, byte>(curr.Title);
                nextImageI = new Image<Bgr, byte>(next.Title);
                DetectFaceInfo();
                InitializeComponent();
            }
            else
            {
                InitializeComponent();
                morphBtn.IsEnabled = false;
                mySlider.IsEnabled = false;
            }

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
            if (next == null)
            {
                facesCountCurr.Content = $"Nothing to morph";
            }
            else if (!facesDetected)
            {
                DrawFaceRectsCurr(facesListCurr); // todo cleanup
                DrawFaceRectsNext(facesListNext);
                facesDetected = true;
                facesCountCurr.Content = $"{facesListCurr.Count}";
                facesCountNext.Content = $"{facesListNext.Count}";

            }
        }

        public void DetectFaceInfo()
        {
            string facePath;
            try
            {
                // get face detect dataset
                facePath = Path.GetFullPath(@"../../data/haarcascade_frontalface_default.xml");

                // get FFP dataset
                facemarkParam = new FacemarkLBFParams();
                facemark = new FacemarkLBF(facemarkParam);
                facemark.LoadModel(@"../../data/lbfmodel.yaml");
            }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            // initialize imageMat
            currImageMat = currImageI.Mat;
            nextImageMat = nextImageI.Mat;

            // resize image
            System.Drawing.Size currImageSize = new System.Drawing.Size(currImageI.Width, currImageI.Height);
            System.Drawing.Size nextImageSize = new System.Drawing.Size(nextImageI.Width, nextImageI.Height);

            if (currImageSize.Height > nextImageSize.Height || currImageSize.Width > nextImageSize.Width)
            {

                var tmp = currImageI.Mat; // unnecessary copy
                currImageMat = GetSquareImage(tmp, nextImageI.Width);
                currImageI = currImageMat.ToImage<Bgr, byte>();
                //CvInvoke.Resize(currImageI, currImageI, nextImageSize);
                //CvInvoke.Imwrite("testimages/currImageIResized.jpg", currImageMat);

            }
            else
            {
                var tmp = nextImageI.Mat; // unnecessary copy
                nextImageMat = GetSquareImage(tmp, currImageI.Width);
                nextImageI = nextImageMat.ToImage<Bgr, byte>();
                //CvInvoke.Resize(nextImageI, nextImageI, currImageSize);
                //CvInvoke.Imwrite("testimages/nextImageIresized.jpg", nextImageMat);
            }


            CascadeClassifier classifierFace = new CascadeClassifier(facePath);
            Image<Gray, byte> imgGrayCurr = currImageI.Convert<Gray, byte>().Clone();
            Image<Gray, byte> imgGrayNext = nextImageI.Convert<Gray, byte>().Clone();


            // defines size of face in picture to be found 
            int minWidthCurr = (int)(currImageI.Width * HAAR_MIN_FACE_FACTOR);
            int minHeightCurr = (int)(currImageI.Height * HAAR_MIN_FACE_FACTOR);
            int maxWidthCurr = (int)(currImageI.Width * HAAR_MAX_FACE_FACTOR);
            int maxHeightCurr = (int)(currImageI.Height * HAAR_MAX_FACE_FACTOR);

            int minWidthNext = (int)(nextImageI.Width * HAAR_MIN_FACE_FACTOR);
            int minWHeightNext = (int)(nextImageI.Height * HAAR_MIN_FACE_FACTOR);
            int maxWidthNext = (int)(nextImageI.Width * HAAR_MAX_FACE_FACTOR);
            int maxHeightNext = (int)(nextImageI.Height * HAAR_MAX_FACE_FACTOR);

            System.Drawing.Size minSizeCurr = new System.Drawing.Size(minWidthCurr, minHeightCurr);
            System.Drawing.Size maxSizeCurr = new System.Drawing.Size(maxWidthCurr, maxHeightCurr);

            System.Drawing.Size minSizeNext = new System.Drawing.Size(minWidthNext, minWHeightNext);
            System.Drawing.Size maxSizeNext = new System.Drawing.Size(maxWidthNext, maxHeightNext);


            // Detect Faces
            facesArrCurr = classifierFace.DetectMultiScale(imgGrayCurr, HAAR_SCALE_FACTOR, HAAR_SCALE_MIN_NEIGHBOURS, minSizeCurr, maxSizeCurr);
            facesArrNext = classifierFace.DetectMultiScale(imgGrayNext, HAAR_SCALE_FACTOR, HAAR_SCALE_MIN_NEIGHBOURS, minSizeNext, maxSizeNext);

            // if no faces found, send error to user
            if (facesArrCurr != null)
            {
                facesListCurr = facesArrCurr.OfType<Rectangle>().ToList();

                // Find facial feature points
                VectorOfRect vrLeft = new VectorOfRect(facesArrCurr);
                landmarksCurr = new VectorOfVectorOfPointF();

                // fill mat
                //currImageMat = CvInvoke.Imread(curr.Title);

                facemark.Fit(currImageMat, vrLeft, landmarksCurr); // ?
                ffpCurr = landmarksCurr[0];

            }
            if (facesArrNext != null)
            {
                facesListNext = facesArrNext.OfType<Rectangle>().ToList();

                // Find facial feature points
                VectorOfRect vrRight = new VectorOfRect(facesArrNext);
                landmarksNext = new VectorOfVectorOfPointF();

                // fill mat
                //nextImageMat = CvInvoke.Imread(next.Title);

                facemark.Fit(nextImageMat, vrRight, landmarksNext);
                ffpNext = landmarksNext[0]; // todo: user needs to be able to choose face
            }



            // Delaunay
            if (facesArrNext != null && facesArrCurr != null)
            {
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

            }

            //DrawFFPCurr();
            //DrawFFPNext();
            //DrawDelaunayCurr();
            //DrawDelaunayNext();
        }

        
        private Mat GetSquareImage(Mat img, int targetWidth)
        {
            int width = img.Cols;
            int height = img.Rows;

            Mat square = Mat.Zeros(targetWidth,targetWidth,Emgu.CV.CvEnum.DepthType.Cv8U,3);

            int maxDim = (width >= height) ? width : height;
            float scale = ((float)targetWidth)/maxDim;
            Rectangle roi = new Rectangle();
            if (width >=height)
            {
                roi.Width = targetWidth;
                roi.X = 0;
                roi.Height = (int)(height * scale);
                roi.Y = (targetWidth - roi.Height) / 2;
            } else
            {
                roi.Y = 0;
                roi.Height = targetWidth;
                roi.Width = (int)(width * scale);
                roi.X = (targetWidth - roi.Width) / 2;
            }
            CvInvoke.Resize(img, square,roi.Size);
            return square;
            
        }

        public void DrawFaceRectsCurr(List<Rectangle> facesList)
        {
            if (facesList != null)
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

        }

        public void DrawFaceRectsNext(List<Rectangle> facesList)
        {
            if (facesList != null)
            {
                if (facesList.Count > 0)
                {
                    for (int i = 0; i < facesList.Count; i++)
                    {
                        Console.WriteLine($"Width: {facesList[i].Width}, Height: {facesList[i].Height}");
                        if (i == 0)
                        {
                            // draw green rect
                            nextImageI.Draw(facesList[i], new Bgr(0, 255, 0), RECT_WIDTH);
                            currentFaceNext = i;
                        }
                        else if (i > 0)
                        {
                            // draw red rect
                            nextImageI.Draw(facesList[i], new Bgr(0, 0, 255), RECT_WIDTH);
                        }

                    }

                }
                PreviewImageHolder updatedImage = new PreviewImageHolder
                {
                    NextImage = BitmapSourceConvert.ToBitmapSource(nextImageI),
                };

                nextImage.DataContext = updatedImage;

            }

            //CvInvoke.Imwrite("test.jpg",currImageI);
        }

        public void DrawFFPCurr()
        {
            Image<Bgr, byte> myImage = currImageMat.ToImage<Bgr, byte>();
            FaceInvoke.DrawFacemarks(myImage, ffpCurr, new MCvScalar(255, 0, 0));
            //CvInvoke.Imwrite("testffp.jpg", myImage);
            currImage.Source = BitmapSourceConvert.ToBitmapSource(myImage);
        }

        public void DrawFFPNext()
        {
            Image<Bgr, byte> myImage = nextImageMat.ToImage<Bgr, byte>();
            FaceInvoke.DrawFacemarks(myImage, ffpNext, new MCvScalar(255, 0, 0));
            //CvInvoke.Imwrite("testffp.jpg", myImage);
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

        private void MorphButton_Click(object sender, RoutedEventArgs e)
        {
            CvInvoke.Imwrite("testimages/morpph1.jpg", currImageMat);
            CvInvoke.Imwrite("testimages/morpph2.jpg", nextImageMat);
            MorphImage m = new MorphImage(currImageMat, nextImageMat, ffpCurr, ffpNext, defaultAlpha);

            morphImage.Source = m.GetMorphedImage();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double sliderValue = mySlider.Value;
            float updatedAlpha = (float)sliderValue;
            MorphImage m = new MorphImage(currImageMat, nextImageMat, ffpCurr, ffpNext, updatedAlpha);
            morphImage.Source = m.GetMorphedImage();
        }

        private void ChangeFaceLeftButton_Click(object sender, RoutedEventArgs e)
        {
            string buttonName = ((Button)sender).Uid;
            Console.WriteLine(buttonName);

            switch ((sender as Button).Uid)
            {
                case "leftButtonCurr":

                    if (facesDetected)
                    {
                        if (facesListCurr.Count > 1)
                        {
                            if (currentFaceCurr == 0)
                            {
                                currentFaceCurr = facesListCurr.Count - 1;
                                RedrawFaces(curr.Title, false);
                            }
                            else
                            {
                                currentFaceCurr--;
                                RedrawFaces(curr.Title, false);
                            }
                        }
                    }
                    break;
                case "leftButtonNext":
                    if (facesDetected)
                    {
                        if (facesListNext.Count > 1)
                        {
                            if (currentFaceNext == 0)
                            {
                                currentFaceNext = facesListNext.Count - 1;
                                RedrawFaces(next.Title, true);
                            }
                            else
                            {
                                currentFaceNext--;
                                RedrawFaces(next.Title, true);
                            }
                        }
                    }
                    break;
                default:
                    break;
            }



        }

        private void ChangeFaceRightButton_Click(object sender, RoutedEventArgs e)
        {
            //if (facesDetected)
            //{
            //    if (facesListCurr.Count > 1)
            //    {
            //        if (currentFaceCurr == facesListCurr.Count - 1)
            //        {
            //            currentFaceCurr = 0;
            //            //RedrawFaces();
            //        }
            //        else
            //        {
            //            currentFaceCurr++;
            //            //RedrawFaces();
            //        }
            //    }
            //}

            string buttonName = ((Button)sender).Uid;
            Console.WriteLine(buttonName);

            switch ((sender as Button).Uid)
            {
                case "rightButtonCurr":

                    if (facesDetected)
                    {
                        if (facesListCurr.Count > 1)
                        {
                            if (currentFaceCurr == facesListCurr.Count - 1)
                            {
                                currentFaceCurr = 0;
                                RedrawFaces(curr.Title, false);
                            }
                            else
                            {
                                currentFaceCurr++;
                                RedrawFaces(curr.Title, false);
                            }
                        }
                    }
                    break;
                case "rightButtonNext":
                    if (facesDetected)
                    {
                        if (facesListNext.Count > 1)
                        {
                            if (currentFaceNext == facesListNext.Count - 1)
                            {
                                currentFaceNext = 0;
                                RedrawFaces(next.Title, true);
                            }
                            else
                            {
                                currentFaceNext++;
                                RedrawFaces(next.Title, true);
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// imgLocation: false for left/curr, true for right/next
        /// </summary>
        /// <param name="imgName"></param>
        /// <param name="imgLocation"></param>
        public void RedrawFaces(string imgName, bool imgLocation)
        {

            int tmpcurrentFace = 0;
            List<Rectangle> tmpfacesList;
            Image<Bgr, byte> tmpImageI;

            if (!imgLocation) // true -> curr // false -> next
            {
                tmpcurrentFace = currentFaceCurr;
                tmpfacesList = facesListCurr;
                tmpImageI = new Image<Bgr, byte>(imgName);
            }
            else
            {
                tmpcurrentFace = currentFaceNext;
                tmpfacesList = facesListNext;
                tmpImageI = new Image<Bgr, byte>(imgName);
            }


            if (tmpfacesList.Count > 0)
            {
                for (int i = 0; i < tmpfacesList.Count; i++)
                {
                    if (i == tmpcurrentFace)
                    {
                        tmpImageI.Draw(tmpfacesList[i], new Bgr(0, 255, 0), RECT_WIDTH);
                    }
                    else if (i != tmpcurrentFace)
                    {
                        tmpImageI.Draw(tmpfacesList[i], new Bgr(0, 0, 255), RECT_WIDTH);
                    }

                }

            }

            if (!imgLocation)
            {
                PreviewImageHolder updatedImage = new PreviewImageHolder
                {
                    CurrImage = BitmapSourceConvert.ToBitmapSource(tmpImageI),
                };
                currImage.DataContext = updatedImage;
                curr.FaceLocation = facesListCurr[tmpcurrentFace];


                currentFaceCurr = tmpcurrentFace;
                facesListCurr = tmpfacesList;
                currImageI = tmpImageI;
            }
            else
            {
                PreviewImageHolder updatedImage = new PreviewImageHolder
                {
                    NextImage = BitmapSourceConvert.ToBitmapSource(tmpImageI),
                };
                nextImage.DataContext = updatedImage;
                next.FaceLocation = facesListNext[tmpcurrentFace];

                currentFaceNext = tmpcurrentFace;
                facesListNext = tmpfacesList;
                nextImageI = tmpImageI;

            }

        }

    }
}
