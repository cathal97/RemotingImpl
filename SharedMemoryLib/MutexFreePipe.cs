using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;

namespace SharedMemoryLib
{
    public class MutexFreePipe : SafeDisposable
    {
        protected const int MinimumBufferSize = 0x10000;
        protected readonly int MessageHeaderLength = sizeof(int);
        protected readonly int StartingOffset = sizeof(int) + sizeof(bool);
        
        public readonly string Name;
        protected readonly EventWaitHandle NewMessageSignal;
        protected SafeMemoryMappedFile Buffer;
        protected int Offset, Length;

        protected MutexFreePipe(string name, bool createBuffer)
        {
            Name = name;
            var mmFile = createBuffer
                ? MemoryMappedFile.CreateNew(name + ".0", MinimumBufferSize, MemoryMappedFileAccess.ReadWrite)
                : MemoryMappedFile.OpenExisting(name + ".0");

            Buffer = new SafeMemoryMappedFile(mmFile);

            NewMessageSignal = new EventWaitHandle(false, EventResetMode.AutoReset, name + ".signal");
            Length = Buffer.Length;
            Offset = StartingOffset;
        }

        protected override void DisposeCore()
        {
            base.DisposeCore();
            Buffer.Dispose();
            NewMessageSignal.Dispose();
        }
    }
}
