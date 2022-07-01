using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using UCH_ImageToLevelConverter.Model;

namespace UCH_ImageToLevelConverter.ViewModels;

public static class BitmapSourceExtensions
{
    public static void WriteTransformedBitmapToFile<T>(this BitmapSource bitmapSource, string fileName) where T : BitmapEncoder, new()
    {
        var frame = BitmapFrame.Create(bitmapSource); ;
        var encoder = new T();
        encoder.Frames.Add(frame);
        using var fs = new FileStream(fileName, FileMode.Create);
        encoder.Save(fs);
    }

    public static BitmapSource Format(this BitmapSource bitmap, PixelFormat destinationFormat)
    {
        return new FormatConvertedBitmap(bitmap, destinationFormat, null, 0);
    }

    public static BitmapSource Resize(this BitmapSource bitmap, int width, int height)
    {
        return new TransformedBitmap(bitmap, new ScaleTransform(
            (double)width / bitmap.PixelWidth,
            (double)height / bitmap.PixelHeight
        ));
    }

    public static IEnumerable<PixelData> GetPixelData(this BitmapSource bitmap)
    {
        int sourceBytesPerPixel = bitmap.Format.BitsPerPixel / 8;
        var pixelCount = bitmap.PixelHeight * bitmap.PixelWidth;
        byte[] sourcePixels = new byte[pixelCount * sourceBytesPerPixel];

        bitmap.CopyPixels(sourcePixels, bitmap.PixelWidth * sourceBytesPerPixel, 0);

        for (int row = 0, idx = 0; row < bitmap.PixelHeight; row++)
        {
            for (int col = 0; col < bitmap.PixelWidth; col++, idx += sourceBytesPerPixel)
            {
                var color = Color.FromRgb(sourcePixels[idx + 0], sourcePixels[idx + 1], sourcePixels[idx + 2]);

                yield return new PixelData
                {
                    Row = row,
                    Col = col,
                    Color = color
                };
            }
        }
    }

    public static BitmapSource KNNReduceColors(this BitmapSource bitmap, int nColors)
    {
        var mlContext = new MLContext();

        var pixels = new PixelEntry[bitmap.PixelWidth * bitmap.PixelHeight];

        int i = 0;
        foreach (PixelData pixelData in bitmap.GetPixelData())
        {
            pixels[i++] = new PixelEntry
            {
                Features = new[]
                {
                    pixelData.Color.R / 255.0f,
                    pixelData.Color.G / 255.0f,
                    pixelData.Color.B / 255.0f
                }
            };
        }

        var img = new ImageEntry
        {
            Data = pixels,
            Width = bitmap.PixelWidth,
            Height = bitmap.PixelHeight
        };

        var fullData = mlContext.Data.LoadFromEnumerable(pixels);
        var trainingData = mlContext.Data.LoadFromEnumerable(SelectRandom(pixels, 2000));
        var model = Train(mlContext, trainingData, numberOfClusters: nColors);


        VBuffer<float>[] centroidsBuffer = default;
        model.Model.GetClusterCentroids(ref centroidsBuffer, out int k);

        var labels = mlContext.Data
            .CreateEnumerable<Prediction>(model.Transform(fullData), reuseRowObject: false)
            .ToArray();

        return ReconstructImage(labels, centroidsBuffer, img.Width, img.Height);
    }

    static BitmapSource ReconstructImage(Prediction[] labels, VBuffer<float>[] centroidsBuffer, int width, int height)
    {
        int sourceBytesPerPixel = 3;
        var pixelCount = height * width;
        byte[] sourcePixels = new byte[pixelCount * sourceBytesPerPixel];

        for (int i = 0, j = 0; j < sourcePixels.Length; i++, j += 3)
        {
            var label = labels[i].PredictedLabel;
            var centroid = centroidsBuffer[label - 1].DenseValues().ToArray();
            sourcePixels[j] = (byte)(centroid[0] * 255);
            sourcePixels[j + 1] = (byte)(centroid[1] * 255);
            sourcePixels[j + 2] = (byte)(centroid[2] * 255);
        }

        var resultImg = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, null);
        resultImg.WritePixels(new Int32Rect(0, 0, width, height), sourcePixels, width * 3, 0, 0);

        return resultImg;
    }

    private static ClusteringPredictionTransformer<KMeansModelParameters> Train(MLContext mlContext, IDataView data, int numberOfClusters)
    {
        var pipeline = mlContext.Clustering.Trainers.KMeans(new KMeansTrainer.Options()
        {
            NumberOfClusters = numberOfClusters,
            InitializationAlgorithm = KMeansTrainer.InitializationAlgorithm.KMeansPlusPlus
        });

        Console.WriteLine("Training model...");
        var sw = Stopwatch.StartNew();
        var model = pipeline.Fit(data);
        Console.WriteLine("Model trained in {0} ms.", sw.Elapsed.Milliseconds);

        return model;
    }

    private static T[] SelectRandom<T>(T[] array, int count)
    {
        var result = new T[count];
        var rnd = new Random();
        var chosen = new HashSet<int>();

        for (var i = 0; i < count; i++)
        {
            int r = rnd.Next(0, array.Length);

            result[i] = array[r];
        }

        return result;
    }


    public class PixelEntry
    {
        // Normalized RGB values, e.g. [0.02323, 0.23013, 0.359305]
        [VectorType(3)]
        public float[] Features { get; set; }
    }

    public class ImageEntry
    {
        public PixelEntry[] Data { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class Prediction
    {
        public uint PredictedLabel { get; set; }
    }
}