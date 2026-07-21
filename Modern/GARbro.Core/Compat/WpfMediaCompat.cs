// Compatibility surface for the legacy format decoders. It intentionally contains no UI code.
using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;

namespace System.Windows
{
    public struct Int32Rect
    {
        public static readonly Int32Rect Empty = new Int32Rect();
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Int32Rect (int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}

namespace System.Windows.Media
{
    public struct Color : IEquatable<Color>
    {
        public byte A { get; }
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public static Color FromRgb (byte r, byte g, byte b) => FromArgb (255, r, g, b);
        public static Color FromArgb (byte a, byte r, byte g, byte b) => new Color (a, r, g, b);

        private Color (byte a, byte r, byte g, byte b)
        {
            A = a;
            R = r;
            G = g;
            B = b;
        }

        public bool Equals (Color other) => A == other.A && R == other.R && G == other.G && B == other.B;
        public override bool Equals (object value) => value is Color color && Equals (color);
        public override int GetHashCode () => HashCode.Combine (A, R, G, B);
    }

    public struct PixelFormat : IEquatable<PixelFormat>
    {
        internal PixelFormat (string name, int bitsPerPixel)
        {
            Name = name;
            BitsPerPixel = bitsPerPixel;
        }

        internal string Name { get; }
        public int BitsPerPixel { get; }
        public bool Equals (PixelFormat other) => Name == other.Name && BitsPerPixel == other.BitsPerPixel;
        public override bool Equals (object value) => value is PixelFormat format && Equals (format);
        public override int GetHashCode () => HashCode.Combine (Name, BitsPerPixel);
        public static bool operator == (PixelFormat left, PixelFormat right) => left.Equals (right);
        public static bool operator != (PixelFormat left, PixelFormat right) => !left.Equals (right);
    }

    public static class PixelFormats
    {
        public static readonly PixelFormat BlackWhite = new PixelFormat ("BlackWhite", 1);
        public static readonly PixelFormat Indexed1 = new PixelFormat ("Indexed1", 1);
        public static readonly PixelFormat Indexed2 = new PixelFormat ("Indexed2", 2);
        public static readonly PixelFormat Indexed4 = new PixelFormat ("Indexed4", 4);
        public static readonly PixelFormat Indexed8 = new PixelFormat ("Indexed8", 8);
        public static readonly PixelFormat Gray4 = new PixelFormat ("Gray4", 4);
        public static readonly PixelFormat Gray8 = new PixelFormat ("Gray8", 8);
        public static readonly PixelFormat Gray16 = new PixelFormat ("Gray16", 16);
        public static readonly PixelFormat Bgr555 = new PixelFormat ("Bgr555", 16);
        public static readonly PixelFormat Bgr565 = new PixelFormat ("Bgr565", 16);
        public static readonly PixelFormat Bgr24 = new PixelFormat ("Bgr24", 24);
        public static readonly PixelFormat Rgb24 = new PixelFormat ("Rgb24", 24);
        public static readonly PixelFormat Bgr32 = new PixelFormat ("Bgr32", 32);
        public static readonly PixelFormat Bgra32 = new PixelFormat ("Bgra32", 32);
        public static readonly PixelFormat Pbgra32 = new PixelFormat ("Pbgra32", 32);
    }

    public sealed class BitmapPalette
    {
        public BitmapPalette (IList<Color> colors)
        {
            Colors = new List<Color> (colors ?? Array.Empty<Color>()).AsReadOnly();
        }

        public IReadOnlyList<Color> Colors { get; }
    }

    public sealed class ScaleTransform
    {
        public double ScaleX { get; set; } = 1;
        public double ScaleY { get; set; } = 1;
    }
}

namespace System.Windows.Media.Imaging
{
    using System.Windows;
    using System.Windows.Media;

    public enum BitmapCreateOptions { None, PreservePixelFormat }
    public enum BitmapCacheOption { Default, OnLoad }

    public class BitmapSource
    {
        protected byte[] m_pixels;

        protected BitmapSource () : this (0, 0, PixelFormats.Bgra32, null, Array.Empty<byte>(), 0) { }

        protected BitmapSource (int width, int height, PixelFormat format, BitmapPalette palette, byte[] pixels, int stride)
        {
            PixelWidth = width;
            PixelHeight = height;
            Format = format;
            Palette = palette;
            m_pixels = pixels ?? Array.Empty<byte>();
            Stride = stride;
        }

        public int PixelWidth { get; protected set; }
        public int PixelHeight { get; protected set; }
        public PixelFormat Format { get; protected set; }
        public BitmapPalette Palette { get; protected set; }
        internal int Stride { get; set; }

        public static BitmapSource Create (int width, int height, double dpiX, double dpiY, PixelFormat format,
                                           BitmapPalette palette, Array pixels, int stride)
        {
            if (!(pixels is byte[] source))
                throw new NotSupportedException ("Legacy bitmap compatibility requires a byte pixel buffer.");
            var copy = new byte[source.Length];
            Buffer.BlockCopy (source, 0, copy, 0, copy.Length);
            return new BitmapSource (width, height, format, palette, copy, stride);
        }

        public virtual void Freeze () { }

        public void CopyPixels (Array pixels, int stride, int offset)
        {
            CopyPixels (Int32Rect.Empty, pixels, stride, offset);
        }

        public void CopyPixels (Int32Rect area, Array pixels, int stride, int offset)
        {
            if (!(pixels is byte[] destination))
                throw new NotSupportedException ("Legacy bitmap compatibility requires a byte pixel buffer.");
            var sourceArea = NormalizeArea (area);
            var bytesPerPixel = Math.Max (1, (Format.BitsPerPixel + 7) / 8);
            var rowBytes = Math.Min (sourceArea.Width * bytesPerPixel, stride);
            for (var row = 0; row < sourceArea.Height; ++row)
            {
                var sourceOffset = (sourceArea.Y + row) * Stride + sourceArea.X * bytesPerPixel;
                var destinationOffset = offset + row * stride;
                Buffer.BlockCopy (m_pixels, sourceOffset, destination, destinationOffset, rowBytes);
            }
        }

        internal byte[] GetPixels () => m_pixels;

        private Int32Rect NormalizeArea (Int32Rect area)
        {
            return area.Width == 0 && area.Height == 0
                ? new Int32Rect (0, 0, PixelWidth, PixelHeight)
                : area;
        }
    }

    public sealed class BitmapFrame : BitmapSource
    {
        private BitmapFrame (BitmapSource source) : base (source.PixelWidth, source.PixelHeight, source.Format,
                                                           source.Palette, source.GetPixels(), source.Stride) { }
        public static BitmapFrame Create (BitmapSource source) => new BitmapFrame (source);
        public static BitmapFrame Create (BitmapSource source, object thumbnail, object metadata, object colorContexts) => new BitmapFrame (source);
    }

    public abstract class BitmapDecoder
    {
        public IList<BitmapFrame> Frames { get; } = new List<BitmapFrame>();
    }

    public sealed class PngBitmapDecoder : BitmapDecoder
    {
        public PngBitmapDecoder (Stream input, BitmapCreateOptions options, BitmapCacheOption cache) => Frames.Add (BitmapFrame.Create (ImageCodec.Decode (input)));
    }

    public sealed class JpegBitmapDecoder : BitmapDecoder
    {
        public JpegBitmapDecoder (Stream input, BitmapCreateOptions options, BitmapCacheOption cache) => Frames.Add (BitmapFrame.Create (ImageCodec.Decode (input)));
    }

    public sealed class BmpBitmapDecoder : BitmapDecoder
    {
        public BmpBitmapDecoder (Stream input, BitmapCreateOptions options, BitmapCacheOption cache) => Frames.Add (BitmapFrame.Create (ImageCodec.Decode (input)));
    }

    public sealed class TiffBitmapDecoder : BitmapDecoder
    {
        public TiffBitmapDecoder (Stream input, BitmapCreateOptions options, BitmapCacheOption cache) => Frames.Add (BitmapFrame.Create (ImageCodec.Decode (input)));
    }

    public abstract class BitmapEncoder
    {
        public IList<BitmapFrame> Frames { get; } = new List<BitmapFrame>();
        public abstract void Save (Stream output);
        protected SKBitmap ToImage () => ImageCodec.ToBitmap (Frames[0]);
    }

    public enum TiffCompressOption { None, Zip }

    public sealed class PngBitmapEncoder : BitmapEncoder
    {
        public override void Save (Stream output) { using (var image = ToImage()) ImageCodec.Encode (image, output, SKEncodedImageFormat.Png, 100); }
    }

    public sealed class JpegBitmapEncoder : BitmapEncoder
    {
        public int QualityLevel { get; set; } = 90;
        public override void Save (Stream output) { using (var image = ToImage()) ImageCodec.Encode (image, output, SKEncodedImageFormat.Jpeg, QualityLevel); }
    }

    public sealed class BmpBitmapEncoder : BitmapEncoder
    {
        public override void Save (Stream output) => ImageCodec.EncodeBmp (Frames[0], output);
    }

    public sealed class TiffBitmapEncoder : BitmapEncoder
    {
        public TiffCompressOption Compression { get; set; }
        public override void Save (Stream output) => throw new NotSupportedException ("TIFF writing will be provided by the dedicated TIFF adapter.");
    }

    public sealed class FormatConvertedBitmap : BitmapSource
    {
        public BitmapSource Source { get; set; }
        public PixelFormat DestinationFormat { get; set; }

        public FormatConvertedBitmap () { }

        public FormatConvertedBitmap (BitmapSource source, PixelFormat destinationFormat, BitmapPalette destinationPalette, double alphaThreshold)
        {
            Source = source;
            DestinationFormat = destinationFormat;
            EndInit();
        }

        public void BeginInit () { }

        public void EndInit ()
        {
            if (Source == null)
                throw new InvalidOperationException ("A source bitmap is required.");
            PixelWidth = Source.PixelWidth;
            PixelHeight = Source.PixelHeight;
            Format = DestinationFormat;
            Palette = null;
            Stride = PixelWidth * Math.Max (1, (DestinationFormat.BitsPerPixel + 7) / 8);
            m_pixels = ImageCodec.Convert (Source, DestinationFormat, Stride);
        }
    }

    public sealed class TransformedBitmap : BitmapSource
    {
        public TransformedBitmap (BitmapSource source, ScaleTransform transform)
            : base (source.PixelWidth, source.PixelHeight, source.Format, source.Palette,
                    ImageCodec.Transform (source, transform), source.Stride) { }
    }

    internal static class ImageCodec
    {
        public static BitmapSource Decode (Stream input)
        {
            using (var image = SKBitmap.Decode (input))
            {
                if (image == null)
                    throw new InvalidDataException ("Unsupported image data.");
                var pixels = new byte[image.Width * image.Height * 4];
                for (var y = 0; y < image.Height; ++y)
                {
                    for (var x = 0; x < image.Width; ++x)
                    {
                        var dst = (y * image.Width + x) * 4;
                        var color = image.GetPixel (x, y);
                        pixels[dst] = color.Blue;
                        pixels[dst + 1] = color.Green;
                        pixels[dst + 2] = color.Red;
                        pixels[dst + 3] = color.Alpha;
                    }
                }
                return BitmapSource.Create (image.Width, image.Height, 96, 96, PixelFormats.Bgra32, null, pixels, image.Width * 4);
            }
        }

        public static byte[] Transform (BitmapSource source, ScaleTransform transform)
        {
            var pixels = source.GetPixels();
            if (transform.ScaleY >= 0)
                return (byte[])pixels.Clone();
            var result = new byte[pixels.Length];
            for (var row = 0; row < source.PixelHeight; ++row)
                Buffer.BlockCopy (pixels, row * source.Stride, result, (source.PixelHeight - row - 1) * source.Stride, source.Stride);
            return result;
        }

        public static byte[] Convert (BitmapSource source, PixelFormat destinationFormat, int destinationStride)
        {
            if (source.Format == destinationFormat)
                return (byte[])source.GetPixels().Clone();

            var destination = new byte[destinationStride * source.PixelHeight];
            var sourceBytes = source.GetPixels();
            var sourceBpp = Math.Max (1, (source.Format.BitsPerPixel + 7) / 8);
            var destinationBpp = Math.Max (1, (destinationFormat.BitsPerPixel + 7) / 8);
            for (var y = 0; y < source.PixelHeight; ++y)
            for (var x = 0; x < source.PixelWidth; ++x)
            {
                var src = y * source.Stride + x * sourceBpp;
                var dst = y * destinationStride + x * destinationBpp;
                var b = sourceBytes[src];
                var g = sourceBpp > 1 ? sourceBytes[src + 1] : b;
                var r = sourceBpp > 2 ? sourceBytes[src + 2] : b;
                var a = sourceBpp > 3 ? sourceBytes[src + 3] : (byte)255;
                if (destinationFormat == PixelFormats.Gray8)
                    destination[dst] = (byte)((r * 77 + g * 150 + b * 29) >> 8);
                else
                {
                    destination[dst] = b;
                    if (destinationBpp > 1) destination[dst + 1] = g;
                    if (destinationBpp > 2) destination[dst + 2] = r;
                    if (destinationBpp > 3) destination[dst + 3] = a;
                }
            }
            return destination;
        }

        public static SKBitmap ToBitmap (BitmapSource source)
        {
            var image = new SKBitmap (source.PixelWidth, source.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            var pixels = source.GetPixels();
            for (var y = 0; y < source.PixelHeight; ++y)
            {
                for (var x = 0; x < source.PixelWidth; ++x)
                {
                    var src = y * source.Stride + x * 4;
                    image.SetPixel (x, y, new SKColor (pixels[src + 2], pixels[src + 1], pixels[src], pixels[src + 3]));
                }
            }
            return image;
        }

        public static void Encode (SKBitmap image, Stream output, SKEncodedImageFormat format, int quality)
        {
            using (var encoded = image.Encode (format, quality))
                encoded.SaveTo (output);
        }

        public static void EncodeBmp (BitmapSource source, Stream output)
        {
            var stride = source.PixelWidth * 4;
            var imageSize = checked (stride * source.PixelHeight);
            using (var writer = new BinaryWriter (output, System.Text.Encoding.UTF8, true))
            {
                writer.Write ((byte)'B');
                writer.Write ((byte)'M');
                writer.Write (54 + imageSize);
                writer.Write (0);
                writer.Write (54);
                writer.Write (40);
                writer.Write (source.PixelWidth);
                writer.Write (source.PixelHeight);
                writer.Write ((short)1);
                writer.Write ((short)32);
                writer.Write (0);
                writer.Write (imageSize);
                writer.Write (0);
                writer.Write (0);
                writer.Write (0);
                writer.Write (0);

                var row = new byte[stride];
                for (var y = source.PixelHeight - 1; y >= 0; --y)
                {
                    source.CopyPixels (new Int32Rect (0, y, source.PixelWidth, 1), row, stride, 0);
                    for (var x = 0; x < source.PixelWidth; ++x)
                        row[x * 4 + 3] = 0;
                    writer.Write (row);
                }
            }
        }
    }
}
