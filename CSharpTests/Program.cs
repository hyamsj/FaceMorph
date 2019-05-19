using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpTests
{
    class Program
    {
        static void Main(string[] args)
        {
            DemoCode demo = new DemoCode();
            int result = demo.GrandParentMethod(3);
            Console.WriteLine($"The value at the given position is {result}");

            Console.ReadLine();
        }
    }
}
