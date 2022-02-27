using  System;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace WebServer.Middleware
{
    public class WebSocketConnectionManager
    {
        private ConcurrentDictionary<string, WebSocket> _socket = new ConcurrentDictionary<string, WebSocket>();

        public ConcurrentDictionary<string, WebSocket> GetAllSockets()
        {
            return _socket;
        }

        public string AddSocket(WebSocket webSocket)
        {
            string ConnID = Guid.NewGuid().ToString();
            _socket.TryAdd(ConnID, webSocket);
            Console.WriteLine("Connection Added: " + ConnID);
            return ConnID;
        }
    }
}