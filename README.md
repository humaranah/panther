# Panther

A modern, cross-platform music player written in .Net.

It uses Bass audio library since it is cross-platform and it can handle a large variety of audio formats and effects.

## Prerequisites

The following prerequisites are needed for the development:

- [Visual Studio 2022](https://visualstudio.microsoft.com/) for Windows and Mac development, [Visual Studio Code](https://code.visualstudio.com/) for Linux development.
- [.NET 7.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
- [Uno Platform](https://platform.uno/docs/articles/get-started.html?tabs=windows)
- [Un4seen Bass](https://www.un4seen.com/) and [BASS.Net](http://bass.radio42.com/) DLLs
- A valid license from **BASS.Net** project *(License is free for non-commercial applications)*

## Project list

The following projects are located inside the **Source** folder:

| Project | Description |
|-|-|
| Panther.Core | Core files |
| Panther.TestConsole | Console project for testing music playback |
| Panther.UI | Multiplatform UI using `Uno Platform` |

The following projects are for unit tests, and they are located inside the **Tests** folder:

| Project | Description |
|-|-|
| Panther.Core.UnitTests | Unit tests for `Panther.Core` |

## Third-party libraries used

| Library | Nuget URL | Project URL |
|-|-|-|
| Un4seen Bass | *Download from the project URL.* | <https://www.un4seen.com/> |
| BASS.Net | *Download from the project URL.* | <http://bass.radio42.com/> |
| Taglib-sharp | <https://www.nuget.org/packages/taglib-sharp-netstandard2.0> | <https://github.com/mono/taglib-sharp> |
| Uno Platform | <https://www.nuget.org/packages/Uno.WinUI> | <https://platform.uno/> |
| Serilog | <https://www.nuget.org/packages/serilog> | <https://serilog.net/> |

---
[Documentation in progress...]
