using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FaceMorphTest
{
    [TestClass]
    public class EventsAndDelegateTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            Console.WriteLine("Hello test");
            Debug.WriteLine("Using debug writeline");
            Assert.AreEqual("a", "a", null, "Please work");
        }


    }
}
