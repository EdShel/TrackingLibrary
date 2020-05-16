using System;
using System.IO;
using TrackingLibrary;

namespace ClientExample
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create an object
            var obj = new
            {
                Hello = "Lol",
                World = 123,
                Lol = new float[] { 1.2f, 2.4f }
            };

            // Binary serialiation
            using (var str = new FileStream(@"C:\Users\Admin\Desktop\test.txt", FileMode.Create, FileAccess.ReadWrite))
            {
                using (var wr = new BinaryWriter(str))
                {
                    BinarySerializator.Serialize(wr, obj);
                }
            }

            dynamic deserialized;

            // Binary deserialization
            using (var str = new FileStream(@"C:\Users\Admin\Desktop\test.txt", FileMode.Open, FileAccess.ReadWrite))
            {
                using (var wr = new BinaryReader(str))
                {
                    deserialized = BinarySerializator.Deserialize(wr);
                }
            }

            Console.WriteLine(deserialized.Hello);
            Console.Read();
        }
    }
}
