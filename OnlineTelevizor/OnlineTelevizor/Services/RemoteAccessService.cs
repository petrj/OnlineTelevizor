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

namespace OnlineTelevizor.Services
{
    public class RemoteAccessService
    {
        // https://stackoverflow.com/questions/50689842/how-to-make-sockets-work-in-xamarin

        private ILoggingService _loggingService;

        private const int BufferSize = 1024;

        public RemoteAccessService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        private void SendKey(string code)
        {
            _loggingService.Debug($"[RAS]:   command keyDown: {code}");

            Android.Views.Keycode keyCode;
            if (Enum.TryParse<Android.Views.Keycode>(code, out keyCode))
            {
                new Instrumentation().SendKeyDownUpSync(keyCode);
            } else
            {
                _loggingService.Info($"[RAS]: invalid key code {code}");
            }
        }

        public void StartListening(string ip, int port, string securityKey)
        {
            _loggingService.Info("[RAS]: Starting Remote Access Service");

            try
            {
                // Data buffer for incoming data.
                var bytes = new Byte[BufferSize];

                IPAddress ipAddress;

                if (string.IsNullOrEmpty(ip))
                {
                    var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                    ipAddress = ipHostInfo.AddressList[0];
                } else
                {
                    ipAddress = IPAddress.Parse(ip);
                }

                _loggingService.Info($"[RAS]: Endpoint: {ip}:{port}");

                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

                // Create a TCP/IP socket.

                using (var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(10);

                    MessagingCenter.Send($"Vzdálené ovládání aktivováno ({ipAddress.ToString()}:{port})", BaseViewModel.MSG_ToastMessage);

                    // Start listening for connections.
                    while (true)
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

                                if (message.securityKey != securityKey)
                                {
                                    _loggingService.Info("[RAS]: invalid security key");
                                } else
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


                            } catch (Exception ex)
                            {
                                _loggingService.Info("[RAS]: unknown message");
                            }

                            handler.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(responseMessage)));
                            handler.Shutdown(SocketShutdown.Both);
                            handler.Close();
                        }
                    }
                }
            } catch (Exception ex)
            {
                _loggingService.Error(ex, "[RAS]");
            }
        }
    }
}
