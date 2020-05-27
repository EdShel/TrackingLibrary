using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using TrackingLibrary;
using System.Data.SqlClient;
using System.Configuration;
using System.Text;
using System.Dynamic;
using System.Linq;
using System.Data;

namespace ServerExample
{
    public class Server : IDisposable
    {
        private HttpListener listener;

        private string dbConnectionString = ConfigurationManager.ConnectionStrings["events"].ConnectionString;

        /// <summary>
        /// Stores serialization formats with their MIME types.
        /// </summary>
        private static IDictionary<string, Serialization> ContentTypes =
            new Dictionary<string, Serialization>
            {
                ["text/csv"] = Serialization.CSV,
                ["application/json"] = Serialization.Json,
                ["application/xml"] = Serialization.XML
            };

        public Task Run()
        {
            return Task.Run(() => ListenAsync(new ServerOptions()));
        }

        private void ProcessRequest(string str, string contentType)
        {
            Serialization type = ContentTypes[contentType];

            List<object> events = DeserializeEvents(str, type);

            using (var db = new SqlConnection(dbConnectionString))
            {
                try
                {
                    db.Open();


                    foreach (var eventObj in events)
                    {
                        var flat = eventObj.ToDictionary();

                        // Table is created now

                    }

                }
                finally
                {
                    db.Close();
                }
            }
        }


        /// <summary>
        /// Searches the table for this object to insert in.
        /// If there is no table found, the one will be created
        /// automagically.
        /// </summary>
        /// <param name="flat">Dictionary with keys as object's
        /// properties, values as values of these properties.
        /// All the values must return true when
        /// called on <see cref="SqlDynamic.IsSupportedType(Type)"/>.</param>
        /// <returns>Table name to which this object must be inserted.</returns>
        public string PrepareTableForInsert(IDictionary<string, object> flat)
        {
            var db = new SqlConnection(dbConnectionString);
            db.Open();
            string tableName = GetTableName(flat);
            if (!SqlDynamic.IsTableCreated(tableName, db))
            {
                var createCommand = db.CreateCommand();

                createCommand.CommandText = SqlDynamic.CreateTableCommand(tableName, 
                    flat.ToDictionary(k => k.Key, v => v.Value.GetType()));

                Console.WriteLine(createCommand.CommandText);

                createCommand.ExecuteNonQuery();
            }
            db.Close();

            return tableName;
        }

        /// <summary>
        /// Retrieves table name from the properties of the object
        /// or generates new name using object's scheme.
        /// </summary>
        /// <param name="flat">Dictionary with keys as object's
        /// properties, values as values of these properties.
        /// All the values must return true when
        /// called on <see cref="SqlDynamic.IsSupportedType(Type)"/>.</param>
        /// <returns>Table name to which this object must be inserted.</returns>
        public static string GetTableName(IDictionary<string, object> flat)
        {
            var predefName = flat.FirstOrDefault(s => s.Value is string && "EventName".Equals(s.Key));
            if (predefName.Equals(default))
            {
                return (string)predefName.Value;
            }

            var sorted = flat.OrderBy(v => v.Key);

            int hash = 0;
            foreach (var prop in sorted)
            {

                hash ^= prop.Key.FirstOrDefault() ^ (prop.Value.GetType().FullName.Length << 4);
                hash <<= 1;
            }

            return $"event{hash}";
        }



        private static List<object> DeserializeEvents(String str, Serialization type)
        {
            var list = new List<object>();
            return list;
        }

        private async Task ListenAsync(ServerOptions o)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(o.ServerUri.ToString());
            listener.Start();

            while (true)
            {
                // Wait for request
                HttpListenerContext context = await listener.GetContextAsync();

                HttpListenerRequest request = context.Request;

                Stream body = request.InputStream;

                // Read request
                StreamReader reader = new StreamReader(body, o.DataEncoding);
                var str = reader.ReadToEnd();

                ProcessRequest(str, request.ContentType);


                // Answer to the client
                HttpListenerResponse response = context.Response;
                response.StatusCode = (int)HttpStatusCode.OK;

                string responseString = ObjectSerializer.SerializeJson(
                    new { status = "OK" });
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;

                Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
