// Permanent location - https://github.com/alexeidjango/ovsst

using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Lab4Server
{
    class Program
    {
        static int port = 8005;
        private static int maxQueueLength = 10;

        class ConnectionWorker
        {
            private Thread thread = null;
            private static Socket sck = null;
            public ConnectionWorker(Socket handler, string name) 
            {
                thread = new Thread(this.func);
                sck = handler;
                thread.Name = name;
                Console.WriteLine("Starting new connection worker (uuid = {0})", name);
                thread.Start(handler); 
            }
            
            void func(object hnd)
            {
                var handler = (Socket) hnd;
                var threadName = Thread.CurrentThread.Name;
                while (true) { 
                    StringBuilder builder = new StringBuilder();
                    byte[] data = new byte[255];
                    int totalRecvd = 0;
                    int chunkSize = 0;
                    try { 
                        do { 
                            chunkSize = handler.Receive(data);
                            var decodedStr = Encoding.UTF8.GetString(data, 0, chunkSize);
                            builder.Append(decodedStr);
                             totalRecvd += chunkSize;
                        } while (handler.Available > 0);
                    } catch { 
                        Console.WriteLine("Client {0} disconnected", threadName);
                        break;
                    }
                    if (totalRecvd == 0) {
                        Console.WriteLine("{0} left.", threadName);
                        break;
                    }
                    var msg = builder.ToString().TrimEnd('\r', '\n').Trim();
                    var exitRequested = false;
                    var broadcastMsg = "";
                    var targetMsg = "";
                    if (msg.StartsWith("/")) {
                        // this is a command. I'm not particularly proud of the code below,
                        // but I'd really like to avoid writing any more or less complex parser here
                        var commandParts = msg.Split(" ");
                        var command = commandParts[0];
                        switch (command) {
                            case "/join":
                                var name = msg.Replace("/join", "").Trim();
                                if (name.Length > 0)
                                {
                                    threadName = name;
                                    broadcastMsg = string.Format("{0} joined", name);
                                    targetMsg = string.Format(
                                        "Hello {0}! Welcome to the server! Type {1} to exit.\n", name, "/exit");
                                    whisperToClient(targetMsg);
                                    broadcastMessage(broadcastMsg);
                                }
                                break;
                            case "/list": break;
                            case "/exit":
                                broadcastMsg = string.Format("{0} left.", threadName);
                                targetMsg = string.Format("Good bye, {0}!\n", threadName);
                                whisperToClient(targetMsg);
                                broadcastMessage(broadcastMsg);
                                exitRequested = true;
                                break;
                            default: 
                                whisperToClient("Sorry, this command is not recognized\n");
                                break;
                        }
                    } else {
                        broadcastMsg = string.Format("{0}: {1}: {2}",
                            threadName, DateTime.Now.ToLocalTime(), msg);
                        targetMsg = string.Format("YOU: {1}: {2}",
                            DateTime.Now.ToLocalTime(), msg);
                        whisperToClient(targetMsg);
                        broadcastMessage(broadcastMsg);
                    }

                    if (exitRequested) {
                        break;
                    }
                }
                 // handler.Send(Encoding.UTF8.GetBytes("Closing connection, bye-bye!"));
                 handler.Shutdown(SocketShutdown.Both);
                 handler.Close();
            }

            private static void broadcastMessage(string msg) {
                Console.WriteLine(msg);
            }

            private static void whisperToClient(string msg) {
                sck.Send(Encoding.UTF8.GetBytes(msg));
            }
        }

        private static List<ConnectionWorker> _connectionWorkers;

        static void Main(string[] args)
        {
            _connectionWorkers = new List<ConnectionWorker>();
            var myHost = Dns.GetHostName();
            var myIpV4Ips = Dns.GetHostAddresses(myHost)
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork);
            Console.Write("Server is running on {0}. Available IPs: ", myHost);
            var lastAddr = myIpV4Ips.Last();
            foreach (var ipv4addr in myIpV4Ips)
            {
                Console.Write("{0}{1}", ipv4addr, lastAddr == ipv4addr ? "\n" : ", ");
            }

            var listenAddr = myIpV4Ips.First();
            var ipPoint = new IPEndPoint(listenAddr, port);
            var listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                // Listen on the socket. Whenever new connection comes in, start
                // and pass it to the new thread
                listenSocket.Bind(ipPoint);
                listenSocket.Listen(maxQueueLength);
                Console.WriteLine("Listening on {0}:{1}...", listenAddr, port);
                while (true)
                {
                    var handler = listenSocket.Accept();
                    Guid uuid = Guid.NewGuid();
                    string uuidStr = uuid.ToString();
                    Console.WriteLine("Starting new session...");
                    var th = new ConnectionWorker(handler, uuidStr);
                    _connectionWorkers.Append(th);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to start server: {0}. Exiting.", e.Message);
            }
        }
   }
}
