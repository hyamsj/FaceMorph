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

        List<FaceInfoHolder> faceInfoHoldersList = new List<FaceInfoHolder>();


        VectorOfVectorOfInt delaunayTri = new VectorOfVectorOfInt();

        private ImagePreprocessor _preprocessor;

        float defaultAlpha = 0.5f;

        public const int RECT_WIDTH = 5;
        public const double HAAR_SCALE_FACTOR = 1.05;
        public const int HAAR_SCALE_MIN_NEIGHBOURS = 4;
        public const double HAAR_MIN_FACE_FACTOR = 0.3;
        public const double HAAR_MAX_FACE_FACTOR = 0.8;

        private bool faceInfoListInit = false;
        private bool facesDetected = false;
        private int selectedFaceCurr = 0;
        private int selectedFaceNext = 0;

        private bool displayMorphOk = false;
        ActiveButtonEnum activeButtonEnumCurr;
        ActiveButtonEnum activeButtonEnumNext;

        /// <summary>
        /// Displays the PreviewWindow showing the current and next image, and if possible the morphed image
        /// </summary>
        /// <param name="imageDetails"></param>
        /// <param name="images"></param>
        public PreviewWindow(ImageDetails imageDetails, ObservableCollection<ImageDetails> images)
        {
            try
            {
                this.Images = images;
                this.curr = imageDetails;

                activeButtonEnumCurr = ActiveButtonEnum.None;
                activeButtonEnumNext = ActiveButtonEnum.None;

                DisplayImages();


                if (next != null)
                {
                    RefreshDisplayedImages();
                }
                InitializeComponent();

                if (displayMorphOk)
                {
                    MorphImage m = new MorphImage(_preprocessor.curr, _preprocessor.next, _preprocessor.ffpCurr, _preprocessor.ffpNext, 0.5f);
                    morphImage.Source = m.GetMorphedImage();
                    mySlider.Value = 0.5;
                }
                else
                {
                    InitializeComponent();
                    var uriSource = new Uri(@"/FaceMorph;component/data/MyDefaultImage.png", UriKind.Relative);
                    morphImage.Source = new BitmapImage(uriSource);
                }
                //InitializeFacesInfoList();
            }

            catch (Exception ex)
            {

                //Write ex.Message to a file
                using (StreamWriter outfile = new StreamWriter(@".\error.txt"))
                {
                    outfile.Write(ex.Message.ToString());
                }
            }    

            
        }

        /// <summary>
        /// When an image changes, this method updates all images
        /// </summary>
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
                //morphBtn.IsEnabled = false;
                mySlider.IsEnabled = false;
                displayMorphOk = false;
            }
            else
            {
                InitializeComponent();
                //morphBtn.IsEnabled = true;
                mySlider.IsEnabled = true;
                displayMorphOk = true;
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

        /// <summary>
        /// Displays the images
        /// </summary>
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
                next = Images.Where(x => x.Id == curr.Id + 1).FirstOrDefault();

                this.DataContext = new PreviewImageHolder()
                {
                    CurrImage = new BitmapImage(new Uri(curr.Title)),
                    NextImage = new BitmapImage(new Uri(next.Title)),
                };

                var pff = BitmapSourceConvert.ToBitmapSource(new Image<Bgr, byte>(curr.Title));

            }

        }

        /// <summary>
        /// Display Face button clicked, show found ROIs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        }


        /// <summary>
        /// Draws the rectangles with the found faces of the current image
        /// </summary>
        /// <param name="facesList"></param>
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
                currImage.Source = BitmapSourceConvert.ToBitmapSource(currDetectedFacesImg);

            }

        }

        /// <summary>
        /// Draws the rectangles with the found faces of the next image
        /// </summary>
        /// <param name="facesList"></param>
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
                        if (i == 0)
                        {
                            // draw green rect
                            nextDetectedFacesImg.Draw(facesList[i], new Bgr(0, 255, 0), RECT_WIDTH);
                            selectedFaceNext = i;
                        }
                        else if (i > 0)
                        {
                            // draw red rect
                            nextDetectedFacesImg.Draw(facesList[i], new Bgr(0, 0, 255), RECT_WIDTH);
                        }

                    }

                }
                nextImage.Source = BitmapSourceConvert.ToBitmapSource(nextDetectedFacesImg);
            }
        }

        private void MorphButton_Click(object sender, RoutedEventArgs e)
        {
            //MorphImage m = new MorphImage(_preprocessor.CurrImageI.Mat, _preprocessor.NextImageI.Mat, _preprocessor.ffpCurr, _preprocessor.ffpNext, defaultAlpha);

            try
            {
                MorphImage m = new MorphImage(_preprocessor.curr, _preprocessor.next, _preprocessor.ffpCurr, _preprocessor.ffpNext, defaultAlpha);
                morphImage.Source = m.GetMorphedImage();
                mySlider.Value = 0.5;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Something went wrong: {ex}");
            }

        }

        /// <summary>
        /// Slider value changed, this method updates the morphed image according to the alpha value of the slider
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double sliderValue = mySlider.Value;
            float updatedAlpha = (float)sliderValue;
            //MorphImage m = new MorphImage(_preprocessor.CurrImageI.Mat, _preprocessor.NextImageI.Mat, _preprocessor.ffpCurr, _preprocessor.ffpNext, updatedAlpha);
            MorphImage m = new MorphImage(_preprocessor.curr, _preprocessor.next, _preprocessor.ffpCurr, _preprocessor.ffpNext, updatedAlpha);
            morphImage.Source = m.GetMorphedImage();
        }

        /// <summary>
        /// Changes selected face of the buttons pointing left
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeFaceLeftButton_Click(object sender, RoutedEventArgs e)
        {
            string buttonName = ((Button)sender).Uid;

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
                                curr.SelectedFace = selectedFaceCurr;
                                _preprocessor.ffpCurr = _preprocessor.landmarksCurr[selectedFaceCurr];
                                RedrawFaces((int)ImageEnum.Curr);
                            }
                            else
                            {
                                selectedFaceCurr--;
                                next.SelectedFace = selectedFaceNext;
                                _preprocessor.ffpNext = _preprocessor.landmarksCurr[selectedFaceNext];
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
                                next.SelectedFace = selectedFaceNext;
                                _preprocessor.ffpNext = _preprocessor.landmarksCurr[selectedFaceNext];
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
        /// Changes current face of the buttons pointing right
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeFaceRightButton_Click(object sender, RoutedEventArgs e)
        {
            string buttonName = ((Button)sender).Uid;

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
                                curr.SelectedFace = selectedFaceCurr;
                                _preprocessor.ffpCurr = _preprocessor.landmarksCurr[selectedFaceCurr];
                                
                                RedrawFaces((int)ImageEnum.Curr);
                            }
                            else
                            {
                                selectedFaceCurr++;
                                curr.SelectedFace = selectedFaceCurr;
                                _preprocessor.ffpCurr = _preprocessor.landmarksCurr[selectedFaceCurr];
                                var x = curr.Id;
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
                                next.SelectedFace = selectedFaceNext;
                                _preprocessor.ffpNext = _preprocessor.landmarksCurr[selectedFaceNext];
                                RedrawFaces((int)ImageEnum.Next);
                            }
                            else
                            {
                                selectedFaceNext++;
                                next.SelectedFace = selectedFaceNext;
                                _preprocessor.ffpNext = _preprocessor.landmarksCurr[selectedFaceNext];
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

        /// <summary>
        /// Displays delaunay triangulation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelaunayCheckBox_Clicked(object sender, RoutedEventArgs e)
        {
            Mat tmp = new Mat();
            string buttonName = ((RadioButton)sender).Uid;
            switch (buttonName)
            {
                case "delaunayRBCurr":
                    if (_preprocessor.delaunayTrianglesCurr != null)
                    {
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
                        activeButtonEnumCurr = ActiveButtonEnum.Delaunay;
                    }


                    break;
                case "delaunayRBNext":
                    if (_preprocessor.delaunayTrianglesNext != null)
                    {
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
                        activeButtonEnumNext = ActiveButtonEnum.Delaunay;

                    }
                    break;
                default:
                    throw new MissingFieldException();

            }

        }
        /// <summary>
        /// Displays facial feature points
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FFPCheckbox_Clicked(object sender, RoutedEventArgs e)
        {
            Mat tmp = new Mat();
            string buttonName = ((RadioButton)sender).Uid;
            switch (buttonName)
            {
                case "ffpRBCurr":
                    if (_preprocessor.ffpCurr != null)
                    {
                        currImageI.Mat.CopyTo(tmp);
                        this.currFFPImg = tmp.ToImage<Bgr, byte>();

                        FaceInvoke.DrawFacemarks(currFFPImg, _preprocessor.ffpCurr, new MCvScalar(255, 0, 0));
                        currImage.Source = BitmapSourceConvert.ToBitmapSource(currFFPImg);
                        activeButtonEnumCurr = ActiveButtonEnum.FFP;

                    }
                    break;
                case "ffpRBNext":
                    if (_preprocessor.ffpCurr != null)
                    {
                        nextImageI.Mat.CopyTo(tmp);
                        this.nextFFPImg = tmp.ToImage<Bgr, byte>();
                        FaceInvoke.DrawFacemarks(nextFFPImg, _preprocessor.ffpNext, new MCvScalar(255, 0, 0));
                        nextImage.Source = BitmapSourceConvert.ToBitmapSource(nextFFPImg);
                        activeButtonEnumNext = ActiveButtonEnum.FFP;
                    }
                    break;
                default:
                    throw new MissingFieldException();

            }


        }

        /// <summary>
        /// shows images unaltered
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NoneRB_Clicked(object sender, RoutedEventArgs e)
        {

            string buttonName = ((RadioButton)sender).Uid;
            switch (buttonName)
            {
                case "noneRBCurr":
                    currImage.Source = BitmapSourceConvert.ToBitmapSource(currImageI);
                    activeButtonEnumCurr = ActiveButtonEnum.None;
                    break;
                case "noneRBNext":
                    nextImage.Source = BitmapSourceConvert.ToBitmapSource(nextImageI);
                    activeButtonEnumNext = ActiveButtonEnum.None;
                    break;
                default:
                    throw new MissingFieldException();
            }




        }
        /// <summary>
        /// Changes active pictures
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

                        if (displayMorphOk)
                        {
                            DisplayImagesCorrectCB();

                        }

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
                        //DisplayImages();
                        if (displayMorphOk)
                        {
                            DisplayImagesCorrectCB();

                        }
                    }

                    break;
                default:
                    throw new MissingFieldException();
            }

            if (displayMorphOk)
            {
                MorphImage m = new MorphImage(_preprocessor.curr, _preprocessor.next, _preprocessor.ffpCurr, _preprocessor.ffpNext, 0.5f);
                morphImage.Source = m.GetMorphedImage();
                mySlider.Value = 0.5;
            }
            else
            {
                var uriSource = new Uri(@"/FaceMorph;component/data/MyDefaultImage.png", UriKind.Relative);
                morphImage.Source = new BitmapImage(uriSource);
            }

            DisplayImages();

        }

        /// <summary>
        /// Makes sure that settings are kept when switching images
        /// </summary>
        private void DisplayImagesCorrectCB()
        {
            Mat tmp = new Mat();
            switch (activeButtonEnumCurr)
            {
                case ActiveButtonEnum.None:
                    currImage.Source = BitmapSourceConvert.ToBitmapSource(currImageI);
                    break;

                case ActiveButtonEnum.FFP:
                    currImageI.Mat.CopyTo(tmp);
                    this.currFFPImg = tmp.ToImage<Bgr, byte>();
                    FaceInvoke.DrawFacemarks(currFFPImg, _preprocessor.ffpCurr, new MCvScalar(255, 0, 0));
                    currImage.Source = BitmapSourceConvert.ToBitmapSource(currFFPImg);
                    break;

                case ActiveButtonEnum.Delaunay:
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
                    break;

                default:
                    break;
            }

            switch (activeButtonEnumNext)
            {
                case ActiveButtonEnum.None:
                    nextImage.Source = BitmapSourceConvert.ToBitmapSource(nextImageI);
                    break;

                case ActiveButtonEnum.FFP:
                    nextImageI.Mat.CopyTo(tmp);
                    this.nextFFPImg = tmp.ToImage<Bgr, byte>();
                    FaceInvoke.DrawFacemarks(nextFFPImg, _preprocessor.ffpNext, new MCvScalar(255, 0, 0));
                    nextImage.Source = BitmapSourceConvert.ToBitmapSource(nextFFPImg);
                    break;

                case ActiveButtonEnum.Delaunay:
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
                    break;

                default:
                    break;
            }

        }

        private void CurrImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ((System.Windows.Controls.Image)sender).Source = new BitmapImage(new Uri("/Assets/MyDefaultImage.png", UriKind.Relative));

        }

        /// <summary>
        /// Generates short Video of current 2 images
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VideoBtn_Click(object sender, RoutedEventArgs e)
        {

            int fps = 0;
            float alpha = 0f;
            string incomingFps = fpsCountUI.Text;
            string incomingAlpha = alphaValueUI.Text;

            if (int.TryParse(incomingFps, out fps))
            {
                if (fps < 0 || fps > 60)
                {
                    fps = 20;
                }
            }
            else
            {
                if (fps <= 0)
                {
                    fps = 20;
                }
            }

            if (float.TryParse(incomingAlpha, out alpha))
            {
                if (alpha <= 0 || alpha > 1)
                {
                    alpha = 0.1f;
                }
            }
            else
            {
                alpha = 0.1f;
            }

            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.AddExtension = true;
            sfd.DefaultExt = ".mp4";
            sfd.Filter = "Media Files|*.mp4";
            //sfd.ShowDialog();

            string path;

            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                path = sfd.FileName;
                
                VideoGenerator vg = new VideoGenerator(_preprocessor.curr, _preprocessor.next, _preprocessor.ffpCurr, _preprocessor.ffpNext, fps, alpha, path);
            }

        }

        /// <summary>
        /// Generates video of all images in the images list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VideoSaveFullVidBtn_Click(object sender, RoutedEventArgs e)
        {
            int fps = 0;
            float alpha = 0f;
            string incomingFps = fpsCountUI.Text;
            string incomingAlpha = alphaValueUI.Text;
            //string incomingData = fpsCountUI.Text;

            if (int.TryParse(incomingFps, out fps))
            {
                if (fps < 0 || fps > 60)
                {
                    fps = 20;
                }
            }
            else
            {
                if (fps <= 0)
                {
                    fps = 20;
                }
            }

            if (float.TryParse(incomingAlpha, out alpha))
            {
                if (alpha <= 0 || alpha > 1)
                {
                    alpha = 0.1f;
                }
            }
            else
            {
                alpha = 0.1f;
            }

            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.AddExtension = true;
            sfd.DefaultExt = ".mp4";
            sfd.Filter = "Media Files|*.mp4";
            //sfd.ShowDialog();

            string path;

            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                path = sfd.FileName;
                VideoGenerator vg = new VideoGenerator(Images, fps, alpha, path);
            }
        }
    }
}
