using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Collections.Generic;
using DynamicLanguage;
using static PTMS.OpenCvExtension;
namespace PhotoToys.Features.Analysis;
[DisplayName(
    Default: "Analysis",
    Sinhala = "විශ්ලේෂණය"
)]
[DisplayDescription(
    Default: "Analyze image by applying one of these feature extractor to see details of the image!",
    Sinhala = "රූපයේ විස්තර බැලීමට මෙම විශේෂාංගවලින් එකක් යෙදීමෙන් රූපය විශ්ලේෂණය කරන්න!"
)]
[DisplayIcon((Symbol)0xE9f5)] // Processing
class Analysis : Category
{
    public override Feature[] Features { get; } = new Feature[]
    {
        new HistogramEqualization(),
        new EdgeDetection(),
        new HeatmapGeneration(),
        new Morphology()
    };
}

[DisplayName("Histogram Equalization", Sinhala = "Histogram සමීකරණය")]
[DisplayDescription(
    Default: "Apply Histogram Equalization to see some details in the image. Keeps photo opacity the same",
    Sinhala = "රූපයේ සමහර විස්තර බැලීමට Histogram සමීකරණය යොදන්න. පින්තුර පාරාන්ධතාව එලෙසම තබා ගනී"
)]
class HistogramEqualization : Feature
{
    public override IEnumerable<string> Allias => new string[] { "Detail", "Extract Feature", "Feature Extraction" };
    public HistogramEqualization()
    {

    }
    protected override UIElement CreateUI()
    {
        UIElement? Element = null;
        return Element = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ImageParameter(OneChannelModeEnabled: true).Assign(out var ImageParam),
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var mat = ImageParam.Result.Track(tracker);
                
                // Reference: https://stackoverflow.com/a/38312281
                var output = new Mat().Track(tracker);
                var arr = mat.InplaceCvtColor(ColorConversionCodes.BGR2YUV).Split().Track(tracker);
                Cv2.EqualizeHist(arr[0], arr[0]);
                Cv2.Merge(arr, output);
                output.InplaceCvtColor(ColorConversionCodes.YUV2BGR);
                output = ImageParam.PostProcess(output);

                output.ImShow(MatImage);
            }
        );
    }
}

[DisplayName("Edge Detection", Sinhala = "දාර හඳුනාගැනීම")]
[DisplayDescription(
    Default: "Apply Simple Edge Detection by finding standard deviation of the photo. Keeps photo opacity the same",
    Sinhala = "ඡායාරූපයේ සම්මත අපගමනය සොයා ගැනීමෙන් සරල දාර හඳුනාගැනීම යොදන්න. පින්තුර පාරාන්ධතාව එලෙසම තබා ගනී"
)]
class EdgeDetection : Feature
{
    public override IEnumerable<string> Allias => new string[] { "Detect Edge", "Detecting Edge" };
    enum OutputModes
    {
        Matrix,
        NormalizedGrayscale,
        NormalizedColor
    }
    public EdgeDetection()
    {

    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            MatDisplayer: new DoubleMatDisplayer(),
            Parameters: new ParameterFromUI[] {
                new ImageParameter(OneChannelModeEnabled: true).Assign(out var ImageParam),
                new IntSliderParameter(Name: new DisplayTextAttribute("Kernel Size"){Sinhala = "කර්නල් (Kernel) ප්‍රමාණය"}.ToDisplayText(), 1, 11, 3, 1).Assign(out var KernalSizeParam),
                //new CheckboxParameter(Name: "Output as Heatmap", Default: false).Assign(out var HeatmapParam)
                //    .AddDependency(ImageParam.OneChannelReplacement, x => !x, onNoResult: true),
                //new SelectParameter<ColormapTypes>(Name: "Heatmap Colormap", Enum.GetValues<ColormapTypes>(), 2).Assign(out var ColormapTypeParam)
                //    .AddDependency(HeatmapParam, x => x, onNoResult: true)
                new SelectParameter<OutputModes>(Name: new DisplayTextAttribute("Output as Matrix"){Sinhala= "ප්‍රතිදානය Matrix ලෙස"}.ToDisplayText(), Enum.GetValues<OutputModes>()).Assign(out var OutputModeParam)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                Mat original = ImageParam.Result.Track(tracker);
                //bool HeatmapMode = HeatmapParam.Result && !ImageParam.OneChannelReplacement.Result;
                //ColormapTypes colormap = ColormapTypeParam.Result;
                OutputModes OutputMode = OutputModeParam.Result;
                Size kernalSize = new(KernalSizeParam.Result, KernalSizeParam.Result);
                Mat output;

                output = original.StdFilter(kernalSize).Track(tracker);
                output = OutputMode switch
                {
                    OutputModes.Matrix => output,
                    OutputModes.NormalizedGrayscale =>
                    ImageParam.PostProcess((
                            (
                                output.ExtractChannel(0).Track(tracker) +
                                output.ExtractChannel(1).Track(tracker)
                            ).Track(tracker) +
                            output.ExtractChannel(2).Track(tracker)
                        ).Track(tracker).ToMat().Track(tracker).NormalBytes().Track(tracker)
                    ).Track(tracker),
                    OutputModes.NormalizedColor =>
                    ImageParam.PostProcess(
                        output.Split().Track(tracker)
                        .InplaceSelect(x => x.NormalBytes().Track(tracker))
                        .Merge()
                    ).Track(tracker),
                    _ => throw new ArgumentOutOfRangeException()
                };

                output.Clone().ImShow(MatImage);
            }
        );
    }
}

[DisplayName("Heatmap Generation", Sinhala = "තාප සිතියම් (Heatmap) උත්පාදනය")]
[DisplayDescription(
    Default: "Construct Heatmap from Grayscale Images",
    Sinhala = "Grayscale පින්තූර වලින් තාප සිතියම සාදන්න"
)]
class HeatmapGeneration : Feature
{
    public HeatmapGeneration()
    {

    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[] {
                new ImageParameter(Name: new DisplayTextAttribute("Grayscale Image"){Sinhala = "Grayscale රූපය"}.ToDisplayText(), ColorMode: false).Assign(out var ImageParam),
                new SelectParameter<ColormapTypes>(Name: "Mode", Enum.GetValues<ColormapTypes>(), 2).Assign(out var ColormapTypeParam)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var original = ImageParam.Result.Track(tracker);
                var colormap = ColormapTypeParam.Result;
                Mat output;

                output = original.Heatmap(colormap).Track(tracker);
                output = ImageParam.PostProcess(output);

                output.ImShow(MatImage);
            }
        );
    }
}

[DisplayName("Morphology", Sinhala = "රූප විද්‍යාව")]
[DisplayDescription(
    Default: "Apply morphological operations to remove noise, see more details, or extract feature",
    Sinhala = "ශබ්දය (noise) ඉවත් කිරීමට, වැඩි විස්තර බලන්න, හෝ විශේෂාංගය උපුටා ගැනීමට රූප විද්‍යාත්මක මෙහෙයුම් යොදන්න"
)]
class Morphology : Feature
{
    enum ChannelName : int
    {
        Default = 0,
        ColorWithoutAlpha = 1,
        ConvertToGrayscale = 2,
        Red = 5,
        Green = 4,
        Blue = 3,
        Alpha = 6
    }
    public override IEnumerable<string> Allias => new string[] { $"{nameof(Morphology)}Ex", "Remove noise", "Detail", "Extract Feature", "Feature Extraction", "Detect Edge", "Detecting Edge" };
    public Morphology()
    {

    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new IntSliderParameter(Name: new DisplayTextAttribute("Kernel Size"){Sinhala = "කර්නල් (Kernel) ප්‍රමාණය"}.ToDisplayText(), 1, 100, 3).Assign(out var KernalSizeParam),
                new SelectParameter<MorphShapes>(Name: new DisplayTextAttribute("Kernel Shape"){Sinhala = "කර්නල් (Kernel) හැඩය"}.ToDisplayText(), Enum.GetValues<MorphShapes>()).Assign(out var KernalShapeParam),
                new SelectParameter<MorphTypes>(Name: "Morphology Type", Enum.GetValues<MorphTypes>(), ConverterToDisplay: x => (x.ToString(), x switch
                {
                    MorphTypes.Erode => new DisplayTextAttribute
                    ("Remove noise from the image and make most of the element smaller")
                    {
                        Sinhala= "රූපයෙන් ශබ්දය (noise) ඉවත් කර බොහෝ මූලද්‍රව්‍ය (elements) කුඩා කරන්න"
                    }.ToDisplayText(),
                    MorphTypes.Dilate => new DisplayTextAttribute
                    ("Enlarge the small details and make most of the element larger")
                    {
                        Sinhala = "කුඩා විස්තර විශාල කර බොහෝ මූලද්‍රව්‍ය (elements) විශාල කරන්න"
                    }.ToDisplayText(),
                    MorphTypes.Open => new DisplayTextAttribute
                    ("Remove noise from the image while trying to maintain the same size")
                    {
                        Sinhala = "එකම ප්‍රමාණය පවත්වා ගැනීමට උත්සාහ කරන අතරතුර රූපයෙන් ශබ්දය (noise) ඉවත් කරන්න"
                    }.ToDisplayText(),
                    MorphTypes.Close => new DisplayTextAttribute
                    ("Fill in the hole while trying to maintain the same size")
                    {
                        Sinhala="එකම ප්‍රමාණය පවත්වා ගැනීමට උත්සාහ කරන අතරතුර සිදුර පුරවන්න"
                    }.ToDisplayText(),
                    _ => null
                })).Assign(out var MorphTypeParam)
                .Edit(x => x.ParameterValueChanged += delegate
                {
                    ImageParam.ColorMode = x.Result != MorphTypes.HitMiss;
                })
            },
            OnExecute: (MatImage) =>
            {

                using var tracker = new ResourcesTracker();
                Mat mat = ImageParam.Result.Track(tracker);
                int ks = KernalSizeParam.Result;
                MorphTypes mt = MorphTypeParam.Result;
                
                Cv2.MorphologyEx(mat, mat, mt,
                    Cv2.GetStructuringElement(KernalShapeParam.Result, new Size(ks, ks)).Track(tracker)
                );

                mat = ImageParam.PostProcess(mat);
                mat.ImShow(MatImage);
            }
        );
    }
}