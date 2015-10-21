using System;
using System.IO;
using System.Text;

    /// <summary>
    /// This is a custom stream implementation that inserts a set of bytes into the byte stream.
    /// </summary>
    public class PostRenderModifier : Stream
    {

        // Passed in by the HTTPModule for processing
        public string StringToInsert = string.Empty;

        // Set of bytes, where the StringToInsert will be inserted
        private byte[] m_InsertionPointUTF8Bytes;

        // The output stream that will include inserted data
        private Stream m_ResponseOutputStream;

        /// <summary>
        /// Consturctor
        /// </summary>
        /// <param name="OutputStream">The response output stream</param>
        /// <param name="InsertionPoint">Token where the data will be inserted</param>
        public PostRenderModifier(Stream OutputStream, string InsertionPoint)
        {
            m_InsertionPointUTF8Bytes = Encoding.UTF8.GetBytes(InsertionPoint);
            m_ResponseOutputStream = OutputStream;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="OutputStream">The response output stream</param>
        /// <param name="InsertionPointUTF8">Token where the data will be inserted</param>
        public PostRenderModifier(Stream OutputStream, byte[] InsertionPointUTF8)
        {
            m_InsertionPointUTF8Bytes = InsertionPointUTF8;
            m_ResponseOutputStream = OutputStream;
        }


        // The following members of Stream must be overridden. Simply delegate to the contained stream
        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanWrite { get { return true; } }
        public override long Length { get { return 0; } }
        public override long Position { get { return m_ResponseOutputStream.Position; } set { m_ResponseOutputStream.Position = value; } }
        public override long Seek(long offset, System.IO.SeekOrigin direction) { return m_ResponseOutputStream.Seek(offset, direction); }
        public override void SetLength(long length) { m_ResponseOutputStream.SetLength(length); }
        public override void Close() { m_ResponseOutputStream.Close(); }
        public override void Flush() { m_ResponseOutputStream.Flush(); }
        public override int Read(byte[] buffer, int offset, int count) { return m_ResponseOutputStream.Read(buffer, offset, count); }

        /// <summary>
        /// Writes modified data to response output stream
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            // Validate arguments
            if (null == buffer)
                throw new ArgumentNullException("buffer", "buffer cannot be null");
            if (0 > offset)
                throw new ArgumentOutOfRangeException("offset", "offset cannot be negative");
            if (0 > count)
                throw new ArgumentOutOfRangeException("count", "offset cannot be negative");

            // Modify the stream if we have StringToInsert
            if ((0 < count) &&
               (0 != StringToInsert.Length))
            {
                //Find if the current buffer contains the set of bytes used as an insertion token
                int insertionTokenIndex = PostRenderModifier.LastIndexOf(buffer, offset, count, this.m_InsertionPointUTF8Bytes);

                if (insertionTokenIndex < 0)
                {
                    // insertion token was not found, output the original bytes to the response stream
                    m_ResponseOutputStream.Write(buffer, offset, count);
                }
                else
                {
                    // Insertion token was found.  the steps are:
                    // 1) write bytes before the token
                    // 2) write the StringToInsert
                    // 3) write bytes including and after the token

                    if (insertionTokenIndex > 0)
                    {
                        // 1. write bytes before the impression token
                        m_ResponseOutputStream.Write(buffer, offset, insertionTokenIndex - offset);
                    }

                    // 2. write the StringToInsert
                    // convert the string to bytes for output
                    byte[] insertionStringUTF8Bytes = Encoding.UTF8.GetBytes(StringToInsert);
                    m_ResponseOutputStream.Write(insertionStringUTF8Bytes, 0, insertionStringUTF8Bytes.Length);

                    if (count - insertionTokenIndex > 0)
                    {
                        // 3. write bytes including and after the insertion token
                        m_ResponseOutputStream.Write(buffer, insertionTokenIndex, count - insertionTokenIndex);
                    }
                }
            }
            else // Do nothing and delegate
                m_ResponseOutputStream.Write(buffer, offset, count);
        }


        /// <summary>
        /// Calculates the index of the last occurrence of a series of bytes in another series of bytes.
        /// </summary>
        /// <param name="buffer">The bytes to search</param>
        /// <param name="bufferStartIdx">The offset where the buffer starts</param>
        /// <param name="bufferLength">The size of the data in the buffer</param>
        /// <param name="findSeries">The series of bytes to locate in the buffer</param>
        /// <returns>The index of the last occurrence of the series of bytes, or -1 if it could not be found</returns>
        private static int LastIndexOf(byte[] buffer, int bufferStartIdx, int bufferLength, byte[] findSeries)
        {
            bool found = false; // true if we found the series in the buffer
            int findIdx; // index into the findSeries
            int srcLocalIdx; // used so we don't mess up the index used by the outer loop
            int srcIdx = bufferStartIdx + bufferLength; // the index is really 1 less, but the first while loop condition does the necessary decrement
            int srcLeftMostSearchIdx; // the left most index in the buffer were a match could happen

            // Loop backwards through the buffer.  For each index in the buffer, loop backwards through the findSeries.
            // If the series does not match, exit the inner loop and try again with the next backwards starting point in 
            // the buffer.  If the series matches, set found to true so we exit out of the loop.  Return the index.

            srcLeftMostSearchIdx = bufferStartIdx + findSeries.Length;

            while (!found && srcIdx-- >= srcLeftMostSearchIdx)
            {
                srcLocalIdx = srcIdx;
                for (findIdx = findSeries.Length - 1; findIdx >= 0; --findIdx)
                {
                    if (findSeries[findIdx] == buffer[srcLocalIdx])
                    {
                        if (0 == findIdx)
                        {
                            found = true;
                        }
                        else
                        {
                            --srcLocalIdx;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

            }
            return found ? srcIdx - findSeries.Length + 1 : -1;
        }
    }
