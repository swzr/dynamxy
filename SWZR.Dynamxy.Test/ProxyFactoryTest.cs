using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SWZR.Dynamxy
{
    /// <summary>
    /// Tests for <see cref="ProxyFactory{TInterceptor}"/>.
    /// </summary>
    [
        TestClass
    ]
    public class ProxyFactoryTest
    {
        /// <summary>
        /// Ensure parameter and return values are successfully transported.
        /// Positive.
        /// </summary>
        [
            TestMethod,
            Description("Ensure parameter and return values are successfully transported")
        ]
        public void Test_Positive_SimpleEcho()
        {
            var factory = new ProxyFactory<EchoInterceptor>();
            var test = factory.Create<ITestInterface>();

            // String
            var value = test.Echo("Hello");
            object expected = "Hello";
            Assert.AreEqual(value, expected);

            // Integer
            value = test.Echo(123);
            expected = 123;
            Assert.AreEqual(value, expected);

            // Double
            value = test.Echo(123.123);
            expected = 123.123;
            Assert.AreEqual(value, expected);

            // Complex object
            object obj = new StringBuilder();
            value = test.Echo(obj);
            expected = obj;
            Assert.AreEqual(value, expected);
        }
    }
}
