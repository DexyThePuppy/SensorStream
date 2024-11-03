using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using Newtonsoft.Json;
using SensorStream.Monitor;

namespace SensorStream.Transports
{
    public class WebSocketTransport : ITransport, IDisposable
    {
        private WebSocketServer server;
        private readonly ConcurrentDictionary<Guid, IWebSocketConnection> activeSockets;
        private volatile string lastData;
        private readonly SemaphoreSlim socketLock;
        private bool isDisposed;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly int port;
        private readonly ManualResetEventSlim _stopEvent;

        public WebSocketTransport(int port)
        {
            if (port <= 0 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535");

            this.port = port;
            activeSockets = new ConcurrentDictionary<Guid, IWebSocketConnection>();
            socketLock = new SemaphoreSlim(1, 1);
            _cancellationTokenSource = new CancellationTokenSource();
            _stopEvent = new ManualResetEventSlim(false);
        }

        public async Task StartAsync()
        {
            try
            {
                server = new WebSocketServer($"ws://0.0.0.0:{port}");
                server.Start(socket =>
                {
                    socket.OnOpen = () => OnSocketOpen(socket);
                    socket.OnClose = () => OnSocketClose(socket);
                    socket.OnMessage = async message => await HandleMessageAsync(socket, message);
                    socket.OnError = exception => OnSocketError(socket, exception);
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to start WebSocket server", ex);
            }
        }

        public void Start()
        {
            StartAsync().GetAwaiter().GetResult();
        }

        private void OnSocketOpen(IWebSocketConnection socket)
        {
            activeSockets.TryAdd(socket.ConnectionInfo.Id, socket);
        }

        private void OnSocketClose(IWebSocketConnection socket)
        {
            activeSockets.TryRemove(socket.ConnectionInfo.Id, out _);
        }

        private void OnSocketError(IWebSocketConnection socket, Exception ex)
        {
            // Log the error (replace with your logging framework)
            Console.WriteLine($"Socket error: {ex.Message}");
            activeSockets.TryRemove(socket.ConnectionInfo.Id, out _);
        }

        private async Task HandleMessageAsync(IWebSocketConnection socket, string message)
        {
            if (string.IsNullOrEmpty(lastData))
            {
                await socket.Send("No data available");
                return;
            }

            try
            {
                var data = JsonConvert.DeserializeObject<List<HardwareContent>>(lastData);
                var response = ProcessMessage(message, data);
                if (!string.IsNullOrEmpty(response))
                {
                    await socket.Send(response);
                }
            }
            catch (JsonException ex)
            {
                await socket.Send($"Error processing data: {ex.Message}");
            }
            catch (Exception ex)
            {
                await socket.Send($"Internal error: {ex.Message}");
            }
        }

        private string ProcessMessage(string message, List<HardwareContent> data)
        {
            var parts = message.ToLower().Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;

            return parts[0] switch
            {
                "system" => HandleSystemCommand(parts.Skip(1).ToArray(), data),
                _ => HandleHardwareCommand(parts, data)
            };
        }

        private string HandleSystemCommand(string[] parts, List<HardwareContent> data)
        {
            if (parts.Length == 0) return null;

            if (parts[0] == "components")
            {
                if (parts.Length > 1 && parts[1] == "all")
                {
                    return GenerateFullComponentList(data);
                }
                return string.Join(",", data.Select(h => h.type.ToLower()));
            }

            return null;
        }

        private string GenerateFullComponentList(List<HardwareContent> data)
        {
            var commands = new List<string>();
            foreach (var hardware in data)
            {
                var componentType = hardware.type.ToLower();
                if (componentType.Contains("gpu"))
                    componentType = "gpu";
                
                commands.Add($"{componentType} = Lists all {hardware.type} data");
                commands.Add($"{componentType}/name = {hardware.name}");
                commands.Add($"{componentType}/sensorcount = {hardware.sensorCount}");

                if (componentType == "gpu")
                {
                    // Group GPU sensors by type
                    var gpuSensors = hardware.sensors
                        .GroupBy(s => {
                            var name = s.name.ToLower();
                            if (name.Contains("fan")) return "fan";
                            if (name.StartsWith("d3d")) return "d3d";
                            if (name.Contains("power")) return "power";
                            if (name.Contains("memory")) return "memory";
                            return "other";
                        });

                    foreach (var group in gpuSensors)
                    {
                        foreach (var sensor in group)
                        {
                            var sensorName = NormalizeSensorName(componentType, sensor.name);
                            commands.Add($"gpu/{sensorName} = {FormatSensorValue(sensor.value)}");
                        }
                    }
                }
                else if (componentType == "cpu")
                {
                    // Group sensors by their core affiliation
                    var coreSensors = hardware.sensors
                        .Where(s => s.name.Contains("Core #"))
                        .GroupBy(s => {
                            var match = System.Text.RegularExpressions.Regex.Match(s.name, @"Core #(\d+)");
                            return match.Success ? int.Parse(match.Groups[1].Value) : -1;
                        })
                        .Where(g => g.Key != -1)
                        .OrderBy(g => g.Key);

                    // Process core-specific sensors
                    foreach (var coreGroup in coreSensors)
                    {
                        var coreNum = coreGroup.Key;
                        var sensors = coreGroup.ToList();

                        // Add core sensors in a specific order
                        foreach (var sensor in sensors.OrderBy(s => s.type))
                        {
                            commands.Add($"cpu/{coreNum}/{sensor.type.ToLower()} = {FormatSensorValue(sensor.value)}");
                        }
                        commands.Add(string.Empty);
                    }

                    // Add all non-core sensors
                    var otherSensors = hardware.sensors
                        .Where(s => !s.name.Contains("Core #"))
                        .OrderBy(s => s.name);

                    foreach (var sensor in otherSensors)
                    {
                        var sensorName = sensor.name.ToLower()
                            .Replace(" ", "")
                            .Replace("#", "")
                            .Replace("(", "")
                            .Replace(")", "")
                            .Replace("/", "");
                        commands.Add($"cpu/{sensorName} = {FormatSensorValue(sensor.value)}");
                    }
                }
                else
                {
                    // Handle all other hardware types
                    var processedSensors = new HashSet<string>();
                    foreach (var sensor in hardware.sensors.OrderBy(s => s.name))
                    {
                        var sensorName = sensor.name.ToLower()
                            .Replace(" ", "")
                            .Replace("#", "")
                            .Replace("(", "")
                            .Replace(")", "");
                        
                        if (processedSensors.Add(sensorName))
                        {
                            commands.Add($"{componentType}/{sensorName} = {FormatSensorValue(sensor.value)}");
                        }
                    }
                }
                
                commands.Add(string.Empty);
            }
            return string.Join("\n", commands);
        }

        private string FormatSensorValue(object value)
        {
            if (value == null) return string.Empty;
            
            if (value is double d)
            {
                if (d > 1_000_000)
                    return d.ToString("N0");
                return d.ToString("F2").Replace(",", ".");
            }
            
            return value.ToString().Replace(",", ".");
        }

        private string HandleHardwareCommand(string[] parts, List<HardwareContent> data)
        {
            var hardware = data.FirstOrDefault(h => h.type.ToLower() == parts[0]);
            if (hardware == null) return null;

            if (parts.Length == 1)
            {
                return JsonConvert.SerializeObject(new[] { hardware });
            }

            return parts[1] switch
            {
                "name" => hardware.name,
                "sensorcount" => hardware.sensorCount.ToString(),
                "sensors" => JsonConvert.SerializeObject(hardware.sensors),
                _ => GetSensorValue(hardware, string.Join("/", parts.Skip(1)))
            };
        }

        private string GetSensorValue(HardwareContent hardware, string sensorPath)
        {
            var sensor = hardware.sensors.FirstOrDefault(s =>
            {
                var normalizedSensorName = s.name.ToLower()
                    .Replace(" ", "")
                    .Replace("#", "")
                    .Replace("(", "")
                    .Replace(")", "");
                
                var normalizedSensorPath = sensorPath.ToLower()
                    .Replace(" ", "")
                    .Replace("#", "")
                    .Replace("(", "")
                    .Replace(")", "");
                
                return normalizedSensorName == normalizedSensorPath;
            });

            return sensor?.value?.ToString() ?? string.Empty;
        }

        public async Task SendMessageAsync(string msg)
        {
            if (string.IsNullOrEmpty(msg) || isDisposed)
                return;

            try
            {
                await socketLock.WaitAsync(_cancellationTokenSource.Token);
                try
                {
                    lastData = msg;
                    var tasks = activeSockets.Values
                        .Where(socket => socket.IsAvailable)
                        .Select(async socket =>
                        {
                            try
                            {
                                await socket.Send(msg);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to send message: {ex.Message}");
                                activeSockets.TryRemove(socket.ConnectionInfo.Id, out _);
                            }
                        });

                    await Task.WhenAll(tasks);
                }
                finally
                {
                    socketLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, ignore
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in sendMessage: {ex.Message}");
            }
        }

        public void sendMessage(string msg)
        {
            SendMessageAsync(msg).GetAwaiter().GetResult();
        }

        public async Task StopAsync()
        {
            if (isDisposed) return;

            try
            {
                _cancellationTokenSource.Cancel();
                _stopEvent.Set();

                var closeTasks = activeSockets.Values.Select(async socket =>
                {
                    try
                    {
                        await Task.Run(() => socket.Close());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error closing socket: {ex.Message}");
                    }
                });

                await Task.WhenAll(closeTasks);
                activeSockets.Clear();
                
                await Task.Run(() => server?.Dispose());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during stop: {ex.Message}");
            }
        }

        public void Stop()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            Stop();
            socketLock.Dispose();
            _stopEvent.Dispose();
            _cancellationTokenSource.Dispose();
            isDisposed = true;
            GC.SuppressFinalize(this);
        }

        private string NormalizeSensorName(string type, string name)
        {
            if (type.ToLower().Contains("gpu"))
            {
                // Handle GPU fan speeds
                if (name.ToLower().Contains("fan"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(name, @"(\d+)");
                    if (match.Success)
                    {
                        return $"fan/{match.Groups[1].Value}";
                    }
                    return "fan/1";  // Default to fan/1 if no number found
                }

                // Handle D3D related metrics
                if (name.ToLower().StartsWith("d3d"))
                {
                    return $"d3d/{name.ToLower().Replace("d3d", "").Replace(" ", "")}";
                }

                // Handle power related metrics
                if (name.ToLower().Contains("power"))
                {
                    return "power";
                }

                // Handle memory related metrics
                if (name.ToLower().Contains("memory"))
                {
                    return name.ToLower()
                        .Replace("gpu", "")
                        .Replace(" ", "");
                }

                // Handle other GPU metrics
                return name.ToLower()
                    .Replace("gpu", "")
                    .Replace(" ", "");
            }

            // Extract core number if present (e.g., "Core #1" -> "1")
            var coreNumber = "";
            if (name.Contains("#"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(name, @"#(\d+)");
                if (match.Success)
                {
                    coreNumber = match.Groups[1].Value;
                }
            }

            // Handle specific sensor types
            switch (type.ToLower())
            {
                case "clock" when name.StartsWith("Core"):
                    return $"{coreNumber}/clock";
                case "factor" when name.StartsWith("Core"):
                    return $"{coreNumber}/factor";
                case "power" when name.StartsWith("Core"):
                    return $"{coreNumber}/power";
                case "voltage" when name.Contains("VID"):
                    return $"{coreNumber}/voltage";
                case "temperature":
                    return "temp";
                default:
                    // Handle other cases as before
                    return name.ToLower()
                        .Replace(" ", "")
                        .Replace("#", "")
                        .Replace("(", "")
                        .Replace(")", "");
            }
        }

        public void UpdateData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;
            
            lastData = data;
        }
    }
}