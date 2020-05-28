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
    public class Server
    {
        /// <summary>
        /// Options for this server to listen to.
        /// </summary>
        private ServerOptions options;

        /// <summary>
        /// Object for listening HTTP requests.
        /// </summary>
        private HttpListener listener;

        /// <summary>
        /// Connection string for the db where to add events.
        /// </summary>
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

        /// <summary>
        /// Creates new server-event listener with the given options.
        /// </summary>
        /// <param name="sqlDbConnectionString">SQL database connection string
        /// where this server must create tables for events and insert them.</param>
        /// <param name="opt">Options for the server.</param>
        public Server(string sqlDbConnectionString, ServerOptions opt)
        {
            if (string.IsNullOrWhiteSpace(sqlDbConnectionString))
            {
                throw new ArgumentNullException(nameof(sqlDbConnectionString));
            }
            if (opt == null)
            {
                throw new ArgumentNullException(nameof(opt));
            }

            dbConnectionString = sqlDbConnectionString;
            options = opt;
        }

        /// <summary>
        /// Makes server listen to the Url recorded in <see cref="ServerOptions.ServerUri"/>.
        /// The operation is performed into another thread.
        /// </summary>
        /// <returns>Task associated with this thread.</returns>
        public Task Run()
        {
            return Task.Run(ListenAsync);
        }

        /// <summary>
        /// Procedure of processing the given HTTP input.
        /// </summary>
        /// <param name="str">Body of the HTTP request.</param>
        /// <param name="contentType">MIME type of the event.
        /// You can find them in <see cref="Server.ContentTypes"/>.</param>
        private void ProcessRequest(string str, string contentType)
        {
            // Find serialization for this MIME type
            Serialization type = ContentTypes[contentType];

            // Get all the events
            List<object> events = DeserializeEvents(str, type);

            // Connect to the db
            using (var db = new SqlConnection(dbConnectionString))
            {
                try
                {
                    db.Open();

                    foreach (var eventObj in events)
                    {
                        var flat = eventObj.ToDictionary();

                        // Find the table for this object
                        string tableName = PrepareTableForInsert(flat);

                        // Get insert command
                        var insert = SqlDynamic.GetInsert(tableName, flat, db);

                        insert.ExecuteNonQuery();

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

            // Find out table name for this event
            string tableName = GetTableName(flat);

            // If table is not created yet
            if (!SqlDynamic.IsTableCreated(tableName, db))
            {
                // Create this table
                var createCommand = db.CreateCommand();

                createCommand.CommandText = SqlDynamic.CreateTableCommand(
                    name: tableName, 
                    idCol: options.EventPrimaryKeyColumn,
                    scheme: flat.ToDictionary(k => k.Key, v => v.Value.GetType()));

                createCommand.ExecuteNonQuery();

                // Tell that the table was tried to create
                System.Diagnostics.Debug.WriteLine(createCommand.CommandText);

            }
            db.Close();

            return tableName;
        }

        /// <summary>
        /// Retrieves table name from the properties of the object
        /// or generates new name using object's scheme.
        /// The first-priority names are written in the <see cref="ServerOptions.EventPropertiesForSqlTable"/>
        /// </summary>
        /// <param name="flat">Dictionary with keys as object's
        /// properties, values as values of these properties.
        /// All the values must return true when
        /// called on <see cref="SqlDynamic.IsSupportedType(Type)"/>.</param>
        /// <returns>Table name to which this object must be inserted.</returns>
        public string GetTableName(IDictionary<string, object> flat)
        {
            // Try to find this event in predefined table name properties
            var predefName = flat.FirstOrDefault(
                s => s.Value is string && options.EventPropertiesForSqlTable.Contains(s.Key));

            // If found
            if (!predefName.Equals(default))
            {
                return (string)predefName.Value;
            }

            // Not found, generate new name
            var sorted = flat.OrderBy(v => v.Key);

            int hash = 0;
            foreach (var prop in sorted)
            {

                hash ^= prop.Key.FirstOrDefault() ^ (prop.Value.GetType().FullName.Length << 4);
                hash <<= 1;
            }

            return $"event{hash}";
        }

        /// <summary>
        /// Deserializes events collection.
        /// </summary>
        /// <param name="str">Collection of the serialization type.</param>
        /// <param name="type">Type of the serialization.</param>
        /// <returns>List of <see cref="ExpandoObject"/>.</returns>
        private static List<object> DeserializeEvents(String str, Serialization type)
        {
            var list = ObjectSerializer.Deserialize(str, type).ToList();
            return list;
        }

        /// <summary>
        /// Listens to the URL and calls processing of the events.
        /// </summary>
        private async Task ListenAsync()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(options.ServerUri.ToString());
            listener.Start();

            // Listen while there is an electricity
            while (true)
            {
                // Wait for request
                HttpListenerContext context = await listener.GetContextAsync();

                // Get it
                HttpListenerRequest request = context.Request;

                // Read request
                Stream body = request.InputStream;
                StreamReader reader = new StreamReader(body, options.DataEncoding);
                var str = reader.ReadToEnd();

                // Answer to the client 
                HttpListenerResponse response = context.Response;
                string responseString = null;

                try
                {
                    // Process it
                    ProcessRequest(str, request.ContentType);

                    // Put here a little present for him with "OK" message.
                    response.StatusCode = (int)HttpStatusCode.OK;
                    responseString = "OK";
                }
                catch(Exception ex)
                {
                    response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                    responseString = ex.Message;
                }
                finally
                {
                    // Create response body
                    var responseBody = ObjectSerializer.SerializeJson(
                            new { status = responseString });
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;

                    // Send it
                    Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }

            }
        }
    }
}
