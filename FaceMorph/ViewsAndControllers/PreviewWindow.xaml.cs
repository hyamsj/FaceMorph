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
        
        Rectangle[] faces;
        //ObservableCollection<Rectangle> facesList = new ObservableCollection<Rectangle>();
        List<Rectangle> facesList;
        

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
            DetectFaces(); // todo: should be called earlier (not the drawing, but the detecting)
            DrawFaceRects();
        }

        private void RectangleClicked(object sender, MouseEventArgs e)
        {
            if ( facesList != null)
            {
                System.Windows.Point wpt = e.GetPosition((UIElement)sender);
                //System.Drawing.Point dpt = new System.Drawing.Point()
                //{
                //    X = (int)wpt.X,
                //    Y = (int)wpt.Y,
                //};

                var transform = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice;
                var mouse = transform.Transform(GetMousePosition());

                System.Drawing.Point newmouse = new System.Drawing.Point()
                {
                    X = (int)wpt.X,
                    Y = (int)wpt.Y,
                };

                //System.Drawing.Point mouseD = new System.Drawing.Point(Convert.ToInt32(wpt.X), Convert.ToInt32(wpt.Y));

                foreach (Rectangle face in facesList)
                {
                    if (face.Contains(newmouse))
                    {
                        //MessageBox.Show($"Mouse X: {newmouse.X}\nMouse Y: {newmouse.Y}\nRect0 X: {facesList[0].X}\nRect1 :Y {facesList[0].Y}");
                    }
                }

            }
        }

        public System.Windows.Point GetMousePosition()
        {
            System.Drawing.Point point = System.Windows.Forms.Control.MousePosition;
            return new System.Windows.Point(point.X, point.Y);
        }

        public void DetectFaces()
        {
            try
            {
                string facePath = Path.GetFullPath(@"../../data/haarcascade_frontalface_default.xml");
                CascadeClassifier classifierFace = new CascadeClassifier(facePath);

                var imgGray = imgInput.Convert<Gray, byte>().Clone();
                faces = classifierFace.DetectMultiScale(imgGray, 1.1, 4);
                facesList = faces.OfType<Rectangle>().ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public void DrawFaceRects()
        {
            foreach (var face in facesList)
            {
                
                imgInput.Draw(face, new Bgr(0, 0, 255), 2);
            }

            PreviewImageHolder updatedImage = new PreviewImageHolder
            {
                CurrImage = BitmapSourceConvert.ToBitmapSource(imgInput),
            };

            currImage.DataContext = updatedImage;
        }


    }
}
