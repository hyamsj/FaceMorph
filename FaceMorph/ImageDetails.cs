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
        private BitmapImage _imageData;
        private Image _imageElement;
        private string _borderColor;
        private bool _isSelected = false;
        private bool _toDelete = false;
        private int _id = 0;
        

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

        public BitmapImage ImageData
        {
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

        public bool ToDelete
        {
            get { return _toDelete; }
            set { _toDelete = value; }
        }

        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }




        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



    }
}
