# GML-Mod-Loader
**GML Mod Loader** is a program that makes use of [UndertaleModLib](https://github.com/krzys-h/UndertaleModTool) in order to build a game data file before running the game, which would then allows us to use **multiple mods at once**. This mod loader could work with any game made in Game Maker Studio, as long as it's version is supported by the UndertaleModLib.

The way we do this is something like the Sonic Mania or Sonic 3 AIR mod loaders do: put all assets in a folder, run the mod loader and they get injected into the file. This removes the need of .xdeltas which are only a compressed binary diff file, which restricts the max mod amount to just one.

As of now we are working in a Proof of Concept command line version for testing the capabilities and testing. Once we reach a point where it's usable enough, we'll release a public beta of the command line version to get feedback and issues so that we can keep on developing, meanwhile we develop the user interface.

### Other sources

- [GameBanana's WIP Page](https://gamebanana.com/wips/76181)
- [BlurOne!'s YouTube channel](https://www.youtube.com/@_blurone_) (For video updates of the project)
- [Development Discord Server](https://discord.gg/y8jBh3jhw2)
