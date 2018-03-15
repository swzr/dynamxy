using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace SWZR.Dynamxy
{
    /// <summary>
    /// Factory for creating dynamic proxy objects.
    /// </summary>
    /// <typeparam name="TInterceptor">Type of the used interceptor.</typeparam>
    public class ProxyFactory<TInterceptor> where TInterceptor : class, IInterceptor
    {
        /// <summary>
        /// Cache for types.
        /// </summary>
        private readonly IDictionary<Type, Type> types = new Dictionary<Type, Type>();

        /// <summary>
        /// Attributes of methodes which get intercepted.
        /// </summary>
        private const MethodAttributes Attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;

        /// <summary>
        /// The module builder for creating dynamic types.
        /// </summary>
        private readonly ModuleBuilder moduleBuilder;

        /// <summary>
        /// The interceptor instance.
        /// </summary>
        private readonly TInterceptor interceptor;

        /// <summary>
        /// Constructs an instance of <see cref="ProxyFactory{TProxy}"/>.
        /// </summary>
        /// <param name="assemblyName">Name of the dynamic assembly.</param>
        /// <param name="moduleName">Name of the dynamic module.</param>
        public ProxyFactory(string assemblyName = null, string moduleName = null)
            : this(Activator.CreateInstance<TInterceptor>(), assemblyName, moduleName)
        {
            // Empty.
        }

        /// <summary>
        /// Constructs an instance of <see cref="ProxyFactory{TProxy}"/>.
        /// </summary>
        /// <param name="interceptor">Instance of the used interceptor.</param>
        /// <param name="assemblyName">Name of the dynamic assembly.</param>
        /// <param name="moduleName">Name of the dynamic module.</param>
        /// <exception cref="ArgumentNullException">'interceptor' is null.</exception>
        public ProxyFactory(TInterceptor interceptor, string assemblyName = null, string moduleName = null)
        {
            this.interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));

            if (assemblyName == null)
            {
                assemblyName = typeof(TInterceptor).Name;
            }

            if (moduleName == null)
            {
                moduleName = assemblyName;
            }

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);
        }

        /// <summary>
        /// Constructs a dynamic proxy object.
        /// </summary>
        /// <typeparam name="TInterface">Type of the proxy interface.</typeparam>
        /// <returns>Proxy instance.</returns>
        /// <exception cref="NullReferenceException">No constructor defined.</exception>
        /// <exception cref="NullReferenceException">AssemblyQualifiedName or MethodInfo is null.</exception>
        public TInterface Create<TInterface>()
        {
            if (!types.TryGetValue(typeof(TInterface), out var type))
            {
                var signature = typeof(TInterface).Name.Substring(1, typeof(TInterface).Name.Length - 1);

                var typeBuilder = moduleBuilder.DefineType(signature,
                        TypeAttributes.Public |
                        TypeAttributes.Class |
                        TypeAttributes.AutoClass |
                        TypeAttributes.AnsiClass |
                        TypeAttributes.BeforeFieldInit |
                        TypeAttributes.AutoLayout,
                        null);

                typeBuilder.AddInterfaceImplementation(typeof(TInterface));

                var proxyField = typeBuilder.DefineField("interceptor", typeof(IInterceptor), FieldAttributes.Private);

                var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] { typeof(IInterceptor) });
                var ilGen = ctorBuilder.GetILGenerator();

                var getTypeMethodInfo = typeof(Type).GetMethod(nameof(Type.GetType), new[] { typeof(string) });

                // Push the pointer to 'this' on the stack.
                ilGen.Emit(OpCodes.Ldarg_0);

                // Push the first argument (of type 'IProxy') on the stack.
                ilGen.Emit(OpCodes.Ldarg_1);

                // Set the value of field 'interceptor' to the first argument of the constructor.
                ilGen.Emit(OpCodes.Stfld, proxyField);

                // Return at end of constructor.
                ilGen.Emit(OpCodes.Ret);

                foreach (var methodInfo in typeof(TInterface).GetMethods())
                {
                    var parameter = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();

                    var mb = typeBuilder.DefineMethod(methodInfo.Name, Attributes, methodInfo.ReturnType, parameter);
                    ilGen = mb.GetILGenerator();

                    var parameterCount = methodInfo.GetParameters().Length;

                    // Declare a local variable holding an array.
                    var paramTypeArray = ilGen.DeclareLocal(typeof(Type[]));

                    // Push the amount of the parameter on the stack as integer.
                    ilGen.Emit(OpCodes.Ldc_I4, parameterCount);

                    // Allocate and create a new array of type 'Type[]'.
                    ilGen.Emit(OpCodes.Newarr, typeof(Type));

                    // Connect the pointer of the newly created array and the previously created local variable.
                    ilGen.Emit(OpCodes.Stloc, paramTypeArray);

                    // Declare a local variable holding an array.
                    var paramArray = ilGen.DeclareLocal(typeof(object[]));

                    // Push the amount of the parameter on the stack as integer.
                    ilGen.Emit(OpCodes.Ldc_I4, parameterCount);

                    // Allocate and create a new array of type 'object[]'.
                    ilGen.Emit(OpCodes.Newarr, typeof(object));

                    // Connect the pointer of the newly created array and the previously created local variable.
                    ilGen.Emit(OpCodes.Stloc, paramArray);

                    // For each parameter of the method, do:
                    for (var i = 0; i < parameterCount; i++)
                    {
                        // Get the type of the parameter.
                        var parameterType = methodInfo.GetParameters()[i].ParameterType;

                        // Push the array holding all parameters on the stack.
                        ilGen.Emit(OpCodes.Ldloc, paramArray);

                        // Push the index of the parameter on the stack.
                        ilGen.Emit(OpCodes.Ldc_I4, i);

                        // Push the value of the parameter on the stack.
                        ilGen.Emit(OpCodes.Ldarg, i + 1);

                        // Box the value type to a reference type.
                        ilGen.Emit(OpCodes.Box, typeof(object));

                        // Put the object into the specified index in the array.
                        ilGen.Emit(OpCodes.Stelem, typeof(object));

                        // Push the array holding all parameters on the stack.
                        ilGen.Emit(OpCodes.Ldloc, paramTypeArray);

                        // Push the index of the parameter on the stack.
                        ilGen.Emit(OpCodes.Ldc_I4, i);

                        ilGen.Emit(OpCodes.Ldstr, parameterType.AssemblyQualifiedName ?? throw new NullReferenceException());

                        ilGen.EmitCall(OpCodes.Call, getTypeMethodInfo ?? throw new NullReferenceException(), new[] { typeof(string) });

                        // Put the object into the specified index in the array.
                        ilGen.Emit(OpCodes.Stelem, typeof(Type));
                    }

                    // Push the pointer to 'this' on the stack.
                    ilGen.Emit(OpCodes.Ldarg_0);

                    // Push the value of the field 'interceptor' on the stack (= instance of IProxy).
                    ilGen.Emit(OpCodes.Ldfld, proxyField);

                    // Push the instance of the newly created type on the stack.
                    ilGen.Emit(OpCodes.Ldarg_0);

                    // Push the instance of the newly created type on the stack.
                    ilGen.Emit(OpCodes.Dup);

                    // Call the method 'GetType()' on the instance of the newly created type.
                    ilGen.EmitCall(OpCodes.Call, typeof(object).GetMethod(nameof(GetType)) ?? throw new NullReferenceException(), Type.EmptyTypes);

                    // Push the name of the interface on the stack.
                    ilGen.Emit(OpCodes.Ldstr, typeof(TInterface).Name);

                    // Get the type of the interface and push it on the stack.
                    ilGen.EmitCall(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetInterface), new[] { typeof(string) }) ?? throw new NullReferenceException(), Type.EmptyTypes);

                    // Push the name of the newly created method on the stack.
                    ilGen.Emit(OpCodes.Ldstr, methodInfo.Name);

                    ilGen.Emit(OpCodes.Ldloc, paramTypeArray);

                    // Get the corresponding 'MethodInfo' of the created method and push it on the stack.
                    ilGen.EmitCall(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetMethod), new[] { typeof(string), typeof(Type[]) }) ?? throw new NullReferenceException(), new[] { typeof(string) });

                    // Load the previously created array of parameter and push it on the stack.
                    ilGen.Emit(OpCodes.Ldloc, paramArray);

                    // Call the method 'InterceptMethod' of the instance of type 'IProxy'.
                    ilGen.EmitCall(OpCodes.Call, typeof(TInterceptor).GetMethod(nameof(IInterceptor.InterceptMethod)) ?? throw new NullReferenceException(), new[] { typeof(object), typeof(MethodInfo), typeof(object[]) });

                    // Return the computed value of the interception method.
                    ilGen.Emit(OpCodes.Ret);
                }

                type = typeBuilder.CreateType();
            }

            var ctor = type.GetConstructor(new[] { typeof(IInterceptor) });

            if (ctor == null)
            {
                throw new NullReferenceException("No constructor defined.");
            }

            types.Add(typeof(TInterface), type);

            var instance = ctor.Invoke(new object[] { interceptor });

            return (TInterface)instance;
        }
    }
}
