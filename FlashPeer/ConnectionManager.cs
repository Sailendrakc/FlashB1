using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Timers;

namespace FlashPeer
{
    public class ConnectionManager
    {
        /// <summary>
        /// This instantiate with custom max client count.
        /// </summary>
        /// <param name="Max_clients">cutom max clients that can be connected to this peer server.</param>
        public ConnectionManager(int Max_clients)
        {
            setMaxClientCount(Max_clients);
        }

        /// <summary>
        /// This ctor will instantiate with max client count of 1000.
        /// </summary>
        public ConnectionManager()
        {
            setMaxClientCount(1000);
        }

        /// <summary>
        /// This stores all the active connection with clients.
        /// </summary>
        public Dictionary<string, FlashPeer> connectedClients { get; private set; } = new Dictionary<string, FlashPeer>();

        /// <summary>
        /// This stores all the active connection with servers.
        /// Capped at 3.
        /// </summary>
        public Dictionary<string, FlashPeer> connectedServers { get; private set; } = new Dictionary<string, FlashPeer>();

        /// <summary>
        /// This stores all the active connection with clients.
        /// </summary>
        public Dictionary<string, HSfromServer> pendingClients { get; private set; } = new Dictionary<string, HSfromServer>(2);

        /// <summary>
        /// This stores all the active connection with servers.
        /// Capped at 3.
        /// </summary>
        public Dictionary<string, HSfromClient> pendingServers { get; private set; } = new Dictionary<string, HSfromClient>();

        /// <summary>
        /// When false, wont accept any new client connections.
        /// Currently connected clients will remain connected until they are connected.
        /// Connected clients can be disconnected or good bye-d by calling other functions.
        /// </summary>
        public bool acceptingClientConnections { get; private set; } = true;

        /// <summary>
        /// When false, wont accept any new server connections.
        /// Currently connected servers will remain connected until they are connected.
        /// Connected servers can be disconnected or good bye-d by calling other functions.
        /// </summary>
        public bool acceptingServerConnections { get; private set; } = true;

        /// <summary>
        /// It indicates the maximum number of clients this peer can be connected to at a time.
        /// </summary>
        public int Max_clients { get; private set; }

        /// <summary>
        /// It indicates the maximum number of server this peer can be connected to at a time.
        /// </summary>
        public int Max_servers { get; private set; } = 3;

        /// <summary>
        /// It indicates the current number of clients this peer is connected to.
        /// </summary>
        public int CountOfConnectedClients { get; private set; }

        /// <summary>
        /// It indicates the current number of server this peer is connected to.
        /// </summary>
        public int CountOfConnectedServers { get; private set; }

        public void setMaxClientCount(int newMaxClientCount)
        {
            if(newMaxClientCount <= CountOfConnectedClients)
            {
                Console.WriteLine("Cannot change max number clients." +
                    "Because There are more connected clients than new max client numbers");
                return;
            }
            
            this.Max_clients = newMaxClientCount;
        }

        public void setMaxServerCount(int newMaxServerCount)
        {
            if (newMaxServerCount <= CountOfConnectedServers)
            {
                Console.WriteLine("Cannot change max number of servers." +
                    "Because There are more connected servers than new max server numbers");
                return;
            }

            this.Max_clients = newMaxServerCount;
        }

        /// <summary>
        /// Removes a connection
        /// </summary>
        /// <param name="IP">IP of the connection</param>
        /// <param name="fromClient">Is the connection a client peer?</param>
        /// <returns>True for sucess, false for unsucess.</returns>
        public bool RemovePeerFromConnections(string IP, bool fromClient)
        {
            if (fromClient)
            {
                if(connectedClients.TryGetValue(IP, out var peer))
                {
                    lock (connectedClients)
                    {
                        connectedClients.Remove(IP);
                        CountOfConnectedClients--;
                    }
                    if(CountOfConnectedClients <= 0)
                    {
                        //stop offline timer.
                        pauseTimer(false);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (connectedServers.TryGetValue(IP, out var peer))
                {
                    lock (connectedServers)
                    {
                        connectedServers.Remove(IP);
                        CountOfConnectedServers--;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// This is used for connection which fails on handshakes.
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="fromClient"></param>
        /// <param name="safely">Method wont check if IP exist if safely is false. 
        /// Pass it false if the checking is done beforehand.</param>
        /// <returns>True of sucess.</returns>
        public bool RemoveFromPendingConnections(string IP, bool fromClient, bool safely)
        {
            if (safely)
            {
                if (fromClient)
                {
                    if(pendingClients.TryGetValue(IP, out var hsfromServer))
                    {
                        //remove it.
                        pendingClients.Remove(IP);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (pendingServers.TryGetValue(IP, out var hsfromClient))
                    {
                        //remove it.
                        pendingServers.Remove(IP);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            else
            {
                try
                {
                    if (fromClient)
                    {
                        pendingClients.Remove(IP);
                    }
                    else
                    {
                        pendingServers.Remove(IP);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("FATAL Error: when removing pending unsafely" + e.ToString());
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// This method transfers pending connection from pending pool to connected pools.
        /// </summary>
        /// <param name="IP">The ip of peer we are confirmed to be connected to.</param>
        /// <param name="otherPeerIsClient">Is the confirmed connected peer client?</param>
        public bool OnConnectionConfirmed(string IP, bool otherPeerIsClient)
        {
            if (otherPeerIsClient)
            {
                if (!acceptingClientConnections) { return false ; }
                if(CountOfConnectedClients >= Max_clients)
                {
                    Console.WriteLine("Connection pool for clients is full. Not accepting any more clients");
                    return false;
                }

                if(pendingClients.TryGetValue(IP, out var peer)){
                    lock (connectedClients)
                    {
                        connectedClients.Add(IP, peer.Ipeer);
                        CountOfConnectedClients++;
                        RemoveFromPendingConnections(IP, true, false);
                    }

                    if(CountOfConnectedClients == 1) // meaning some client just got connected above.
                    {
                        resumeTimer();
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (!acceptingServerConnections) { return false; }
                if (CountOfConnectedServers >= Max_servers)
                {
                    Console.WriteLine("Connection pool for servers is full. Not accepting any more servers");
                    return false;
                }

                if (pendingServers.TryGetValue(IP, out var peer))
                {
                    lock (connectedServers)
                    {
                        connectedClients.Add(IP, peer.Speer);
                        CountOfConnectedServers++;
                        RemoveFromPendingConnections(IP, false, false);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }

        }

        /// <summary>
        /// This timer will be responsible to check clients activity/inactivity for connection handling.
        /// </summary>
        private Timer connectionTimer;

        /// <summary>
        /// This is the interval in seconds in which the connection check will begain.
        /// </summary>
        public int connectionTimerInterval { get; private set; }  = 18; //sec

        /// <summary>
        /// It prepares the connection timer.
        /// </summary>
        /// <param name="start">If true, timer will be prepared and started.</param>
        private void prepareConnectionTimer(bool start)
        {
            connectionTimer = new Timer();
            connectionTimer.Interval = connectionTimerInterval;
            connectionTimer.Elapsed -= CheckConnection;
            connectionTimer.Elapsed += CheckConnection;
            connectionTimer.AutoReset = true;

            if (start)
            {
                connectionTimer.Start();
            }
        }

        /// <summary>
        /// Pauses the connection timer.
        /// </summary>
        /// <param name="stop">If true, will un register the callback method.</param>
        private void pauseTimer(bool stop)
        {
            if(connectionTimer == null) { return; }
            connectionTimer.Stop();

            if (stop)
            {
                connectionTimer.Close();
                connectionTimer = null;
            }
        }

        /// <summary>
        /// resumes the timer. If timer is not instantiated, it will instantiate a new one.
        /// </summary>
        private void resumeTimer()
        {
            if(connectionTimer != null)
            {
                connectionTimer.Start();
            }
            else
            {
                prepareConnectionTimer(true);
            }
        }

        /// <summary>
        /// If client is inactive for this second, they are disconnected.
        /// </summary>
        public int Max_inactivity { get; set; } = 63;

        /// <summary>
        /// callback of checking connection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckConnection(object sender, ElapsedEventArgs e)
        {
            KillOfflineClient(Max_inactivity);
        }

        /// <summary>
        /// Actual method to check connection.
        /// </summary>
        /// <param name="timeS"></param>
        private void KillOfflineClient(int timeS)
        {
            //RaiseOtherEvent($"Refreshing peer list.", null, EventType.ConsoleMessage, null);
            DateTime d = DateTime.UtcNow;
            List<string> dated = new List<string>();
            double diff = 0;
            foreach (var item in connectedClients)
            {
                diff = (d - item.Value.lastDateTime).TotalSeconds;
                if (diff > timeS)
                {
                    item.Value.connected = false;
                    dated.Add(item.Key);
                }
            }

            foreach (var item in dated)
            {
                Console.WriteLine($"Last time: {connectedClients[item].lastDateTime.ToString("MM/dd/yyyy hh:mm:ss.fff")} , Now time: {d.ToString("MM/dd/yyyy hh:mm:ss.fff")} and diff is: {diff}");
                RemovePeerFromConnections(item, true);
            }
        }

        /// <summary>
        /// Call this function to start connecting with a server.
        /// </summary>
        /// <param name="ep">The endpoint of server.</param>
        public bool ConnectToAServer(IPEndPoint ep)
        {
            if(Max_servers >= CountOfConnectedServers)
            {
                Console.WriteLine("Server slots are full, cannot connect to more than " + Max_servers + " servers");
                return false;
            }

            FlashPeer target = new FlashPeer(ep);
            var sh = new HSfromClient(target);
            pendingServers.Add(ep.ToString(), sh);
            return true;
        }

        public void RespondToClientConnection(IPEndPoint ep)
        {
            if(Max_clients >= CountOfConnectedClients)
            {
                Console.WriteLine("Client slots are full, cannot connect to more than " + Max_clients + " clients");
                return;
            }

            FlashPeer client = new FlashPeer(ep);
            var ch = new HSfromServer(client);
            pendingClients.Add(ep.ToString(), ch);
        }

        /// <summary>
        /// When we receive ping, call this method, A server will never ping a client.
        /// </summary>
        /// <param name="ep"></param>
        /// <param name="data"></param>
        public void Ping(IPEndPoint ep, byte[] data)
        {
            if(ep != null && data != null)
            {
                FlashProtocol.Instance.channel.StartSendingData(data, ep);
            }
        }
        /**private Timer AckTimer = new Timer();
        private int AckInterval = 30;//sec*/


    }
}
