<img src="https://cdn.rawgit.com/drewnoakes/xmp-core-dotnet/master/docs/logo.svg" width="260" height="260" />

[![Build status](https://ci.appveyor.com/api/projects/status/38jnjb2phnn7fqxs?svg=true)](https://ci.appveyor.com/project/drewnoakes/xmp-core-dotnet) [![XmpCore NuGet version](https://img.shields.io/nuget/v/XmpCore.svg)](https://www.nuget.org/packages/XmpCore/) [![XmpCore NuGet pre-release version](https://img.shields.io/nuget/vpre/XmpCore.svg)](https://www.nuget.org/packages/XmpCore/)

This library is a port of Adobe's XMP SDK to .NET.

The API should be familiar to users of Adobe's XMPCore 5.1.2, though it has been modified
in places to better suit .NET development.

## Sample Usage

```csharp
IXmpMeta xmp;
using (var stream = File.OpenRead("some-file.xmp"))
    xmp = XmpMetaFactory.Parse(stream);

foreach (var property in xmp.Properties)
    Console.WriteLine($"Path={property.Path} Namespace={property.Namespace} Value={property.Value}");
```

`XmpMetaFactory` has other methods for reading from `string` and `byte[]`, as well as support for parsing options.
Returned properties provide additional information, but the above example should be enough to get you started.

## Installation

The easiest way to reference this project is to install the [`XmpCore` package](https://www.nuget.org/packages/XmpCore/):

    PM> Install-Package XmpCore

The NuGet package has no other dependencies.

If you require strongly named assemblies, use the [`XmpCore.StrongName` package](https://www.nuget.org/packages/XmpCore.StrongName/) instead.

## Framework support

The project targets `net35` and `netstandard1.0`, meaning you can use it pretty much anywhere these days.

## History

Initially ported by Yakov Danila and Nathanael Jones, the project is now maintained
by Drew Noakes and contributors on GitHub.

## License

The [same BSD license](http://www.adobe.com/devnet/xmp/library/eula-xmp-library-java.html) applies to this project
as to Adobe's open source XMP SDK, from which it is derived.
