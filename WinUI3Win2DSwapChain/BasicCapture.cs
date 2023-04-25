using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using System;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;

namespace WinUI3Win2DSwapChain
{
    class BasicCapture : IDisposable
    {
        private CanvasDevice _device;
        private CanvasSwapChain _swapChain;

        private Direct3D11CaptureFramePool _framePool;
        private GraphicsCaptureSession _session;
        private GraphicsCaptureItem _item;

        private SizeInt32 _lastSize;
        private object _lock;
        private DispatcherQueue _uiThread;

        public event EventHandler<SizeInt32> SizeChanged;

        public BasicCapture(CanvasDevice device, GraphicsCaptureItem item)
        {
            _uiThread = DispatcherQueue.GetForCurrentThread();
            _device = device;
            _item = item;

            _lastSize = item.Size;

            _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(_device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 3, _lastSize);
            _session = _framePool.CreateCaptureSession(item);
            _framePool.FrameArrived += OnFrameArrived;

            _swapChain = new CanvasSwapChain(_device, _lastSize.Width, _lastSize.Height, 96);
            _lock = new object();
        }

        public void StartCapture()
        {
            _session.StartCapture();
        }

        public void AttachToPanel(CanvasSwapChainPanel panel)
        {
            panel.SwapChain = _swapChain;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _session?.Dispose();
                _framePool?.Dispose();
                _swapChain?.Dispose();

                _swapChain = null;
                _framePool = null;
                _session = null;
                _item = null;
            }
        }

        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            lock (_lock)
            {
                var newSize = false;

                using (var frame = sender.TryGetNextFrame())
                {
                    if (frame.ContentSize.Width != _lastSize.Width ||
                        frame.ContentSize.Height != _lastSize.Height)
                    {
                        newSize = true;
                        _lastSize = frame.ContentSize;
                        _swapChain.ResizeBuffers(_lastSize.Width, _lastSize.Height);
                    }

                    using (var bitmap = CanvasBitmap.CreateFromDirect3D11Surface(_device, frame.Surface))
                    using (var drawingSession = _swapChain.CreateDrawingSession(Colors.Transparent))
                    {
                        drawingSession.DrawImage(bitmap);
                    }
                }

                _swapChain.Present();

                if (newSize)
                {
                    _framePool.Recreate(
                        _device,
                        DirectXPixelFormat.B8G8R8A8UIntNormalized,
                        3,
                        _lastSize);

                    _uiThread.TryEnqueue(() =>
                    {
                        SizeChanged?.Invoke(this, _lastSize);
                    });
                }
            }
        }
    }
}
