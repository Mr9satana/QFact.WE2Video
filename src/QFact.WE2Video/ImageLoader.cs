using System.Drawing;
using System.Drawing.Imaging;

namespace QFact.WE2Video;

internal static class ImageLoader
{
    public static Image? LoadUnlocked(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var image = Image.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: false);
            return new Bitmap(image);
        }
        catch
        {
            return null;
        }
    }
}
