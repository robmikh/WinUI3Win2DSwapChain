using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml;
using System;
using Windows.Graphics;
using Windows.Graphics.Capture;
using WinRT.Interop;

namespace WinUI3Win2DSwapChain
{
    public sealed partial class MainWindow : Window
    {
        private CanvasDevice _device;
        private BasicCapture _capture;

        public MainWindow()
        {
            this.InitializeComponent();

            _device = new CanvasDevice();
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new GraphicsCapturePicker();
            var windowHandle =  WinRT.Interop.WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, windowHandle);

            var item = await picker.PickSingleItemAsync();
            if (item != null)
            {
                StartCapture(item);
            }
        }

        private void StartCapture(GraphicsCaptureItem item)
        {
            if (_capture != null)
            {
                _capture.SizeChanged -= OnCaptureSizeChanged;
                _capture.Dispose();
            }

            _capture = new BasicCapture(_device, item);
            _capture.SizeChanged += OnCaptureSizeChanged;
            _capture.AttachToPanel(CaptureSwapChainPanel);
            ResizeSwapChainPanel(item.Size);
            _capture.StartCapture();
        }

        private void OnCaptureSizeChanged(object sender, SizeInt32 e)
        {
            ResizeSwapChainPanel(e);
        }

        private void ResizeSwapChainPanel(SizeInt32 size)
        {
            CaptureSwapChainPanel.Width = size.Width;
            CaptureSwapChainPanel.Height = size.Height;
        }
    }
}
