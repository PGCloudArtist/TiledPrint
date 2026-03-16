// GenerateIcon — writes TiledPrint.ico into the parent project folder.
// Run automatically by build.bat before building the main app.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;

// Write 16×16, 32×32 and 48×48 sizes into one .ico file
int[] sizes = { 16, 32, 48 };
var bitmaps = new System.Drawing.Bitmap[sizes.Length];
for (int i = 0; i < sizes.Length; i++)
    bitmaps[i] = DrawIcon(sizes[i]);

string outPath = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "TiledPrint.ico"));

SaveIco(bitmaps, outPath);
Console.WriteLine($"Icon written → {outPath}");

// ── Drawing ───────────────────────────────────────────────────────────────────
static System.Drawing.Bitmap DrawIcon(int size)
{
    var bmp = new System.Drawing.Bitmap(size, size);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode     = SmoothingMode.AntiAlias;
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.Clear(Color.Transparent);

    float s = size / 32f;  // scale factor relative to 32px design

    // Back paper sheet
    using var paperBrush = new SolidBrush(Color.White);
    using var paperPen   = new Pen(Color.FromArgb(160, 160, 200), s);
    g.FillRectangle(paperBrush, 13*s, 2*s, 14*s, 17*s);
    g.DrawRectangle(paperPen,   13*s, 2*s, 14*s, 17*s);
    // Front paper sheet
    g.FillRectangle(paperBrush, 5*s, 8*s, 14*s, 17*s);
    g.DrawRectangle(paperPen,   5*s, 8*s, 14*s, 17*s);

    // Printer body
    using var bodyBrush = new SolidBrush(Color.FromArgb(60, 120, 210));
    using var bodyPen   = new Pen(Color.FromArgb(30, 80, 170), s);
    g.FillRectangle(bodyBrush, 4*s, 18*s, 24*s, 10*s);
    g.DrawRectangle(bodyPen,   4*s, 18*s, 24*s, 10*s);

    // Paper slot line
    using var slotPen = new Pen(Color.FromArgb(30, 80, 170), 1.5f*s);
    g.DrawLine(slotPen, 8*s, 18*s, 24*s, 18*s);

    // Output tray
    g.FillRectangle(paperBrush, 10*s, 25*s, 12*s, 5*s);
    g.DrawRectangle(paperPen,   10*s, 25*s, 12*s, 5*s);

    // Green indicator light
    using var dotBrush = new SolidBrush(Color.FromArgb(80, 220, 80));
    g.FillEllipse(dotBrush, 22*s, 21*s, 4*s, 4*s);

    return bmp;
}

// ── ICO file writer ───────────────────────────────────────────────────────────
// Writes a valid .ico with multiple sizes from an array of Bitmaps.
static void SaveIco(System.Drawing.Bitmap[] bitmaps, string path)
{
    int n = bitmaps.Length;
    // Each image stored as 32-bit ARGB PNG inside the ICO container
    var pngs = new byte[n][];
    for (int i = 0; i < n; i++)
    {
        using var ms = new MemoryStream();
        bitmaps[i].Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        pngs[i] = ms.ToArray();
    }

    using var fs = new FileStream(path, FileMode.Create);
    using var w  = new BinaryWriter(fs);

    // ICONDIR header
    w.Write((short)0);   // reserved
    w.Write((short)1);   // type = ICO
    w.Write((short)n);   // image count

    // Calculate offsets: header(6) + n*ICONDIRENTRY(16) + image data
    int offset = 6 + n * 16;
    for (int i = 0; i < n; i++)
    {
        int sz = bitmaps[i].Width;
        w.Write((byte)(sz >= 256 ? 0 : sz));  // width  (0 = 256)
        w.Write((byte)(sz >= 256 ? 0 : sz));  // height
        w.Write((byte)0);    // color count (0 = no palette)
        w.Write((byte)0);    // reserved
        w.Write((short)1);   // color planes
        w.Write((short)32);  // bits per pixel
        w.Write(pngs[i].Length);
        w.Write(offset);
        offset += pngs[i].Length;
    }

    // Write image data
    foreach (var png in pngs)
        w.Write(png);
}
