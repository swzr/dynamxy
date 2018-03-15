using System.Reflection;

namespace SWZR.Dynamxy
{
    /// <summary>
    /// A interceptor which returns the first parameter or null.
    /// </summary>
    public class EchoInterceptor : IInterceptor
    {
        /// <inheritdoc />
        public object InterceptMethod(object instance, MethodInfo info, object[] parameter)
        {
            return parameter.Length > 0 ? parameter[0] : null;
        }
    }
}