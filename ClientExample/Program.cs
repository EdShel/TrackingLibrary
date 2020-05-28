using System;
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
                // Url to send data to
                ServerUri = new Uri(@"http://localhost:8000"),

                // Where to save event batches
                EventBatchesDirectory = @"C:\Users\Admin\Desktop\batches",

                // Type of the serialization when sending events
                Serialization = Serialization.Json,

                // Size of the batch to send events to the server
                EventBatchSize = 2
            };

            // Create objects
            var obj1 = new
            {
                EventName = "EventHello",
                Hello = "Lol",
                World = 123,
            };

            var obj2 = new
            {
                EventName = "EventHello",
                Hello = "Kek",
                World = 123456,
            };

            if (EventSender.BatchEventToSend(obj1, options))
            {
                Console.WriteLine($"Server accepted events after batching {nameof(obj1)}!");
            }

            if (EventSender.BatchEventToSend(obj2, options))
            {
                Console.WriteLine($"Server accepted events after batching {nameof(obj2)}!");
            }

            var complexOne = new
            {
                EventName = "EventComplex",
                ObjectProp = new
                {
                    DateOfTheProp = DateTime.Now,
                    CodeOfTheProp = 999
                },
                Integers = new int[] { 5, 6, 7 }
            };

            EventSender.DefaultOptions = options;

            if (EventSender.SendEventNow(complexOne))
            {
                Console.WriteLine($"Server accepted when sending {nameof(complexOne)}!");
            }

            Console.WriteLine("Done");
        }
    }
}
