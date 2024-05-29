using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Images;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;

namespace tests
{
    [TestClass]
    public class UnitTests
    {
        /// <summary>
        /// Тест, просто ради контента
        /// </summary>
        [TestMethod]
        public void isAllowedExtension()
        {
            var f = new Images.MainForm();
            MethodInfo method = f.GetType().GetMethod("isAllowedExtension", (BindingFlags)(-1));

            Assert.AreEqual(method.Invoke(f, new object[] { "fofo.jpg" }), true);
            Assert.AreEqual(method.Invoke(f, new object[] { "" }), false);
            Assert.AreEqual(method.Invoke(f, new object[] { "C:\\Users\\1\\miniconda3\\envs\\tensorflow\\Lib\\site-packages\\google\\protobuf\\internal\\fofo.jpg" }), true);
            Assert.AreEqual(method.Invoke(f, new object[] { "fgasdfgasdg" }), false);
            Assert.AreEqual(method.Invoke(f, new object[] { String.Empty }), false);
            Assert.AreEqual(method.Invoke(f, new object[] { ".pNg" }), true);
        }


    }
}
