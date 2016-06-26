using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Streamer;
using System.Windows.Threading;

namespace ProbeController
{
    public partial class MainWindow : Window
    {
        private static WriteableBitmap _wb = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr24, null);
        private StreamReceiver streamReceiver = new StreamReceiver("http://devjhlab.iptime.org:8080/?action=stream");
        private DispatcherTimer dispatcherTimer = new DispatcherTimer();

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }
        public MainWindow()
        {
            InitializeComponent();
            frame.Stretch = Stretch.None;
            frame.Source = _wb;

            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.IsEnabled = true;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 90);
            dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            OpenCvSharp.Mat k;
            streamReceiver.GetFrameAsMat(out k);
            OpenCvSharp.Extensions.WriteableBitmapConverter.ToWriteableBitmap(k, _wb);

            k.Release();
            //frame.Source = recv.GetFrameAsBitmapFrame();
        }

        private void startStreamButton_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(_wb.BackBufferStride.ToString());

            //OpenCvSharp.Mat k = recv.GetFrameAsMat();
            //frame.Source = OpenCvSharp.Extensions.BitmapSourceConverter.ToBitmapSource(k);
        }

        private void frame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(frame.ActualHeight.ToString());
        }

        private void endStreamButton_Click(object sender, RoutedEventArgs e)
        {
            streamReceiver.Close();
            dispatcherTimer.Stop();
            
        }
    }
}
