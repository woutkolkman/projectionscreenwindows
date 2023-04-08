### Install
Install via the Steam Workshop: <>  
Also install Five Pebbles Pong: https://github.com/woutkolkman/fivepebblespong

To manually install, download the most recent .zip from the [releases page](https://github.com/woutkolkman/projectionscreenwindows/releases) and extract it to "Rain World\RainWorld_Data\StreamingAssets\mods\projectionscreenwindows".

Next, enable these mods via the in-game Remix menu.


### Credits
Thanks to [forthbridge and his original Five Pebbles video player](https://github.com/forthbridge/five-pebbles-bad-apple)! This mod is just another possible implementation of the same idea.  
Thanks to the [Rain World Modding Wiki](https://rainworldmodding.miraheze.org/), without this site these mods wouldn't exist.


### Description
A Rain World mod that can project any program on a in-game ProjectionScreen.

Tested on v1.9.07b

Please report any bug, problem or feature request via the [Issues](https://github.com/woutkolkman/projectionscreenwindows/issues) tab, or on the Steam Workshop page, or message me on the [Rain World Discord](https://discord.gg/rainworld): Maxi Mol#3079


### Decisions
- Bitmaps are not supported directly in the plugin (System.PlatformNotSupportedException). So taking screenshots of other windows using the Win32 API is not possible within the plugin itself.
- Using OBS Studio to capture video, and streaming it at UDP over the local network should be possible. Manually writing an UDP client and transforming every frame would become quite complex.
- There's a [OBS Client plugin](https://github.com/tinodo/obsclient) by tinodo which can receive frames via obs-websocket protocol. This client is written in .NET 6.0, which [won't be compatible](https://stackoverflow.com/questions/74344769/how-to-reference-net-6-0-dll-in-net-framework-4-8) with this .NET 4.8 plugin.
- A separate independent console application in .NET 6.0 was created, which will catch the OBS stream and convert it to PNG, which the plugin can read.
- This console application would send Base64 PNG strings to the plugin. The plugin then needs to convert this string into a Texture2D. This works fairly well, but it is definitely not the most optimised solution.
- No in-game sound, this is probably played by the recorded program anyway.
- Note that with this method, you won't get above 10-15 fps on the projection.
- To increase performance, the Win32 API is used which would probably be faster at taking screenshots. This is added to a separate plugin, because I don't know if this plugin will be accepted on the Workshop because of security reasons.


### Tips
- Any issues? Check BepInEx logs located in "Rain World\BepInEx\LogOutput.log", or enable a console window in "Rain World\BepInEx\config\BepInEx.cfg"
- Scale down other windows (& fullscreen off) for better performance, and for better visibility in-game
