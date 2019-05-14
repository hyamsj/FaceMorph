using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FaceMorph.Helpers
{
    public class PreviewImageHolder
    {
        public ImageSource MorphImage { get; set; }
        public ImageSource CurrImage { get; set; }
        public ImageSource NextImage { get; set; }

        
    }
}
