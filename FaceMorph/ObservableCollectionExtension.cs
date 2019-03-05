using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceMorph
{
    public static class ObservableCollectionExtension
    {

        public static ObservableCollection<T> Swap<T>(this ObservableCollection<T> list, int indexA, int indexB)
        {
            if (indexB < MainWindow.NumOfImages && indexB >= 0)
            {
                T tmp = list[indexA];
                list[indexA] = list[indexB];
                list[indexB] = tmp;
            }
            return list;
        }



    }
}
