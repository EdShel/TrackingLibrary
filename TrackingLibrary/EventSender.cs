using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;

namespace TrackingLibrary
{
    /// <summary>
    /// Provides service for sending and batching events.
    /// </summary>
    public static class EventSender
    {
        /// <summary>
        /// Http method to use to send events to the server.
        /// </summary>
        private const string USED_HTTP_METHOD = "POST";

        /// <summary>
        /// Stores serialization formats with their MIME types.
        /// </summary>
        private static IDictionary<Serialization, string> ContentTypes =
            new Dictionary<Serialization, string>
            {
                [Serialization.CSV] = "text/csv",
                [Serialization.Json] = "application/json",
                [Serialization.XML] = "application/xml"
            };

        /// <summary>
        /// Default options to use when there are no options given.
        /// </summary>
        public static EventSenderOptions DefaultOptions { set; get; } = new EventSenderOptions();

        public static bool BatchEvent(object eventObj, EventSenderOptions options)
        {
            // Get batched files
            List<FileInfo> batchedEventsFiles = Directory
                .GetFiles(options.EventBatchesDirectory)
                .Select(filePath => new FileInfo(filePath))
                .ToList();

            // If there is an overflow of events (ready to send)
            if (batchedEventsFiles.Count + 1 >= options.EventBatchSize)
            {
                // Deserialize binary files & append to them new event
                List<object> events = batchedEventsFiles
                    .Select(file =>
                    {
                        using (var eventFileReader =
                            new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                        {
                            using (var binaryReader = new BinaryReader(eventFileReader))
                            {
                                return binaryReader.Deserialize();
                            }
                        }
                    })
                    .Union(new[] { eventObj })
                    .ToList();

                // Serialize this object
                string serialized = ObjectSerializer.Serialize(events, options.Serialization);

                // Send data
                bool sent = SendDataToServer(
                    uri: options.ServerUri,
                    whatToSend: options.DataEncoding.GetBytes(serialized),
                    mimeType: ContentTypes[options.Serialization]);

                // If the server accepted the events
                if (sent)
                {
                    // Delete the files as they ain't needed
                    batchedEventsFiles
                        .ForEach(file => File.Delete(file.FullName));

                    return true;
                }

                // Delete the oldest events to fit into the required size
                while (batchedEventsFiles.Count + 1 >= options.MaxOfflineSavedEvents)
                {
                    var theOldestFile = batchedEventsFiles
                        .Aggregate((leastRecent, x) =>
                            leastRecent == null ||
                                x.CreationTime < leastRecent.CreationTime
                                ? x
                                : leastRecent);

                    batchedEventsFiles.Remove(theOldestFile);

                    File.Delete(theOldestFile.FullName);
                }
            }

            // Write this event to a new file
            var eventFile = Path.Combine(
                path1: options.EventBatchesDirectory, 
                path2: Guid.NewGuid().ToString());

            using (var wr = new BinaryWriter(File.Open(eventFile, FileMode.CreateNew)))
            {
                wr.Serialize(eventObj);
            }

            return false;
        }

        /// <summary>
        /// Sends the event object directly to the server using default
        /// EventSenderOptions.
        /// </summary>
        /// <returns>Whether the server responded OK (200).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SendEventNow(object eventObj)
        {
            return SendEventNow(eventObj, DefaultOptions);
        }

        /// <summary>
        /// Sends the event object directly to the server using specific
        /// EventSenderOptions.
        /// </summary>
        /// <returns>Whether the server responded OK (200).</returns>
        public static bool SendEventNow(object eventObj, EventSenderOptions options)
        {
            Serialization serialization = options.Serialization;
            string serialized = ObjectSerializer.Serialize(eventObj, serialization);

            return SendDataToServer(options.ServerUri, options.DataEncoding
                .GetBytes(serialized), ContentTypes[serialization]);
        }

        /// <summary>
        /// Sends bytes to the server and returns true if the server
        /// responded with OK (200).
        /// </summary>
        /// <param name="uri">Server uri.</param>
        /// <param name="whatToSend">Bytes to send.</param>
        /// <param name="mimeType">MIME type of the bytes being sent.</param>
        private static bool SendDataToServer(Uri uri, byte[] whatToSend, string mimeType)
        {
            try
            {
                HttpWebRequest req = WebRequest.CreateHttp(uri);
                req.Method = USED_HTTP_METHOD;
                req.ContentLength = whatToSend.Length;
                req.ContentType = mimeType;

                // Write data
                using (var dataStream = req.GetRequestStream())
                {
                    dataStream.Write(whatToSend, 0, whatToSend.Length);
                }

                // Get response
                HttpWebResponse response = req.GetResponse() as HttpWebResponse;

                return response.StatusCode != HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

    }
}
