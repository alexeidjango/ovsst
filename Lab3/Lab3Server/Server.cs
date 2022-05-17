// Permanent location - https://github.com/alexeidjango/ovsst

using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Lab3Server
{
    class Program
    {
        static int port = 8005;
        private static int maxQueueLength = 10;
        private static string exitKeyword = "exit";

        class ConnectionWorker
        {
            private Thread thread = null;
            public ConnectionWorker(Socket handler, string name) 
            {
                thread = new Thread(this.func);
                thread.Name = name;
                Console.WriteLine("Starting new connection worker (uuid = {0})", name);
                thread.Start(handler); 
            }
            
            void func(object hnd)
            {
                var handler = (Socket) hnd;
                var threadName = Thread.CurrentThread.Name;
            
                 var welcomeMsg = String.Format(
                     "Welcome to the server. Please type \"{0}\" to disconnect\n\n", exitKeyword);
                 handler.Send(Encoding.ASCII.GetBytes(welcomeMsg.ToString()));
                 
                 while (true)
                 {
                     StringBuilder builder = new StringBuilder();
                     byte[] data = new byte[255];
                     int totalRecvd = 0;
                     int chunkSize = 0;
                     do {
                         chunkSize = handler.Receive(data);
                         var decodedStr = Encoding.UTF8.GetString(data, 0, chunkSize);
                         builder.Append(decodedStr);
                         totalRecvd += chunkSize;
                     } while (handler.Available > 0);
                 
                     var msg = builder.ToString().TrimEnd('\r', '\n');
                     Console.WriteLine("{0}: {1}: Got msg: {2}", 
                         DateTime.Now.ToLocalTime(), threadName, msg);
                     // check exit conditions
                     if (msg.Equals(exitKeyword.ToString())) {
                         Console.WriteLine("Stop keyword received; closing session.");
                         break;
                     }
                     
                     if (totalRecvd == 0) {
                         Console.WriteLine("Session ended by client.");
                         break;
                     }
                    
                     handler.Send(Encoding.UTF8.GetBytes(builder.ToString()));
                 }
                 handler.Send(Encoding.UTF8.GetBytes("Closing connection, bye-bye!"));
                 handler.Shutdown(SocketShutdown.Both);
                 handler.Close();
            }
        }
        
        
        static void Main(string[] args)
        {     
             var myHost = Dns.GetHostName();
             var myIpV4Ips = Dns.GetHostAddresses(myHost)
                 .Where(a => a.AddressFamily == AddressFamily.InterNetwork);
             Console.Write("Server is running on {0}. Available IPs: ", myHost);
             var lastAddr = myIpV4Ips.Last();
             foreach (var ipv4addr in myIpV4Ips) {
                 Console.Write("{0}{1}", ipv4addr, lastAddr == ipv4addr ? "\n" : ", ");
             }

             var listenAddr = myIpV4Ips.First();
             var ipPoint = new IPEndPoint(listenAddr, port);
             var listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
             try {
                 // Listen on the socket. Whenever new connection comes in, start
                 // and pass it to the new thread
                 listenSocket.Bind(ipPoint);
                 listenSocket.Listen(maxQueueLength);
                 Console.WriteLine("Listening on {0}:{1}...", listenAddr, port);
                 while (true) {
                     var handler = listenSocket.Accept();
                     Guid uuid = Guid.NewGuid();
                     string uuidStr = uuid.ToString();
                     Console.WriteLine("Staring new session...");
                     new ConnectionWorker(handler, uuidStr);
                 }
             }
             catch (Exception e) {
                 Console.WriteLine("Unable to start server: {0}. Exiting.", e.Message);
             }
        }
   }
}
