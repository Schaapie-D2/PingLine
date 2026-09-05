using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PingLine.Notification;

internal static class AsciiArtGenerator
{
    private static readonly Dictionary<string, AsciiArtImage> Cache = new();

    public static async Task<AsciiArtImage> GenerateFromUrl(string url, int outputHeight = 30)
    {
        if(Cache.TryGetValue(url, out var art)) return art;

        using HttpClient client = new HttpClient();
        byte[] data = await client.GetByteArrayAsync(url);

        using var image = Image.Load<Rgba32>(data);
        art = getArt(image, outputHeight);
        Cache[url] = art;
        return art;
    }

    private static AsciiArtImage getArt(Image<Rgba32> image, int height)
    {
        var frames = new List<AsciiArtImageFrame>();
        var frameTimings = new List<float>();

        int width = (int)(image.Width / (double)image.Height * height * 2.0);
        image.Mutate(x => x.Resize(width, height * 2));

        foreach (var frame in image.Frames)
        {
            var rows = new List<string>();

            for (int y = 0; y < frame.Height; y += 2)
            {
                var sb = new StringBuilder();

                for (int x = 0; x < frame.Width; x++)
                {
                    Rgba32 top = frame[x, y];
                    Rgba32 bottom = (y + 1 < frame.Height) ? frame[x, y + 1] : new Rgba32(0, 0, 0);

                    sb.Append($"\x1b[38;2;{top.R};{top.G};{top.B}m"); // Set foreground color
                    sb.Append($"\x1b[48;2;{bottom.R};{bottom.G};{bottom.B}m"); // Set background color
                    sb.Append("▀");
                }

                sb.Append("\x1b[0m");

                rows.Add(sb.ToString());
            }

            var asciiFrame = new AsciiArtImageFrame()
            {
                FrameRows = rows.ToArray()
            };

            frames.Add(asciiFrame);
            frameTimings.Add(frame.Metadata.GetGifMetadata().FrameDelay / 100f);
        }

        return new AsciiArtImage()
        {
            Frames = frames.ToArray(),
            FrameTimings = frameTimings.ToArray()
        };
    }
}

public struct AsciiArtImage
{
    public AsciiArtImageFrame[] Frames;
    public float[] FrameTimings;
}

public struct AsciiArtImageFrame
{
    public string[] FrameRows;
}