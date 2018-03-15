# Dynamxy

Library for .NET enabling the use of dynamic proxy objects.

## Getting Started

Either fork, clone or download this repository. Or install the library via NuGet:

```
Install-Package SWZR.Dynamxy
```

## How does it work

During instantiation of a proxy object, code is dynamically emitted using primarly IL to create a concrete implementation based on a given interface.
This type functions as intermediary and forwards any method call to the predefined interceptor.

## Usage

In order to use dynamic proxy objects first define an interface:

```c#
public interface ITestInterface
{
	object Echo(object obj);
}
```

Afterwards define an interceptor which handles all method calls:

```c#
using System.Reflection;
using SWZR.Dynamxy;

public class EchoInterceptor : IInterceptor
{
    public object InterceptMethod(object instance, MethodInfo info, object[] parameter)
    {
        return parameter.Length > 0 ? parameter[0] : null;
    }
}
```

The last step is to construct an instance of the interceptor and eventually the dynamic proxy itself:

```c#
var factory = new ProxyFactory<EchoInterceptor>();
var proxy = factory.Create<ITestInterface>();

// Will yield 'Hello World'.
var value = proxy.Echo("Hello World");
```

## Use Cases

 - Authorization
 - Logging
 - REST
 - RPC
 - ...

## Roadmap

 - Improve and extend unit tests
 - Allow intercepting methods of concrete types
 - Add an example

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details