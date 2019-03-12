using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FaceMorph
{
    public class ImageDetails : INotifyPropertyChanged
    {

        private string _title;
        private BitmapSource _imageData; // was BitmapImage
        private Image _imageElement;
        private string _borderColor;
        private bool _isSelected = false;
        private bool _toDelete = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public ImageDetails()
        {
            Console.WriteLine("An image was added");

        }

        public string Title
        {
            get { return this._title; }
            set { this._title = value; NotifyPropertyChanged(); }
        }

        public BitmapSource ImageData
        {
            //get
            //{
            //    BitmapSource source = this._imageData as BitmapSource;
            //    if (source != null)
            //        this._imageData = source;
            //    return this._imageData;
            //}
            //set
            //{
            //    Console.WriteLine("Do i get reached?");
            //    BitmapSource source = value as BitmapSource;
            //    if (source != null)
            //        this._imageData = source;
            //}
            get { return this._imageData; }
            set { this._imageData = value; }
        }

        public Image ImageElement
        {
            get { return this._imageElement; }
            set { this._imageElement = value; }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; }
        }

        public string BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; NotifyPropertyChanged(); }
        }

        public bool ToDelete { get; set; }


        public int Id { get; set; } = 0;




        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



    }
}
