using System;
using System.Collections.Generic;
using System.Linq;
using Fleck;
using Newtonsoft.Json;
using SensorStream.Monitor;

namespace SensorStream.Transports
{
    class WebSocketTransport : ITransport
    {
        private WebSocketServer server;
        private IWebSocketConnection socket;
        private IList<IWebSocketConnection> allSockets;
        private string lastData;

        public WebSocketTransport(int port)
        {
            server = new WebSocketServer("ws://0.0.0.0:" + port);
            server.RestartAfterListenError = true;
        }

        public void Start()
        {
            allSockets = new List<IWebSocketConnection>();
            server.Start(socket =>
            {
                this.socket = socket;
                socket.OnOpen = () => allSockets.Add(socket);
                socket.OnClose = () => { };
                socket.OnMessage = message => HandleMessage(socket, message);
            });
        }

        private void HandleMessage(IWebSocketConnection socket, string message)
        {
            if (string.IsNullOrEmpty(lastData)) return;

            var data = JsonConvert.DeserializeObject<List<HardwareContent>>(lastData);
            var parts = message.ToLower().Split('/');

            if (parts[0] == "system")
            {
                if (parts.Length > 1 && parts[1] == "components")
                {
                    if (parts.Length > 2 && parts[2] == "all")
                    {
                        var commands = new List<string>();
                        foreach (var h in data)
                        {
                            commands.Add($"{h.type.ToLower()} = Lists all {h.type} data");
                            commands.Add($"{h.type.ToLower()}/name = {h.name}");
                            commands.Add($"{h.type.ToLower()}/sensorcount = {h.sensorCount}");
                            foreach (var s in h.sensors)
                            {
                                commands.Add($"{h.type.ToLower()}/{s.name.ToLower().Replace(" ", "")} = {s.value}");
                            }
                            commands.Add(""); // Empty line between components
                        }
                        socket.Send(string.Join("\n", commands));
                        return;
                    }
                    var components = string.Join(",", data.Select(h => h.type.ToLower()));
                    socket.Send(components);
                    return;
                }
            }

            var hardware = data.FirstOrDefault(h => h.type.ToLower() == parts[0]);
            if (hardware != null)
            {
                if (parts.Length == 1)
                {
                    socket.Send(JsonConvert.SerializeObject(new[] { hardware }));
                    return;
                }

                switch (parts[1])
                {
                    case "name":
                        socket.Send(hardware.name);
                        break;
                    case "sensorcount":
                        socket.Send(hardware.sensorCount.ToString());
                        break;
                    case "sensors":
                        socket.Send(JsonConvert.SerializeObject(hardware.sensors));
                        break;
                    default:
                        var sensorPath = string.Join("/", parts.Skip(1));
                        var sensor = hardware.sensors.FirstOrDefault(s => {
                            var normalizedSensorName = s.name.ToLower().Replace(" ", "");
                            var normalizedSensorPath = sensorPath.Replace(" ", "");
                            return normalizedSensorName.Contains(normalizedSensorPath);
                        });
                        if (sensor != null)
                        {
                            socket.Send(sensor.value?.ToString() ?? "");
                        }
                        break;
                }
            }
        }

        public void sendMessage(string msg)
        {
            lastData = msg; // Store the last data update
            
            // Clean up disconnected sockets
            var disconnectedSockets = allSockets.Where(s => s == null || !s.IsAvailable).ToList();
            foreach (var s in disconnectedSockets)
            {
                allSockets.Remove(s);
            }
        }

        public void Stop()
        {
            server.Dispose();
            foreach (var socket in allSockets)
            {
                if (socket != null)
                {
                    socket.Close();
                }
            }
        }
    }
}
