using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using SecureApp.Model.Networking;
using Random = SecureAppUtil.Extensions.Random;

namespace SecureApp
{
    internal static class Program
    {
        private static readonly Dictionary<Guid, ClientSession> ConnectedClients = new Dictionary<Guid, ClientSession>();
        private static readonly SecureAppUtil.Extensions.Networking.Socket.Server Server = new SecureAppUtil.Extensions.Networking.Socket.Server();

        public static void Main()
        {
            Server.OnClientConnect += OnClientConnect;
            Server.OnClientConnecting += OnClientConnecting;
            Server.OnClientDisconnect += OnClientDisconnect;
            Server.OnDataRetrieved += OnDataRetrieved;

            Console.WriteLine(Server.Start(0709) ? "Server is live!" : "Server failed to go live!");

            SecureAppUtil.Extensions.Networking.Socket.Client fakeClient = CreateFakeClient();
            
            // Recommended to change this in production, client to server verification..
            // Credits to stringencrypt for free version, other alternatives are a captcha or nothing at all
            string secure = "\x0B\x8B\x93";
 
            for (int umzoP = 0, MKdOg = 0; umzoP < 3; umzoP++)
            {
                MKdOg = secure[umzoP];
                MKdOg = ((MKdOg << 5) | ( (MKdOg & 0xFF) >> 3)) & 0xFF;
                MKdOg ^= umzoP;
                secure = secure.Substring(0, umzoP) + (char)(MKdOg & 0xFF) + secure.Substring(umzoP + 1);
            }
            
            fakeClient.Send(Guid.Empty, "Handshake", secure);
            
            
            
            Console.Read();
        }
        
        private static SecureAppUtil.Extensions.Networking.Socket.Client CreateFakeClient()
        {
            SecureAppUtil.Extensions.Networking.Socket.Client fakeClient = new SecureAppUtil.Extensions.Networking.Socket.Client();
            fakeClient.Connect("localhost", 0709);
            return fakeClient;
        }

        #region " Network Callbacks "
        
        private static void OnDataRetrieved(SecureAppUtil.Extensions.Networking.Socket.Server sender, SecureAppUtil.Extensions.Networking.Socket.Server.SocketClient socketClient, object[] data)
        {            
            lock (socketClient)
            {
                ClientSession clientSession = (ClientSession) socketClient.Tag;
                Guid guid = (Guid) data[0];
                    
                if (guid == Guid.Empty)
                {
                    // TODO:: find a better way to do this..
                    Type type = Type.GetType($"SecureApp.Core.Command.{data[1]}");
                    object obj = Activator.CreateInstance(type);
                    MethodInfo methodInfo = type.GetMethod("Work");
                    methodInfo.Invoke(obj, new object[] {data, clientSession, socketClient});
                }
            }
        }

        private static void OnClientDisconnect(SecureAppUtil.Extensions.Networking.Socket.Server sender, SecureAppUtil.Extensions.Networking.Socket.Server.SocketClient socketClient, SocketError er)
        {            
            ClientSession clientData = (ClientSession)socketClient.Tag;
            
            if (ConnectedClients.ContainsKey(clientData.Id))
                ConnectedClients.Remove(clientData.Id);
        }

        private static bool OnClientConnecting(SecureAppUtil.Extensions.Networking.Socket.Server sender, Socket csock)
        {
            return true;
        }

        private static void OnClientConnect(SecureAppUtil.Extensions.Networking.Socket.Server sender, SecureAppUtil.Extensions.Networking.Socket.Server.SocketClient socketClient)
        {
            ClientSession clientSession = new ClientSession(Random.Guid(), socketClient);
            
            ConnectedClients.Add(clientSession.Id, clientSession);
            socketClient.Tag = clientSession;
        }
        
        #endregion
    }
}