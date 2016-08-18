using System.Threading.Tasks;
using JHStreamReceiver;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using OpenCvSharp;
using ImageProcessing;

namespace ImageProcessing
{
    public class StreamWorker
    {
        public enum Mode { NORMAL, TRACKING }

        public StreamWorker(string _url, WriteableBitmap _wb)
        {
            Debug.Assert(_url != null && _url.Length > 0 && _wb != null);

            Receiver = new StreamReceiver();
            RemoteURL = _url;
            Wb = _wb;
            StreamingMode = Mode.NORMAL;
        }

        /// <summary>
        /// remote Device에 접속.
        /// </summary>
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
        public void ChangeToTrackingMode(ObjectTracker _tracker)
        {
            Debug.Assert(_tracker != null && _tracker.IsReadyToTrack == true);
            Tracker = _tracker;
            StreamingMode = Mode.TRACKING;
        }
        public void ChangeToNormalMode()
        {
            // release resources if any
            StreamingMode = Mode.NORMAL;
        }

        /// <summary>
        /// 이 함수는 AsyncStreamingTask에 의해 수행되는 Callback 함수입니다.
        /// </summary>
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
                    if (StreamingMode == Mode.NORMAL)
                    {
                        Wb.Dispatcher.Invoke(() =>
                        {
                            OpenCvSharp.Extensions.WriteableBitmapConverter.ToWriteableBitmap(currentFrameMat, Wb);
                        });
                    }
                    else if (StreamingMode == Mode.TRACKING)
                    {
                        using (var trackingResult = Tracker.DoTrackUsing(currentFrameMat))
                        {
                            Wb.Dispatcher.Invoke(() =>
                            {
                                OpenCvSharp.Extensions.WriteableBitmapConverter.ToWriteableBitmap(trackingResult.Frame, Wb);
                            });
                        }
                    }
                }
            }
        }

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
