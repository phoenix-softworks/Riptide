﻿// This file is provided under The MIT License as part of RiptideNetworking.
// Copyright (c) Tom Weiland
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/RiptideNetworking/Riptide/blob/main/LICENSE.md

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Riptide.Transports.Tcp
{
    /// <summary>A client which can connect to a <see cref="TcpServer"/>.</summary>
    public class TcpClient : TcpPeer, IClient
    {
        /// <inheritdoc/>
        public event EventHandler Connected;
        /// <inheritdoc/>
        public event EventHandler ConnectionFailed;
        /// <inheritdoc/>
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <summary>The connection to the server.</summary>
        private TcpConnection tcpConnection;

        /// <inheritdoc/>
        /// <remarks>Expects the host address to consist of an IP and port, separated by a colon. For example: <c>127.0.0.1:7777</c>.</remarks>
        public bool Connect(string hostAddress, out Connection connection, out string connectError)
        {
            connectError = $"Invalid host address '{hostAddress}'! IP and port should be separated by a colon, for example: '127.0.0.1:7777'.";
            if (!ParseHostAddress(hostAddress, out IPAddress ip, out ushort port))
            {
                connection = null;
                return false;
            }

            IPEndPoint remoteEndPoint = new IPEndPoint(ip, port);
            socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                SendBufferSize = socketBufferSize,
                ReceiveBufferSize = socketBufferSize,
                NoDelay = true,
            };
            
            try
            {
                socket.Connect(remoteEndPoint); // TODO: do something about the fact that this is a blocking call
            }
            catch (SocketException)
            {
                // The connection failed, but invoking the transports ConnectionFailed event from
                // inside this method will cause problems, so we're just goint to eat the exception,
                // call OnConnected(), and let Riptide detect that no connection was established.
            }

            connection = tcpConnection = new TcpConnection(socket, remoteEndPoint, this);
            OnConnected();
            return true;
        }

        /// <summary>Parses <paramref name="hostAddress"/> into <paramref name="ip"/> and <paramref name="port"/>, if possible.</summary>
        /// <param name="hostAddress">The host address to parse.</param>
        /// <param name="ip">The retrieved IP.</param>
        /// <param name="port">The retrieved port.</param>
        /// <param name="addressFamily">Returns InterNetwork for IPV4, InterNetworkV6 for IPV6.</param>
        /// <returns>Whether or not <paramref name="hostAddress"/> was in a valid format.</returns>
        private bool ParseHostAddress(string hostAddress, out IPAddress ip, out ushort port)
        {
            string[] ipAndPort = hostAddress.Split(':');
            string ipString = "";
            string portString = "";
            if (ipAndPort.Length > 2)
            {
                // There was more than one ':' in the host address, might be IPv6

                // ipString = string.Join(":", ipAndPort.Take(ipAndPort.Length - 1));
                ipString = string.Join(":", ipAndPort.Take(ipAndPort.Length - 1).ToArray()); // KK91: framework 3.5 needs it to be converted to an array
                portString = ipAndPort[ipAndPort.Length - 1];
            }
            else if (ipAndPort.Length == 2)
            {
                // IPv4

                ipString = ipAndPort[0];
                portString = ipAndPort[1];
            }

            port = 0; // Need to make sure a value is assigned in case IP parsing fails
            return IPAddress.TryParse(ipString, out ip) && ushort.TryParse(portString, out port);
        }

        /// <inheritdoc/>
        public void Poll()
        {
            if (tcpConnection != null)
                tcpConnection.Receive();
        }

        /// <inheritdoc/>
        public void Disconnect()
        {
            socket.Close();
            tcpConnection = null;
        }

        /// <summary>Invokes the <see cref="Connected"/> event.</summary>
        protected virtual void OnConnected()
        {
            Connected?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Invokes the <see cref="ConnectionFailed"/> event.</summary>
        protected virtual void OnConnectionFailed()
        {
            ConnectionFailed?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        protected internal override void OnDataReceived(int amount, TcpConnection fromConnection)
        {
            DataReceived?.Invoke(this, new DataReceivedEventArgs(ReceiveBuffer, amount, fromConnection));
        }
    }
}
