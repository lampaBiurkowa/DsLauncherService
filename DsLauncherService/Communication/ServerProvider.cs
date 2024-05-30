using Microsoft.Extensions.Configuration;
using WatsonWebsocket;

namespace DsLauncherService.Communication
{
    internal class ServerProvider : IDisposable
    {
        private readonly WatsonWsServer serverInstance;
        public ClientMetadata? UiClient { get; private set; }

        public ServerProvider(IConfiguration configuration)
        {
            serverInstance = new WatsonWsServer(port: configuration.GetValue<int>("port"));
            serverInstance.ClientConnected += OnClientConnected;
            serverInstance.ClientDisconnected += OnClientDisconnected;       
        }

        public WatsonWsServer GetRunningServerInstance()
        {
            if (!serverInstance.IsListening)
            {
                serverInstance.Start();
            }

            return serverInstance;
        }

        public async Task SendAsync(string message) 
        { 
            if (UiClient is null)
            {
                return;
            }

            await GetRunningServerInstance().SendAsync(UiClient.Guid, message);
        }

        public void Dispose()
        {
            serverInstance.ClientConnected -= OnClientConnected;

            foreach (var client in serverInstance.ListClients())
            {
                serverInstance.DisconnectClient(client.Guid);
            }

            serverInstance.Stop();
            serverInstance.Dispose();
        }

        private void OnClientConnected(object? sender, ConnectionEventArgs e)
        {
            UiClient = e.Client;
        }

        private void OnClientDisconnected(object? sender, DisconnectionEventArgs e)
        {
            if (e.Client == UiClient)
            {
                UiClient = null;
            }
        }
    }
}
