using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing
{
    public partial class ObjectTracker
    {
        public class TrackingResult : IDisposable
        {
            public int XPos { get; internal set; }
            public int YPos { get; internal set; }
            public int Width { get; internal set; }
            public int Height { get; internal set; }
            public Mat Frame { get; internal set; }

            private void destroy()
            {
                if (Frame != null && Frame.IsDisposed == false)
                {
                    Frame.Dispose();
                }
                Frame = null;
            }

            public void Dispose()
            {
                destroy();
                GC.SuppressFinalize(this);
            }
        }
        public void Dispose()
        {
            releaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 내부 리소스들을 모두 해제한다.
        /// </summary>
        protected void releaseUnmanagedResources()
        {
            Debug.Assert(mModelHistogram != null && mBackProjectionMat != null);
            //Debug.Assert(mModelHistogram.IsDisposed == false && mBackProjectionMat.IsDisposed == false);
            mModelHistogram.Release();
            mBackProjectionMat.Release();
        }

    }
}
