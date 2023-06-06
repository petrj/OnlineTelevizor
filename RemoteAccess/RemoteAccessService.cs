using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.ComponentModel;
using LoggerService;
using Newtonsoft.Json;
using System.Threading;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;

namespace RemoteAccess
{
    public class RemoteAccessService
    {
        private BackgroundWorker _worker;

        private ILoggingService _loggingService;

        private const int BufferSize = 1024;
        private const int ConnectTimeoutMS = 2000;
        private const int ReceiveTimeoutMS = 2000;
        private const int SendTimeoutMS = 2000;
        private const int MaxMessageSize = 1000000;

        private const string TerminateString = "b9fb065b-dee4-4b1e-b8b4-b0c82556380c";
        private string _ip;
        private int _port;
        private string _securityKey;
        private string _serverSenderName;

        private Action<RemoteAccessMessage> _messageReceived = null;

        public RemoteAccessService(ILoggingService loggingService)
        {
            _loggingService = loggingService;

            _worker = new BackgroundWorker();
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += _worker_DoWork;
        }

        public bool ParamsChanged(string ip, int port, string securityKey)
        {
            if (ip != _ip ||
                port != _port ||
                securityKey != _securityKey)
            {
                return true;
            }

            return false;
        }

        private void _worker_DoWork(object sender, DoWorkEventArgs e)
        {
            _loggingService.Info("[RAS]: Starting background thread");

            try
            {
                // Data buffer for incoming data.
                var bytes = new Byte[BufferSize];

                var ipAddress = IPAddress.Parse(_ip);

                _loggingService.Info($"[RAS]: Endpoint: {_ip}:{_port}");

                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, _port);

                using (var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(10);

                    // Start listening for connections.
                    while (!_worker.CancellationPending)
                    {
                        // Program is suspended while waiting for an incoming connection.
                        using (var handler = listener.Accept())
                        {
                            IPAddress clientIP = ((IPEndPoint)handler.RemoteEndPoint).Address;

                            string data = null;

                            while (true)
                            {
                                bytes = new byte[BufferSize];
                                int bytesRec = handler.Receive(bytes);
                                data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                                if (data.IndexOf(TerminateString) > -1)
                                {
                                    break;
                                }
                                if (data.Length > MaxMessageSize)
                                {
                                    break;
                                }
                            }

                            var messageString = data.Substring(0,data.Length-TerminateString.Length);

                            var ok = true;

                            try
                            {
                                var decryptedData = CryptographyService.DecryptString(_securityKey, messageString);

                                var message = JsonConvert.DeserializeObject<RemoteAccessMessage>(decryptedData);
                                message.senderIP = clientIP.ToString();

                                if (_messageReceived != null)
                                {
                                    _messageReceived(message);
                                }
                            }
                            catch (Exception ex)
                            {
                                _loggingService.Error(ex, "[RAS]: error receiving message");
                                ok = false;
                            }

                            // sending response

                            if (ok)
                            {
                                try
                                {
                                    var responseMessage = new RemoteAccessMessage()
                                    {
                                        command = "responseStatus",
                                        commandArg1 = "OK",
                                        sender = _serverSenderName
                                    };
                                    var response = JsonConvert.SerializeObject(responseMessage);
                                    var responseEncrypted = CryptographyService.EncryptString(_securityKey, response);

                                    handler.SendTimeout = SendTimeoutMS;

                                    handler.Send(Encoding.ASCII.GetBytes(responseEncrypted));
                                    handler.Send(Encoding.ASCII.GetBytes(TerminateString));
                                }
                                catch (Exception ex)
                                {
                                    _loggingService.Error(ex, "[RAS]: error sending response");
                                }
                            }

                            try
                            {
                                handler.Shutdown(SocketShutdown.Both);
                                handler.Close();
                            }
                            catch (Exception ex)
                            {
                                _loggingService.Error(ex, "[RAS]: error closing socket");
                            }
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                e.Cancel = true;
                Thread.ResetAbort();

                _loggingService.Info("[RAS] background thread aborted");
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "[RAS] background thread stopped");
            }
        }

        public bool IsBusy
        {
            get
            {
                return _worker.IsBusy;
            }
        }

        public bool SetConnection(string ip, int port, string securityKey)
        {
            if (_worker.IsBusy)
                return false;

            _ip = ip;
            _port = port;
            _securityKey = securityKey;

            return true;
        }

        public void StartListening(Action<RemoteAccessMessage> messageReceived, string serverSenderName)
        {
            if (_worker.IsBusy)
                return;

            _serverSenderName = serverSenderName;
            _messageReceived = messageReceived;

            _worker.RunWorkerAsync();
        }

        public void StopListening()
        {
            if (!_worker.IsBusy)
                return;

            _worker.CancelAsync();
        }

        public async Task<RemoteAccessMessage> SendMessage(RemoteAccessMessage message)
        {
            _loggingService.Info($"[RAS] Sending: {message}");

            var bytes = new Byte[BufferSize];

            var ipAddress = IPAddress.Parse(_ip);
            var remoteEndPoint = new IPEndPoint(ipAddress, _port);

            using (var sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    int connectionTimeoutMs = ConnectTimeoutMS; // Set the connection timeout value in milliseconds

                    Task connectTask = sender.ConnectAsync(remoteEndPoint);
                    Task delayTask = Task.Delay(connectionTimeoutMs);

                    if (await Task.WhenAny(connectTask, delayTask) == connectTask)
                    {
                        var messageJSON = JsonConvert.SerializeObject(message);
                        var messageEncrypted = CryptographyService.EncryptString(_securityKey, messageJSON);

                        int bytesSent = sender.Send(Encoding.ASCII.GetBytes(messageEncrypted));
                        bytesSent += sender.Send(Encoding.ASCII.GetBytes(TerminateString));

                        // Receive response

                        sender.ReceiveTimeout = ReceiveTimeoutMS;

                        string data = null;

                        while (true)
                        {
                            bytes = new byte[BufferSize];
                            int bytesRec = sender.Receive(bytes);
                            if (bytesRec == 0)
                            {
                                _loggingService.Info($"[RAS] No byte received!");
                                break;
                            }
                            data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                            if (data.IndexOf(TerminateString) > -1)
                            {
                                break;
                            }
                            if (data.Length > MaxMessageSize)
                            {
                                break;
                            }
                        }

                        RemoteAccessMessage responseMessage = null;
                        if (data != null)
                        {
                            try
                            {
                                var messageString = data.Substring(0, data.Length - TerminateString.Length);

                                var decryptedData = CryptographyService.DecryptString(_securityKey, messageString);

                                responseMessage = JsonConvert.DeserializeObject<RemoteAccessMessage>(decryptedData);

                                _loggingService.Info($"[RAS] Response: {responseMessage}");

                            }
                            catch (Exception ex)
                            {
                                _loggingService.Error(ex, "[RAS]: error reading message response");
                            }
                        }

                        sender.Shutdown(SocketShutdown.Both);
                        sender.Close();

                        return responseMessage;
                    }
                    else
                    {
                        // Connection attempt timed out
                        _loggingService.Info($"[RAS] Timeout");
                        return null;
                    }

                }
                catch (Exception e)
                {
                    _loggingService.Error(e,"[RAS]");
                    return null;
                }
            }
        }
    }
}
