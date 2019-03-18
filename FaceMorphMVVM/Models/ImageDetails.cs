using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FaceMorphMVVM.Models
{
    public class ImageDetails : INotifyPropertyChanged, IDataErrorInfo
    {
        private string _title;
        private int _id;
        private BitmapSource _imageData; // was BitmapImage
        private Image _imageElement;
        private string _borderColor;
        private bool _isSelected = false;
        private bool _toDelete = false;


        /// <summary>
        /// Initializes a new instance of the ImageDetails class
        /// </summary>
        public ImageDetails()
        {
            Console.WriteLine("An image was added");
        }

        /// <summary>
        /// Gets or sets the ImageDetails' title
        /// </summary>
        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                OnPropertyChanged("Title");
            }
        }

        public int ID
        {
            get { return _id; }
            set
            {
                _id = value;
                OnPropertyChanged("Id");
            }
        }

        public BitmapSource ImageData
        {
            get { return _imageData; }
            set
            {
                _imageData = value;
                OnPropertyChanged("ImageData");
            }
        }

        public Image ImageElement
        {
            get { return _imageElement; }
            set
            {
                _imageElement = value;
                OnPropertyChanged("ImageElement");
            }
        }

        public string BorderColor
        {
            get { return _borderColor; }
            set
            {
                _borderColor = value;
                OnPropertyChanged("ImageElement");
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                OnPropertyChanged("ImageElement");
            }
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;


        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDataErrorInfo Members

        public string Error
        {
            get;
            private set;
        }

        public string this[string columnName]
        {
            get
            {
                if (columnName == "Title")
                {
                    if (String.IsNullOrWhiteSpace(Title))
                    {
                        Error = "Name cannot be null or empty.";
                    }
                    else
                    {
                        Error = null;
                    }
                }

                return Error;
            }
        }

        #endregion

    }
}
