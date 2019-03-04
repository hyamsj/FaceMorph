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

        private string _Title;
        private BitmapImage _ImageData;
        private Image _ImageElement;
        private string _BorderColor;
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
            get { return this._Title; }
            set { this._Title = value; NotifyPropertyChanged(); }
        }

        public BitmapImage ImageData
        {
            get { return this._ImageData; }
            set { this._ImageData = value; }
        }

        public Image ImageElement
        {
            get { return this._ImageElement; }
            set { this._ImageElement = value; }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { _isSelected = value; }
        }

        public string BorderColor
        {
            get { return _BorderColor; }
            set { _BorderColor = value; NotifyPropertyChanged(); }
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
