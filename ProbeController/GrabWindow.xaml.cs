using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Cv2 = OpenCvSharp;

namespace ProbeController
{
    /// <summary>
    /// Interaction logic for GrabWindow.xaml
    /// </summary>
    public partial class GrabWindow : Window
    {
        private WriteableBitmap mWb;
        public Cv2.Mat SnippetMat { get; private set; }

        public GrabWindow(Cv2.Mat GrappedMat)
        {
            InitializeComponent();

            SnippetMat = GrappedMat;
            if (SnippetMat != null)
            {
                mWb = new WriteableBitmap(SnippetMat.Width, SnippetMat.Height, 96, 96, PixelFormats.Bgr24, null);
                
                snippetFrame.Source = mWb;
                snippetFrame.Stretch = Stretch.None;

                Cv2.Extensions.WriteableBitmapConverter.ToWriteableBitmap(SnippetMat, mWb);
            }
        }
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        private void onExitButton(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // When this dialog exits... it is a sort of destructor.
        // clear all resources.. such as grabbed mat
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            //SnippetMat.Release();
        }
    }
}
