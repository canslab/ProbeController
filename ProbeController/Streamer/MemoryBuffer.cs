using System;
using System.IO;
using System.Threading.Tasks;

namespace Streamer
{
    /// <summary>
    /// It encapsulates memory buffer
    /// It is very easy to use. 
    /// </summary>
    class MemoryBuffer
    {
        /********************************************************************/
        /*******            Constructrs                                 *****/
        /********************************************************************/
        /// <summary>
        /// Creates memory object and its initial size is given by the first parameter.
        /// </summary>
        /// <param name="initialCapacity"> initial capacity in bytes</param>
        public MemoryBuffer(int initialCapacity)
        {
            mBuffer = new byte[initialCapacity];
            mCount = 0;
        }
        
        /********************************************************************/
        /*******            Public Methods                              *****/
        /********************************************************************/
        /// <summary>
        /// It appends data from BinaryReader up to #(nSize) bytes
        /// 
        /// </summary>
        /// <param name="reader"> BinaryReader that will be read up to (nSize) bytes</param>
        /// <param name="nSize"> total # of bytes to be read from BinaryReader and appended </param>
        /// <returns> If either reader is null or nSize is negative, it returns false </returns>
        public bool AppendDataFromBinaryReader(BinaryReader reader, int nSize)
        {
            if (reader == null || nSize < 0)
            {
                return false;
            }

            int moreRequiredSize = nSize - RemainingSpace;
            if (moreRequiredSize > 0)
            {
                Array.Resize(ref mBuffer, Capacity + moreRequiredSize);
            }

            int readSize = reader.Read(mBuffer, mCount, nSize);
            mCount += readSize;

            return true;
        }
        /// <summary>
        /// It appends data from sourceBuffer(index starts from sourceBufferFromIndex) up to #(totalLength) bytes
        /// 
        /// </summary>
        /// <param name="sourceBuffer"> Source Buffer </param>
        /// <param name="sourceBufferFromIndex"> The start index of sourceBuffer </param>
        /// <param name="totalLength"> The total # of bytes to be appended </param>
        public void Append(byte[] sourceBuffer, int sourceBufferFromIndex, int totalLength)
        {
            int moreRequiredSize = totalLength - RemainingSpace;

            if (moreRequiredSize > 0)
            {
                Array.Resize(ref mBuffer, Capacity + moreRequiredSize);
            }
            Array.Copy(sourceBuffer, sourceBufferFromIndex, mBuffer, mCount, totalLength);
            mCount += totalLength;
        }
        /// <summary>
        /// This clears buffer, internally it doesn't mainpulate buffer directly, it just set count variable to 0
        /// In the other words, this is shallow deletion..
        /// </summary>
        public void ClearContents()
        {
            mCount = 0;
        }
  
        /// <summary>
        /// It finds given pattern within this buffer
        /// 
        /// ex) pattern = {0xff, 0xfd8}, this buffer = {0xaa, 0xbb, 0xff, 0xd8, 0xd9}
        ///     it returns 2
        /// 
        /// </summary>
        /// <param name="from"> Search task starts from this number </param>
        /// <param name="pattern"> The pattern to be found </param>
        /// <returns> The found index of the pattern. If this function fails, it returns -1 </returns>
        public int FindPattern(int from, byte[] pattern)
        {
            int index = -1;

            for (int i = from; i < mCount - 1; ++i)
            {
                if (mBuffer[i] == pattern[0])
                {
                    bool bFound = false;
                    for (int j = 0; j < pattern.Length - 1; ++j)
                    {
                        if (mBuffer[i + j + 1] == pattern[1 + j])
                        {
                            bFound = true;
                        }
                        else
                        {
                            bFound = false;
                            break;
                        }
                    }
                    if (bFound == true)
                    {
                        index = i;
                        break;
                    }
                }
            }

            return index;
        }

        /********************************************************************/
        /*******            Properties                                  *****/
        /********************************************************************/
        /// <summary>
        /// It indicates how many bytes are in this buffer 
        /// </summary>
        public int Count { get { return mCount; } }
        /// <summary>
        /// It indicates the total number of bytes this buffer can store.
        /// </summary>
        public int Capacity { get { return mBuffer.Length; } }
        /// <summary>
        /// It indicates the remaining space in bytes
        /// </summary>
        public int RemainingSpace { get { return mBuffer.Length - mCount; } }
        /// <summary>
        /// This is the internal buffer reference. 
        /// </summary>
        public byte[] Buffer { get { return mBuffer; } }

        /********************************************************************/
        /*******            Private variables                           *****/
        /********************************************************************/
        private byte[] mBuffer;
        private int mCount;
    }
}
