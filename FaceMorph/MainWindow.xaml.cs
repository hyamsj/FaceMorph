using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
            Console.WriteLine(filePath);
            if (filePath != "")
            {
                string[] files = Directory.GetFiles(folderBrowserDialog.SelectedPath);

                foreach (string f in files)
                {
                    AddImageHelper(f);
                }
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
            Console.WriteLine(filePath);
            if (filePath != "")
            {
                AddImageHelper(filePath);
            }

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
            bool? rbDelete = RBDelete.IsChecked;
            bool? rbMove = RBMove.IsChecked;
            //WrapPanel panel = (WrapPanel)sender;
            Image im = (Image)sender;
            Border b = im.Parent as Border;
            b.BorderBrush = Brushes.Red;
            //if (im.Parent is Border)
            //{
            //    if ((bool)rbDelete)
            //    {
            //        //imagePreview.Children.
            //        Border b = im.Parent as Border;
            //        b.BorderBrush = Brushes.Red;
            //        Console.WriteLine("Delete rbutton active");
            //    }

            //    if ((bool)rbMove)
            //    {
            //        Border b = im.Parent as Border;
            //        b.BorderBrush = Brushes.Green;
            //        Console.WriteLine("Move rbutton active");
            //    }
            //    //    //imagePreview.Children.Remove(b);
            //}

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
            image.Margin = new Thickness(10, 10, 10, 10);
            image.MouseUp += ImageClicked;
            //images.Add(new ImageDetails {
            //    Title = image.Name,
            //    ImageData = new BitmapImage(new Uri(filePath)),
            //    ImageElement = image,
            //    ImageBorder = border
            //});
            //border.BorderThickness = new Thickness(1);
            //border.Margin = new Thickness(10, 10, 10, 10);
            //border.Child = images.Last().ImageElement;
            //imagePreview.Children.Add(border);
            border.BorderThickness = new Thickness(1);
            border.Margin = new Thickness(10, 10, 10, 10);

            images.Add(

                new ImageDetails
                {
                    Title = filePath,
                    ImageData = new BitmapImage(new Uri(filePath)),
                    ImageElement = image,
                    ImageBorder = border

                });


            imagePreview.ItemsSource = null;
            imagePreview.ItemsSource = images;
            Console.WriteLine($"Image Name {Title}");
            Console.WriteLine($"Total items in list {images.Count}");

        }

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("left button clicked");
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            ImageDetails first = images[0];
            first.ImageBorder = new Border();
            first.ImageBorder.BorderThickness = new Thickness(1);
            first.ImageBorder.BorderBrush = Brushes.Red;

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
            // bool ?x = RBDelete.IsChecked;

        }

        public void RearrangeImages()
        {
            // what is selected
            // status move
            // where to move
        }

        public void RemoveImages()
        {

        }
    }
}
