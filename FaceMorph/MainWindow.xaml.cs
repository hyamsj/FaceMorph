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
        public ObservableCollection<ImageDetails> images = new ObservableCollection<ImageDetails>();
        private System.Windows.Forms.BindingSource imagesBindingSource = new System.Windows.Forms.BindingSource();
        private int _currentImage = 0;
        private int _previousImage = 0;
        private static int _imagesCounter = 0;


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
        /// Image Click Event handler. 
        /// </summary>
        private void ImageClicked(object sender, MouseButtonEventArgs e)
        {
            bool? rbDelete = RBDelete.IsChecked;
            bool? rbMove = RBMove.IsChecked;

            Image im = (Image)sender;
            _previousImage = _currentImage;
            _currentImage = Int32.Parse(im.Uid);

            ImageDetails curr = images.Where(x => x.Id == _currentImage).FirstOrDefault();

            Console.WriteLine($"Current Image Method: {GetCurrentImage().Id}");

            if (im.Parent is StackPanel)
            {
                Console.WriteLine($"User Id: {im.Uid}");

                if ((bool)rbDelete)
                {

                    if (curr.ToDelete == false)
                    {
                        curr.ToDelete = true;
                        curr.BorderColor = "Red";
                    }
                    else
                    {
                        curr.ToDelete = false;
                        curr.BorderColor = "Transparent";
                    }
                }
            }
        }

        /// <summary>
        /// Adds Image to image List
        /// </summary>
        private void AddImageHelper(string filePath)
        {
            Border border = new Border();
            Image image = new Image();
            ImageSource imageSource = new BitmapImage(new Uri(filePath));
            image.Source = imageSource;
            image.Width = IMAGE_WIDTH;
            //image.Margin = new Thickness(10, 10, 10, 10);
            image.MouseUp += ImageClicked;

            border.BorderThickness = new Thickness(1);
            border.Margin = new Thickness(10, 10, 10, 10);

            images.Add(

                new ImageDetails
                {
                    Title = filePath,
                    ImageData = new BitmapImage(new Uri(filePath)),
                    ImageElement = image,
                    BorderColor = "",
                    Id = _imagesCounter
                });
            _imagesCounter++;
            this.imagesBindingSource.DataSource = images;
            //imagePreview.ItemsSource = null;
            imagePreview.ItemsSource = imagesBindingSource;
            imagePreview.Items.Refresh(); // todo: add to listener

            Console.WriteLine($"Image Name {Title}");
            Console.WriteLine($"Total items in list {images.Count}");

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
            CleanUpIndex(); // todo: add to listener
        }

        private void CmRemove_Click(object sender, RoutedEventArgs e)
        {
            ImageDetails img = GetCurrentImage();
            images.Remove(img);
            imagePreview.Items.Refresh();
            CleanUpIndex(); // todo: add to listener
        }

        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            bool? rbMove = RBMove.IsChecked;
            if ((bool)rbMove)
            {
                int currentImage = GetCurrentImage().Id;
                int nextImage = currentImage - 1;

                images = ObservableCollectionExtension.Swap(images, currentImage, nextImage);
                CleanUpIndex(); // todo: add listener
                imagePreview.Items.Refresh(); // todo: add to listener
                _currentImage--;
            }
        }

        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            bool? rbMove = RBMove.IsChecked;
            if ((bool)rbMove)
            {
                int currentImage = GetCurrentImage().Id;
                int nextImage = currentImage + 1;

                images = ObservableCollectionExtension.Swap(images, currentImage, nextImage);
                CleanUpIndex(); // todo: add listener
                imagePreview.Items.Refresh(); // todo: add to listener
                _currentImage++;
            }

            


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



        public void LoadProgramWithImages()
        {

        }

        public ImageDetails GetCurrentImage()
        {
            foreach (ImageDetails im in images)
            {
                if (im.Id == _currentImage)
                    return im;
            }
            return images.ElementAt(0);
        }

        public ImageDetails GetNextImage()
        {
            ImageDetails curr = GetCurrentImage();
            int i = curr.Id;
            if (i+1 <= images.Count)
                return images.ElementAt(i+1);

            return curr;
        }

        public void CleanUpIndex()
        {
            for (int i = 0; i < images.Count; i++)
            {
                images.ElementAt(i).Id = i;
            }
            _imagesCounter = images.Count;
        }

        public static int NumOfImages
        {
            get { return _imagesCounter; }
        }




    }
}
