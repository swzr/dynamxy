using System.Reflection;

namespace SWZR.Dynamxy
{
    /// <summary>
    /// Interface for interceptors.
    /// </summary>
    public interface IInterceptor
    {
        /// <summary>
        /// Intercepts methods called on a proxy object.
        /// </summary>
        /// <param name="instance">The virtual object which method calls get intercepted by this proxy.</param>
        /// <param name="info">The object containing reflected information about the intercepted method.</param>
        /// <param name="parameter">The parameters which were handed to the original method.</param>
        /// <returns>The calculated value of the interception method.</returns>
        object InterceptMethod(object instance, MethodInfo info, object[] parameter);
    }
}
