using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FaceMorph
{
    public class ImageDetails {

        private string _Title;
        private BitmapImage _ImageData;
        private Image _ImageElement;
        private Border _ImageBorder;
        private bool _isSelected = false;
        private bool _toDelete = false;



        public ImageDetails()
        {
            Console.WriteLine("An image was added");
        }

        public string Title
        {
            get { return this._Title; }
            set { this._Title = value; }
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

        public Border ImageBorder
        {
            get { return _ImageBorder; }
            set { _ImageBorder = value; }
        }

        public bool ToDelete
        {
            get { return _toDelete; }
            set { _toDelete = value; }
        }






    }
}
