using System;
using System.Threading.Tasks;
using OpenCvSharp;
using System.Diagnostics;

namespace ProbeController
{
    /// <summary>
    /// It does all kinds of image processing task.
    /// 
    /// </summary>
    public static class ImageProcessingUnit
    {
        public class IPUResult : IDisposable
        {
            public Mat Frame { get; set; }
            public bool CompletedWell { get; internal set; }
            public IPUResult()
            {
                Frame = null;
                CompletedWell = false;
            }
            protected void destroy()
            {
                if (Frame != null && Frame.IsDisposed == false)
                {
                    Frame.Release();
                    Frame = null;
                }
                CompletedWell = false;
            }

            public void Dispose()
            {
                destroy();
                GC.SuppressFinalize(this);
            }
            ~IPUResult()
            {
                destroy();
            }
        }
        
        public static IPUResult ConvertToMat(byte[] frameAsByteArray)
        {
            Debug.Assert(frameAsByteArray != null && frameAsByteArray.Length > 0);

            IPUResult result = new IPUResult();

            result.Frame = Cv2.ImDecode(frameAsByteArray, ImreadModes.Unchanged);
            result.CompletedWell = true;

            return result;
        }

        public static async Task<IPUResult> TrackObject(byte[] targetAsByteArray)
        {
            Debug.Assert(targetAsByteArray != null && targetAsByteArray.Length > 0);

            IPUResult result = null;

            //byte[] currentFrame = null;
            //Mat currentFrameMat = null;
            //result = new IPUResult();

            //// Get Current Frame 
            //currentFrame = await Streamer.GetFrameAsByteArray();
            //currentFrameMat = Cv2.ImDecode(currentFrame, ImreadModes.Unchanged);
                
            // Get Position Vector originated from center position.
            return result;
        }
    }
}
