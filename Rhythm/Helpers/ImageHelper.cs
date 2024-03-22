using System.Drawing;
using System.Drawing.Imaging;

namespace Rhythm.Helpers;

public static class ImageHelper
{
    public static byte[] CompressImage(byte[] rawImageBytes, long qualityLevel, ImageFormat imageFormat)
    {
        try
        {
            using MemoryStream inStream = new MemoryStream(rawImageBytes);
            using MemoryStream outStream = new MemoryStream();
            using Bitmap bmp = new Bitmap(inStream);
            ImageCodecInfo? encoder = GetEncoder(imageFormat);
            if (encoder is null) return rawImageBytes;
            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, qualityLevel);
            bmp.Save(outStream, encoder, encoderParams);
            return outStream.ToArray();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e.Message);
            return rawImageBytes;
        }
    }

    public static ImageFormat GetImageFormat(string format)
    {
        return format.ToLower() switch
        {
            ".png" => ImageFormat.Png,
            _ => ImageFormat.Jpeg
        };
    }

    private static ImageCodecInfo? GetEncoder(ImageFormat format)
    {
        return Array.Find(ImageCodecInfo.GetImageEncoders(), x => x.FormatID == format.Guid);
    }
}
