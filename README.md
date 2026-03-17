# TiledPrint — Tiled Printing Tool

**Version:** v1.0.1  
**Date:** 17 March 2026  
**Authors:** Pascal Gaudé & Claude.ai (Anthropic)  
**License:** MIT  

---

## What it does

TiledPrint prints any image tiled across multiple pages so the assembled result
matches the image's true physical size. It was built to fill a gap in Paint.NET,
which has no built-in tiled / poster print feature.

Export your image from Paint.NET as a PNG (PNG preserves the DPI setting), open
it in TiledPrint, and print. The assembled pages reproduce the canvas at exactly
the size you designed it.

---

## Features

- **Reads DPI automatically** from the image file
- **Configurable** paper size, orientation, margins, and page overlap
- **Alignment tick marks** in the margin strip to help you tape pages together accurately
- **Page-by-page print preview** via the standard Windows print preview dialog
- **Full tiled preview** — see the complete poster grid with all pages at once, with zoom
- **Print range** — print all pages, a single page, or a custom range
- **No installation required** — single `.exe`, no .NET runtime needed on the target PC

---

## Supported image formats

PNG, JPEG, BMP, TIFF, GIF

---

## Usage

1. Export your image from Paint.NET (`File → Save As → PNG`)
2. Run `TiledPrint.exe`
3. Open the image with the **Open Image** button or drag-and-drop it onto the window
4. Check the **DPI** — read automatically from the file.  
   To verify or change in Paint.NET: `Image → Resize → Resolution`
5. Choose **paper size**, **orientation**, **margins**, and **overlap**
6. Select a **print range** if needed (default: all pages)
7. Click **Tiled Preview** to see the full poster layout
8. Click **Page Preview** to check individual pages
9. Click **Print…** to send to your printer

---

## Assembling the pages

Each page has small tick marks in the margin strip at the trim edges:

1. Print all pages (or your selected range)
2. Trim pages along the right tick mark (all columns except the last)
3. Trim pages along the bottom tick mark (all rows except the last)
4. Align trimmed edges and tape or glue pages together

Each page also has a small label at the bottom:
> Page 3/6  [col 3/3, row 1/2] — TiledPrint

---

## Building from source

### Requirements
- Windows 10 or 11 (64-bit)
- [.NET 9 SDK](https://dotnet.microsoft.com/download) (free)

### Quick build (development)
```
dotnet build -c Release
```

### Full build + publish (single-file exe for sharing)
```
build.bat
```

This generates:
- `publish\TiledPrint.exe` — self-contained, runs on any Windows PC without .NET
- `TiledPrint-release.zip` — ready to attach to a GitHub Release or forum post

---

## About

TiledPrint was created by **Pascal Gaudé** in collaboration with
**[Claude.ai](https://claude.ai)** (Anthropic's AI assistant), which co-authored
the majority of the C# code through an iterative conversation-driven development
process.

The project was motivated by the frequency of requests on the Paint.NET forum
from users wanting to print large canvases at true physical size — a feature
not natively available in Paint.NET 5.x, and no longer covered by the deprecated
PrintIt! plugin.

---

## License

MIT License — see [LICENSE](LICENSE) for details.

Copyright (c) 2026 Pascal Gaudé
