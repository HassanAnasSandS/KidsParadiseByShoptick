namespace KidsParadiseByShoptick.AdminApp.Views;

public partial class ImageViewerPage : ContentPage, IQueryAttributable
{
    private double _currentScale = 1;
    private double _startScale = 1;
    private double _xOffset;
    private double _yOffset;

    public ImageViewerPage()
    {
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("url", out var raw) || raw is null)
            return;

        var url = Uri.UnescapeDataString(raw.ToString() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(url))
            return;

        ViewerImage.Source = url;
        ResetTransform();
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e) => ResetTransform();

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Started)
        {
            _startScale = ViewerImage.Scale;
            ViewerImage.AnchorX = 0;
            ViewerImage.AnchorY = 0;
        }

        if (e.Status == GestureStatus.Running)
        {
            _currentScale = Math.Clamp(_startScale * e.Scale, 1, 4);
            ViewerImage.Scale = _currentScale;
        }

        if (e.Status == GestureStatus.Completed)
            _currentScale = ViewerImage.Scale;
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (ViewerImage.Scale <= 1)
            return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                ViewerImage.TranslationX = _xOffset + e.TotalX;
                ViewerImage.TranslationY = _yOffset + e.TotalY;
                break;
            case GestureStatus.Completed:
                _xOffset = ViewerImage.TranslationX;
                _yOffset = ViewerImage.TranslationY;
                break;
        }
    }

    private void ResetTransform()
    {
        _currentScale = 1;
        _startScale = 1;
        _xOffset = 0;
        _yOffset = 0;
        ViewerImage.Scale = 1;
        ViewerImage.TranslationX = 0;
        ViewerImage.TranslationY = 0;
        ViewerImage.AnchorX = 0.5;
        ViewerImage.AnchorY = 0.5;
    }
}
