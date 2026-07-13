@echo off
setlocal

:: --- CONFIGURATION ---
set "TargetFile=%~dp0ScreenSaver.cmd"
set "ShortcutName=Aerials ScreenSaver.lnk"
set "HotKey=Ctrl+Alt+S"
:: ---------------------

set "VBSFile=%TEMP%\CreateShortcut.vbs"
set "DesktopPath=%USERPROFILE%\Desktop"

echo Set oWS = WScript.CreateObject("WScript.Shell") > "%VBSFile%"
echo sLinkFile = "%DesktopPath%\%ShortcutName%" >> "%VBSFile%"
echo Set oLink = oWS.CreateShortcut(sLinkFile) >> "%VBSFile%"
echo oLink.TargetPath = "%TargetFile%" >> "%VBSFile%"
echo oLink.Hotkey = "%HotKey%" >> "%VBSFile%"
:: 7 = Minimized, 1 = Normal, 3 = Maximized
echo oLink.WindowStyle = 7 >> "%VBSFile%"
echo oLink.Save >> "%VBSFile%"

cscript //nologo "%VBSFile%"
del "%VBSFile%"

echo Shortcut created on Desktop with Hotkey %HotKey%!
pause