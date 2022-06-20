﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Linq;
namespace PhotoToys.Features;

class Filter : Category
{
    public override string Name { get; } = nameof(Filter).ToReadableName();
    public override string Description { get; } = "Apply Filter to enhance or change the look of the photo!";
    public override Feature[] Features { get; } = new Feature[]
    {
        new Blur(),
        new MedianBlur(),
        new GaussianBlur(),
        new Grayscale(),
        new Invert(),
        new Sepia()
    };
}
class Grayscale : Feature
{
    public override string Name { get; } = nameof(Grayscale).ToReadableName();
    public override string Description { get; } = "Turns photo into grayscale";
    public override UIElement UIContent { get; }
    public Grayscale()
    {
        UIContent = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new PercentSliderParameter("Intensity", 1.00).Assign(out var IntensityParm)
            },
            OnExecute: (MatImage) =>
            {
                var original = ImageParam.Result;
                var intensity = IntensityParm.Result;
                Mat output;
                //switch (original.Channels())
                //{
                //    case 1:
                //        output = original.Clone();
                //        return;
                //    case 4:
                //        output = original.CvtColor(ColorConversionCodes.BGRA2GRAY);
                //        break;
                //    case 3:
                //        output = original.CvtColor(ColorConversionCodes.BGR2GRAY);
                //        break;
                //    default:
                //        return;
                //}
                output = original.ToGray(out var alpha);
                using var t = new ResourcesTracker();
                if (intensity != 1)
                {
                    if (original.Channels() == 3)
                        output = t.T(output).CvtColor(ColorConversionCodes.GRAY2BGR);
                    else if (original.Channels() == 4)
                        output = t.T(output).CvtColor(ColorConversionCodes.GRAY2BGRA);

                    Cv2.AddWeighted(output, intensity, original, 1 - intensity, 0, output);
                }
                output = output.ToBGR();
                if (alpha != null) output = t.T(output).InsertAlpha(alpha);


                if (UIContent != null) output.ImShow(MatImage);
            }
        );
    }
}
class Invert : Feature
{
    public override string Name { get; } = nameof(Invert).ToReadableName();
    public override string Description { get; } = "Invert RGB Color of the photo";
    public override UIElement UIContent { get; }
    public Invert()
    {
        UIContent = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new PercentSliderParameter("Intensity", 1.00).Assign(out var IntensityParm)
            },
            OnExecute: (MatImage) =>
            {
                var original = ImageParam.Result;
                var intensity = IntensityParm.Result;
                using var t = new ResourcesTracker();
                var output = new Mat();
                if (original.Type() == MatType.CV_8UC3)
                    Cv2.Merge(new Mat[] {
                        t.T(255 - t.T(original.ExtractChannel(0))),
                        t.T(255 - t.T(original.ExtractChannel(1))),
                        t.T(255 - t.T(original.ExtractChannel(2)))
                    }, output);
                else if (original.Type() == MatType.CV_8UC4)
                    Cv2.Merge(new Mat[] {
                        t.T(255 - t.T(original.ExtractChannel(0))),
                        t.T(255 - t.T(original.ExtractChannel(1))),
                        t.T(255 - t.T(original.ExtractChannel(2))),
                        t.T(original.ExtractChannel(3))
                    }, output);
                else if (original.Type() == MatType.CV_8UC1 || original.Type() == MatType.CV_8U)
                    output = 255 - output;
                else
                {
                    original.MinMaxIdx(out var min, out var max);
                    output = 255 - original;
                }
                Cv2.AddWeighted(output, intensity, original, 1 - intensity, 0, output);
                if (UIContent != null) output.ImShow(MatImage);
            }
        );
    }
}
class Sepia : Feature
{
    public override string Name { get; } = nameof(Sepia).ToReadableName();
    public override string Description { get; } = "Apply Sepia filter to the photo";
    public override UIElement UIContent { get; }
    public Sepia()
    {
        UIContent = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new PercentSliderParameter("Intensity", 1.00).Assign(out var IntensityParm)
            },
            OnExecute: (MatImage) =>
            {
                using var t = new ResourcesTracker();
                var original = ImageParam.Result;
                var intensity = IntensityParm.Result;
                Mat output;
                
                switch (original.Channels())
                {
                    case 1:
                        output = t.T(original.CvtColor(ColorConversionCodes.GRAY2BGR));
                        output = t.T(output.SepiaFilter());
                        output = output.AsBytes();
                        break;
                    case 4:
                        var originalA = original.ExtractChannel(2);
                        output = t.T(original.CvtColor(ColorConversionCodes.BGRA2BGR));
                        output = t.T(output.SepiaFilter());
                        output = t.T(output.AsBytes());
                        output = output.InsertAlpha(originalA);
                        break;
                    case 3:
                        output = t.T(original.SepiaFilter());
                        output = output.AsBytes();
                        break;
                    default:
                        return;
                }
                Cv2.AddWeighted(output, intensity, original, 1 - intensity, 0, output);
                if (UIContent != null) output.ImShow(MatImage);
            }
        );
    }
}
class Blur : Feature
{
    public override string Name { get; } = nameof(Blur).ToReadableName();
    public override string Description { get; } = "Apply Mean Blur filter to the photo";
    public override UIElement UIContent { get; }
    public Blur()
    {
        UIContent = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new IntSliderParameter("Kernal Size", Min: 1, Max: 101, StartingValue: 3).Assign(out var kernalSizeParam),
                new SelectParameter<BorderTypes>(Name: "Blur Border Mode", Enum.GetValues<BorderTypes>().Where(x => !(x == BorderTypes.Wrap || x == BorderTypes.Transparent)).Distinct().ToArray(), 3, x => x == BorderTypes.Default ? "Default (Reflect101)" : x.ToString()).Assign(out var BorderParam)
            },
            OnExecute: (MatImage) =>
            {
                using var t = new ResourcesTracker();
                var original = ImageParam.Result;
                var k = kernalSizeParam.Result;
                var kernalsize = new Size(k, k);
                Mat output = original.Blur(kernalsize, borderType: BorderParam.Result);
                if (UIContent != null) output.ImShow(MatImage);
            }
        );
    }
}
class MedianBlur : Feature
{
    public override string Name { get; } = nameof(MedianBlur).ToReadableName();
    public override string Description { get; } = "Apply Meadian Blur filter to the photo";
    public override UIElement UIContent { get; }
    public MedianBlur()
    {
        UIContent = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new IntSliderParameter("Kernal Size", Min: 1, Max: 101, Step: 2, StartingValue: 3).Assign(out var kernalSizeParam),
            },
            OnExecute: (MatImage) =>
            {
                using var t = new ResourcesTracker();
                var original = ImageParam.Result;
                var k = kernalSizeParam.Result;
                Mat output = original.MedianBlur(k);
                if (UIContent != null) output.ImShow(MatImage);
            }
        );
    }
}
class GaussianBlur : Feature
{
    public override string Name { get; } = nameof(GaussianBlur).ToReadableName();
    public override string Description { get; } = "Apply Gaussian Blur filter to the photo";
    public override UIElement UIContent { get; }
    public GaussianBlur()
    {
        UIContent = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new IntSliderParameter("Kernal Size", Min: 1, Max: 101, Step: 2, StartingValue: 3).Assign(out var kernalSizeParam),
                new DoubleSliderParameter("Standard Deviation X", Min: 0, Max: 30, Step: 0.01, StartingValue: 0, DisplayConverter: x => x == 0 ? "Default" : x.ToString("N2")).Assign(out var sigmaXParam),
                new DoubleSliderParameter("Standard Deviation Y", Min: 0, Max: 30, Step: 0.01, StartingValue: 0, DisplayConverter: x => x == 0 ? "Same as Standard Deviation X" : x.ToString("N2")).Assign(out var sigmaYParam),
                new SelectParameter<BorderTypes>(Name: "Blur Border Mode", Enum.GetValues<BorderTypes>().Where(x => x != BorderTypes.Transparent).Distinct().ToArray(), 4, x => x == BorderTypes.Default ? "Default (Reflect101)" : x.ToString()).Assign(out var BorderParam)
            },
            OnExecute: (MatImage) =>
            {
                using var t = new ResourcesTracker();
                var original = ImageParam.Result;
                var k = kernalSizeParam.Result;
                var sigmaX = sigmaXParam.Result;
                var sigmaY = sigmaYParam.Result;
                var kernalsize = new Size(k, k);
                Mat output = original.GaussianBlur(kernalsize, sigmaX, sigmaY, borderType: BorderParam.Result);
                if (UIContent != null) output.ImShow(MatImage);
            }
        );
    }
}
