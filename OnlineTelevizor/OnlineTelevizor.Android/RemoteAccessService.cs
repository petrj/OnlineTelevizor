using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using NLog;
using LoggerService;
using Newtonsoft.Json;
using OnlineTelevizor.Models;
using LibVLCSharp.Shared;
using OnlineTelevizor.ViewModels;
using Xamarin.Forms;
using Android.App;
using System.Threading;
using Android.InputMethodServices;
using System.ComponentModel;

namespace OnlineTelevizor.Services
{
    public class RemoteAccessService
    {
        private BackgroundWorker _worker;

        private ILoggingService _loggingService;

        private const int BufferSize = 1024;
        private string _ip;
        private int _port;
        private string _securityKey;

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
            _loggingService.Info("[RAS]: Starting Remote Access Service background thread");

            try
            {
                // Data buffer for incoming data.
                var bytes = new Byte[BufferSize];

                IPAddress ipAddress;

                if (string.IsNullOrEmpty(_ip))
                {
                    var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                    ipAddress = ipHostInfo.AddressList[0];
                }
                else
                {
                    ipAddress = IPAddress.Parse(_ip);
                }

                _loggingService.Info($"[RAS]: Endpoint: {_ip}:{_port}");

                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, _port);

                // Create a TCP/IP socket.

                using (var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(10);

                    MessagingCenter.Send($"Vzdálené ovládání aktivováno ({ipAddress.ToString()}:{_port})", BaseViewModel.MSG_ToastMessage);

                    // Start listening for connections.
                    while (!_worker.CancellationPending)
                    {
                        // Program is suspended while waiting for an incoming connection.
                        using (var handler = listener.Accept())
                        {
                            string data = null;

                            var bytesRec = int.MaxValue;
                            while (bytesRec > 0)
                            {
                                bytesRec = handler.Receive(bytes);
                                data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                            }

                            _loggingService.Debug($"[RAS]: Received data: {data}");

                            var responseMessage = new RemoteAccessMessage()
                            {
                                command = "responseStatus",
                                commandArg1 = "OK"
                            };

                            try
                            {
                                var message = JsonConvert.DeserializeObject<RemoteAccessMessage>(data);

                                if (message.securityKey != _securityKey)
                                {
                                    _loggingService.Info("[RAS]: invalid security key");
                                }
                                else
                                {
                                    switch (message.command)
                                    {
                                        case "keyDown":
                                            SendKey(message.commandArg1);
                                            break;
                                        default:
                                            _loggingService.Debug($"[RAS]:   unknown command: {message.command}");
                                            responseMessage.commandArg1 = "KO";
                                            responseMessage.commandArg2 = "Unknowm command";
                                            break;
                                    }
                                }


                            }
                            catch (Exception ex)
                            {
                                _loggingService.Info("[RAS]: unknown message");
                            }

                            handler.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(responseMessage)));
                            handler.Shutdown(SocketShutdown.Both);
                            handler.Close();
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                e.Cancel = true;
                Thread.ResetAbort();
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "[RAS]");
            }
        }

        private void SendKey(string code)
        {
            _loggingService.Debug($"[RAS]:   command keyDown: {code}");

            Android.Views.Keycode keyCode;
            if (Enum.TryParse<Android.Views.Keycode>(code, out keyCode))
            {
                new Instrumentation().SendKeyDownUpSync(keyCode);
            }
            else
            {
                _loggingService.Info($"[RAS]: invalid key code {code}");
            }
        }

        public bool IsBusy
        {
            get
            {
                return _worker.IsBusy;
            }
        }

        public void StartListening(string ip, int port, string securityKey)
        {
            if (_worker.IsBusy)
                return;

            _ip = ip;
            _port = port;
            _securityKey = securityKey;

            _worker.RunWorkerAsync();
        }

        public void StopListening(bool silent)
        {
            if (!_worker.IsBusy)
                return;

            _worker.CancelAsync();

            if (!silent)
            {
                MessagingCenter.Send($"Vzdálené ovládání deaktivováno", BaseViewModel.MSG_ToastMessage);
            }
        }
    }
}
