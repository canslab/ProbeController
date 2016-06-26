using OpenCvSharp;
using System;
using System.IO;
using System.Net;
using System.Windows.Media.Imaging;

namespace Streamer
{
    /// <summary>
    /// It is the type of StreamReceiver
    /// 
    /// This grabs a frame from remote IP camera.
    /// You should be aware of the URL of IP camera.
    /// </summary>
    class StreamReceiver
    {
        /// <summary>
        /// instantiate StreamReceiver object 
        /// to perfrom real time streaming job
        /// 
        /// </summary>
        /// <param name="url"> ip camera url address </param>
        public StreamReceiver(string url)
        {
            // 39 KB buffer
            mBuffer = new MemoryBuffer(40000);

            // 97 KB image buffer
            mImageBuffer = new MemoryBuffer(100000);

            // make web request. 
            WebRequest mWebRequest = WebRequest.Create(url);

            // get the response from web request 
            WebResponse mWebResponse = mWebRequest.GetResponse();
            
            // get the stream from WebResponse
            Stream mStream = mWebResponse.GetResponseStream();

            // make binary reader using web stream
            mReader = new BinaryReader(mStream);
        }

        /// <summary>
        /// Grabs a frame as a OpenCVSharp.Mat 
        /// 
        /// </summary>
        /// <param name="outputMat"> This is a output frame </param>
        public void GetFrameAsMat(out Mat outputMat)
        {
            byte[] recvFrameAsByteArray = null;

            GetFrameBytes(out recvFrameAsByteArray);
            outputMat = Cv2.ImDecode(recvFrameAsByteArray, ImreadModes.Unchanged);
        }

        public BitmapFrame GetFrameAsBitmapFrame()
        {
            byte[] recvFrameAsBytes = null;
            GetFrameBytes(out recvFrameAsBytes);
            MemoryStream mMemoryStream = new MemoryStream(recvFrameAsBytes);
            JpegBitmapDecoder mDecoder = new JpegBitmapDecoder(mMemoryStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

            return mDecoder.Frames[0];
        }

        /// <summary>
        /// Get a frame as byte[] 
        /// It is a private method and invoked by GetFrameAsMat()
        /// </summary>
        /// <returns> output byte </returns>
        private void GetFrameBytes(out byte[] outputFrame)
        {
            int startFlagLocation = -1;
            int endFlagLocation = -1;

            while (true)
            {
                // read the stream data from BinaryReader and save these data to mBuffer up to 100000(97KB)
                mBuffer.AppendDataFromBinaryReader(mReader, 100000);

                // find the start flag of JPEG Format
                startFlagLocation = mBuffer.FindPattern(0, soi);

                // if the star flag has been found, we should find the end flag 
                if (startFlagLocation != -1)
                {
                    // start search task from startLocation in order to make sure that startLocation < endLocation
                    endFlagLocation = mBuffer.FindPattern(startFlagLocation, eoi);
                }

                if (startFlagLocation != -1 && endFlagLocation != -1)
                {
                    // Data Move:  mBuffer ---> mImageBuffer
                    // After that, mImageBuffer would be the output and used to make Mat or BitmapFrame
                    mImageBuffer.Append(mBuffer.Buffer, startFlagLocation, endFlagLocation + 2 - startFlagLocation);

                    // clear the contents of mImageBuffer, mBuffer
                    mImageBuffer.ClearContents();
                    mBuffer.ClearContents();

                    break;
                }
            }
            // outputFrame will point to mImageBuffer.Buffer
            outputFrame = mImageBuffer.Buffer;
        }

        /// <summary>
        /// Terminates this object 
        /// </summary>
        public void Close()
        {
            mBuffer = null;
            mImageBuffer = null;
            mReader.Close();
        }

        /// <summary>
        /// soi means the start flags of JPEG Format
        /// </summary>
        private static byte[] soi = { 0xff, 0xd8 };
        /// <summary>
        /// eoi means the end flags of JPEG Format
        /// </summary>
        private static byte[] eoi = { 0xff, 0xd9 };
        
        /// <summary>
        /// This buffer is used to receive data directly from BinaryReader
        /// </summary>
        private MemoryBuffer mBuffer;
        /// <summary>
        /// This buffer is used to make 
        /// </summary>
        private MemoryBuffer mImageBuffer;
        private BinaryReader mReader;
    }
}
