using OpenCvSharp;
using System;
using System.Diagnostics;

namespace ImageProcessing
{
    /// <summary>
    /// 오브젝트 트랙커이다.
    /// 
    /// 사용법: 먼저 추적하고자 하는 대상의 이미지를 설정하고, 
    /// 현재 프레임을 DoTrackUsing()함수에 넣어줘서 추적한다.
    /// 
    /// 그 함수는 추적결과를 리턴할 것이며, 유저는 그것을 적절히 이용하면 된다.
    /// 
    /// </summary>
    public partial class ObjectTracker : IDisposable
    {
        public static Rangef[] hsRanges = { new Rangef(0, 180), new Rangef(0, 256) };

        private Rect mTrackingWindow;
        private Mat mModelHistogram;
        private Mat mBackProjectionMat;

        private bool mBModelHistogramReady;
        private bool mBInitialSettingReady;

        /// <summary>
        /// 정확도에 관련된 변수, 크면 클수록 정확해 지지만, 연산량이 증가한다.
        /// 디폴트로 10이 설정되어 있다.
        /// </summary>
        public int MaxIterationCount { get; set; }
        /// <summary>
        /// 정확도에 관련된 변수, Epsilon이 작을수록 정확도가 높아진다, 하지만 연산량이 증가한다.
        /// 디폴트로 1이 설정되어 있다.
        /// </summary>
        public double Epsilon { get; set; }

        ////////////////////////////////////////// singleton related methods
        static ObjectTracker()
        {
            Instance = new ObjectTracker();
        }

        /// <summary>
        /// ObjectTracker 싱글턴 객체를 얻어온다.
        /// </summary>
        public static ObjectTracker Instance { get; }

        /// <summary>
        /// 현재 트렉킹할 준비가 되어 있는가? 
        /// </summary>
        public bool IsReadyToTrack
        {
            get
            {
                return mBModelHistogramReady && mBInitialSettingReady;
            }
        }

        // methods 
        private ObjectTracker()
        {
            mTrackingWindow = new Rect();
            mModelHistogram = new Mat();
            mBackProjectionMat = new Mat();

            mBModelHistogramReady = false;
            mBInitialSettingReady = false;

            MaxIterationCount = 10;
            Epsilon = 1;
        }
        ~ObjectTracker()
        {
            releaseUnmanagedResources();
        }

        /// <summary>
        /// 모델 이미지를 OpenCVSharp.Mat 타입으로 받는다
        /// 그러고 나서 내부적으로 모델 히스토그램을 구해서 ObjectTracker 내부에서 관리한다.
        /// </summary>
        /// <param name="modelMat"> 모델 Mat</param>
        /// <param name="normalizedRanges"> 값을 정규화할 범위 </param>
        /// <param name="channels"> 채널( [0,2] ) </param>
        /// <param name="binSizes"> 각 축의 bin size들 (이전 파라미터인 채널들에 대응됨) </param>
        /// <param name="ranges"> 각 축의 최소, 최대 범위 </param>
        public void SetModelImageAsMat(Mat modelMat, int[] normalizedRanges, int[] channels, int[] binSizes, Rangef[] ranges)
        {
            Debug.Assert(modelMat != null
                        && modelMat.IsDisposed == false
                        && channels != null
                        && channels.Length <= 3
                        && binSizes != null && binSizes.Length == channels.Length
                        && normalizedRanges != null && normalizedRanges.Length == 2);

            using (var hsvModelMat = modelMat.CvtColor(ColorConversionCodes.BGR2HSV))
            {
                Cv2.CalcHist(new Mat[] { hsvModelMat }, channels, null, mModelHistogram, channels.Length, binSizes, ranges);
                Cv2.Normalize(mModelHistogram, mModelHistogram, normalizedRanges[0], normalizedRanges[1], NormTypes.MinMax);
            }

            mBModelHistogramReady = true;
        }

        /// <summary>
        /// 모델 이미지를 인코딩된(JPEG) 바이트배열로 받는다
        /// 그러고 나서 내부적으로 모델 히스토그램을 구해서 ObjectTracker 내부에서 관리한다.
        /// </summary>
        /// <param name="modelAsJPEGEncodedArray"> 모델 Mat</param>
        /// <param name="normalizedRanges"> 값을 정규화할 범위 </param>
        /// <param name="channels"> 채널( [0,2] ) </param>
        /// <param name="binSizes"> 각 축의 bin size들 (이전 파라미터인 채널들에 대응됨) </param>
        /// <param name="ranges"> 각 축의 최소, 최대 범위 </param>
        public void SetModelImageAsJPEGEncodedArray(byte[] modelAsJPEGEncodedArray, int[] normalizedRanges, int[] channels, int[] binSizes, Rangef[] ranges)
        {
            Debug.Assert(modelAsJPEGEncodedArray != null
                        && modelAsJPEGEncodedArray.Length > 0
                        && channels != null
                        && channels.Length <= 3
                        && binSizes != null && binSizes.Length == channels.Length
                        && normalizedRanges != null && normalizedRanges.Length == 2);


            // 모델 이미지를 Mat 타입으로 변환
            using (var tempModelMat = decodeToMat(modelAsJPEGEncodedArray))
            {
                SetModelImageAsMat(tempModelMat, normalizedRanges, channels, binSizes, ranges);
            }
        }

        /// <summary>
        /// 초기 트랙킹 윈도우의 값 들을 설정한다.
        /// </summary>
        /// <param name="initX"> 초기 Top-Left corner X 값</param>
        /// <param name="initY"> 초기 Top-Left corner y 값</param>
        /// <param name="width"> 초기 너비 </param>
        /// <param name="height"> 초기 높이 </param>
        public void SetInitialTrackingWindowProperties(int initX, int initY, int width, int height)
        {
            Debug.Assert(mTrackingWindow != null);
            mTrackingWindow.X = initX;
            mTrackingWindow.Y = initY;
            mTrackingWindow.Width = width;
            mTrackingWindow.Height = height;

            mBInitialSettingReady = true;
        }

        /// <summary>
        /// 초기 트랙킹 윈도우의 값을 설정하는데, OpenCVSharp.Rect로 설정한다
        /// </summary>
        /// <param name="roiRect">관심영역</param>
        public void SetInitialTrackingWindowProperties(OpenCvSharp.Rect roiRect)
        {
            Debug.Assert(mTrackingWindow != null);
            SetInitialTrackingWindowProperties(roiRect.X, roiRect.Y, roiRect.Width, roiRect.Height);
        }

        /// <summary>
        /// 현재 프레임(OpenCVSharp.Mat)을 받아서, 트랙킹 작업을 수행한다.
        /// 주의: 이 함수가 호출되기 전에, 반드시 모델 이미지 설정과 초기 윈도우 위치, 크기등을 설정해야한다.
        /// </summary>
        /// <param name="currentFrameMat"> 현재 프레임(OpenCVSharp.Mat) </param>
        /// <returns> 트랙킹 결과를 리턴 (프레임 + 윈도우) </returns>
        public TrackingResult DoTrackUsing(Mat currentFrameMat)
        {
            Debug.Assert(currentFrameMat != null && currentFrameMat.IsDisposed == false);
            Debug.Assert(mModelHistogram != null && mModelHistogram.Type() == MatType.CV_32FC1);
            Debug.Assert(IsReadyToTrack == true);

            // 현재 프레임을 HSV 포맷으로 바꾼다. 그리고 HSV프레임은, 백프로젝션 수행후 release해야하므로 using구문을 사용한다.
            using (var hsvCurrentFrameMat = currentFrameMat.CvtColor(ColorConversionCodes.BGR2HSV))
            {
                // 백프로젝션 수행
                Cv2.CalcBackProject(new Mat[] { hsvCurrentFrameMat }, new int[] { 0, 1 }, mModelHistogram, mBackProjectionMat, hsRanges);
            }

            Cv2.MeanShift(mBackProjectionMat, ref mTrackingWindow, TermCriteria.Both(MaxIterationCount, Epsilon));
            Cv2.Rectangle(currentFrameMat, mTrackingWindow, 255, 3);

            // 결과를 IPUResult 구조체에 저장, 리턴될 예정
            TrackingResult result = new TrackingResult()
            {
                Frame = currentFrameMat.Clone(),
                XPos = mTrackingWindow.X,
                YPos = mTrackingWindow.Y,
                Width = mTrackingWindow.Width,
                Height = mTrackingWindow.Height
            };

            return result;
        }

        /// <summary>
        /// 현재 프레임(JPEG format으로 encoding된)을 받아서, 트랙킹 작업을 수행한다.
        /// 주의: 이 함수가 호출되기 전에, 반드시 모델 이미지 설정과 초기 윈도우 위치, 크기등을 설정해야한다.
        /// </summary>
        /// <param name="currentFrameArray"> 현재 프레임(JPEG Encoded format) </param>
        /// <returns> 트랙킹 결과를 리턴 (프레임 + 윈도우) </returns>
        public TrackingResult DoTrackUsing(byte[] currentFrameArray)
        {
            Debug.Assert(currentFrameArray != null && mModelHistogram != null);
            Debug.Assert(mModelHistogram.Type() == MatType.CV_32FC1);
            Debug.Assert(IsReadyToTrack == true);

            TrackingResult retResult = null;

            // 바이트 배열에서 Mat type으로 변환 + HSV format으로 변환
            using (var tempMat = decodeToMat(currentFrameArray))
            {
                retResult = DoTrackUsing(tempMat);
            }

            return retResult;
        }

        /// <summary>
        /// 다른 트랙킹작업을 위해 리소스들을 해제하고, ObjectTracker를 처음 상태로 되돌린다.
        /// </summary>
        public void FinishTrackingAndResetAll()
        {
            // 먼저 내부에서 소유하고 있는 unmanaged resource들을 해제한다.
            releaseUnmanagedResources();

            // 다시 트랙킹하기 이전에, 모든 객체들을 새로 생성하고 값을 초기화 한다.
            mTrackingWindow = new Rect();
            mModelHistogram = new Mat();
            mBackProjectionMat = new Mat();

            mBModelHistogramReady = false;
            mBInitialSettingReady = false;
        }

        ///////////////////////////////////////////// static methods
        private static Mat decodeToMat(byte[] frameArray)
        {
            Debug.Assert(frameArray != null && frameArray.Length > 0);
            return Cv2.ImDecode(frameArray, ImreadModes.Unchanged);
        }
    }
}
