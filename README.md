## What is this repository?

This is a fork of DTXManiaNX, with the goal of modernizing and refactoring the codebase, to eventually have the ability to add various features.
The main focus right now is restructuring up the codebase to a point where various parts of the game, such as the menu systems, gameplay, and configuration systems, are all abstracted to their own semi self contained systems.
One part that has made a decent amount of progress is rewriting the UI system to be hierarchy based, and render elements using matrices instead of hardcoding everything to fixed positions on screen. As part of this, this UI rendering code also uses an abstract rendering backend, to in the future more easily allow porting to other graphics backends, such as OpenGL, rather than just DirectX9. Ideally this will allow better multi platform support in the future.

## Changes from base DTXManiaNX

- Newly written UI framework
  - Supports translating, rotating and scaling elements according to a hierarchy
  - Fully serializable and skinnable
- Rewritten song database
  - ~4-8x performance speedups
  - Safer handling of files
  - No more reliance on legacy (unsafe) BinaryFormatter
  - Song name transliteration (romanization)
- Greatly improved sorting functionality
  - Sorting functionality similar to GITADORA, with songs grouped in various categories
  - Group by BOX, Difficulty, Level, Title, Artist, etc
- Song selection UI recreated from scratch
  - Support for new skinning system
  - Customizable options for various display parts
  - Grealy improved smoothness and UX
- Improved Guitar navigation
  - P + strum to confirm
  - Y + strum to go back
- Improved settings menu
  - Most menu options are now sorted into categories (for example, Audio, Video, Gameplay, etc)

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
