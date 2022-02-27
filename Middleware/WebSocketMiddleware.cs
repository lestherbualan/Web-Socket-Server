using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Linq;

namespace WebServer.Middleware
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly WebSocketConnectionManager _manager;

        public WebSocketMiddleware(RequestDelegate next, WebSocketConnectionManager manager)
        {
            _next = next;
            _manager = manager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
             if(context.WebSockets.IsWebSocketRequest)
                {
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    Console.WriteLine("WebSocket Connected");

                    string ConnID = _manager.AddSocket(webSocket);

                    await SendIdentificationAsync(webSocket, ConnID);

                    await ReceiveMessage(webSocket, async(result, buffer) =>
                    {
                        if(result.MessageType == WebSocketMessageType.Text)
                        {
                            Console.WriteLine("Message Received.");
                            Console.WriteLine($"Message: {Encoding.UTF8.GetString(buffer, 0, result.Count)}");
                            await RouteJSONMessageAsync(Encoding.UTF8.GetString(buffer, 0, result.Count));
                            return;
                        }else if(result.MessageType == WebSocketMessageType.Close)
                        {
                            string id = _manager.GetAllSockets().FirstOrDefault(s => s.Value == webSocket).Key;

                            Console.WriteLine("Received Close Message");

                            _manager.GetAllSockets().TryRemove(id,out WebSocket sock);

                            await sock.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                            return;
                        }
                    });
                }
                else
                {
                    //if not a websocket request triggers the Run delegates
                    await _next(context);
                }
        }

        private async Task SendIdentificationAsync(WebSocket webSocket, string connId)
        {
            //BUG
            var buffer = Encoding.UTF8.GetBytes(connId);
            await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task ReceiveMessage(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];
            
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),cancellationToken: CancellationToken.None);
                handleMessage(result,buffer);
            }
        }

        public async Task RouteJSONMessageAsync(string message)
        {
            var routeOb = JsonConvert.DeserializeObject<dynamic>(message);

            if(Guid.TryParse(routeOb.To.ToString(), out Guid guidOutput))
            {
                //targeted message eg. with ID
                var sock = _manager.GetAllSockets().FirstOrDefault(s => s.Key == routeOb.To.ToString());
                if(sock.Value != null)
                {
                    if(sock.Value.State == WebSocketState.Open)
                    {
                        await sock.Value.SendAsync(Encoding.UTF8.GetBytes(routeOb.Message.ToString()), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
            else
            {
                Console.WriteLine("Broadcast");
                foreach(var sock in _manager.GetAllSockets())
                {
                    if(sock.Value.State == WebSocketState.Open)
                    {
                        await sock.Value.SendAsync(Encoding.UTF8.GetBytes(routeOb.Message.ToString()), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
        }

    }
}