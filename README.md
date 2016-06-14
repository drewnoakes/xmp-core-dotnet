<img src="https://cdn.rawgit.com/drewnoakes/xmp-core-dotnet/master/docs/logo.svg" width="260" height="260" />

[![Build status](https://ci.appveyor.com/api/projects/status/38jnjb2phnn7fqxs?svg=true)](https://ci.appveyor.com/project/drewnoakes/xmp-core-dotnet) [![XmpCore NuGet version](https://img.shields.io/nuget/v/XmpCore.svg)](https://www.nuget.org/packages/XmpCore/)

This library is a port of Adobe's XMP SDK to .NET.

The API should be familiar to users of Adobe's XMPCore 5.1.2, though it has been modified
in places to better suit .NET development.

## Installation

The easiest way to reference this project is to install [its NuGet package](https://www.nuget.org/packages/XmpCore/):

    PM> Install-Package XmpCore

## Requirements

Requires .NET 4.0 Client Profile or higher.

The NuGet package comprises a single DLL with no other dependencies.

## History

Initially ported by Yakov Danila and Nathanael Jones, the project is now maintained
by Drew Noakes and contributors on GitHub.

## License

The [same BSD license](http://www.adobe.com/devnet/xmp/library/eula-xmp-library-java.html) applies to this project
as to Adobe's open source XMP SDK, from which it is derived.
