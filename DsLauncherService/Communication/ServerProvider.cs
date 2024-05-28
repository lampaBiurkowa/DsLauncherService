using Microsoft.Extensions.Configuration;
using WatsonWebsocket;

namespace DsLauncherService.Communication
{
    internal class ServerProvider : IDisposable
    {
        private readonly WatsonWsServer _serverInstance;

        public ServerProvider(IConfiguration configuration)
        {
            _serverInstance = new WatsonWsServer(port: configuration.GetValue<int>("port"));
        }

        public WatsonWsServer GetRunningServerInstance()
        {
            if (!_serverInstance.IsListening)
            {
                _serverInstance.Start();
            }

            return _serverInstance;
        }

        public void Dispose()
        {
            foreach (var client in _serverInstance.ListClients())
            {
                _serverInstance.DisconnectClient(client.Guid);
            }

            _serverInstance.Stop();
            _serverInstance.Dispose();
        }
    }
}
