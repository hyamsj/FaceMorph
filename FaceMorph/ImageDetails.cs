using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FaceMorph
{
    class ImageDetails : Image {

        public ImageDetails()
        {
            Console.WriteLine("An image was added");
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Path { get; set; }

        public string FileName { get; set; }

        public string Extension { get; set; }

        public int Height { get; set; }

        public int Width { get; set; }

        public long Size { get; set; }

    }
}
