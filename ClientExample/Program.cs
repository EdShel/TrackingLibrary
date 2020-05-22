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

            // Create options object
            var options = new EventSenderOptions
            {
                ServerUri = new Uri(@"http://localhost:8000"),
                EventBatchesDirectory = @"C:\Users\Admin\Desktop\batches",
                Serialization = Serialization.XML
            };

            // Create an object
            var obj = new
            {
                Hello = "Lol",
                World = 123,
            };

            if (EventSender.BatchEventToSend(obj, options))
            {
                Console.WriteLine("Server accepted!");
            }

            Console.WriteLine("Done");
            Console.Read();
        }
    }
}
