using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.StarmaidSettings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;

namespace StarmaidIntegrationComputer.UdpThalassaControl
{
    public class UdpCommandListener : IDisposable
    {
        private bool isRunning = false;
        public int? RunningPortNumber { get; private set; } = null;

        private readonly UdpCommandSettings settings;
        private readonly SpeechComputer speechComputer;
        private readonly RemoteThalassaControlInterpreter remoteThalassaControlInterpreter;
        private readonly ILogger<UdpCommandListener> logger;

        private Socket? socket;
        private EndPoint? remoteEndPoint;

        private const int BUFFER_LENGTH = 100;


        private List<IDisposable> disposables { get; } = new List<IDisposable>();

        public UdpCommandListener(UdpCommandSettings settings, ILoggerFactory loggerFactory, SpeechComputer speechComputer, RemoteThalassaControlInterpreter remoteThalassaControlInterpreter)
        {
            this.settings = settings;
            this.speechComputer = speechComputer;
            this.remoteThalassaControlInterpreter = remoteThalassaControlInterpreter;
            this.logger = loggerFactory.CreateLogger<UdpCommandListener>();
        }

        public void Start()
        {
            if (isRunning)
            {
                return;
            }

            isRunning = true;

            socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            disposables.Add(socket);

            var ipBytes = settings.IpAddress
                .TrimStart("htps:/".ToCharArray())
                .Split(".")
                .Select(octet =>
                {
                    byte result;
                    if (!byte.TryParse(octet, out result))
                    {
                        throw new InvalidOperationException("Invalid IP address in the General Settings UDP Listener config - it should be in the format xxx.xxx.xxx.xxx");
                    }
                    return result;
                });


            try
            {
                socket.Bind(new IPEndPoint(new IPAddress(ipBytes.ToArray()), 0));
                RunningPortNumber = (socket.LocalEndPoint as IPEndPoint).Port;

                byte[] buffer = new byte[BUFFER_LENGTH];

                remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                StartListening(buffer);
                WritePortNumber();
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error starting UDP command listener: {ex.Message}";
                speechComputer.Speak("Error starting UDP command listener. See log for details.");
                logger.LogError(ex, errorMessage);
                Stop(); // Intentionally not bothering to await this
            }
        }

        public async Task Stop()
        {
            if (!isRunning)
            {
                return;
            }

            RunningPortNumber = null;
            WritePortNumber();

            await socket.DisconnectAsync(false);
            socket.Close();
            socket.Dispose();
            disposables.Remove(socket);

            isRunning = false;
        }

        private void WritePortNumber()
        {
            File.WriteAllText(settings.OutputPortFilePath, RunningPortNumber?.ToString());
        }

        private EndPoint StartListening(byte[] buffer)
        {
            socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEndPoint, OnDataReceived_InterpretCommand, buffer);
            return remoteEndPoint;
        }


        private void OnDataReceived_InterpretCommand(IAsyncResult result)
        {
            byte[]? buffer = result.AsyncState as byte[];
            if (buffer == null || buffer.Length == 0)
            {
                string warningMessage = "Warning: UDP packet received, but no command was included. This means something is misconfigured!";
                speechComputer.Speak(warningMessage);
                logger.LogWarning(warningMessage);
            }
            else
            {
                string? message = Encoding.UTF8.GetString(buffer);
                if (message != null)
                {
                    message = message.Trim('\0').Trim().ToUpper();
                    remoteThalassaControlInterpreter.Interpret(message);
                    buffer = new byte[BUFFER_LENGTH];
                }
                StartListening(buffer);
            }
        }

        public void Dispose()
        {
            foreach (IDisposable disposable in disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
