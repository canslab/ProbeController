using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Streamer;
using System.Windows.Threading;

namespace ProbeController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static WriteableBitmap _wb = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr24, null);
        //private static Int32Rect _rect = new Int32Rect(0, 0, _wb.PixelWidth , _wb.PixelHeight);
        //private static int _bytesPerPixel = (_wb.Format.BitsPerPixel + 7) / 8;
        //private static int _stride = _wb.PixelWidth * _bytesPerPixel;

        // Create a byte array for a the entire size of bitmap.
        //private static int _arraySize = _stride * _wb.PixelHeight;
        //private static byte[] _colorArray = new byte[_arraySize];
        private StreamReceiver recv = new StreamReceiver("http://devjhlab.iptime.org:8080/?action=stream");

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            
        }
        public MainWindow()
        {
            InitializeComponent();
            frame.Stretch = Stretch.None;
            frame.Source = _wb;

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.IsEnabled = true;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //frame.Source = recv.GetFrameAsBitmapFrame();
            OpenCvSharp.Mat k = recv.GetFrameAsMat();
            OpenCvSharp.Extensions.WriteableBitmapConverter.ToWriteableBitmap(k, _wb);
            k.Release();
        }

        private void startStreamButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(_wb.BackBufferStride.ToString());

            OpenCvSharp.Mat k = recv.GetFrameAsMat();
            frame.Source = OpenCvSharp.Extensions.BitmapSourceConverter.ToBitmapSource(k);
        }

        private void frame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(frame.ActualHeight.ToString());

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
