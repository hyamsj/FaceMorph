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
        string MyImage = @"C:\Users\joni\Pictures\_Thesis Images\DSC00023.JPG";
        Rectangle[] faces;
        public MainWindow()
        {

            imgInput = new Image<Bgr, byte>(MyImage);

            DataContext = new ImageHolder
            {
                MyImage = BitmapSourceConvert.ToBitmapSource(imgInput),
            };

            InitializeComponent();
        }
        /// <summary>
        /// Uses Haarcascade
        /// </summary>
        private void Button_Haar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string facePath = Path.GetFullPath(@"../../data/haarcascade_frontalface_default.xml");
                CascadeClassifier classifierFace = new CascadeClassifier(facePath);

                var imgGray = imgInput.Convert<Gray, byte>().Clone();
                faces = classifierFace.DetectMultiScale(imgGray, 1.1, 4);
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

        private void Button_LBP_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string facePath = Path.GetFullPath(@"../../data/lbpcascade_frontalface.xml");
                CascadeClassifier classifierFace = new CascadeClassifier(facePath);

                var imgGray = imgInput.Convert<Gray, byte>().Clone();
                Rectangle[] faces = classifierFace.DetectMultiScale(imgGray, 1.1, 4);
                foreach (var face in faces)
                {
                    imgInput.Draw(face, new Bgr(0, 255, 0), 2);
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
