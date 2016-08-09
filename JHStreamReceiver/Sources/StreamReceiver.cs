using System.Diagnostics;
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
            Debug.Assert(IsConnected == false && url != null);

            // make web request. It's like a connection trial.
            WebRequest webRequest = WebRequest.Create(url);
            WebResponse webResponse = null;

            // it can take some time..... 
            webResponse = await webRequest.GetResponseAsync();
            
            if(webResponse == null)
            {
                // WebException has occured because it can't connect to Ip camera
                webResponse.Dispose();
                webResponse = null;
                webRequest = null;        
                
                // just return false
                return false;
            }

            // get the stream from WebResponse
            Stream stream = webResponse.GetResponseStream();
            mReader = new BinaryReader(stream);
            
            // connection success
            return true;
        }

        /// <summary>
        /// Get Frame(byte array) asynchronously
        /// </summary>
        /// <returns> Frame(byte array) </returns>
        public async Task<byte[]> GetFrameAsByteArrayAsync()
        {
            Debug.Assert(IsConnected == true);
            byte[] retByteArray = null;
            retByteArray = await Task<byte[]>.Factory.StartNew(() =>
            {
                byte[] tempByteArray = null;
                tempByteArray = GetFrameAsByteArray();
                return tempByteArray;
            });
            return retByteArray;
        }

        /// <summary>
        /// Get Frame(byte array) synchronously
        /// </summary>
        /// <returns> Frame(byte array) </returns>
        public byte[] GetFrameAsByteArray()
        {
            Debug.Assert(IsConnected == true);
            byte[] retByteArray = null;
            getFrameBytes(out retByteArray);
            return retByteArray;
        }
                
        /// <summary>
        /// Terminates this object 
        /// </summary>
        public void Disconnect()
        {
            Debug.Assert(IsConnected == true);
            // clear all buffers
            mBuffer.SetOffsetTo(0);
            mImageBuffer.SetOffsetTo(0);

            // dispose BinaryReader 
            mReader?.Close();
            mReader?.Dispose();
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
            Debug.Assert(IsConnected == true);
            int startFlagLocation = -1;
            int endFlagLocation = -1;
            int receivedImageSizeInBytes = 0;

            while (true)
            {
                // read the stream data from BinaryReader and save these data to mBuffer up to 100000(97KB)
                mBuffer.AppendDataFrom(mReader, 100000);

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
                    receivedImageSizeInBytes = endFlagLocation - startFlagLocation + 2;
                    mImageBuffer.Append(mBuffer.Buffer, startFlagLocation, receivedImageSizeInBytes);

                    // set buffers to origin (set buffer's interal count variable to zero)
                    // These will be used again to receive next call.
                    mImageBuffer.SetOffsetTo(0);
                    mBuffer.SetOffsetTo(0);

                    break;
                }
            }
            // outputFrame will point to mImageBuffer.Buffer
            outputFrame = mImageBuffer.Buffer;
        }

        /********************************************************************/
        /*******            Private CONSTANTS                           *****/
        /********************************************************************/
        // SOI = start flags of JPEG format 
        // EOI = end flags of JPEG format
        private static byte[] SOI = { 0xff, 0xd8 };
        private static byte[] EOI = { 0xff, 0xd9 };

        /********************************************************************/
        /*******            Private variables                           *****/
        /********************************************************************/

        // mBuffer is a buffer that received data directly from mReader
        private MemoryBuffer mBuffer;
        // mImageBuffer is buffer that stores complete a single-jpeg binary data.
        private MemoryBuffer mImageBuffer;
        /// <summary>
        /// It is a stream that is used to receive data from the remote IP camera.
        /// </summary>
        private BinaryReader mReader;
    }
}
