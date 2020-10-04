﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace Reko.Chromely.BrowserHost.Functions
{
    public class Proto_GeneratePng
    {
        public static byte[] Generate()
        {
            using var bmp = new Bitmap(300, 30);
            using var font = new Font("Arial", 7.0F);
            using var g = Graphics.FromImage(bmp);
            using var bg = new SolidBrush(Color.FromArgb(unchecked((int)0xFF80E080u)));
            using var fg = new SolidBrush(Color.FromArgb(unchecked((int)0xFF101020u)));
            g.FillRectangle(bg, 0, 0, 300, 30);
            g.DrawString("Reko", font, fg, new PointF(3, 3));
            using var mem = new MemoryStream();
            bmp.Save(mem, ImageFormat.Png);
            mem.Flush();
            var bytes = mem.ToArray();
            return bytes;
        }

        public static void Execute(PromiseTask promise)
        {
            var bytes = Generate();
            promise.Resolve(bytes);
        }
    }
}