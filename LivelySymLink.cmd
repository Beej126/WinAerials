@echo off
setlocal enabledelayedexpansion

:: Get the current script directory and replace backslashes with forward slashes
set "BASE_DIR=%~dp0"
set "BASE_DIR=%BASE_DIR:\=/%"

:: Define the output JSON file path
set "OUTPUT_FILE=%~dp0LivelyInfo.json"

:: Write the JSON content to the file
(
echo {
echo   "AppVersion": "1.0.0.0",
echo   "Title": "WinAerials",
echo   "Thumbnail": "LivelyThumb.jpg",
echo   "Preview": null,
echo   "Desc": "Run Apple TV Aerials videos as a desktop wallpaper.",
echo   "Author": "Beej",
echo   "License": "MIT",
echo   "Contact": "https://github.com/Beej126/WinAerials",
echo   "Type": 3,
echo   "FileName": "file:///!BASE_DIR!WinAerialsPage.html",
echo   "Arguments": null,
echo   "IsAbsolutePath": false,
echo   "Id": null,
echo   "Tags": null,
echo   "Version": 0
echo }
) > "%OUTPUT_FILE%"

echo JSON file generated successfully at: "%OUTPUT_FILE%"
pause


mklink /D "%LocalAppData%\Lively Wallpaper\Library\wallpapers\WinAerials" "%~p0"
pause