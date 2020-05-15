using System;
using System.Collections;
using System.Collections.Generic;
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
                [Serialization.CSV] = "application/xml"
            };

        /// <summary>
        /// Default options to use when there are no options given.
        /// </summary>
        public static EventSenderOptions DefaultOptions { set; get; } = new EventSenderOptions();

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
                HttpWebResponse responce;
                HttpWebRequest req = WebRequest.CreateHttp(uri);
                req.Method = USED_HTTP_METHOD;
                req.ContentLength = whatToSend.Length;
                req.ContentType = mimeType;

                // Write data
                using (var dataStream = req.GetRequestStream())
                {
                    dataStream.Write(whatToSend, 0, whatToSend.Length);
                }

                // Get responce
                responce = req.GetResponse() as HttpWebResponse;

                return responce.StatusCode != HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

    }
}
