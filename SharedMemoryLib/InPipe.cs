using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SharedMemoryLib
{
	class InPipe : MutexFreePipe
	{
		int _lastMessageProcessed;
		int _bufferCount;

		readonly Action<byte[]> _onMessage;

		public InPipe(string name, bool createBuffer, Action<byte[]> onMessage) : base(name, createBuffer)
		{
			_onMessage = onMessage;
			new Thread(Go).Start();
		}

		void Go()
		{
			int spinCycles = 0;
			while (true)
			{
				int? latestMessageID = GetLatestMessageID();
				if (latestMessageID == null) return;            // We've been disposed.

				if (latestMessageID > _lastMessageProcessed)
				{
					Thread.MemoryBarrier();    // We need this because of lock-free implementation						
					byte[] msg = GetNextMessage();
					if (msg == null) return;
					if (msg.Length > 0 && _onMessage != null) _onMessage(msg);       // Zero-length msg will be a buffer continuation 
					spinCycles = 1000;
				}
				if (spinCycles == 0)
				{
					NewMessageSignal.WaitOne();
					if (IsDisposed) return;
				}
				else
				{
					Thread.MemoryBarrier();    // We need this because of lock-free implementation		
					spinCycles--;
				}
			}
		}

		unsafe int? GetLatestMessageID()
		{
			lock (DisposeLock)
				lock (Buffer.DisposeLock)
					return IsDisposed || Buffer.IsDisposed ? (int?)null : *((int*)Buffer.Pointer);
		}

		unsafe byte[] GetNextMessage()
		{
			_lastMessageProcessed++;

			lock (DisposeLock)
			{
				if (IsDisposed) return null;

				lock (Buffer.DisposeLock)
				{
					if (Buffer.IsDisposed) return null;

					byte* offsetPointer = Buffer.Pointer + Offset;
					var msgPointer = (int*)offsetPointer;

					int msgLength = *msgPointer;

					Offset += MessageHeaderLength;
					offsetPointer += MessageHeaderLength;

					if (msgLength == 0)
					{
						Buffer.Accessor.Write(4, true);   // Signal that we no longer need file				
						Buffer.Dispose();
						string newName = Name + "." + ++_bufferCount;
						Buffer = new SafeMemoryMappedFile(MemoryMappedFile.OpenExisting(newName));
						Offset = StartingOffset;
						return new byte[0];
					}

					Offset += msgLength;

					//MMF.Accessor.ReadArray (Offset, msg, 0, msg.Length);    // too slow			
					var msg = new byte[msgLength];
					Marshal.Copy(new IntPtr(offsetPointer), msg, 0, msg.Length);
					return msg;
				}
			}
		}

		protected override void DisposeCore()
		{
			NewMessageSignal.Set();
			base.DisposeCore();
		}
	}
}
}
