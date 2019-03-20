using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.IO;
using System.Drawing;

namespace OpenCVTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Image<Bgr, byte> imgInput;
        string MyImage = @"C:\Users\joni\Pictures\_Thesis Images\tmp\DSC00021.JPG";
        public MainWindow()
        {

            imgInput = new Image<Bgr, byte>(MyImage);

            DataContext = new ImageHolder
            {
                MyImage = BitmapSourceConvert.ToBitmapSource(imgInput),
            };

            InitializeComponent();
        }

        public void DetectFaceHaar()
        {
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string facePath = Path.GetFullPath(@"../../data/haarcascade_frontalface_default.xml");
                CascadeClassifier classifierFace = new CascadeClassifier(facePath);

                var imgGray = imgInput.Convert<Gray, byte>().Clone();
                Rectangle[] faces = classifierFace.DetectMultiScale(imgGray, 1.1, 4);
                foreach (var face in faces)
                {
                    imgInput.Draw(face, new Bgr(0, 0, 255), 2);
                }

                DataContext = new ImageHolder
                {
                    MyImage = BitmapSourceConvert.ToBitmapSource(imgInput),
                };
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }
    }
}
