using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SharedMemoryLib
{
    public class OutPipe : MutexFreePipe
    {
        int _messageNumber;
        int _bufferCount;
        readonly List<SafeMemoryMappedFile> _oldBuffers = new List<SafeMemoryMappedFile>();
        public int PendingBuffers => _oldBuffers.Count;

        public OutPipe(string name, bool createBuffer) : base(name, createBuffer)
        {

        }

        public unsafe void Write(byte[] data)
        {
            lock (DisposeLock)
            {
                AssertSafe();
                if (data.Length > Length - Offset - 8)
                {
                    // not enough space left in the shared memory buffer to write the message
                    WriteContinuation(data.Length);
                }
                WriteMessage(data);
                NewMessageSignal.Set(); // signal reader that a message is available
            }
        }

        unsafe void WriteMessage(byte[] block)
        {
            byte* ptr = Buffer.Pointer;
            //Console.WriteLine($"ptr Addr = {*ptr}");
            byte* offsetPointer = ptr + Offset;

            var msgPointer = (int*)offsetPointer;
            *msgPointer = block.Length;

            Offset += MessageHeaderLength;
            offsetPointer += MessageHeaderLength;

            if(block != null && block.Length > 0)
            {
                Marshal.Copy(block, 0, new IntPtr(offsetPointer), block.Length);
                Offset += block.Length;
            }

            //Write the latest message number to the start of the buffer:
            int* iptr = (int*)ptr;
            *iptr = ++_messageNumber;


        }

        private void WriteContinuation(int messageSize)
        {
            string newName = Name + "." + ++_bufferCount;
            int newLength = Math.Max(messageSize * 10, MinimumBufferSize);
            var newFile = new SafeMemoryMappedFile(MemoryMappedFile.CreateNew(newName, newLength, MemoryMappedFileAccess.ReadWrite));

            //Write a message to the old buffer indicating the address of the new buffer:
            WriteMessage(new byte[0]);

            // Keep the old buffer alive until the reader has indicated that its seen it:
            _oldBuffers.Add(Buffer);

            // Make the new buffer current:
            Buffer = newFile;
            Length = newFile.Length;
            Offset = StartingOffset;

            //release old buffers that have been read;
            foreach(var buffer in _oldBuffers.Take(_oldBuffers.Count - 1).ToArray())
            {
                lock (DisposeLock)
                {
                    if (buffer.Accessor.ReadBoolean(4))
                    {
                        _oldBuffers.Remove(buffer);
                        buffer.Dispose();
                        // cleaned file

                    }
                }
            }
        }

        protected override void DisposeCore()
        {
            base.DisposeCore();
            foreach (var buffer in _oldBuffers) buffer.Dispose();
        }
    }
}
