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

        public GrabWindowResult Result { get; private set; }

        public class GrabWindowResult : IDisposable
        {
            public ImageCv2.Mat ROIFrame { get; set; }
            public ImageCv2.Rect ROIRect { get; set; }

            public void Dispose()
            {
                ROIFrame?.Release();
                ROIFrame = null;
            }

            ~GrabWindowResult()
            {
                ROIFrame?.Release();
            }
        }

        public GrabWindow(ImageCv2.Mat deliveredGrappedMat)
        {
            Debug.Assert(deliveredGrappedMat != null);
            InitializeComponent();

            // MainWindow로부터 받아온 Mat을 EntireMat에 보관
            EntireMat = deliveredGrappedMat.Clone();
            Result = null; 

            mWb = new WriteableBitmap(EntireMat.Width, EntireMat.Height, 96, 96, PixelFormats.Bgr24, null);

            // set properties of frame(image)
            snippetFrame.Source = mWb;
            snippetFrame.Stretch = Stretch.None;

            // 얻어온 프레임을 표시
            ImageCv2.Extensions.WriteableBitmapConverter.ToWriteableBitmap(EntireMat, mWb);

            selectedRegion.Visibility = Visibility.Hidden;

            confirmButton.IsEnabled = true;
            saveButton.IsEnabled = false;
            readyTrackingButton.IsEnabled = false;
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
            Console.WriteLine("Grapped Window 닫는중.. 리소스 해제중");

            EntireMat?.Release();
            CrappedMat?.Release();
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
            // 마우스를 눌렀다는 것은 Grapping을 시작했다는 것이다. 
            var mouseDownPosition = e.GetPosition(sender as IInputElement);
            
            // 이전에 선택해둔 영역이 있다면 해제하고 시작한다. 
            if (CrappedMat != null && CrappedMat.IsDisposed == false)
            {
                CrappedMat.Release();
            }

            // ROI 사각형의 x좌표 y좌표를 mouse down된 position으로 설정한다.
            selectedRegion.SetValue(Canvas.LeftProperty, mouseDownPosition.X);
            selectedRegion.SetValue(Canvas.TopProperty, mouseDownPosition.Y);

            // 기본적으로 클릭이 되면 width = 1, height = 1부터 시작된다.
            selectedRegion.Width = 1;
            selectedRegion.Height = 1;
            selectedRegion.Visibility = Visibility.Visible;
            selectedRegion.Stroke = Brushes.Yellow;

            // 최소, width >= 1, height >= 1 설정됐으므로 confirm 버튼을 활성화 시킨다. 
            confirmButton.IsEnabled = true;
            saveButton.IsEnabled = false;
        }

        private void onMouseMoveAtCanvas(object sender, MouseEventArgs e)
        {
            // 왼쪽 버튼이 안눌러져있는 상태로 움직이는 것은 Grapping 행위가 아니므로, 메소드를 중단한다.
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }
            var currentMousePos = e.GetPosition(sender as IInputElement);
            var currentMouseMoveX = (int)currentMousePos.X;
            var currentMouseMoveY = (int)currentMousePos.Y;
            
            selectedRegion.Width = Math.Abs(currentMouseMoveX - SelectedROI.X);
            selectedRegion.Height = Math.Abs(currentMouseMoveY - SelectedROI.Y);

            // 대쉬보드에 ROI의 위치, 크기 등을 업데이트 한다.
            snippetOriginLabel.Content = string.Format("({0}, {1})", SelectedROI.X, SelectedROI.Y);
            snippetSizeLabel.Content = string.Format("({0}, {1})", SelectedROI.Width, SelectedROI.Height);
        }   

        private void onConfirmButtonClicked(object sender, RoutedEventArgs e)
        {
            if (SelectedROI.Width <= 5 || SelectedROI.Height <= 5)
            {
                MessageBox.Show("Selected Region is so small or not selected.. Retry !", "Region size is invalid", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 선택된 ROI로 전체프레임을 Crap해서 저장한다.
            CrappedMat = EntireMat.SubMat(SelectedROI);
            confirmButton.IsEnabled = false;
            saveButton.IsEnabled = true;
            readyTrackingButton.IsEnabled = true;
            selectedRegion.Stroke = Brushes.Red;
        }

        private void onReadyTrackingButton(object sender, RoutedEventArgs e)
        {
            Debug.Assert(CrappedMat != null && CrappedMat.IsDisposed == false);
            // Start Tracking을 누르면, 다이얼로그가 닫힌다.
            // 이후 MainWindow는 Result를 이용해 GrapWindow의 결과물에 접근한다. 
            Result = new GrabWindowResult();
            Result.ROIFrame = CrappedMat.Clone();
            Result.ROIRect = SelectedROI;

            // 창을 닫는다. 닫을 때 모든 리소스들을 해제한다.
            Close();
        }

    }
}