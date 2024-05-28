using Microsoft.Extensions.Configuration;
using WatsonWebsocket;

namespace DsLauncherService.Communication
{
    internal class ServerProvider : IDisposable
    {
        private readonly WatsonWsServer serverInstance;

        public ServerProvider(IConfiguration configuration)
        {
            serverInstance = new WatsonWsServer(port: configuration.GetValue<int>("port"));
        }

        public WatsonWsServer GetRunningServerInstance()
        {
            if (!serverInstance.IsListening)
            {
                serverInstance.Start();
            }

            return serverInstance;
        }

        public void Dispose()
        {
            foreach (var client in serverInstance.ListClients())
            {
                serverInstance.DisconnectClient(client.Guid);
            }

            serverInstance.Stop();
            serverInstance.Dispose();
        }
    }
}
