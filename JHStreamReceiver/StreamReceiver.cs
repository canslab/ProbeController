using OpenCvSharp;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace JHStreamReceiver
{
    /// <summary>
    /// It is the type of StreamReceiver
    /// 
    /// This grabs a frame from remote IP camera.
    /// You should be aware of the URL of IP camera.
    /// </summary>
    public class StreamReceiver
    {
        /********************************************************************/
        /*******            Constructrs                                 *****/
        /********************************************************************/
        /// <summary>
        /// instantiate StreamReceiver object 
        /// to perfrom real time streaming job
        /// 
        /// </summary>
        /// <param name="url"> ip camera url address </param>
        public StreamReceiver()
        {
            // 39 KB buffer
            mBuffer = new MemoryBuffer(40000);

            // 97 KB image buffer
            mImageBuffer = new MemoryBuffer(100000);

            // init mReader 
            mReader = null;
        }

        /********************************************************************/
        /*******            Public Methods                              *****/
        /********************************************************************/
        /// <summary>
        /// Connect to remote IP camera using given 'url' asynchronously
        /// </summary>
        /// <param name="url"> The Url address of the remote ip camera </param>
        /// <returns> Task </returns>
        public async Task<bool> ConnectToURLAsync(string url)
        {
            if (IsConnected == true)
            {
                // if it is connected, first disconnect the current connection, 
                // and newly connect to the ipcamera by using given url.
                Disconnect();
            }

            // make web request. It's like a connection trial.
            WebRequest webRequest = WebRequest.Create(url);

            // get the response from web request 
            WebResponse webResponse = null;

            try
            {
                // it can take some time..... 
                webResponse = await webRequest.GetResponseAsync();
            }
            catch(WebException e)
            {
                // WebException has occured because it can't connect to Ip camera.
                webResponse = null;
                webRequest = null;        
                
                // just return false
                return false;
            }

            // get the stream from WebResponse
            Stream stream = webResponse.GetResponseStream();
            
            // make binary reader using web stream
            mReader = new BinaryReader(stream);

            // connection success
            return true;
        }
    
        /// <summary>
        /// Grab a Frame as a OpenCVSharp.Mat from the remote IP camera
        /// 
        /// </summary>
        /// <returns> A grabbed Frame </returns>
        public async Task<Mat> GetFrameAsMatAsync()
        {
            Mat returnMat = null;

            // async job 
            returnMat = await Task<Mat>.Factory.StartNew(() =>
            {
                byte[] resultFrameAsByteArray = null;
                getFrameBytes(out resultFrameAsByteArray);

                return Cv2.ImDecode(resultFrameAsByteArray, ImreadModes.Unchanged);
            });

            return returnMat;
        }

        /// <summary>
        /// Grabs a frame as a BitmapFrame
        /// </summary
        public void GetFrameAsBitmapFrame(out BitmapFrame outputBitmapFrame)
        {
            byte[] recvFrameAsBytes = null;
            getFrameBytes(out recvFrameAsBytes);
            MemoryStream mMemoryStream = new MemoryStream(recvFrameAsBytes);
            JpegBitmapDecoder mDecoder = new JpegBitmapDecoder(mMemoryStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

            outputBitmapFrame = mDecoder.Frames[0];
        }
                
        /// <summary>
        /// Terminates this object 
        /// </summary>
        public void Disconnect()
        {
            // clear all buffers
            mBuffer.ClearContents();
            mImageBuffer.ClearContents();

            // dispose BinaryReader 
            mReader.Dispose();
            mReader.Close();
            // make mReader null
            mReader = null;
        }
        /********************************************************************/
        /*******            Properties                                  *****/
        /********************************************************************/
        /// <summary>
        /// Check whether this StreamReceiver is connected or not
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if ( mReader != null && mReader.BaseStream != null)
                {
                    return true;
                }            
                else
                {
                    return false;
                }
            }
        }

        /********************************************************************/
        /*******            Private Methods                             *****/
        /********************************************************************/
        /// <summary>
        /// Get a frame as byte[], and it is synchronous method.
        /// So if you call this method, you can be stucked.
        /// 
        /// It is a private method and invoked by GetFrameAsMat()
        /// </summary>
        /// <returns> output byte </returns>
        private void getFrameBytes(out byte[] outputFrame) 
        {
            int startFlagLocation = -1;
            int endFlagLocation = -1;

            while (true)
            {
                // read the stream data from BinaryReader and save these data to mBuffer up to 100000(97KB)
                mBuffer.AppendDataFromBinaryReader(mReader, 100000);

                // find the start flag of JPEG Format
                startFlagLocation = mBuffer.FindPattern(0, SOI);

                // if the star flag has been found, we should find the end flag 
                if (startFlagLocation != -1)
                {
                    // start search task from startLocation in order to make sure that startLocation < endLocation
                    endFlagLocation = mBuffer.FindPattern(startFlagLocation, EOI);
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

        /********************************************************************/
        /*******            Private CONSTANTS                           *****/
        /********************************************************************/
        /// <summary>
        /// soi means the start flags of JPEG Format
        /// </summary>
        private static byte[] SOI = { 0xff, 0xd8 };
        /// <summary>
        /// eoi means the end flags of JPEG Format
        /// </summary>
        private static byte[] EOI = { 0xff, 0xd9 };

        /********************************************************************/
        /*******            Private variables                           *****/
        /********************************************************************/
        /// <summary>
        /// This buffer is used to receive data directly from BinaryReader
        /// </summary>
        private MemoryBuffer mBuffer;
        /// <summary>
        /// This buffer is used to make 
        /// </summary>
        private MemoryBuffer mImageBuffer;
        /// <summary>
        /// It is a stream that is used to receive data from the remote IP camera.
        /// </summary>
        private BinaryReader mReader;
    }
}
