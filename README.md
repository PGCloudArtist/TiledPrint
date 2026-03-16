# TiledPrint — Standalone Tiled Printing Tool

Prints any image tiled across multiple pages so the assembled result matches
the image's true physical size. Works with Paint.NET, Photoshop, GIMP, or any
image editor.

---
## Release
Version 1.0
16 March 2026
Pascal Gaudé and Claude.ai

## Why standalone instead of a Paint.NET plugin?

Paint.NET 5.x completely redesigned its plugin API, and the legacy
`Effect`/`EffectConfigDialog` system is deprecated and difficult to target
without a paid Visual Studio license. This standalone app has **zero external
dependencies** — only the free .NET SDK is needed to build it.

---

## Build instructions

### Requirements
- Windows 10 or 11 (64-bit)
- [.NET 9 SDK](https://dotnet.microsoft.com/download) (free)

### Build
Create a TiledPrint folder and copy all the files

For PC with .Net, open a command prompt in this folder and run:
```
dotnet build -c Release
```
The executable is at:
```
bin\Release\net9.0-windows\TiledPrint.exe
```

You can also publish a self-contained single file (runs on PCs without .NET):
```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

To publish a self-contained single file with icon (runs on PCs without .NET), open a command prompt in this folder and run:
```
build.bat
```

---

## Usage

1. **Export** your image from Paint.NET (PNG recommended — preserves DPI metadata)
2. **Run** `TiledPrint.exe`
3. **Open** the image with the button or drag-and-drop it onto the window
4. **Check the DPI** — the app reads it from the file automatically.
   To verify or change the DPI in Paint.NET: `Image → Resize → Resolution`
5. Choose your **paper size** and **orientation**
6. Set the **overlap** (default 10 mm — helps align and tape pages)
7. Click **Print Preview** to check the layout
8. Click **Print…** to send to your printer

---

## Assembling the pages

Each printed page has a dashed line near the right and/or bottom edge
marking the overlap zone:

1. Print all pages
2. **Right column trim**: trim pages along the right dashed line (except the last column)
3. **Bottom row trim**: trim pages along the bottom dashed line (except the last row)
4. Align trimmed edges and tape or glue pages together

Each page also has a small label at the bottom:
> Page 3/6  [col 3/3, row 1/2]

This tells you exactly where each page belongs in the grid.

---

## Supported image formats

PNG, JPEG, BMP, TIFF, GIF

---

## License

MIT — free to use, modify, and redistribute.
