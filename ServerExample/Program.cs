using System;
using System.Configuration;

namespace ServerExample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Server started");

            var options = new ServerOptions("http://localhost:8000")
            {
                EventPrimaryKeyColumn = "Id",
            };

            string dbConnectionStr = ConfigurationManager.ConnectionStrings["events"].ConnectionString;

            Server server = new Server(dbConnectionStr, options);

            server.Run().Wait();
        }
    }
}
