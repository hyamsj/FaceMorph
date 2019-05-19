using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpTests
{
    public class DemoCode
    {
        public int GrandParentMethod(int position)
        {
            return ParentMethod(position);
        }

        public int ParentMethod(int position)
        {
            return GetNumber(position);
        }
            

        public int GetNumber(int position)
        {
            int[] numbers = new int[] { 1,4,7,2};
            return numbers[position];
        }
    }
}
