using System.Drawing.Drawing2D;
using OopGames.Core.Arena;
using OopGames.Core.ElementalTower;

namespace OopGames.Desktop.Ui;

/// <summary>
/// Draws simple original sprites with GDI+ at runtime, so the app ships without any third-party art.
/// Each sprite is a coloured token with a symbol for its weapon or element.
/// </summary>
internal static class Sprites
{
    public const int Size = 72;

    public static Bitmap ForWeapon(Weapon weapon, Team team) =>
        Draw(team == Team.Player ? Color.SteelBlue : Color.Firebrick, g =>
        {
            using var pen = new Pen(Color.WhiteSmoke, 4) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            switch (weapon)
            {
                case Weapon.Sword:
                    g.DrawLine(pen, 22, 50, 50, 22);   // Blade.
                    g.DrawLine(pen, 22, 38, 34, 50);   // Cross-guard.
                    break;
                case Weapon.Axe:
                    g.DrawLine(pen, 24, 52, 46, 22);   // Handle.
                    g.FillPie(Brushes.WhiteSmoke, 34, 12, 26, 26, 200, 180); // Blade.
                    break;
                case Weapon.Spear:
                    g.DrawLine(pen, 18, 56, 48, 26);   // Shaft.
                    g.FillPolygon(Brushes.WhiteSmoke, new Point[] { new(44, 22), new(56, 14), new(50, 30) }); // Tip.
                    break;
            }
        });

    public static Bitmap ForElement(Element element) =>
        Draw(element switch
        {
            Element.Fire => Color.OrangeRed,
            Element.Water => Color.DodgerBlue,
            Element.Grass => Color.ForestGreen,
            _ => Color.SlateGray,
        }, g =>
        {
            using var font = new Font("Segoe UI", 22f, FontStyle.Bold);
            string symbol = element.ToString()[..1];
            SizeF size = g.MeasureString(symbol, font);
            g.DrawString(symbol, font, Brushes.White, (Size - size.Width) / 2, (Size - size.Height) / 2);
        });

    /// <summary>A tombstone for fallen units.</summary>
    public static Bitmap Grave()
    {
        var bitmap = new Bitmap(Size, Size);
        using Graphics g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var stone = new GraphicsPath();
        stone.AddArc(18, 10, 36, 30, 180, 180);
        stone.AddLine(54, 25, 54, 62);
        stone.AddLine(54, 62, 18, 62);
        stone.CloseFigure();
        g.FillPath(Brushes.Gray, stone);
        using var pen = new Pen(Color.Gainsboro, 3);
        g.DrawLine(pen, 36, 22, 36, 46);
        g.DrawLine(pen, 28, 30, 44, 30);
        return bitmap;
    }

    private static Bitmap Draw(Color background, Action<Graphics> drawSymbol)
    {
        var bitmap = new Bitmap(Size, Size);
        using Graphics g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(background);
        g.FillEllipse(brush, 4, 4, Size - 8, Size - 8);
        drawSymbol(g);
        return bitmap;
    }
}
