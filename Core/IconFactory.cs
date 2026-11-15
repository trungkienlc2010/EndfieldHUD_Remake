using System.Drawing;

namespace EndfieldHud.Core;

internal static class IconFactory
{
    /// <summary>Icon khay hệ thống vẽ bằng code (ô vàng + chữ E) để khỏi cần file .ico.</summary>
    public static Icon Create()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(255, 229, 0));
            using var font = new Font("Segoe UI", 20, FontStyle.Bold, GraphicsUnit.Pixel);
            g.DrawString("E", font, Brushes.Black, 6, 4);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }
}
