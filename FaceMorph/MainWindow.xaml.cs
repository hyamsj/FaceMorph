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
using Path = System.IO.Path;

namespace FaceMorph
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public const int IMAGE_WIDTH = 200;
        private List<ImageDetails> images = new List<ImageDetails>();

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Adds content of a Folder (Multiple Images)
        /// </summary>
        private void FileAddFolder_Click(object sender, RoutedEventArgs e)
        {
            var filePath = "";
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
            folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filePath = folderBrowserDialog.SelectedPath;
            }

            string[] files = Directory.GetFiles(folderBrowserDialog.SelectedPath);

            foreach (string f in files)
            {
                AddImageHelper(f);
            }
            // TODO error handling when string is empty

        }

        /// <summary>
        /// Adds single Image to screen
        /// </summary>
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

            AddImageHelper(filePath);
            // TODO error handling when string is empty

        }

        /// <summary>
        /// Dockpanel Add more pictures
        /// </summary>
        private void AddMorePictures_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Add more pictures clicked");
        }

        /// <summary>
        /// Image Click Event handler
        /// </summary>
        private void ImageClicked(object sender, MouseButtonEventArgs e)
        {
            Image im = (Image)sender;

            if (im.Parent is Border)
            {
                Border b = im.Parent as Border;
                b.BorderBrush = Brushes.Red;
                
                //imagePreview.Children.Remove(b);
            }

            //Console.WriteLine($"Number of images in list: {images.Count}");
            //Console.WriteLine($"Width: {im.Source.Width} Height: {im.Source.Height} Source: {im.Source}");

        }

        private void AddImageHelper(string filePath)
        {
            Border border = new Border();
            Image image = new Image();
            ImageSource imageSource = new BitmapImage(new Uri(filePath));
            image.Source = imageSource;
            image.Width = IMAGE_WIDTH;
            //image.Margin = new Thickness(10, 10, 10, 10);
            image.MouseUp += ImageClicked;
            images.Add(new ImageDetails {
                Title = image.Name,
                ImageData = new BitmapImage(new Uri(filePath)),
                ImageElement = image,
                ImageBorder = border
            });
            border.BorderThickness = new Thickness(1);
            border.Margin = new Thickness(10,10,10,10);
            border.Child = images.Last().ImageElement;
            imagePreview.Children.Add(border);
        }

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("left button clicked");
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Remove button clicked");
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Right button clicked");
        }

        private void RadioButtonMove_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("RadioButton move clicked");
        }

        private void RadioButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("RadioButton delete clicked");
        }
    }
}
