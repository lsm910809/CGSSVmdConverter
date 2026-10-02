using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CGSSVmdConverter.Core;
using CGSSVmdConverter.Models;

namespace CGSSVmdConverter;

public partial class MainWindow : Window
{
    private string? _inputPath;

    private UnityAnimation? _animation;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void BrowseButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        try
        {
            var files =
                await StorageProvider
                    .OpenFilePickerAsync(
                        new FilePickerOpenOptions
                        {
                            Title =
                                "Select CGSS Animation",

                            AllowMultiple =
                                false,

                            FileTypeFilter =
                            [
                                new FilePickerFileType(
                                    "Unity Animation")
                                {
                                    Patterns =
                                        ["*.anim"]
                                }
                            ]
                        });

            if (files.Count == 0)
                return;

            _inputPath =
                files[0]
                    .TryGetLocalPath();

            if (_inputPath is null)
            {
                StatusText.Text =
                    "Could not access selected file.";

                return;
            }

            InputPathBox.Text =
                _inputPath;

            StatusText.Text =
                "Reading animation...";

            _animation =
                AnimParser.Load(
                    _inputPath);

            StatusText.Text =
                $"{_animation.Name}\n" +
                $"{_animation.RotationCurves.Count} rotation curves\n" +
                $"{_animation.PositionCurves.Count} position curves\n" +
                $"{_animation.Length:F3} sec";
        }
        catch (Exception ex)
        {
            _animation =
                null;

            StatusText.Text =
                $"ERROR: {ex.Message}";

            Console.WriteLine(ex);
        }
    }

    private async void ConvertButton_OnClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (_animation is null ||
            _inputPath is null)
        {
            StatusText.Text =
                "Select a CGSS .anim file first.";

            return;
        }

        try
        {
            float fps =
                GetSelectedFps();

            string defaultName =
                Path.GetFileNameWithoutExtension(
                    _inputPath) +
                ".vmd";

            var output =
                await StorageProvider
                    .SaveFilePickerAsync(
                        new FilePickerSaveOptions
                        {
                            Title =
                                "Save VMD Motion",

                            SuggestedFileName =
                                defaultName,

                            DefaultExtension =
                                "vmd",

                            FileTypeChoices =
                            [
                                new FilePickerFileType(
                                    "MikuMikuDance Motion")
                                {
                                    Patterns =
                                        ["*.vmd"]
                                }
                            ]
                        });

            if (output is null)
                return;

            string? outputPath =
                output.TryGetLocalPath();

            if (outputPath is null)
            {
                StatusText.Text =
                    "Could not access output location.";

                return;
            }

            StatusText.Text =
                "Converting...";

            var frames =
                VmdConverter.Convert(
                    _animation,
                    fps);

            VmdWriter.Write(
                outputPath,
                "CGSS",
                frames);

            StatusText.Text =
                $"Done!\n" +
                $"{frames.Count:N0} bone frames\n" +
                $"{outputPath}";

            Console.WriteLine();
            Console.WriteLine(
                "========== COMPLETE ==========");

            Console.WriteLine(
                $"Input: {_inputPath}");

            Console.WriteLine(
                $"Output: {outputPath}");

            Console.WriteLine(
                $"Frames: {frames.Count:N0}");

            Console.WriteLine(
                "==============================");
        }
        catch (Exception ex)
        {
            StatusText.Text =
                $"CONVERSION ERROR: {ex.Message}";

            Console.WriteLine();
            Console.WriteLine(
                "========== ERROR ==========");

            Console.WriteLine(ex);

            Console.WriteLine(
                "===========================");
        }
    }

    private float GetSelectedFps()
    {
        if (FpsBox.SelectedItem
            is ComboBoxItem item &&
            float.TryParse(
                item.Content?.ToString(),
                out float fps))
        {
            return fps;
        }

        return 30.0f;
    }
}