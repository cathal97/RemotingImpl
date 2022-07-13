using System;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace SharedMemoryReader
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.Sleep(500);
            using (var mmf = MemoryMappedFile.OpenExisting("Shm Demo"))
            using (var accessor = mmf.CreateViewAccessor())
            {
                var sharedValue = accessor.ReadInt32(500);
                Console.WriteLine(sharedValue);
                Console.ReadLine();
            } 
        }
    }
}
