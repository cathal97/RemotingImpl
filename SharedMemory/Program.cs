using System;
using System.IO.MemoryMappedFiles;

namespace SharedMemoryWriter
{
    class Program
    {
        static void Main(string[] args)
        {
            /*unsafe
            {
                using (var mmf = MemoryMappedFile.CreateNew("Shm Demo", 1000))
                using (var accessor = mmf.CreateViewAccessor())
                {
                    byte* pointer = (byte*)accessor.SafeMemoryMappedViewHandle.DangerousGetHandle().ToPointer();
                    pointer += 500;
                    int* intPointer = (int*)pointer;

                    *intPointer = 69420;
                    Console.ReadLine();
                }*/
            //}

            string greet = "Hello";
            string copy = greet;
            greet = "World";
            bool test = Object.ReferenceEquals(greet, copy);
            Console.WriteLine($"{copy} {test}");
            Console.WriteLine();

            var val = -10;
            var bytes = BitConverter.GetBytes(val);
            Console.WriteLine(val);
        }
    }
}
