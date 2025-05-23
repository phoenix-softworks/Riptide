﻿// This file is provided under The MIT License as part of RiptideNetworking.
// Copyright (c) Tom Weiland
// For additional information please see the included LICENSE.md file or view it on GitHub:
// https://github.com/RiptideNetworking/Riptide/blob/main/LICENSE.md

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Riptide.Transports.Tcp
{
    /// <summary>A server which can accept connections from <see cref="TcpClient"/>s.</summary>
    public class TcpServer : TcpPeer, IServer
    {
        /// <inheritdoc/>
        public event EventHandler<ConnectedEventArgs> Connected;
        /// <inheritdoc/>
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <inheritdoc/>
        public ushort Port { get; private set; }
        /// <summary>The maximum number of pending connections to allow at any given time.</summary>
        public int MaxPendingConnections { get; private set; } = 5;

        /// <summary>Whether or not the server is running.</summary>
        private bool isRunning = false;
        /// <summary>The currently open connections, accessible by their endpoints.</summary>
        private Dictionary<IPEndPoint, TcpConnection> connections;
        /// <summary>Connections that have been closed and need to be removed from <see cref="connections"/>.</summary>
        private readonly List<IPEndPoint> closedConnections = new List<IPEndPoint>();
        /// <summary>The IP address to bind the socket to.</summary>
        private readonly IPAddress listenAddress;

        /// <inheritdoc/>
        public TcpServer(int socketBufferSize = DefaultSocketBufferSize) : this(IPAddress.IPv6Any, socketBufferSize) { }
        
        /// <summary>Initializes the transport, binding the socket to a specific IP address.</summary>
        /// <param name="listenAddress">The IP address to bind the socket to.</param>
        /// <param name="socketBufferSize">How big the socket's send and receive buffers should be.</param>
        public TcpServer(IPAddress listenAddress, int socketBufferSize = DefaultSocketBufferSize) : base(socketBufferSize)
        {
            this.listenAddress = listenAddress;
        }

        /// <inheritdoc/>
        public void Start(ushort port)
        {
            Port = port;
            connections = new Dictionary<IPEndPoint, TcpConnection>();

            StartListening(port);
        }

        /// <summary>Starts listening for connections on the given port.</summary>
        /// <param name="port">The port to listen on.</param>
        private void StartListening(ushort port)
        {
            if (isRunning)
                StopListening();

            IPEndPoint localEndPoint = new IPEndPoint(listenAddress, port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                SendBufferSize = socketBufferSize,
                ReceiveBufferSize = socketBufferSize,
                NoDelay = true,
            };
            socket.Bind(localEndPoint);
            socket.Listen(MaxPendingConnections);

            isRunning = true;
        }

        /// <inheritdoc/>
        public void Poll()
        {
            if (!isRunning)
                return;

            Accept();
            foreach (TcpConnection connection in connections.Values)
                connection.Receive();

            foreach (IPEndPoint endPoint in closedConnections)
                connections.Remove(endPoint);

            closedConnections.Clear();
        }

        /// <summary>Accepts any pending connections.</summary>
        private void Accept()
        {
            if (socket.Poll(0, SelectMode.SelectRead))
            {
                Socket acceptedSocket = socket.Accept();
                IPEndPoint fromEndPoint = (IPEndPoint)acceptedSocket.RemoteEndPoint;
                if (!connections.ContainsKey(fromEndPoint))
                {
                    TcpConnection newConnection = new TcpConnection(acceptedSocket, fromEndPoint, this);
                    connections.Add(fromEndPoint, newConnection);
                    OnConnected(newConnection);
                }
                else
                    acceptedSocket.Close();
            }
        }

        /// <summary>Stops listening for connections.</summary>
        private void StopListening()
        {
            if (!isRunning)
                return;

            isRunning = false;
            socket.Close();
        }

        /// <inheritdoc/>
        public void Close(Connection connection)
        {
            if (connection is TcpConnection tcpConnection)
            {
                closedConnections.Add(tcpConnection.RemoteEndPoint);
                tcpConnection.Close();
            }
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            StopListening();
            connections.Clear();
        }

        /// <summary>Invokes the <see cref="Connected"/> event.</summary>
        /// <param name="connection">The successfully established connection.</param>
        protected virtual void OnConnected(Connection connection)
        {
            Connected?.Invoke(this, new ConnectedEventArgs(connection));
        }

        /// <inheritdoc/>
        protected internal override void OnDataReceived(int amount, TcpConnection fromConnection)
        {
            if ((MessageHeader)(ReceiveBuffer[0] & Message.HeaderBitmask) == MessageHeader.Connect)
            {
                if (fromConnection.DidReceiveConnect)
                    return;

                fromConnection.DidReceiveConnect = true;
            }

            DataReceived?.Invoke(this, new DataReceivedEventArgs(ReceiveBuffer, amount, fromConnection));
        }
    }
}
