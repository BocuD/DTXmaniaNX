## What is this repository?

This is a fork of DTXManiaNX, with a main focus on improving QOL, as well as cleaning up and improving the codebase. This fork was initially started from frustration with the lack of QOL features or decent UX in essentially all DTXMania forks, and intends to create a gameplay and menu navigation system more in line with the arcade.

## Some of the changes made from upstream DTXManiaNX:

- Newly written UI rendering framework
  - Supports translating, rotating, and scaling elements according to a hierarchy
  - Fully serializable and skinnable
  - Resolution independent

- New theming engine
  - Written on top of new UI framework
  - Doesn't affect old UI elements (for now)
 
- Support for arbitrary resolutions
  - Currently restricted to 16:9
  - UI scales gracefully, independent of resolution
  - Gameplay is still fixed at 720p

- Rewritten song database
  - ~4-8x performance speedup
  - Safer handling of files
  - No more reliance on legacy (unsafe) BinaryFormatter
  - Song name transliteration (romanization)

- Greatly improved sorting functionality
  - Sorting functionality similar to GITADORA, with songs grouped in various categories
  - Group by BOX, Difficulty, Level, Title, Artist, etc

- Song selection UI recreated from scratch
  - Customizable options for various display parts
  - Grealy improved smoothness and UX

- Improved Guitar navigation
  - P + strum to confirm
  - Y + strum to go back

- Improved settings menu
  - Most menu options are now sorted into categories (for example, Audio, Video, Gameplay, etc)
  - Added options for drum velocity
  - Removed options that don't affect the current game
  - Improved naming and description for various elements

- Extensive Dear ImGui-based tooling
  - Inspector and hierarchy views for new UI framework, performance analysis, song database tests, etc
  - Toggle using `ctrl + i`
  - In game log viewer (`ctrl + l`)
 
## Current roadmap

- Feature parity on song select screen with upstream DTXManiaNX
- Song database serialization
- Song favourite filters
- (fuzzy) song search
- Play results screen rework

## Building

The current .net SDK target is 6.0 (mainly so we can still support Windows 7, which is used by real hardware GITADORA machines)
https://dotnet.microsoft.com/en-us/download/dotnet/6.0

Install the required SDK (Currently .net 6.0)

```
winget install Microsoft.DotNet.SDK.6
```

Clone the repository

```
git clone https://github.com/BocuD/DTXmaniaNX.git
cd DTXManiaNX/DTXMania
```

Build a release

```
dotnet build -p:Configuration=Release
```

Run the game

```
cd ../Runtime
./DTXManiaNX.exe
```

In the root of the repository, a `build_and_run.bat` script is included to automate these steps.

## What is DTXManiaNX?
DTXManiaNX is a program that replicates gameplay from Konami's music video game, Gitadora - Drummania/GuitarFreaks. It processes DTX files (including older formats such as BMS/BME or GDA/G2D) and allows playing of custom created charts with a use of a game, keyboard or MIDI controller.

For more information regarding creation of DTX files and its data formats, do visit the original [DTXMania Wiki](https://osdn.net/projects/dtxmania/wiki/DTX%20data%20format). Various video tutorials are available from [APPROVED DTX Gaming's YouTube page](https://youtu.be/9GlSk62pgGw) or [
Furukon Rhythm Gaming's YouTube page](https://www.youtube.com/playlist?list=PLj22ny7-DS2V-l0pWLhp8cLRYLF3jskCs).

## Original and Ongoing Forks
* [DTXMania4](https://dtxmania.net/) ([ＦＲＯＭ](https://github.com/DTXMania))

https://dtxmania.net/

* [DTXManiaNX](https://github.com/limyz/DTXmaniaNX) (limyz / fisyher)

https://github.com/limyz/DTXmaniaNX

* [DTXMania](https://osdn.net/projects/dtxmania) (yyagi)

https://osdn.net/projects/dtxmania

* [DTXMania AL](http://senamih.com/dtxal) (Sena)

http://senamih.com/dtxal

* [DTXManiaXG verK](https://osdn.net/projects/dtxmaniaxg-verk) ([kairera0467](https://github.com/kairera0467))

https://osdn.net/projects/dtxmaniaxg-verk

## Installation
1. Download the [latest release](https://github.com/BocuD/DTXmaniaNX/releases) of DTXMania and extract it to a location of your choice

2. Download and install the [.NET 6.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) (if prompted)

3. Download and install the [DirectX End-User Runtime (DirectX v9.0c)](https://www.microsoft.com/en-us/download/details.aspx?displaylang=en&id=35)

## Community Support
For additional help or support, ask away in DTXMania on Discord! 
[https://discord.gg/ST5MWHe](https://discord.gg/ST5MWHe)
