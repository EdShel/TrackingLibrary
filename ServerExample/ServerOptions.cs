using System;
using System.Text;

namespace ServerExample
{
    public class ServerOptions
    {
        /// <summary>
        /// URL of the server to receive events data from.
        /// </summary>
        private Uri servertUrl;

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
    }
}
