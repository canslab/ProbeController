using System.Threading.Tasks;
using JHStreamReceiver;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using OpenCvSharp;
using ImageProcessing;
using Tracker;
using System.Threading;

namespace ImageProcessing
{
    public class StreamWorker
    {
        // 콜백 델리게이트
        public delegate Task OnReceiveTrackingResult(int centerX, int centerY, bool bExistTarget, double stdev, AutoResetEvent trackingSynchronizer);

        public OnReceiveTrackingResult TrackingAfterCallback { get; private set; }

        public enum Mode { NORMAL, TRACKING }

        public StreamWorker(string _url, WriteableBitmap _wb)
        {
            Debug.Assert(_url != null && _url.Length > 0 && _wb != null);

            Receiver = new StreamReceiver();
            RemoteURL = _url;
            Wb = _wb;
            StreamingMode = Mode.NORMAL;
            TrackingAfterCallback = null;
        }

        public async Task<bool> MakeConnectionAsync()
        {
            Debug.Assert(Receiver != null && Receiver.IsConnected == false);
            return await Receiver.ConnectToURLAsync(RemoteURL);
        }

        /// <summary>
        /// 스트리밍을 시작한다. Parallel 하게 Streaming Task를 실행시킨다. 
        /// </summary>
        public void RunStreamingConcurrently()
        {
            Debug.Assert(Receiver != null && Receiver.IsConnected == true);
            AsyncStreamingTask = new Task(StreamingTaskCallBack);
            AsyncStreamingTask.Start();
        }

        /// <summary>
        /// Frame을 비동기적으로 캡쳐합니다.
        /// </summary>
        /// <returns></returns>
        public async Task<Mat> CaptureFrameAsync()
        {
            Debug.Assert(IsNowStreaming == true &&
                         Receiver != null && Receiver.IsConnected == true);

            /// [큰 구조] 
            /// Capture 하기 위해서 실시간으로 스트리밍 받으면서 UI에 반영하는 
            /// Task 종료( Receiver는 멀쩡히 살아있음 )
            /// Frame을 얻고
            /// 
            /// 실시간 스트리밍 Task를 다시 Run해주고 lastFrame을 반환한다.

            Mat lastFrameMat = null;

            // 현재 스트리밍 task가 완료할 때까지, await한다 
            // GetFrameAsByteArray()를 동시에 호출하면 안되므로, 하나는 일단 완료시킨다.
            bool bStoppedWell = await StopStreamingTaskAndWait();
            if (bStoppedWell == false)
            {
                return null;
            }

            // 마지막프레임을 비동기적으로 가져온다. 
            lastFrameMat = await Task<Mat>.Factory.StartNew(() =>
            {
                var frameAsByteArray = Receiver.GetFrameAsByteArray();

                return Cv2.ImDecode(frameAsByteArray, ImreadModes.Unchanged);
            });

            // 다시 스트리밍을 resume 한다.
            RunStreamingConcurrently();
            return lastFrameMat;
        }

        /// <summary>
        /// 비동기적으로 스트리밍 작업을 중지한다.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> StopStreamingTaskAndWait()
        {
            Debug.Assert(IsNowStreaming == true);
            bool bStreamingCompleted = false;

            // Pause 요청
            PauseRequested = true;

            // Streaming Task가 종료되기 만을 기다린다.
            bStreamingCompleted = await Task<bool>.Factory.StartNew(() =>
            {
                AsyncStreamingTask.Wait();
                return AsyncStreamingTask.IsCompleted;
            });

            return bStreamingCompleted;
        }

        /// <summary>
        /// 스트리밍 모드를 트랙킹 모드로 변경한다.
        /// </summary>
        /// <param name="_tracker"> 트랙커를 등록한다. </param>
        public void ChangeToTrackingMode(ObjectTracker _tracker, OnReceiveTrackingResult trackingAfterCallback)
        {
            Debug.Assert(_tracker != null && _tracker.IsTrackingReady == true);
            Tracker = _tracker;
            StreamingMode = Mode.TRACKING;
            TrackingAfterCallback = trackingAfterCallback;
        }

        public void ChangeToNormalMode()
        {
            // release resources if any
            StreamingMode = Mode.NORMAL;
        }

        // 이 함수는 다른 thread(non ui thread)에서 수행되는 함수입니다.
        private void StreamingTaskCallBack()
        {
            while (true)
            {
                if (PauseRequested == true)
                {
                    // 복원 시켜놓고 메소드 종료
                    PauseRequested = false;
                    break;
                }

                var frameAsByteArray = Receiver.GetFrameAsByteArray();
                using (var currentFrameMat = Cv2.ImDecode(frameAsByteArray, ImreadModes.Unchanged))
                {
                    Mat drawMat = currentFrameMat;

                    if (StreamingMode == Mode.TRACKING)
                    {
                        // 트랙킹한다
                        var trackResult = Tracker.TrackUsing(currentFrameMat, 50, 320, 240);
                        Cv2.Rectangle(drawMat, trackResult.Region, 255, 3);

                        // 유저가 트랙킹 결과를 받고 일을 할 수 있도록 콜백함수를 호출해준다.
                        TrackingAfterCallback(trackResult.CenterX, trackResult.CenterY, trackResult.IsObjectExist, trackResult.RegionStdev, TrackingSynchronizer);
#if MY_DEBUG
                        drawMat = trackResult.BackprojectFrame;
#endif
                        // 콜백함수가 완전히 종료될 때까지 기다린다.
                        TrackingSynchronizer.WaitOne();
                    }

                    Wb.Dispatcher.Invoke(() =>
                    {
                        //OpenCvSharp.Extensions.WriteableBitmapConverter.ToWriteableBitmap(currentFrameMat, Wb);
                        OpenCvSharp.Extensions.WriteableBitmapConverter.ToWriteableBitmap(drawMat, Wb);
                    });
                }
            }
        }

        public AutoResetEvent TrackingSynchronizer { get; private set; } = new AutoResetEvent(false);
        public string RemoteURL { get; private set; }
        public bool IsNowStreaming
        {
            get
            {
                return !AsyncStreamingTask.IsCompleted;
            }
        }
        public WriteableBitmap Wb { get; }
        public ObjectTracker Tracker { get; private set; }
        public Mode StreamingMode { get; private set; }
        public bool Connected
        {
            get
            {
                return Receiver.IsConnected;
            }
        }

        private StreamReceiver Receiver { get; }
        private bool PauseRequested { get; set; }
        private Task AsyncStreamingTask { get; set; }
    }
}
