using System;
using System.Text;

namespace TrackingLibrary
{
    /// <summary>
    /// Options for EventSender.
    /// </summary>
    public class EventSenderOptions
    {
        private Uri servertUrl;

        private int maxOfflineSavedEvents = 3;

        public Uri ServerUri
        {
            get => servertUrl;
        }
        
        public Encoding DataEncoding { set; get; } = Encoding.UTF8;

        public Serialization Serialization { set; get; } = Serialization.Json;

        public string EventBatchesDirectory { set; get; } = "EventBatches";

        public int EventBatchSize { set; get; } = 3;

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
