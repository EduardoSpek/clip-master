using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClipMaster.Presentation.Views;

public partial class WebcamOverlayWindow : Window
{
    private bool _isDragging;
    private Point _dragOffset;
    private WriteableBitmap? _bitmap;

    public WebcamOverlayWindow()
    {
        InitializeComponent();
        Left = 50;
        Top = 50;
    }

    public void UpdateFrame(byte[] bgr24Data, int width, int height)
    {
        if (bgr24Data.Length < width * height * 3) return;

        Dispatcher.Invoke(() =>
        {
            if (_bitmap == null || _bitmap.PixelWidth != width || _bitmap.PixelHeight != height)
            {
                _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
                WebcamImage.Source = _bitmap;
            }

            _bitmap.Lock();

            var srcStride = width * 3;
            var dstStride = _bitmap.BackBufferStride;
            var srcOffset = 0;
            var dstOffset = _bitmap.BackBuffer;

            for (int y = 0; y < height; y++)
            {
                unsafe
                {
                    fixed (byte* srcPtr = &bgr24Data[srcOffset])
                    {
                        Buffer.MemoryCopy(srcPtr, (void*)(dstOffset + y * dstStride), dstStride, srcStride);
                    }
                }
                srcOffset += srcStride;
            }

            _bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            _bitmap.Unlock();
        });
    }

    public void ShowOverlay()
    {
        Show();
    }

    public void HideOverlay()
    {
        Hide();
    }

    private void WebcamBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _dragOffset = e.GetPosition(this);
        WebcamBorder.CaptureMouse();
    }

    private void WebcamBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        var currentPos = e.GetPosition(this);
        var newLeft = Left + currentPos.X - _dragOffset.X;
        var newTop = Top + currentPos.Y - _dragOffset.Y;

        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;

        newLeft = Math.Max(0, Math.Min(newLeft, screenWidth - WebcamBorder.ActualWidth));
        newTop = Math.Max(0, Math.Min(newTop, screenHeight - WebcamBorder.ActualHeight));

        Left = newLeft;
        Top = newTop;
    }

    private void WebcamBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        WebcamBorder.ReleaseMouseCapture();
    }

    private void WebcamBorder_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var delta = e.Delta > 0 ? 10 : -10;
        var newSize = Math.Max(80, Math.Min(400, WebcamBorder.Width + delta));

        WebcamBorder.Width = newSize;
        WebcamBorder.Height = newSize;

        var radius = newSize / 2;
        WebcamBorder.CornerRadius = new CornerRadius(radius);
    }

    public void SetBorderColor(string hexColor)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            WebcamBorder.BorderBrush = new SolidColorBrush(color);
        }
        catch { }
    }

    public void SetSize(int size)
    {
        WebcamBorder.Width = size;
        WebcamBorder.Height = size;
        WebcamBorder.CornerRadius = new CornerRadius(size / 2);
    }
}
