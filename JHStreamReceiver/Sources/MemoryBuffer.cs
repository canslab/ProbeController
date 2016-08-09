using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace JHStreamReceiver
{
    /// <summary>
    /// It wraps memory buffer
    /// It is so easy to use that user doesn't have to worr about details.
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
            Debug.Assert(initialCapacity > 0);
            mBuffer = new byte[initialCapacity];
            mBufferOffset = 0;
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
        public void AppendDataFrom(BinaryReader reader, int nSize)
        {
            Debug.Assert(reader != null && nSize >=0 && reader.BaseStream.CanRead == true);

            int moreRequiredSize = nSize - RemainingSpace;
            if (moreRequiredSize > 0)
            {
                Array.Resize(ref mBuffer, Capacity + moreRequiredSize);
            }

            int readSize = reader.Read(mBuffer, mBufferOffset, nSize);
            mBufferOffset += readSize;
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
            Debug.Assert(sourceBuffer != null && sourceBufferFromIndex >= 0);
            int moreRequiredSize = totalLength - RemainingSpace;

            if (moreRequiredSize > 0)
            {
                Array.Resize(ref mBuffer, Capacity + moreRequiredSize);
            }
            Array.Copy(sourceBuffer, sourceBufferFromIndex, mBuffer, mBufferOffset, totalLength);
            mBufferOffset += totalLength;
        }

        /// <summary>
        /// Set Offset To offset parameter
        /// </summary>
        public void SetOffsetTo(int offset)
        {
            Debug.Assert(offset >= 0);
            mBufferOffset = offset;
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
            Debug.Assert(from >= 0 && pattern != null && pattern.Length > 0);
            int index = -1;

            for (int i = from; i < mBufferOffset - 1; ++i)
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
        /// It indicates the total number of bytes this buffer can store.
        /// </summary>
        public int Capacity { get { return mBuffer.Length; } }
        /// <summary>
        /// It indicates the remaining space in bytes
        /// </summary>
        public int RemainingSpace { get { return mBuffer.Length - mBufferOffset; } }
        /// <summary>
        /// This is the internal buffer reference. 
        /// </summary>
        public byte[] Buffer { get { return mBuffer; } }

        /********************************************************************/
        /*******            Private variables                           *****/
        /********************************************************************/
        private byte[] mBuffer;
        private int mBufferOffset;
    }
}
