using FaceMorph.Helpers;
using FaceMorph.ViewsAndControllers;
using Microsoft.Win32;
using Newtonsoft.Json;
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
        public ObservableCollection<ImageDetails> Images = new ObservableCollection<ImageDetails>();
        private System.Windows.Forms.BindingSource imagesBindingSource = new System.Windows.Forms.BindingSource();
        private int _currentImage = 0;
        private int _previousImage = 0;
        private static int _imagesCounter = 0;
        public const string JSON_FILE = @"images.json";
        private string json;
        public bool loadDataAtStartUp = true;


        public MainWindow()
        {

            try
            {
            InitializeComponent();
            if (loadDataAtStartUp)
                LoadImageHelper();
            //EmguTester em = new EmguTester();

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


        }

        /// <summary>
        /// Adds single Image to screen using AddImageHelper
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
            _currentImage = int.Parse(im.Uid);

            ImageDetails curr = Images.Where(x => x.Id == _currentImage).FirstOrDefault();

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
        /// Opens the Preview Window, with the clicked image in the middle
        /// </summary>
        private void ImageDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            ImageDetails curr = Images.Where(x => x.Id == _currentImage).FirstOrDefault();
            if (_currentImage == Images.Count - 1)
            {
                curr = Images.Where(x => x.Id == _currentImage - 1).FirstOrDefault();
            }
            
            PreviewWindow previewWindow = new PreviewWindow(curr, Images);
            previewWindow.ShowDialog(); // show dialog disables the main window
            
        }

        /// <summary>
        /// Adds Image to image List
        /// </summary>
        private void AddImageHelper(string filePath)
        {

            Image image = new Image();
            ImageSource imageSource = new BitmapImage(new Uri(filePath));
            image.Source = imageSource;
            image.MouseUp += ImageClicked;

            Images.Add(

                new ImageDetails
                {
                    Title = filePath,
                    ImageData = new BitmapImage(new Uri(filePath)),
                    ImageElement = image,
                    BorderColor = "",
                    Id = _imagesCounter
                });
            _imagesCounter++;
            imagesBindingSource.DataSource = Images;
            imagePreview.ItemsSource = imagesBindingSource;
            imagePreview.Items.Refresh(); // todo: add to listener
        }

        public void LoadImageHelper()
        {

            if (File.Exists(JSON_FILE))
            {
                Images.Clear();
                List<TmpImageDetails> tmpList = new List<TmpImageDetails>();

                using (StreamReader r = new StreamReader(JSON_FILE))
                {
                    string json = r.ReadToEnd();
                    tmpList = JsonConvert.DeserializeObject<List<TmpImageDetails>>(json);
                }

                tmpList.ForEach(x => AddImageHelper(x.Title));
                imagePreview.Items.Refresh();
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {

            var selectedItems = imagePreview.SelectedItems;
            foreach (ImageDetails selectedItem in selectedItems)
            {
                selectedItem.ToDelete = true;
            }

            // todo: delete this?
            foreach (var x in Images.ToList())
            {
                if (x.ToDelete)
                {
                    Images.Remove(x);
                    imagePreview.Items.Refresh();

                }
            }
            CleanUpIndex(); // todo: add to listener
        }

        private void CmRemove_Click(object sender, RoutedEventArgs e)
        {
            ImageDetails img = GetCurrentImage();
            Images.Remove(img);
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

                Images = ObservableCollectionExtension.Swap(Images, currentImage, nextImage);
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

                Images = ObservableCollectionExtension.Swap(Images, currentImage, nextImage);
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

        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            List<TmpImageDetails> tmpList = new List<TmpImageDetails>();
            foreach (ImageDetails imgd in Images)
            {
                tmpList.Add(
                    new TmpImageDetails
                    {
                        Title = imgd.Title,
                        //Id = imgd.Id
                    });
            }
            json = JsonConvert.SerializeObject(tmpList);
            File.WriteAllText(JSON_FILE, json);

        }

        private void LoadProject_Click(object sender, RoutedEventArgs e)
        {
            LoadImageHelper();
        }
        


        public ImageDetails GetCurrentImage()
        {
            foreach (ImageDetails im in Images)
            {
                if (im.Id == _currentImage)
                    return im;
            }
            return Images.ElementAt(0);
        }

        public ImageDetails GetNextImage()
        {
            ImageDetails curr = GetCurrentImage();
            int i = curr.Id;
            if (i + 1 <= Images.Count)
                return Images.ElementAt(i + 1);

            return curr;
        }

        public void CleanUpIndex()
        {
            for (int i = 0; i < Images.Count; i++)
            {
                Images.ElementAt(i).Id = i;
            }
            _imagesCounter = Images.Count;
        }

        public static int NumOfImages
        {
            get { return _imagesCounter; }
        }

        public ObservableCollection<ImageDetails> GetImages()
        {
            return Images;
        }

        private void ImagePreview_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //ListViewItem lbi = ((sender as ListView).SelectedItem as ListViewItem);
            //string m = "   You selected " + lbi.Content.ToString() + ".";
            //Console.WriteLine($"Sender: {sender}, Args: {e}");

            var selectedItems = imagePreview.SelectedItems;
            foreach (ImageDetails selectedItem in selectedItems)
            {
                Console.WriteLine(selectedItem.Id);
            }

            //TODO: change deleting and rearranging images to work with this
        }


    }
}
