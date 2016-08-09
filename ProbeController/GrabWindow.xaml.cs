using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
        public ImageCv2.Rect SelectedROI
        {
            get
            {
                var selectedRegionX = (double)selectedRegion.GetValue(Canvas.LeftProperty);
                var selectedRegionY = (double)selectedRegion.GetValue(Canvas.TopProperty);

                if (selectedRegionX + selectedRegion.Width >= 640)
                {
                    selectedRegion.Width = 640 - selectedRegionX;
                }
                if (selectedRegionY + selectedRegion.Height >= 480)
                {
                    selectedRegion.Height = 480 - selectedRegionY;
                }

                return new ImageCv2.Rect((int)selectedRegionX, (int)selectedRegionY,
                    (int)selectedRegion.Width, (int)selectedRegion.Height);
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

            selectedRegion.Visibility = Visibility.Hidden;

            confirmButton.IsEnabled = true;
            saveButton.IsEnabled = false;
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
                saveButton.IsEnabled = false;
            }
        }
        
        private void onMouseDownAtCanvas(object sender, MouseButtonEventArgs e)
        {
            // mouse down means grapping has started! 
            var mouseDownPosition = e.GetPosition(sender as IInputElement);

            selectedRegion.SetValue(Canvas.LeftProperty, mouseDownPosition.X);
            selectedRegion.SetValue(Canvas.TopProperty, mouseDownPosition.Y);

            selectedRegion.Width = 1;
            selectedRegion.Height = 1;
            selectedRegion.Visibility = Visibility.Visible;
            selectedRegion.Stroke = Brushes.Yellow;

            // Change UI
            confirmButton.IsEnabled = true;
            saveButton.IsEnabled = false;
        }

        private void onMouseMoveAtCanvas(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }
            var currentMousePos = e.GetPosition(sender as IInputElement);
            var currentMouseMoveX = (int)currentMousePos.X;
            var currentMouseMoveY = (int)currentMousePos.Y;

            selectedRegion.Width = Math.Abs(currentMouseMoveX - SelectedROI.X);
            selectedRegion.Height = Math.Abs(currentMouseMoveY - SelectedROI.Y);

            // update bottom dashboard
            snippetOriginLabel.Content = string.Format("({0}, {1})", SelectedROI.X, SelectedROI.Y);
            snippetSizeLabel.Content = string.Format("({0}, {1})", SelectedROI.Width, SelectedROI.Height);
        }

        // when user pressed confirm button ==> selected region is used to make snippet image or tracking.
        private void onConfirmButtonClicked(object sender, RoutedEventArgs e)
        {
            if (SelectedROI.Width <= 5 || SelectedROI.Height <= 5)
            {
                MessageBox.Show("Selected Region is so small to prcoess any task.. Retry !", "Region is so small", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CrappedMat = EntireMat.SubMat(SelectedROI);
            confirmButton.IsEnabled = false;
            saveButton.IsEnabled = true;
            selectedRegion.Stroke = Brushes.Red;
        }
    }
}