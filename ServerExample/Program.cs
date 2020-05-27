using System;
using System.Linq;
using TrackingLibrary;

namespace ServerExample
{
    class Program
    {
        static void Main(string[] args)
        {
            var d = new
            {
                Name = "Lol",
                Fuck = 1,
            };

            var x = new object[] { d, d, d };

            Console.WriteLine(ObjectSerializer.SerializeXML(x));
            var o = SqlDynamic.Flatten(ObjectSerializer.DeserializeJson(ObjectSerializer.SerializeJson(d)));

            foreach(var p in o)
            {
                Console.WriteLine($"{p.Key} = {p.Value}");
            }

            Console.WriteLine(o);
            //Server server = new Server();

            //server.PrepareTableForInsert(d.ToDictionary());

            //server.Run();

            // Do another stuff
        }
    }
}
