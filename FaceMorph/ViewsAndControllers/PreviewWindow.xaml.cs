using FaceMorph.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

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

        public PreviewWindow(ImageDetails imageDetails, ObservableCollection<ImageDetails> images)
        {
            this.Images = images;
            this.curr = imageDetails;
            DisplayImages();

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

    }
}
