using System;
using System.Collections.Generic;
using System.Text;

namespace ServerExample
{
    /// <summary>
    /// Options for <see cref="ServerExample.Server"/>.
    /// </summary>
    public class ServerOptions
    {
        /// <summary>
        /// URL of the server to receive events data from.
        /// </summary>
        private Uri serverUrl;

        /// <summary>
        /// URL of the server to make the <see cref="Server"/> object listen
        /// to this address.
        /// </summary>
        public Uri ServerUri
        {
            get => serverUrl;
            set => serverUrl = value;
        }

        /// <summary>
        /// Encoding of the HTTP body with events data. 
        /// If you're not sure what it is, just leave UTF-8.
        /// Must be the same as <see cref="TrackingLibrary.EventSenderOptions.DataEncoding"/>.
        /// </summary>
        public Encoding DataEncoding { set; get; } = Encoding.UTF8;

        /// <summary>
        /// List of properties in the deserialized event objects to be used
        /// as the name of the SQL table for these objects.
        /// If you specify one of these names as the string property of the event,
        /// than it will be easier to identify SQL table to insert this event in and
        /// also the server won't need to generate the name on its own which may
        /// reduce some bugs and ambiguities with different events having the same properties.
        /// </summary>
        public List<string> TableNameProperties { set; get; } = new List<string> { "EventName", "EventId" };

        /// <summary>
        /// If true, the table of events will contain the name of the event.
        /// In other words, there will be a column which holds the table name.
        /// It's recommended to turn it off in order to save disk space.
        /// </summary>
        public bool IncludeEventNameAsTableProperty { set; get; } = false;

        /// <summary>
        /// This property will be used as Primary key in all the tables with events.
        /// </summary>
        public string EventPrimaryKeyColumn { set; get; } = "EventRecordId";

        /// <summary>
        /// Creates new options for the <see cref="Server"/>.
        /// </summary>
        /// <param name="servertUrl">To which URL this server must listen to.</param>
        public ServerOptions(Uri servertUrl)
        {
            this.serverUrl = servertUrl ?? throw new ArgumentNullException(nameof(servertUrl));
        }

        /// <summary>
        /// Creates new options for <see cref="ServerExample.Server"/>.
        /// </summary>
        /// <param name="serverUrl">Url this server must listen to.</param>
        public ServerOptions(string serverUrl)
        {
            if (string.IsNullOrWhiteSpace(serverUrl))
            {
                throw new ArgumentNullException(nameof(ServerOptions.serverUrl));
            }

            if (Uri.TryCreate(serverUrl, UriKind.Absolute, out Uri newUrl))
            {
                this.serverUrl = newUrl;
            }
            else
            {
                throw new ArgumentException(nameof(serverUrl), $"The URL '{serverUrl}' must be absolute!");
            }
        }
    }
}
