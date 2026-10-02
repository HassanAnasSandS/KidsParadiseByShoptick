using Microsoft.Maui.Controls.Shapes;

namespace KidsParadiseByShoptick.AdminApp.Controls;

public class ColorPalettePicker : ContentView
{
    public static readonly BindableProperty SelectedHexProperty =
        BindableProperty.Create(
            nameof(SelectedHex),
            typeof(string),
            typeof(ColorPalettePicker),
            defaultValue: null,
            defaultBindingMode: BindingMode.TwoWay,
            propertyChanged: static (bindable, _, _) => ((ColorPalettePicker)bindable).RefreshSelection());

    public string? SelectedHex
    {
        get => (string?)GetValue(SelectedHexProperty);
        set => SetValue(SelectedHexProperty, value);
    }

    /// <summary>Standard basic colors — none pre-selected until the user picks one.</summary>
    static readonly string[] Palette =
    [
        "#FFFFFF", "#000000", "#808080", "#C0C0C0",
        "#FF0000", "#FF6600", "#FFCC00", "#33CC33",
        "#00CCCC", "#0066FF", "#6600CC", "#FF3399",
    ];

    readonly HorizontalStackLayout _swatches = new() { Spacing = 6 };
    readonly BoxView _preview = new()
    {
        WidthRequest = 28,
        HeightRequest = 28,
        CornerRadius = 6,
        Color = Color.FromArgb("#e2e8f0")
    };
    readonly Entry _entry = new()
    {
        Placeholder = "Custom hex (optional)",
        FontSize = 13,
        VerticalOptions = LayoutOptions.Center
    };
    bool _syncing;

    public ColorPalettePicker()
    {
        var defaultChip = new Border
        {
            Padding = new Thickness(10, 6),
            StrokeThickness = 1,
            Stroke = Color.FromArgb("#cbd5e1"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Color.FromArgb("#f1f5f9"),
            Content = new Label
            {
                Text = "Default",
                FontSize = 11,
                TextColor = Color.FromArgb("#475569"),
                VerticalTextAlignment = TextAlignment.Center
            }
        };
        var defaultTap = new TapGestureRecognizer();
        defaultTap.Tapped += (_, _) => SelectedHex = string.Empty;
        defaultChip.GestureRecognizers.Add(defaultTap);
        _swatches.Children.Add(defaultChip);

        foreach (var hex in Palette)
        {
            var border = new Border
            {
                WidthRequest = 30,
                HeightRequest = 30,
                StrokeThickness = 1,
                Stroke = Color.FromArgb("#cbd5e1"),
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                BackgroundColor = Color.FromArgb(hex),
                StyleId = hex
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => SelectedHex = hex;
            border.GestureRecognizers.Add(tap);
            _swatches.Children.Add(border);
        }

        _entry.TextChanged += OnEntryTextChanged;

        var previewFrame = new Border
        {
            WidthRequest = 32,
            HeightRequest = 32,
            Padding = 1,
            StrokeThickness = 1,
            Stroke = Color.FromArgb("#cbd5e1"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Colors.White,
            Content = _preview,
            VerticalOptions = LayoutOptions.Center
        };

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Auto),
                new(GridLength.Star)
            },
            ColumnSpacing = 8
        };
        row.Add(previewFrame, 0);
        row.Add(_entry, 1);

        Content = new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                new ScrollView
                {
                    Orientation = ScrollOrientation.Horizontal,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
                    Content = _swatches
                },
                row
            }
        };

        RefreshSelection();
    }

    void OnEntryTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_syncing)
            return;

        SelectedHex = e.NewTextValue;
    }

    void RefreshSelection()
    {
        var raw = SelectedHex?.Trim() ?? string.Empty;
        var hasColor = TryParseHex(raw, out var color);
        var isBlank = string.IsNullOrWhiteSpace(raw);

        _preview.Color = hasColor ? color : Color.FromArgb("#e2e8f0");

        _syncing = true;
        try
        {
            if (!string.Equals(_entry.Text, raw, StringComparison.Ordinal))
                _entry.Text = raw;
        }
        finally
        {
            _syncing = false;
        }

        var normalized = NormalizeHex(raw);
        foreach (var child in _swatches.Children)
        {
            if (child is not Border border)
                continue;

            var isPaletteSwatch = !string.IsNullOrEmpty(border.StyleId);
            var isSelected = isPaletteSwatch
                ? !isBlank && string.Equals(NormalizeHex(border.StyleId), normalized, StringComparison.OrdinalIgnoreCase)
                : isBlank;

            border.StrokeThickness = isSelected ? 2.5 : 1;
            border.Stroke = isSelected
                ? Color.FromArgb("#1a6fe8")
                : Color.FromArgb("#cbd5e1");
        }
    }

    static string? NormalizeHex(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var hex = value.Trim();
        if (!hex.StartsWith('#'))
            hex = "#" + hex;

        hex = hex.ToUpperInvariant();
        return hex.Length is 4 or 5 or 7 or 9 ? hex : null;
    }

    static bool TryParseHex(string value, out Color color)
    {
        color = Colors.Transparent;
        var hex = NormalizeHex(value);
        if (hex is null)
            return false;

        try
        {
            color = Color.FromArgb(ExpandShortHex(hex));
            return true;
        }
        catch
        {
            return false;
        }
    }

    static string ExpandShortHex(string hex) => hex.Length switch
    {
        4 => $"#{hex[1]}{hex[1]}{hex[2]}{hex[2]}{hex[3]}{hex[3]}",
        5 => $"#{hex[1]}{hex[1]}{hex[2]}{hex[2]}{hex[3]}{hex[3]}{hex[4]}{hex[4]}",
        _ => hex
    };
}
