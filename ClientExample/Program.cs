using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using TrackingLibrary;

namespace ClientExample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting...");

            var options = new EventSenderOptions
            {
                EventBatchesDirectory = @"C:\Users\Admin\Desktop\batches",
                Serialization = Serialization.XML
            };

            // Create an object
            var obj = new
            {
                Hello = "Lol",
                World = 123,
            };

            EventSender.BatchEvent(obj, options);

            //BinarySerializationExample(obj);

            Console.WriteLine("Done");
            Console.Read();
        }

        private static void BinarySerializationExample(object obj)
        {

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
        }
    }
}
