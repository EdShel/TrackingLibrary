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

        public Uri ServerUri
        {
            get => servertUrl;
        }
        
        public Encoding DataEncoding { set; get; } = Encoding.UTF8;

        public Serialization Serialization { set; get; } = Serialization.Json;

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
