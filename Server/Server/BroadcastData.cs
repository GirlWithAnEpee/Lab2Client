using System;
using System.Net;

namespace Server
{
    [Serializable]
    public class BroadcastData
    {
        public int Port { get; }
        public string Name { get; set; } = Guid.NewGuid().ToString();

        public BroadcastData(int port)
        {
            Port = port;
        }
    }
}