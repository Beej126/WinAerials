# WinAerials
Run Apple's latest Aerial videos as a simple screensaver via Edge

![alt text](readme.png){:width="300px"}

# Install

1. Install [.Net 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) 
1. Clone the repo to a preferred local folder
1. Edit obvious paths in config.yaml
1. Launch `RunMeOnce (as Admin).cmd` first time to download and cache all the latest video URLs and shuffle them into playlist.js
   - **CRUCIAL** I also included registering [Apple's Root Certificates](https://www.apple.com/certificateauthority/) here... this greases pulling some of the necessary urls from Apple's web servers without SSL cert problems

### Run
1. `ScreenSaver.cmd` is the script that runs the videos in MS Edge fullscreen mode with some screen text and action buttons provided by an infinitely customizable [html](WinAerialsPage.html) file.
1. `ShortcutMaker.cmd` will create a desktop shortcut and map CTRL+ALT+S hotkey if you like.

### Lively Wallpaper support (**optional**)
1. Install [Lively Wallpapers](https://www.rocksdanister.com/lively/) to run the videos as a live desktop background
   - <mark>In my humble opinion it's too distracting to get any real work done</mark> =)
   - I am pretty sure the setup based exe Lively install will work better than MS Store version due to permissioning.
1. Run LivelySymLink.cmd to add the repo folder to %LocalAppData%\Lively Wallpaper\Library\wallpapers
   - once Lively is restarted, new "WinAerials" wallpaper entry should show in the Library (definition comes from LivelyInfo.json)

# References
- [Apple TV Screen Saver Compilation](https://www.youtube.com/watch?v=Wb5r3dr70xI)
- [Lively Wallpaper app](https://apps.microsoft.com/detail/9ntm2qc6qws7)

# Notes
- There's of course other cracks at this out there ([OrangeJedi/Aerial](https://github.com/OrangeJedi/Aerial) etc) with more implemenation complexity than i was hoping to see so this overall implementation is meant to be approachable for tweaking:
  - RunMeOnce (as Admin).cmd runs WinAerials.cs which populates playlist.js
  - WinAerialsPage.html walks playlist.js and plays the videos
  - that's really it, all the code is scripted, no binaries, so it's wide open for mods and fixes - hack away!
- if Apple updates it's videos:
  1. update the video sources path in [config.yaml](config.yaml)
  2. delete playlist.js and
  3. RunMeOnce (as Admin).cmd again to download the latest
- Run **pre-configured** Lively wallpaper from command line:
  - `& "C:\Program Files\Lively Wallpaper\Lively.exe" setwp --file "$($env:LocalAppData)\Lively Wallpaper\Library\wallpapers\WinAerials"`
- Stop running lively wallpaper from command line:
  - LivelyStop.cmd
