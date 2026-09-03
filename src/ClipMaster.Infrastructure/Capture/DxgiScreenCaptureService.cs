using System.Diagnostics;
using System.Runtime.InteropServices;
using ClipMaster.Domain.Entities;
using ClipMaster.Domain.Interfaces;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace ClipMaster.Infrastructure.Capture;

public class DxgiScreenCaptureService : IScreenCaptureService
{
    private const int TargetFpsLimit = 60;

    private SharpDX.Direct3D11.Device? _device;
    private OutputDuplication? _duplication;
    private Texture2D? _stagingTexture;
    private Adapter? _adapter;
    private Output? _output;
    private int _width;
    private int _height;
    private int _fps;
    private bool _isCapturing;

    public int Width => _width;
    public int Height => _height;
    public int Fps => _fps;

    public Task StartAsync(int width, int height, int fps, CancellationToken ct)
    {
        _width = width;
        _height = height;
        _fps = Math.Min(fps, TargetFpsLimit);

        try
        {
            InitializeDuplication();
            _isCapturing = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"DXGI Desktop Duplication não disponível. Detalhes: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }

    private void InitializeDuplication()
    {
        ReleaseResources();

        _device = new SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);

        using var dxgiDevice = _device.QueryInterface<SharpDX.DXGI.Device>();
        _adapter = dxgiDevice.Adapter;

        var outputIndex = 0;
        try
        {
            _output = _adapter.GetOutput(outputIndex);
        }
        catch
        {
            _output = _adapter.GetOutput(0);
        }

        using var output1 = _output.QueryInterface<Output1>();
        _duplication = output1.DuplicateOutput(_device);

        var texDesc = new Texture2DDescription
        {
            Width = _width,
            Height = _height,
            MipLevels = 1,
            ArraySize = 1,
            Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging,
            CpuAccessFlags = CpuAccessFlags.Read,
            BindFlags = BindFlags.None
        };

        _stagingTexture = new Texture2D(_device, texDesc);
    }

    public Task<MediaFrame> CaptureFrameAsync(CancellationToken ct)
    {
        if (!_isCapturing || _duplication == null || _device == null || _stagingTexture == null)
            throw new InvalidOperationException("Captura não inicializada.");

        var frame = new MediaFrame
        {
            TimestampMs = Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000),
            Width = _width,
            Height = _height
        };

        try
        {
            var result = _duplication.TryAcquireNextFrame(16, out var frameInfo, out var resource);

            if (result.Failure)
            {
                return Task.FromResult(frame);
            }

            try
            {
                using var texture = resource.QueryInterface<Texture2D>();
                _device.ImmediateContext.CopyResource(texture, _stagingTexture);

                var dataBox = _device.ImmediateContext.MapSubresource(
                    _stagingTexture, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                try
                {
                    var rawData = new byte[_width * _height * 4];
                    var srcPtr = dataBox.DataPointer;
                    var bytesPerRow = _width * 4;

                    for (int y = 0; y < _height; y++)
                    {
                        Marshal.Copy(srcPtr, rawData, y * bytesPerRow, bytesPerRow);
                        srcPtr += dataBox.RowPitch;
                    }

                    frame.VideoData = rawData;
                }
                finally
                {
                    _device.ImmediateContext.UnmapSubresource(_stagingTexture, 0);
                }
            }
            finally
            {
                _duplication.ReleaseFrame();
            }
        }
        catch (SharpDXException)
        {
            Reinitialize();
        }

        return Task.FromResult(frame);
    }

    private void Reinitialize()
    {
        try
        {
            ReleaseResources();
            InitializeDuplication();
        }
        catch
        {
            _isCapturing = false;
        }
    }

    private void ReleaseResources()
    {
        _duplication?.Dispose();
        _duplication = null;
        _stagingTexture?.Dispose();
        _stagingTexture = null;
        _output?.Dispose();
        _output = null;
        _adapter?.Dispose();
        _adapter = null;
        _device?.Dispose();
        _device = null;
    }

    public void Stop()
    {
        _isCapturing = false;
    }

    public void Dispose()
    {
        Stop();
        ReleaseResources();
    }
}
