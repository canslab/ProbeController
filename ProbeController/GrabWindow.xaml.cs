using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageCv2 = OpenCvSharp;

namespace ProbeController
{
    public partial class GrabWindow : Window
    {
        /// <summary>
        /// To render grapped frame
        /// </summary>
        private WriteableBitmap mWb;
        public ImageCv2.Mat EntireMat { get; }
        public ImageCv2.Mat CrappedMat { get; private set; }

        private ImageCv2.Point mTargetOrigin;
        private ImageCv2.Size mTargetSize;

        public ImageCv2.Rect TargetROI
        {
            get
            {
                Debug.Assert(mTargetOrigin != null && mTargetSize != null);
                return new ImageCv2.Rect(mTargetOrigin, mTargetSize);
            }
        }

        public GrabWindow(ImageCv2.Mat deliveredGrappedMat)
        {
            Debug.Assert(deliveredGrappedMat != null);
            InitializeComponent();

            EntireMat = deliveredGrappedMat;
            CrappedMat = EntireMat;
            mWb = new WriteableBitmap(EntireMat.Width, EntireMat.Height, 96, 96, PixelFormats.Bgr24, null);

            // set properties of frame(image)
            snippetFrame.Source = mWb;
            snippetFrame.Stretch = Stretch.None;

            // set frame as grapped frame
            ImageCv2.Extensions.WriteableBitmapConverter.ToWriteableBitmap(EntireMat, mWb);

            mTargetOrigin = new ImageCv2.Point(0, 0);
            mTargetSize = new ImageCv2.Size(0, 0);
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
            Debug.Assert(EntireMat != null);
            base.OnClosed(e);
            EntireMat.Release();
        }

        private async void onSaveButton(object sender, RoutedEventArgs e)
        {
            Debug.Assert(CrappedMat != null);

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "snippet.jpg";
            dlg.DefaultExt = ".jpg";
            dlg.Filter = "JPEG image (.jpg)|*.jpg";

            bool? result = dlg.ShowDialog(this);

            if (result == true)
            {
                await Task.Factory.StartNew(() => { ImageCv2.Cv2.ImWrite(dlg.FileName, CrappedMat); });
                MessageBox.Show("Save Complete!");
            }
        }
        private void onMouseUpAtFrame(object sender, MouseButtonEventArgs e)
        {
            Debug.Assert(EntireMat != null);
            var mouseUpPosition = e.GetPosition(sender as IInputElement);
            var mouseUpX = (int)mouseUpPosition.X;
            var mouseUpY = (int)mouseUpPosition.Y;

            // calculate the Width and Height of snippet frame.
            mTargetSize.Width = Math.Abs(mouseUpX - mTargetOrigin.X);
            mTargetSize.Height = Math.Abs(mouseUpY - mTargetOrigin.Y);

            // calculate the effective origin(top left corner point). 
            if (mouseUpX <= mTargetOrigin.X)
            {
                mTargetOrigin.X = mouseUpX;
            }

            if (mouseUpY <= mTargetOrigin.Y)
            {
                mTargetOrigin.Y = mouseUpY;
            }

            // update bottom dashboard
            snippetOriginLabel.Content = string.Format("({0}, {1})", mTargetOrigin.X, mTargetOrigin.Y);
            snippetSizeLabel.Content = string.Format("({0}, {1})", mTargetSize.Width, mTargetSize.Height);

            // when selected region is so small 
            if (mTargetSize.Height <= 5 || mTargetSize.Width <= 5)
            {
                MessageBox.Show("Selected Region is so small to prcoess any task.. Retry !", "Region is so small", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                CrappedMat = EntireMat.SubMat(TargetROI);
            }
        }
        private void onMouseDownAtFrame(object sender, MouseButtonEventArgs e)
        {
            var mouseDownPosition = e.GetPosition(sender as IInputElement);
            var mouseDownX = (int)mouseDownPosition.X;
            var mouseDownY = (int)mouseDownPosition.Y;

            mTargetOrigin.X = mouseDownX;
            mTargetOrigin.Y = mouseDownY;
        }
    }
}
