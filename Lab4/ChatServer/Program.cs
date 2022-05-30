// Permanent location - https://github.com/alexeidjango/ovsst

using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Lab4Server
{
    class Program
    {
        private static int maxQueueLength = 10;
        
        class ConnectionWorker
        {
            private Socket _sck = null;
            
            private Guid _uuid;
            public Guid uuid => _uuid;
            private string _userName = "";
            public string userName => _userName;

            public ConnectionWorker(Socket sck) 
            {
                var _thread = new Thread(this.func);
                _uuid = Guid.NewGuid();
                _sck = sck;
                _thread.Name = "";
                Console.WriteLine("Starting a worker thread for the new connection.");
                _thread.Start(sck); 
            }
            
            void func(object hnd)
            {
                var handler = (Socket) hnd;
                _userName = Thread.CurrentThread.Name;
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
                        Console.WriteLine("Client {0} disconnected", userName);
                        break;
                    }
                    if (totalRecvd == 0) {
                        Console.WriteLine("{0} left.", userName);
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
                                if (userName.Length > 0) {
                                    break;
                                }
                                var name = msg.Replace("/join", "").Trim();
                                if (name.Length > 0) {
                                    _userName = name;
                                    broadcastMsg = string.Format("\n{0} joined.\n", name);
                                    targetMsg = string.Format(
                                        "Hello {0}! Welcome to the server! Type {1} to exit.\n", name, "/exit");
                                    whisper(targetMsg);
                                    broadcast(broadcastMsg);
                                }
                                break;
                            case "/list": 
                                whisper("People in this chat:\n");
                                foreach (var worker in _connectionWorkers) {
                                    whisper(String.Format(" * {0}\n", worker.userName));
                                }
                                break;
                            case "/exit":
                                broadcastMsg = string.Format("\n{0} left.\n", userName);
                                targetMsg = string.Format("Good bye, {0}!\n", userName);
                                whisper(targetMsg);
                                broadcast(broadcastMsg);
                                exitRequested = true;
                                break;
                            default: 
                                whisper("Sorry, this command is not recognized\n");
                                break;
                        }
                    } else {
                        broadcastMsg = string.Format("{0}:\t{1}:\t{2}",
                            userName, DateTime.Now.ToLocalTime(), msg);
                        targetMsg = string.Format("You:\t{0}:\t{1}",
                            DateTime.Now.ToLocalTime(), msg);
                        whisper(targetMsg);
                        broadcast(broadcastMsg);
                    }

                    if (exitRequested) {
                        break;
                    }
                }
                 handler.Shutdown(SocketShutdown.Both);
                 handler.Close();
                 _connectionWorkers.Remove(_connectionWorkers.Find(w => w.uuid == uuid));
                 // before disconnecting, remove yourself from the list of workers
            }

            private void broadcast(string msg) {
                Console.WriteLine(msg.TrimEnd('\n').TrimStart('\n'));
                foreach (var worker in _connectionWorkers.Where(w => w.uuid != _uuid)) {
                    worker.whisper(msg);
                }
            }

            private void whisper(string msg) {
                _sck.Send(Encoding.UTF8.GetBytes(msg));
            }
        }

        private static List<ConnectionWorker> _connectionWorkers;

        static void Main(string[] args)
        {
            _connectionWorkers = new List<ConnectionWorker>();
            if (args.Length != 2)
            {
                // see https://stackoverflow.com/questions/616584/how-do-i-get-the-name-of-the-current-executable-in-c
                var executableName = AppDomain.CurrentDomain.FriendlyName;
                Console.WriteLine("Usage: {0} address port", executableName);
                Environment.Exit(-1);
            }
            var address = args[0];
            var port = Int32.Parse(args[1]);
            
            var listenAddr = IPAddress.Parse(address);
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
                    Console.WriteLine("Starting new session...");
                    var th = new ConnectionWorker(handler);
                    _connectionWorkers.Add(th);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to start server: {0}. Exiting.", e.Message);
            }
        }
   }
}
