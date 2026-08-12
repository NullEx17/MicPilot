using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Svg;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var gamesDir = Path.Combine(root, "src", "MicPilot.App", "Assets", "games");
var aboutDir = Path.Combine(root, "src", "MicPilot.App", "Assets", "about");

var colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["fivem"] = "32DC5F",
    ["valorant"] = "FF4655",
    ["counterstrike"] = "F0A030",
    ["rockstargames"] = "FCAF17",
    ["fortnite"] = "FFFFFF",
    ["leagueoflegends"] = "C28F2C",
    ["roblox"] = "E2231A",
    ["minecraft"] = "62B47A",
    ["rust"] = "CD412B",
    ["dota2"] = "D32C2C",
    ["pubg"] = "F2A900",
    ["discord"] = "5865F2",
    ["steam"] = "66C0F4",
    ["epicgames"] = "FFFFFF",
    ["battledotnet"] = "00AEFF",
    ["ea"] = "FF4747",
    ["ubisoft"] = "FFFFFF",
    ["riotgames"] = "D32936",
    ["twitch"] = "9146FF",
    ["github"] = "FFFFFF"
};

foreach (var svgPath in Directory.GetFiles(gamesDir, "*.svg"))
{
    var slug = Path.GetFileNameWithoutExtension(svgPath);
    if (!colors.TryGetValue(slug, out var hex))
    {
        hex = "F5F5F5";
    }

    var pngPath = Path.Combine(gamesDir, slug + ".png");
    Draw(svgPath, pngPath, hex, 64);
    Console.WriteLine("games/" + slug + ".png");
}

var fivemSvg = Path.Combine(gamesDir, "fivem.svg");
Draw(fivemSvg, Path.Combine(gamesDir, "redm.png"), "E03C31", 64);
Console.WriteLine("games/redm.png");

Directory.CreateDirectory(aboutDir);
Draw(Path.Combine(aboutDir, "discord.svg"), Path.Combine(aboutDir, "discord.png"), "5865F2", 64);
Draw(Path.Combine(aboutDir, "github.svg"), Path.Combine(aboutDir, "github.png"), "F5F5F5", 64);
Console.WriteLine("about icons");

static void Draw(string svgPath, string pngPath, string hex, int size)
{
    var document = SvgDocument.Open(svgPath);
    var color = ParseHex(hex);
    Paint(document, color);
    document.Width = size;
    document.Height = size;

    using var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using (var graphics = Graphics.FromImage(bitmap))
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        document.Draw(graphics);
    }

    bitmap.Save(pngPath, ImageFormat.Png);
}

static void Paint(SvgElement element, Color color)
{
    element.Fill = new SvgColourServer(color);
    element.Stroke = SvgPaintServer.None;
    foreach (var child in element.Children)
    {
        Paint(child, color);
    }
}

static Color ParseHex(string hex)
{
    hex = hex.TrimStart('#');
    var value = Convert.ToInt32(hex, 16);
    return Color.FromArgb(255, (value >> 16) & 0xFF, (value >> 8) & 0xFF, value & 0xFF);
}
