using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class BroadcastServer
    {
        public event Action<IPEndPoint, BroadcastData> ClientFound;

        public int BroadcastInterval { get; set; } = 300;

        private readonly UdpClient _client;
        private readonly SynchronizationContext _sync;
        private readonly string _name;
        private readonly int _sendBroadcastPort;
        private readonly bool _blockLocalhostDiscovery;
        private readonly IPAddress _networkAddress;
        private CancellationTokenSource _token;
        private BinaryFormatter _binaryFormatter;
        private BroadcastData _discoveryData;

        public BroadcastServer(string serverName, int receiveBroadcastPort)
        {
            _name = serverName;
            _client = new UdpClient(receiveBroadcastPort) {EnableBroadcast = true};

            // _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _sync = SynchronizationContext.Current ?? new SynchronizationContext();
            _binaryFormatter = new BinaryFormatter();
            _networkAddress = GetNetworkAddress();
            _discoveryData = GetDiscoveryData();
        }

        /// <summary>
        /// Starts discovery in background thread
        /// </summary>
        /// <param name="revealSelf">Block self discovery</param>
        /// <param name="discover">Block discovery of network clients</param>
        public void StartDiscovery()
        {
            if (!_token?.IsCancellationRequested ?? false)
                StopDiscovery();

            _token = new CancellationTokenSource();

            Task.Factory.StartNew(() => ReceiveBroadcast(_token.Token), _token.Token);
        }

        public void StopDiscovery() =>
            _token.Cancel();


        private void ReceiveBroadcast(CancellationToken token)
        {
            var endPoint = new IPEndPoint(IPAddress.Any, 0);
            while (!token.IsCancellationRequested)
            {
                byte[] serverResponse = _client.Receive(ref endPoint);
                BroadcastData data = DeserializeData(serverResponse);
                if (data == null) continue;

                if (_blockLocalhostDiscovery && (Equals(endPoint.Address, IPAddress.Loopback) || Equals(endPoint.Address, _networkAddress)))
                    continue;

                OnClientFound(endPoint, data);
                ReplyClient(endPoint, _discoveryData);
            }
        }

        private void ReplyClient(IPEndPoint endPoint, BroadcastData discoveryData)
        {
            byte[] buffer = SerializeData(discoveryData);
            _client.Send(buffer, buffer.Length, endPoint);
        }

        private BroadcastData DeserializeData(byte[] serverResponse)
        {
            using (var stream = new MemoryStream(serverResponse))
            {
                return _binaryFormatter.Deserialize(stream) as BroadcastData;
            }
        }

        private byte[] SerializeData(BroadcastData discoveryData)
        {
            using (var stream = new MemoryStream())
            {
                _binaryFormatter.Serialize(stream, discoveryData);
                return stream.GetBuffer();
            }
        }

        private void OnClientFound(IPEndPoint ip, BroadcastData data) =>
            _sync.Post((_) => { ClientFound?.Invoke(ip, data); }, null);

        private static IPAddress GetNetworkAddress() =>
            Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .First(ip => ip.AddressFamily == AddressFamily.InterNetwork);

        private BroadcastData GetDiscoveryData()
        {
            int port = ((IPEndPoint) _client.Client.LocalEndPoint).Port;
            return new BroadcastData(port) {Name = _name};
        }
    }
}