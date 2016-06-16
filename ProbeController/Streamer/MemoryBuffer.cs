using System;
using System.IO;
using System.Threading.Tasks;

namespace Streamer
{
    class MemoryBuffer
    {
        /********************************************************************/
        /*******            Constructrs                                 *****/
        /********************************************************************/
        public MemoryBuffer(int capacity)
        {
            mBuffer = new byte[capacity];
            mCount = 0;
        }
        /// <summary>
        /// CAUTION : Shallow Copy... Do NOT EXPECT DEEP COPY
        /// </summary>
        /// <param name="sourceMemory"></param>
        /// <param name="count"></param>
        public MemoryBuffer(byte[] sourceMemory, int count)
        {
            mBuffer = sourceMemory;
            mCount = count;
        }

        /********************************************************************/
        /*******            Public Methods                              *****/
        /********************************************************************/
        public bool AppendDataFromBinaryReader(BinaryReader reader, int nSize)
        {
            bool retResult = false;

            if (reader == null)
            {
                retResult = false;
            }
            int moreRequiredSize = nSize - RemainingSpace;
            if (moreRequiredSize > 0)
            {
                Array.Resize<byte>(ref mBuffer, Capacity + moreRequiredSize);
            }

            int readSize = reader.Read(mBuffer, mCount, nSize);
            mCount += readSize;

            return retResult;
        }
        public void Append(byte[] target, int targetStartIndex, int size)
        {
            int moreRequiredSize = size - RemainingSpace;

            if (moreRequiredSize > 0)
            {
                Array.Resize<byte>(ref mBuffer, Capacity + moreRequiredSize);
            }
            Array.Copy(target, targetStartIndex, mBuffer, mCount, size);
            mCount += size;
        }
        public void ClearContents()
        {
            mCount = 0;
        }
        public void WriteValueToRange(int fromInclusive, int toExclusive, byte value)
        {
            Parallel.For(fromInclusive, toExclusive, (i) =>
            {
                mBuffer[i] = 0;
            });
        }
        public void MoveWithInBufferAndChangeCount(int fromIndex, int fromLength, int toIndex)
        {
            MoveWithinBuffer(fromIndex, fromLength, toIndex);
            mCount = fromLength;
        }
        public void MoveWithinBuffer(int fromIndex, int fromLength, int toIndex)
        {
            Parallel.For(0, fromLength, (i) =>
            {
                mBuffer[toIndex + i] = mBuffer[fromIndex + i];
            });
        }
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
        public int Count { get { return mCount; } }
        public int Capacity { get { return mBuffer.Length; } }
        public int RemainingSpace { get { return Capacity - Count; } }
        public byte[] Buffer { get { return mBuffer; } }

        /********************************************************************/
        /*******            Private variables                           *****/
        /********************************************************************/
        private byte[] mBuffer;
        private int mCount;
    }
}
