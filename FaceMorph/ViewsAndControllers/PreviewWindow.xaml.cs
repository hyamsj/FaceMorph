using Emgu.CV;
using Emgu.CV.Structure;
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
        ImageDetails prev;
        ImageDetails next;
        ObservableCollection<ImageDetails> Images;

        Image<Bgr, byte> imgInput;

        Rectangle[] facesArr;
        List<Rectangle> facesList;

        private bool facesDetected = false;
        private int currentFace = 0;

        public PreviewWindow(ImageDetails imageDetails, ObservableCollection<ImageDetails> images)
        {
            this.Images = images;
            this.curr = imageDetails;
            DisplayImages();

            imgInput = new Image<Bgr, byte>(curr.Title);

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

                prev = Images.Where(x => x.Id == curr.Id - 1).FirstOrDefault();
                this.DataContext = new PreviewImageHolder()
                {
                    PrevImage = new BitmapImage(new Uri(prev.Title)),
                    CurrImage = new BitmapImage(new Uri(curr.Title)),
                };
            }
            else
            {
                prev = Images.Where(x => x.Id == curr.Id - 1).FirstOrDefault();
                next = Images.Where(x => x.Id == curr.Id + 1).FirstOrDefault();
                this.DataContext = new PreviewImageHolder()
                {
                    PrevImage = new BitmapImage(new Uri(prev.Title)),
                    CurrImage = new BitmapImage(new Uri(curr.Title)),
                    NextImage = new BitmapImage(new Uri(next.Title)),
                };

            }

        }

        private void DisplayFaceClicked(object sender, RoutedEventArgs e)
        {
            if (!facesDetected)
            {
                DetectFaces(); // todo: should be called earlier (not the drawing, but the detecting)
                DrawFaceRects();
                facesDetected = true;
                facesCount.Content = $"{facesList.Count}";
            }

        }

        public void DetectFaces() // todo: call earlier
        {
            try
            {
                string facePath = Path.GetFullPath(@"../../data/haarcascade_frontalface_default.xml");
                CascadeClassifier classifierFace = new CascadeClassifier(facePath);

                var imgGray = imgInput.Convert<Gray, byte>().Clone();
                facesArr = classifierFace.DetectMultiScale(imgGray, 1.1, 4);
                facesList = facesArr.OfType<Rectangle>().ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public void DrawFaceRects()
        {
            if (facesList.Count > 0)
            {
                for (int i = 0; i < facesList.Count; i++)
                {
                    if (i == 0)
                    {
                        imgInput.Draw(facesList[i], new Bgr(0, 255, 0), 2);
                        currentFace = i;
                    }
                    else if (i > 0)
                    {
                        imgInput.Draw(facesList[i], new Bgr(0, 0, 255), 2);
                    }

                }

            }
            PreviewImageHolder updatedImage = new PreviewImageHolder
            {
                CurrImage = BitmapSourceConvert.ToBitmapSource(imgInput),
            };

            currImage.DataContext = updatedImage;
        }


        private void ChangeFaceLeftButton_Click(object sender, RoutedEventArgs e)
        {
            if (facesDetected)
            {
                if (facesList.Count > 1)
                {
                    if (currentFace == 0)
                    {
                        currentFace = facesList.Count - 1;
                        RedrawFaces();
                    }
                    else
                    {
                        currentFace--;
                        RedrawFaces();
                    }
                }
            }

        }

        private void ChangeFaceRightButton_Click(object sender, RoutedEventArgs e)
        {
            if (facesDetected)
            {
                if (facesList.Count > 1)
                {
                    if (currentFace == facesList.Count - 1)
                    {
                        currentFace = 0;
                        RedrawFaces();
                    }
                    else
                    {
                        currentFace++;
                        RedrawFaces();
                    }
                }
            }
        }

        public void RedrawFaces()
        {
            
            imgInput = new Image<Bgr, byte>(curr.Title);

            if (facesList.Count > 0)
            {
                for (int i = 0; i < facesList.Count; i++)
                {
                    if (i == currentFace)
                    {
                        imgInput.Draw(facesList[i], new Bgr(0, 255, 0), 2);
                    }
                    else if (i != currentFace)
                    {
                        imgInput.Draw(facesList[i], new Bgr(0, 0, 255), 2);
                    }

                }

            }
            PreviewImageHolder updatedImage = new PreviewImageHolder
            {
                CurrImage = BitmapSourceConvert.ToBitmapSource(imgInput),
            };
            currImage.DataContext = updatedImage;
            curr.FaceLocation = facesList[currentFace];
            
        }

    }
}
