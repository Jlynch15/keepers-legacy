@echo off
REM ============================================================
REM  convert_to_png.bat
REM  Converts all SVG sprite sheets to 512x512 PNG
REM  (512x512 = 128x128 per frame at 4x4 grid)
REM
REM  Requirements: Inkscape installed
REM  Download free: https://inkscape.org
REM
REM  Usage: Double-click this file, or run from Command Prompt
REM  Output: PNG files appear alongside the SVG files
REM ============================================================

SET INKSCAPE="C:\Program Files\Inkscape\bin\inkscape.exe"

IF NOT EXIST %INKSCAPE% (
    echo ERROR: Inkscape not found at %INKSCAPE%
    echo Please install Inkscape from https://inkscape.org
    pause
    exit /b 1
)

echo Converting sprite sheets to PNG...
echo.

SET COUNT=0

FOR %%f IN (*.svg) DO (
    echo Converting %%f ...
    %INKSCAPE% --export-type=png --export-width=512 --export-height=512 "%%f"
    SET /A COUNT+=1
)

echo.
echo Done! Converted %COUNT% file(s).
echo.
echo Next step: Copy the PNG files into Xcode:
echo   Assets.xcassets/Creatures/{name}_sheet.imageset/
echo.
pause
