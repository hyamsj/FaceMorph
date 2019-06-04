using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FaceMorph.Helpers;
using FaceMorph.Morph;
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

        // Mat used for morph
        Mat currImageMat = new Mat();
        Mat nextImageMat = new Mat();
        Mat morphImageMat = new Mat();

        // Mat used to display detected faces to user
        Mat currDetectedFacesMat = new Mat();
        Mat nextDetectedFacesMat = new Mat();
        Image<Bgr, byte> currDetectedFacesImg;
        Image<Bgr, byte> nextDetectedFacesImg;
        Image<Bgr, byte> currDelaunayImg;
        Image<Bgr, byte> nextDelaunayImg;
        Image<Bgr, byte> nextFFPImg;
        Image<Bgr, byte> currFFPImg;


        VectorOfVectorOfInt delaunayTri = new VectorOfVectorOfInt();

        private ImagePreprocessor _preprocessor;

        float defaultAlpha = 0.5f;

        public const int RECT_WIDTH = 5;
        public const double HAAR_SCALE_FACTOR = 1.05;
        public const int HAAR_SCALE_MIN_NEIGHBOURS = 4;
        public const double HAAR_MIN_FACE_FACTOR = 0.3;
        public const double HAAR_MAX_FACE_FACTOR = 0.8;


        private bool facesDetected = false;
        private int selectedFaceCurr = 0;
        private int selectedFaceNext = 0;

        public PreviewWindow(ImageDetails imageDetails, ObservableCollection<ImageDetails> images)
        {
            this.Images = images;
            this.curr = imageDetails;
            DisplayImages();


            if (next != null)
            {
                RefreshDisplayedImages();
            }
            InitializeComponent();
        }

        private void RefreshDisplayedImages()
        {
            currImageI = new Image<Bgr, byte>(curr.Title);
            nextImageI = new Image<Bgr, byte>(next.Title);
            _preprocessor = new ImagePreprocessor(curr, next);

            int currFacesCount = _preprocessor.FacesListCurr.Count;
            int nextFacesCount = _preprocessor.FacesListNext.Count;
            if (currFacesCount > 0 && nextFacesCount > 0)
            {
                facesDetected = true;
            }
            if (!_preprocessor.MorphEnabled)
            {
                InitializeComponent();
                morphBtn.IsEnabled = false;
                mySlider.IsEnabled = false;
            } else
            {
                InitializeComponent();
                morphBtn.IsEnabled = true;
                mySlider.IsEnabled = true;
            }

            // Display errors and number of found faces:
            facesCountCurr.Content = $"{_preprocessor.FacesListCurr.Count}";
            facesCountNext.Content = $"{_preprocessor.FacesListNext.Count}";
            errorMessageCurr.Content = "";
            errorMessageNext.Content = "";
            if (_preprocessor.FacesListCurr.Count == 0)
            {
                errorMessageCurr.Content = "No faces were found";
            }
            errorMessageCurr.Foreground = System.Windows.Media.Brushes.Red;

            if (_preprocessor.FacesListNext.Count == 0)
            {
                errorMessageNext.Content = "No faces were found";
            }
            errorMessageNext.Foreground = System.Windows.Media.Brushes.Red;
            // ---------------------------------------------

            this.currImageI = _preprocessor.CurrImageI;
            this.nextImageI = _preprocessor.NextImageI;


            
        }

        public void DisplayImages()
        {
            Console.WriteLine("test");
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
                next = Images.Where(x => x.Id == curr.Id + 1).FirstOrDefault();

                this.DataContext = new PreviewImageHolder()
                {
                    CurrImage = new BitmapImage(new Uri(curr.Title)),
                    NextImage = new BitmapImage(new Uri(next.Title)),
                };

                var pff = BitmapSourceConvert.ToBitmapSource(new Image<Bgr, byte>(curr.Title));

            }

        }

        private void DisplayFaceClicked(object sender, RoutedEventArgs e)
        {
            if (next == null)
            {
                facesCountCurr.Content = $"Nothing to morph";
            }
            else if (facesDetected)
            {

                DrawFaceRectsCurr(_preprocessor.FacesListCurr); // todo cleanup
                DrawFaceRectsNext(_preprocessor.FacesListNext);
                facesDetected = true;
                facesCountCurr.Content = $"{_preprocessor.FacesListCurr.Count}";
                facesCountNext.Content = $"{_preprocessor.FacesListNext.Count}";

            }
            //else
            //{
            //    facesCountCurr.Content = $"{_preprocessor.FacesListCurr.Count}";
            //    facesCountNext.Content = $"{_preprocessor.FacesListNext.Count}";

            //    if (_preprocessor.FacesListCurr.Count == 0)
            //    {
            //        errorMessageCurr.Content = "No faces were found";
            //    } 
            //    errorMessageCurr.Foreground = System.Windows.Media.Brushes.Red;

            //    if (_preprocessor.FacesListNext.Count == 0)
            //    {
            //        errorMessageNext.Content = "No faces were found";
            //    }
            //    errorMessageNext.Foreground = System.Windows.Media.Brushes.Red;
            //}
        }

        public void DrawFaceRectsCurr(List<Rectangle> facesList)
        {
            if (facesList != null)
            {
                Mat tmp = new Mat();
                currImageI.Mat.CopyTo(tmp);
                currDetectedFacesImg = tmp.ToImage<Bgr, byte>();

                if (facesList.Count > 0)
                {
                    for (int i = 0; i < facesList.Count; i++)
                    {
                        Console.WriteLine($"Width: {facesList[i].Width}, Height: {facesList[i].Height}");
                        if (i == 0)
                        {
                            currDetectedFacesImg.Draw(facesList[i], new Bgr(0, 255, 0), RECT_WIDTH);
                            selectedFaceCurr = i;
                        }
                        else if (i > 0)
                        {
                            currDetectedFacesImg.Draw(facesList[i], new Bgr(0, 0, 255), RECT_WIDTH);
                        }

                    }

                }
                CvInvoke.Imwrite("testimages/detectedface.jpg", currDetectedFacesImg);
                currImage.Source = BitmapSourceConvert.ToBitmapSource(currDetectedFacesImg);

            }

        }

        public void DrawFaceRectsNext(List<Rectangle> facesList)
        {
            if (facesList != null)
            {
                Mat tmp = new Mat();
                nextImageI.Mat.CopyTo(tmp);
                nextDetectedFacesImg = tmp.ToImage<Bgr, byte>();

                if (facesList.Count > 0)
                {
                    for (int i = 0; i < facesList.Count; i++)
                    {
                        Console.WriteLine($"Width: {facesList[i].Width}, Height: {facesList[i].Height}");
                        if (i == 0)
                        {
                            // draw green rect
                            nextDetectedFacesImg.Draw(facesList[i], new Bgr(0, 255, 0), RECT_WIDTH);
                            selectedFaceNext = i;
                            //CvInvoke.Imwrite("testimages/facerectimg.jpg", nextDetectedFacesImg);
                        }
                        else if (i > 0)
                        {
                            // draw red rect
                            nextDetectedFacesImg.Draw(facesList[i], new Bgr(0, 0, 255), RECT_WIDTH);
                        }

                    }

                }
                CvInvoke.Imwrite("testimages/detectedface.jpg", nextDetectedFacesImg);
                nextImage.Source = BitmapSourceConvert.ToBitmapSource(nextDetectedFacesImg);
            }
        }

        public void DrawFFPCurr()
        {
            Image<Bgr, byte> myImage = currImageMat.ToImage<Bgr, byte>();
            FaceInvoke.DrawFacemarks(myImage, _preprocessor.ffpCurr, new MCvScalar(255, 0, 0));
            //CvInvoke.Imwrite("testffp.jpg", myImage);
            currImage.Source = BitmapSourceConvert.ToBitmapSource(myImage);
        }

        public void DrawFFPNext()
        {
            Image<Bgr, byte> myImage = nextImageMat.ToImage<Bgr, byte>();
            FaceInvoke.DrawFacemarks(myImage, _preprocessor.ffpNext, new MCvScalar(255, 0, 0));
            //CvInvoke.Imwrite("testffp.jpg", myImage);
            nextImage.Source = BitmapSourceConvert.ToBitmapSource(myImage);
        }

        private void MorphButton_Click(object sender, RoutedEventArgs e)
        {
            //MorphImage m = new MorphImage(_preprocessor.CurrImageI.Mat, _preprocessor.NextImageI.Mat, _preprocessor.ffpCurr, _preprocessor.ffpNext, defaultAlpha);

            try
            {
            MorphImage m = new MorphImage(_preprocessor.curr, _preprocessor.next, _preprocessor.ffpCurr, _preprocessor.ffpNext, defaultAlpha);
            morphImage.Source = m.GetMorphedImage();
            mySlider.Value = 0.5;

            }catch(Exception ex)
            {
                MessageBox.Show($"Something went wrong: {ex}");
            }
            
            //CvInvoke.Imwrite("testimages/poster1.jpg",m.GetMorphedImageI());
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double sliderValue = mySlider.Value;
            float updatedAlpha = (float)sliderValue;
            //MorphImage m = new MorphImage(_preprocessor.CurrImageI.Mat, _preprocessor.NextImageI.Mat, _preprocessor.ffpCurr, _preprocessor.ffpNext, updatedAlpha);
            MorphImage m = new MorphImage(_preprocessor.curr, _preprocessor.next, _preprocessor.ffpCurr, _preprocessor.ffpNext, updatedAlpha);
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
                        if (_preprocessor.FacesListCurr.Count > 1)
                        {
                            if (selectedFaceCurr == 0)
                            {
                                selectedFaceCurr = _preprocessor.FacesListCurr.Count - 1;
                                RedrawFaces((int)ImageEnum.Curr);
                            }
                            else
                            {
                                selectedFaceCurr--;
                                RedrawFaces((int)ImageEnum.Curr);
                            }
                        }
                    }
                    break;
                case "leftButtonNext":
                    if (facesDetected)
                    {
                        if (_preprocessor.FacesListNext.Count > 1)
                        {
                            if (selectedFaceNext == 0)
                            {
                                selectedFaceNext = _preprocessor.FacesListNext.Count - 1;
                                RedrawFaces((int)ImageEnum.Next);
                            }
                            else
                            {
                                selectedFaceNext--;
                                RedrawFaces((int)ImageEnum.Next);
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
            string buttonName = ((Button)sender).Uid;
            Console.WriteLine(buttonName);

            switch ((sender as Button).Uid)
            {
                case "rightButtonCurr":

                    if (facesDetected)
                    {
                        if (_preprocessor.FacesListCurr.Count > 1)
                        {
                            if (selectedFaceCurr == _preprocessor.FacesListCurr.Count - 1)
                            {
                                selectedFaceCurr = 0;
                                RedrawFaces((int)ImageEnum.Curr);
                            }
                            else
                            {
                                selectedFaceCurr++;
                                RedrawFaces((int)ImageEnum.Curr);
                            }
                        }
                    }
                    break;
                case "rightButtonNext":
                    if (facesDetected)
                    {
                        if (_preprocessor.FacesListNext.Count > 1)
                        {
                            if (selectedFaceNext == _preprocessor.FacesListNext.Count - 1)
                            {
                                selectedFaceNext = 0;
                                RedrawFaces((int)ImageEnum.Next);
                            }
                            else
                            {
                                selectedFaceNext++;
                                RedrawFaces((int)ImageEnum.Next);
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 0 is left (curr), 1 is right (next)
        /// </summary>
        /// <param name="imgLocation"></param>
        public void RedrawFaces(int imgLocation)
        {
            int tmpcurrentFace = 0; // todo: check if safe to remove
            List<Rectangle> tmpfacesList;
            Image<Bgr, byte> tmpImageI;



            if (imgLocation == (int)ImageEnum.Curr)
            {
                tmpcurrentFace = selectedFaceCurr;
                curr.SelectedFace = selectedFaceCurr;
                tmpfacesList = _preprocessor.FacesListCurr;

                Mat tmp = new Mat();
                currImageI.Mat.CopyTo(tmp);
                tmpImageI = tmp.ToImage<Bgr, byte>();
            }
            else
            {
                tmpcurrentFace = selectedFaceNext;
                tmpfacesList = _preprocessor.FacesListNext;
                Mat tmp = new Mat();
                nextImageI.Mat.CopyTo(tmp);
                tmpImageI = tmp.ToImage<Bgr, byte>();
                //CvInvoke.Imwrite("testimages/redraw.jpg", currImageI);
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

            if (imgLocation == (int)ImageEnum.Curr)
            {
                curr.FaceLocation = _preprocessor.FacesListCurr[tmpcurrentFace];
                currImage.Source = BitmapSourceConvert.ToBitmapSource(tmpImageI);


                selectedFaceCurr = tmpcurrentFace;
                curr.SelectedFace = tmpcurrentFace;
                _preprocessor.FacesListCurr = tmpfacesList;
                //currImageI = tmpImageI;
            }
            else
            {
                nextImage.Source = BitmapSourceConvert.ToBitmapSource(tmpImageI);
                next.FaceLocation = _preprocessor.FacesListNext[tmpcurrentFace];

                selectedFaceNext = tmpcurrentFace;
                next.SelectedFace = tmpcurrentFace;
                _preprocessor.FacesListNext = tmpfacesList;
            }
            _preprocessor.UpdateSelectedFace(curr.SelectedFace, next.SelectedFace);

        }

        public void NoFaceFound(int facesCurr, int facesNext)
        {
            facesCountCurr.Content = $"{facesCurr}";
            facesCountNext.Content = $"{facesNext}";
        }

        private void DelaunayCheckBox_Clicked(object sender, RoutedEventArgs e)
        {
            Mat tmp = new Mat();
            string buttonName = ((RadioButton)sender).Uid;
            switch (buttonName)
            {
                case "delaunayRBCurr":
                    currImageI.Mat.CopyTo(tmp);
                    this.currDelaunayImg = tmp.ToImage<Bgr, byte>();

                    foreach (Triangle2DF triangle in _preprocessor.delaunayTrianglesCurr)
                    {

                        System.Drawing.Point[] vertices = Array.ConvertAll<PointF, System.Drawing.Point>(triangle.GetVertices(), System.Drawing.Point.Round);
                        using (VectorOfPoint vp = new VectorOfPoint(vertices))
                        {
                            CvInvoke.Polylines(currDelaunayImg, vp, true, new Bgr(255, 255, 255).MCvScalar);
                        }
                    }
                    currImage.Source = BitmapSourceConvert.ToBitmapSource(currDelaunayImg);
                    //CvInvoke.Imwrite("testimages/delaunay_curr.jpg", currDelaunayImg);


                    break;
                case "delaunayRBNext":
                    nextImageI.Mat.CopyTo(tmp);
                    this.nextDelaunayImg = tmp.ToImage<Bgr, byte>();

                    foreach (Triangle2DF triangle in _preprocessor.delaunayTrianglesNext)
                    {

                        System.Drawing.Point[] vertices = Array.ConvertAll<PointF, System.Drawing.Point>(triangle.GetVertices(), System.Drawing.Point.Round);
                        using (VectorOfPoint vp = new VectorOfPoint(vertices))
                        {
                            CvInvoke.Polylines(nextDelaunayImg, vp, true, new Bgr(255, 255, 255).MCvScalar);
                        }
                    }
                    nextImage.Source = BitmapSourceConvert.ToBitmapSource(nextDelaunayImg);
                    //CvInvoke.Imwrite("testimages/delaunay_next.jpg", nextDelaunayImg);
                    break;
                default:
                    throw new MissingFieldException();

            }

        }

        private void FFPCheckbox_Clicked(object sender, RoutedEventArgs e)
        {
            Mat tmp = new Mat();
            string buttonName = ((RadioButton)sender).Uid;
            switch (buttonName)
            {
                case "ffpRBCurr":
                    currImageI.Mat.CopyTo(tmp);
                    this.currFFPImg = tmp.ToImage<Bgr, byte>();

                    FaceInvoke.DrawFacemarks(currFFPImg, _preprocessor.ffpCurr, new MCvScalar(255, 0, 0));
                    //CvInvoke.Imwrite("testffp.jpg", currFFPImg);
                    currImage.Source = BitmapSourceConvert.ToBitmapSource(currFFPImg);
                    break;
                case "ffpRBNext":
                    nextImageI.Mat.CopyTo(tmp);
                    this.nextFFPImg = tmp.ToImage<Bgr, byte>();
                    FaceInvoke.DrawFacemarks(nextFFPImg, _preprocessor.ffpNext, new MCvScalar(255, 0, 0));
                    nextImage.Source = BitmapSourceConvert.ToBitmapSource(nextFFPImg);
                    //CvInvoke.Imwrite("testimages/ffp.jpg", nextFFPImg);
                    break;
                default:
                    throw new MissingFieldException();

            }


        }

        private void NoneRB_Clicked(object sender, RoutedEventArgs e)
        {
            string buttonName = ((RadioButton)sender).Uid;
            switch (buttonName)
            {
                case "noneRBCurr":
                    currImage.Source = BitmapSourceConvert.ToBitmapSource(currImageI);
                    break;
                case "noneRBNext":
                    nextImage.Source = BitmapSourceConvert.ToBitmapSource(nextImageI);
                    break;
                default:
                    throw new MissingFieldException();
            }




        }

        private void ChangeActivePicturesButton_Clicked(object sender, RoutedEventArgs e)
        {
            string buttonName = ((Button)sender).Uid;
            int currIdOld, currIdNew;

            switch (buttonName)
            {
                case "left":
                    if (0 >= curr.Id)
                    {
                        MessageBox.Show("no next image");
                    } 
                    else
                    {
                        currIdOld = curr.Id;
                        currIdNew = currIdOld - 1;
                        ImageDetails newCurrImageDetails = Images.ElementAt(currIdNew);
                        ImageDetails newNextImageDetails = Images.ElementAt(currIdNew + 1);
                        this.curr = newCurrImageDetails;
                        this.next = newNextImageDetails;

                        currImage.Source = BitmapSourceConvert.ToBitmapSource(new Image<Bgr, byte>(curr.Title));
                        nextImage.Source = BitmapSourceConvert.ToBitmapSource(new Image<Bgr, byte>(next.Title));

                        RefreshDisplayedImages();


                    }
                    break;
                case "right":
                    currIdOld = curr.Id;
                    currIdNew = currIdOld + 1;
                    if (Images.Count <= currIdNew + 1)
                    {
                        MessageBox.Show("no next image");
                    }
                    else
                    {
                        ImageDetails newCurrImageDetails = Images.ElementAt(currIdNew);
                        ImageDetails newNextImageDetails = Images.ElementAt(currIdNew + 1);
                        this.curr = newCurrImageDetails;
                        this.next = newNextImageDetails;

                        currImage.Source = BitmapSourceConvert.ToBitmapSource(new Image<Bgr, byte>(curr.Title));
                        nextImage.Source = BitmapSourceConvert.ToBitmapSource(new Image<Bgr, byte>(next.Title));

                        RefreshDisplayedImages();
                        //CvInvoke.Imwrite("testimages/test04.jpg", new Image<Bgr, byte>(y.Title));
                        //DisplayImages();
                    }

                    break;
                default:
                    throw new MissingFieldException();
            }

            //morphImage.Source = new BitmapImage(new Uri(@"/data/MyDefaultImage.png"));

            //morphImage.Source = new BitmapImage(new Uri("data/MyDefaultImage.png"));

            var uriSource = new Uri(@"/FaceMorph;component/data/MyDefaultImage.png", UriKind.Relative);
            morphImage.Source = new BitmapImage(uriSource);

            //todo remove center image

            DisplayImages(); // delete?

        }

        private void CurrImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ((System.Windows.Controls.Image)sender).Source = new BitmapImage(new Uri("/Assets/MyDefaultImage.png", UriKind.Relative));
        
        }

        private void VideoBtn_Click(object sender, RoutedEventArgs e)
        {
            VideoGenerator vg = new VideoGenerator(_preprocessor.curr, _preprocessor.next, _preprocessor.ffpCurr, _preprocessor.ffpNext);
        }
    }
}
