using OpenCvSharp;
using System;
using System.IO;
using System.Net;
using System.Windows.Media.Imaging;

namespace Streamer
{
    class StreamReceiver : IDisposable
    {
        public StreamReceiver(string url)
        {
            mBuffer = new MemoryBuffer(40000);
            mImageBuffer = new MemoryBuffer(100000);

            mWebRequest = WebRequest.Create(url);
            mWebResponse = mWebRequest.GetResponse();
            mStream = mWebResponse.GetResponseStream();
            mReader = new BinaryReader(mStream);
        }
        public Mat GetFrameAsMat()
        {
            Mat retMat = null;
            byte[] recvFrameAsBytes = GetFrameBytes();
            retMat = Cv2.ImDecode(recvFrameAsBytes, ImreadModes.Unchanged);

            return retMat;
        }

        public BitmapFrame GetFrameAsBitmapFrame()
        {
            byte[] recvFrameAsBytes = GetFrameBytes();
            MemoryStream mMemoryStream = new MemoryStream(recvFrameAsBytes);
            JpegBitmapDecoder mDecoder = new JpegBitmapDecoder(mMemoryStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

            return mDecoder.Frames[0];
        }

        private byte[] GetFrameBytes()
        {
            int startLocation = -1;
            int endLocation = -1;
            mStream.Flush();

            while(true)
            {
                //nRead = mReader.Read(firstBuffer, 0, 100000);
                //mBuffer.Append(firstBuffer, 0, nRead);
                mBuffer.AppendDataFromBinaryReader(mReader, 100000);

                startLocation = mBuffer.FindPattern(0, soi);
                if (startLocation != -1)
                {
                    endLocation = mBuffer.FindPattern(startLocation, eoi);
                }

                if(startLocation != -1 && endLocation !=-1)
                {
                     if (startLocation < endLocation)
                    {
                        mImageBuffer.Append(mBuffer.Buffer, startLocation, endLocation + 2 - startLocation);
                        //mMemoryStream = new MemoryStream(mImageBuffer.Buffer);
                        
                        //mDecoder = new JpegBitmapDecoder(mMemoryStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                        //mMat = Cv2.ImDecode(mImageBuffer.Buffer, ImreadModes.Unchanged);

                        mImageBuffer.ClearContents();
                        mBuffer.ClearContents();
                        break;
                    }
                }
            }
            return mImageBuffer.Buffer;
        }

        public void Dispose()
        {
            mReader.Dispose();
        }

        private static byte[] soi = { 0xff, 0xd8 };
        private static byte[] eoi = { 0xff, 0xd9 };
        
        private MemoryBuffer mBuffer;
        private MemoryBuffer mImageBuffer;
        private WebRequest mWebRequest;
        private WebResponse mWebResponse;
        private Stream mStream;
        private BinaryReader mReader;
    }
}
