using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FaceMorph
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const int IMAGE_WIDTH = 200;
        List<Image> images = new List<Image>();
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FileAddFolder_Click(object sender, RoutedEventArgs e)
        {
            // TODO FolderBrowserDialog
        }

        private void FileAddImage_Click(object sender, RoutedEventArgs e)
        {
            var filePath = "";
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };
            if (openFileDialog.ShowDialog() == true)
            {
                filePath = openFileDialog.FileName;
            }
            Image image = new Image();
            ImageSource imageSource = new BitmapImage(new Uri(filePath));
            image.Source = imageSource;
            image.Width = IMAGE_WIDTH;
            image.Margin = new Thickness(10,10,10,10);
            image.MouseUp += ImageClicked;
            //image.MouseDown

            
            images.Add(image);
            imagePreview.Children.Add(image);
        }

        private void AddMorePictures_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Add more pictures clicked");                
        }

        // Probably doesn't belong in this class
        private void ImageClicked(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("Image was clicked");
        }
    }
}
