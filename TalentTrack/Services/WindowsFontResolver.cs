using PdfSharp.Fonts;
using System.IO;
using System;

namespace TalentTrack.Services
{
    public class WindowsFontResolver : IFontResolver
    {
        public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (familyName.Equals("Arial", StringComparison.OrdinalIgnoreCase))
            {
                string faceName = "Arial";
                if (isBold && isItalic) faceName = "ArialBoldItalic";
                else if (isBold) faceName = "ArialBold";
                else if (isItalic) faceName = "ArialItalic";
                return new FontResolverInfo(faceName);
            }
            return new FontResolverInfo("Arial");
        }

        public byte[]? GetFont(string faceName)
        {
            string fontPath = faceName switch
            {
                "ArialBold" => @"C:\Windows\Fonts\arialbd.ttf",
                "ArialItalic" => @"C:\Windows\Fonts\ariali.ttf",
                "ArialBoldItalic" => @"C:\Windows\Fonts\arialbi.ttf",
                _ => @"C:\Windows\Fonts\arial.ttf"
            };

            if (!File.Exists(fontPath))
            {
                fontPath = @"C:\Windows\Fonts\arial.ttf";
            }
            return File.ReadAllBytes(fontPath);
        }
    }
}
