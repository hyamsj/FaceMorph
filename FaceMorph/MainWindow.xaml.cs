using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        
        //private List<ImageDetails> images = new List<ImageDetails>();
        public ObservableCollection<ImageDetails> images = new ObservableCollection<ImageDetails>();
        private System.Windows.Forms.BindingSource imagesBindingSource = new System.Windows.Forms.BindingSource();

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
            
            // 
            if (filePath != "")
            {
                string[] files = Directory.GetFiles(folderBrowserDialog.SelectedPath);

                foreach (string f in files)
                {
                    AddImageHelper(f);
                }
            }


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

            Image im = (Image)sender;
            int currentImageId = Int32.Parse(im.Uid);
            
            if (im.Parent is StackPanel)
            {
                Console.WriteLine($"{im.Uid}");

                if ((bool)rbDelete)
                {   
                    ImageDetails curr = images.Where(x => x.Id == currentImageId).FirstOrDefault();
                    curr.ToDelete = true;
                    curr.BorderColor = "Red";
                }

                if ((bool)rbMove)
                {
                    ImageDetails curr = images.Where(x => x.Id == currentImageId).FirstOrDefault();
                    curr.BorderColor = "Green";
                }
            }
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
                        
            border.BorderThickness = new Thickness(1);
            border.Margin = new Thickness(10, 10, 10, 10);

            var count = images.Count;

            images.Add(

                new ImageDetails
                {
                    Title = filePath,
                    ImageData = new BitmapImage(new Uri(filePath)),
                    ImageElement = image,
                    BorderColor = "", 
                    Id = count
                });

            this.imagesBindingSource.DataSource = images;
            //imagePreview.ItemsSource = null;
            imagePreview.ItemsSource = imagesBindingSource;
            imagePreview.Items.Refresh();

            Console.WriteLine($"Image Name {Title}");
            Console.WriteLine($"Total items in list {images.Count}");

        }

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {            
            Console.WriteLine("left button clicked");
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {

            foreach (var x in images.ToList())
            {
                if (x.ToDelete)
                {
                    images.Remove(x);
                    imagePreview.Items.Refresh();

                }
            }
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

        public void LoadProgramWithImages()
        {

        }






    }
}
