using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Server
{
    internal class DiscoveryClient
    {
        public event Action<IPEndPoint> ClientFound;

        private readonly UdpClient _client;
        private CancellationTokenSource _token;
        private readonly Random _random = new Random();
        private readonly string _clientName;
        private readonly int _broadcastPort;
        private readonly SynchronizationContext _sync;


        public DiscoveryClient(string clientName, int broadcastPort)
        {
            _clientName = clientName;
            _broadcastPort = broadcastPort;
            _client = new UdpClient(broadcastPort) { EnableBroadcast = true };
            _sync = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        /// <summary>
        /// Starts discovery in background thread
        /// </summary>
        /// <param name="revealSelf">Block self discovery</param>
        /// <param name="discover">Block discovery of network clients</param>
        public void StartDiscovery(bool revealSelf = true, bool discover = true)
        {
            if ( !_token?.IsCancellationRequested ?? false)
                StopDiscovery();

            if (!revealSelf && !discover)
                throw new ArgumentException("Two-way discovery blocked", nameof(discover));

            _token = new CancellationTokenSource();
            if (revealSelf)
                Task.Factory.StartNew(() => BroadCast(_token.Token), _token.Token);

            if (discover)
                Task.Factory.StartNew(() => ReceiveBroadcast(_token.Token), _token.Token);
        }

        public void StopDiscovery() =>
            _token.Cancel();

        private void BroadCast(CancellationToken token)
        {
            byte[] broadCastMessage = Encoding.UTF8.GetBytes(_clientName);
            while (!token.IsCancellationRequested)
            {
                _client.Send(broadCastMessage, broadCastMessage.Length, new IPEndPoint(IPAddress.Broadcast, _broadcastPort));
                Thread.Sleep(300 + _random.Next(10, 90));
            }
        }

        private void ReceiveBroadcast(CancellationToken token)
        {
            var endPoint = new IPEndPoint(IPAddress.Any, 0);
            while (!token.IsCancellationRequested)
            {
                byte[] serverResponse = _client.Receive(ref endPoint);
                string response = Encoding.UTF8.GetString(serverResponse);
                OnClientFound(endPoint);
            }
        }

        private void OnClientFound(IPEndPoint ip) =>
            //"?" нужен, чтобы при отсутствии подписчиков на данное событие ничего не происходило
            _sync.Post((_) => { ClientFound?.Invoke(ip); }, null);
    }
}
