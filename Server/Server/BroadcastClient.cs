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
    public class BroadcastClient
    {
        public event Action<IPEndPoint, BroadcastData> ServerFound;

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


        public BroadcastClient(string name, int receiveBroadcastPort, int sendBroadcastPort)
        {
            _name = name;
            _sendBroadcastPort = sendBroadcastPort;
            _blockLocalhostDiscovery = receiveBroadcastPort == sendBroadcastPort;
            _client = new UdpClient(receiveBroadcastPort) {EnableBroadcast = true};
            // _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _sync = SynchronizationContext.Current ?? new SynchronizationContext();
            _binaryFormatter = new BinaryFormatter();
            _networkAddress = GetNetworkAddress();
            _discoveryData = GetDiscoveryData();
        }

        public BroadcastClient(string name, int receiveBroadcastPort)
            : this(name, receiveBroadcastPort, receiveBroadcastPort)
        {
        }

        /// <summary>
        /// Starts discovery in background thread
        /// </summary>
        /// <param name="revealSelf">Block self discovery</param>
        /// <param name="discover">Block discovery of network clients</param>
        public void StartDiscovery(bool revealSelf = true, bool discover = true)
        {
            if (!_token?.IsCancellationRequested ?? false)
                StopDiscovery();

            if (!revealSelf && !discover)
                throw new ArgumentException("Two-way discovery blocked", nameof(discover));

            _token = new CancellationTokenSource();

            Task.Factory.StartNew(() => BroadCast(_token.Token), _token.Token);
            Task.Factory.StartNew(() => ReceiveBroadcast(_token.Token), _token.Token);
        }

        public void StopDiscovery() =>
            _token.Cancel();

        private void BroadCast(CancellationToken token)
        {
            byte[] broadCastMessage = SerializeData(_discoveryData);
            IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, _sendBroadcastPort);
            while (!token.IsCancellationRequested)
            {
                _client.Send(broadCastMessage, broadCastMessage.Length, broadcastEndPoint);
                Thread.Sleep(BroadcastInterval);
            }
        }

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

                OnServerFound(endPoint, data);
            }
        }

        private void OnServerFound(IPEndPoint ip, BroadcastData data) =>
            //"?" нужен, чтобы при отсутствии подписчиков на данное событие ничего не происходило
            _sync.Post((_) => { ServerFound?.Invoke(ip, data); }, null);

        private BroadcastData GetDiscoveryData()
        {
            int port = ((IPEndPoint) _client.Client.LocalEndPoint).Port;
            return new BroadcastData(port) {Name = _name};
        }

        private static IPAddress GetNetworkAddress() =>
            Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .First(ip => ip.AddressFamily == AddressFamily.InterNetwork);

        private byte[] SerializeData(BroadcastData data)
        {
            using (var stream = new MemoryStream())
            {
                _binaryFormatter.Serialize(stream, data);
                return stream.GetBuffer();
            }
        }

        private BroadcastData DeserializeData(byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            {
                return _binaryFormatter.Deserialize(stream) as BroadcastData;
            }
        }
    }
}