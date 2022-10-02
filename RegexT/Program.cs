// See https://aka.ms/new-console-template for more information\

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;

Console.WriteLine("Hello, World!");

const float cmToInch = .39370079f;
int dpi = 200;

var param = await File.ReadAllLinesAsync(@"I:\Serials\params.txt");
string filename = param[0];
string[] tilesS = param[1].Split(' ');
Size tiles = new(Convert.ToInt32(tilesS[0]), Convert.ToInt32(tilesS[1]));
string[] pageS = param[2].Split(' ');
SizeF page = new(Convert.ToSingle(pageS[0]), Convert.ToSingle(pageS[1]));
string[] overS = param[3].Split(' ');
float upS = Convert.ToSingle(overS[0]), rightS = Convert.ToSingle(overS[1]), downS = Convert.ToSingle(overS[2]), leftS = Convert.ToSingle(overS[3]);

Stopwatch stopwatch = Stopwatch.StartNew();
using (Image image = await Image.LoadAsync(filename))
{
    Size full = new(
        (int)(page.Width * cmToInch * dpi * tiles.Width), 
        (int)(page.Height * cmToInch * dpi * tiles.Height));
    if (image.Width < full.Width || image.Height < full.Height)
    {
        image.Mutate(x => x.Resize(new ResizeOptions()
        {
            Mode = ResizeMode.Crop,
            Size = full,
            Sampler = KnownResamplers.Robidoux,
            Position = AnchorPositionMode.Center,
        }));
    }
    else
    {
        float ratioW = (float)image.Width / full.Width;
        float ratioH = (float)image.Height / full.Height;
        if (ratioW < ratioH)
        {
            full.Width = image.Width;
            full.Height = (int)(full.Height * ratioW);
            dpi = (int)(dpi * ratioW);
        }
        else
        {
            full.Width = (int)(full.Width * ratioH);
            full.Height = image.Height;
            dpi = (int)(dpi * ratioH);
        }
        image.Mutate(x => x.Crop(new Rectangle()
        {
            X = (image.Width - full.Width) / 2,
            Y = (image.Height - full.Height) / 2,
            Width = full.Width,
            Height = full.Height
        }));
    }
    Size imgSize = new(
        (int)((page.Width - rightS - leftS) * cmToInch * dpi), 
        (int)((page.Height - upS - downS) * cmToInch * dpi));
    for (int i = 0; i < tiles.Width; i++)
    {
        for (int j = 0; j < tiles.Height; j++)
        {
            Image tile = image.Clone(x => x.Crop(new Rectangle() 
            {
                X = i * imgSize.Width + (i > 0 ? (int)(leftS * cmToInch * dpi) : 0),
                Y = j * imgSize.Height + (j > 0 ? (int)(upS * cmToInch * dpi) : 0),
                Width = imgSize.Width + (int)((leftS + rightS) * cmToInch * dpi),
                Height = imgSize.Height + (int)((upS + downS) * cmToInch * dpi)
            }));

            await tile.SaveAsync($@"I:\Serials\Tiled-{j}-{i}.jpg", 
                new JpegEncoder() { Quality = 90, ColorType = JpegColorType.YCbCrRatio422 });
        }
    }
}
stopwatch.Stop();
Console.WriteLine("Resulting DPI: " + dpi.ToString());
Console.WriteLine("Elapsed time: " + stopwatch.ElapsedMilliseconds.ToString());
