# Panther
A modern, cross-platform music player written in .Net.

## Project list
| | |
|-|-|
| Panther.Core | Core files |
| Panther.TestConsole | Console project for testing music playback |
| Panther.WPF | Windows Desktop Project |

## Libraries used
| | |
|-|-|
| Un4seen.bass | https://www.un4seen.com/ |
| ManagedBass | https://github.com/ManagedBass/ManagedBass |
| Taglib-sharp | https://github.com/mono/taglib-sharp |

## Compile and run

### Panther.TestConsole

#### Visual Studio
1.	Download bass.dll and put inside `./lib/`.
2.	Put any mp3 song you want inside `./examples` and rename it to `song.mp3`.
	*	You can also use any other format and update the song name string inside `Program.cs`.
3.	Click on Run
