# TrackingLibrary
C# library for sending &amp; receiving tracking events data.
- Supports CSV, Json and XML formats;
- No class declaration is required: just use anonymous or dynamic objects and enjoy your life;
- Automatic *CREATE TABLE* and *INSERT INTO* on the server (so you don't have to struggle with ADO.Net or Entity Framework);
- Simple interface (just 4 public classes for both the client and the server);
- Immediate or delayed event sending in groups;
- Provides automatic deletion of old events;
- Flexible configuration;
- SQL Server database support.

# How to use
## 1. Client side

The only classes you need to use are:
- __EventSender__ - responsible for sending and batching of events;
- __EventSenderOptions__ - configurations for **EventSender** (the most important one is __ServerUri__ - where to send events).

Just look at the following code.
```C#
// Create options object
var options = new EventSenderOptions
{
    // Url to send data to
    ServerUri = new Uri(@"http://localhost:8000"),

    // Where to save event batches
    EventBatchesDirectory = @"C:\Users\Admin\Desktop\batches",

    // Type of the serialization when sending events
    Serialization = Serialization.Json,

    // Size of the batch to send events to the server
    EventBatchSize = 2
};

// Create objects
var obj1 = new
{
    EventName = "EventHello",
    Hello = "Lol",
    World = 123,
};

// Store the event on the device until there are EventSenderOptions.EventBatchSize events
EventSender.BatchEventToSend(obj1, options);

var obj2 = new
{
    EventName = "EventHello",
    Hello = "Kek",
    World = 123456,
};

// Store and this one (there are 2 of them now, so the events will be sent to the server).
EventSender.BatchEventToSend(obj2, options);

var complexOne = new
{
    EventName = "EventComplex",
    ObjectProp = new
    {
        DateOfTheProp = DateTime.Now,
        CodeOfTheProp = 999
    },
    Integers = new int[] { 5, 6, 7 }
};

// Make it always use these options
EventSender.DefaultOptions = options;

// Send now without any batching
EventSender.SendEventNow(complexOne);
```

## 2. Server side
There are also 2 classes:
- __Server__ - the guy who processes all the requests and adds events to the DB
- __ServerOptions__ - his son who tells the dad how to work properly.

The code is much easier.
```C#
// Create options for the server
var options = new ServerOptions("http://localhost:8000")
{
    // Primary key in the SQL table of events
    EventPrimaryKeyColumn = "Id",
};

// Connection string to the SQL Server db to work with
string dbConnectionStr = ConfigurationManager.ConnectionStrings["events"].ConnectionString;

// Create server with options
Server server = new Server(dbConnectionStr, options);

// Run the server and stop this thread
server.Run().Wait();
```

### Note!
- The server automatically creates tables for events and inserts into them.
- If you want to put an event in the specific table (with your name) put into it a string property from 
__ServerOptions.TableNameProperties__ (you can also put here your own property names).
- If you want to make specific PK for an event record, change __ServerOptions.EventPrimaryKeyColumn__.
- __DataEncoding__ in __EventSenderOptions__ and __ServerOptions__ must be the __same__.
