using System;
using System.Text;

namespace TrackingLibrary
{
    /// <summary>
    /// Options for EventSender.
    /// </summary>
    public class EventSenderOptions
    {
        /// <summary>
        /// URL of the server to send events data to.
        /// </summary>
        private Uri servertUrl;

        /// <summary>
        /// How many events can be stored on the device.
        /// Exceeding this amount will force deleting the old
        /// events.
        /// </summary>
        private int maxOfflineSavedEvents = 3;

        /// <summary>
        /// URL of the server to send events data to.
        /// </summary>
        public Uri ServerUri
        {
            get => servertUrl;
            set => servertUrl = value;
        }
        
        /// <summary>
        /// Encoding of the HTTP body with events data. 
        /// If you're not sure what it is, just leave UTF-8.
        /// </summary>
        public Encoding DataEncoding { set; get; } = Encoding.UTF8;

        /// <summary>
        /// How events must be serialized before sending.
        /// </summary>
        public Serialization Serialization { set; get; } = Serialization.Json;

        /// <summary>
        /// The folder where to save batched events.
        /// This directory must not contain any other files
        /// 'cause any file is treated as binary serialized event.
        /// </summary>
        public string EventBatchesDirectory { set; get; } = "EventBatches";

        /// <summary>
        /// Size of the events batch. 
        /// If events batch folder has this number of items,
        /// all the events will be sent to the server.
        /// But if the server doesn't accept the events,
        /// they won't be deleted until their amount
        /// exceeds <see cref="MaxOfflineSavedEvents"/>.
        /// </summary>
        public int EventBatchSize { set; get; } = 3;

        /// <summary>
        /// How many events can be stored on the device.
        /// Exceeding this amount will force deleting the old
        /// events.
        /// </summary>
        public int MaxOfflineSavedEvents
        {
            get
            {
                return maxOfflineSavedEvents;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "You can't save <= 0 events! The number must be a positive integer!");
                }
                if (value < EventBatchSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        $"You can't save less events than {nameof(EventBatchSize)}!" +
                        $"If you want to change this value, change {nameof(EventBatchSize)} first!");
                }

                maxOfflineSavedEvents = value;
            }
        }

        /// <summary>
        /// Designate to which server events data must be sent.
        /// There is no testing of the connection, only URL validation.
        /// </summary>
        /// <param name="uri">URL to the server.</param>
        /// <returns>Server <see cref="Uri"/> object.</returns>
        public Uri SetServerUri(string uri)
        {
            // Try to set valid server Uri
            // via http or https protocol
            if (Uri.TryCreate(uri, UriKind.Absolute, out Uri newUri)
                    && (newUri.Scheme == Uri.UriSchemeHttp
                    || newUri.Scheme == Uri.UriSchemeHttps))
            {
                return servertUrl = newUri;
            }

            throw new UriFormatException($"Invalid server URI is given: {uri}!");
        }
    }
}
