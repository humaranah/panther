# Panther

A modern, cross-platform music player written in .Net.

## Prerequisites

The following prerequisites are needed for the development:

- [Visual Studio 2022](https://visualstudio.microsoft.com/) for Windows and Mac development, [Visual Studio Code](https://code.visualstudio.com/) for Linux development.
- [.NET 7.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
- [Uno Platform](https://platform.uno/docs/articles/get-started.html?tabs=windows)

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
| NAudio | <https://www.nuget.org/packages/NAudio> | <https://github.com/naudio/NAudio/> |
| Taglib-sharp | <https://www.nuget.org/packages/taglib-sharp-netstandard2.0> | <https://github.com/mono/taglib-sharp> |
| Uno Platform | <https://www.nuget.org/packages/Uno.WinUI> | <https://platform.uno/> |

## Compile and run

Before compiling:

- Put any mp3 song you want inside the `./samples` folder and rename it to `song.mp3`.
  _(You can also use any other format and update the song name string inside `Program.cs`.)_
