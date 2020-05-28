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

        /// <summary>
        /// Puts event into the file in the folder specified in options.
        /// If the number of files exceeds EventBatchSize, events will be sent
        /// to the server. 
        /// If the number of files exceeds MaxOfflineSavedEvents, the oldest event
        /// files will be deleted.
        /// </summary>
        /// <param name="eventObj">Event to add to batch.</param>
        /// <param name="options">Options to use.</param>
        /// <returns>Whether the events were successfully sent to the server.</returns>
        public static bool BatchEventToSend(object eventObj, EventSenderOptions options)
        {
            // Write this event to a new file
            var eventFile = Path.Combine(
                path1: options.EventBatchesDirectory,
                path2: Guid.NewGuid().ToString());

            using (var wr = new BinaryWriter(File.Open(eventFile, FileMode.CreateNew)))
            {
                wr.Serialize(eventObj);
            }

            // Get batched files
            List<FileInfo> batchedEventsFiles = GetBatchedEventsFiles(options);

            // If there is an overflow of events (ready to send)
            if (batchedEventsFiles.Count >= options.EventBatchSize)
            {
                bool sent = BatchSendNow(options);

                // If the server accepted the events
                if (sent)
                {
                    // Delete the files as they ain't needed
                    BatchFolderClear(options);

                    return true;
                }

                BatchTruncate(options);
            }

            return false;
        }

        /// <summary>
        /// Deletes all the batched files from batching folder.
        /// </summary>
        /// <param name="options">Options to use.</param>
        public static void BatchFolderClear(EventSenderOptions options)
        {
            GetBatchedEventsFiles(options).ForEach(file => File.Delete(file.FullName));
        }

        /// <summary>
        /// Sends all the events written in the batch.
        /// Note: this method does not delete batch files,
        /// so you need do it manually by calling BatchFolderClear.
        /// </summary>
        /// <returns>Whether the server accepted these events.</returns>
        public static bool BatchSendNow(EventSenderOptions options)
        {
            // Deserialize binary files & append to them new event
            List<object> events = GetBatchedEventsFiles(options)
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
                .ToList();

            // Serialize this object
            string serialized = ObjectSerializer.Serialize(events, options.Serialization);

            // Send data
            bool sent = SendDataToServer(
                uri: options.ServerUri,
                whatToSend: options.DataEncoding.GetBytes(serialized),
                mimeType: ContentTypes[options.Serialization]);
            return sent;
        }

        /// <summary>
        /// Deletes old batched events in order to fit into 
        /// their max count.
        /// </summary>
        /// <param name="options">Options to use.</param>
        /// <returns>How many event files has been deleted.</returns>
        private static int BatchTruncate(EventSenderOptions options)
        {
            int deleted = 0;
            List<FileInfo> batchedEventsFiles = GetBatchedEventsFiles(options);

            // Delete the oldest events to fit into the required size
            while (batchedEventsFiles.Count >= options.MaxOfflineSavedEvents)
            {
                var theOldestFile = batchedEventsFiles
                    .Aggregate((leastRecent, x) =>
                        leastRecent == null ||
                            x.CreationTime < leastRecent.CreationTime
                            ? x
                            : leastRecent);

                batchedEventsFiles.Remove(theOldestFile);

                File.Delete(theOldestFile.FullName);
                deleted++;
            }

            return deleted;
        }

        /// <summary>
        /// Returns all the events written in the batching folder.
        /// </summary>
        private static List<FileInfo> GetBatchedEventsFiles(EventSenderOptions options)
        {
            return Directory.GetFiles(options.EventBatchesDirectory)
                            .Select(filePath => new FileInfo(filePath))
                            .ToList();
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

                return response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

    }
}
