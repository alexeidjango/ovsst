// Permanent location - https://github.com/alexeidjango/ovsst

using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Lab2Server
{
    class Program
    {
        static int port = 8005;
        private static int maxQueueLength = 10;
        private static string exitKeyword = "panda";
        
        static void Main(string[] args)
        {     
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
             try {
                 listenSocket.Bind(ipPoint);
                 listenSocket.Listen(maxQueueLength);
                 Console.WriteLine("Listening on {0}:{1}...", listenAddr, port);

                 while (true) {
                     var handler = listenSocket.Accept();
                     Console.WriteLine("Staring new session...");
                     var welcomeMsg = String.Format(
                         "Welcome to the server. Please type \"{0}\" to disconnect\n\n", exitKeyword);
                     handler.Send(Encoding.ASCII.GetBytes(welcomeMsg.ToString()));

                     while (true)
                     {
                         StringBuilder builder = new StringBuilder();
                         byte[] data = new byte[255];
                         int totalRecvd = 0;
                         int chunkSize = 0;
                         do
                         {
                             chunkSize = handler.Receive(data);

                             // AM: technically, the below sucks: reason being that if the byte data is not aligned
                             // (and it most likely is) - we'll have nasty problems with multi-byte decoding. 
                             // Proper way would be to read the *entire* bytes payload, glue it together, and
                             // only then try to decode.
                             var decodedStr = Encoding.UTF8.GetString(data, 0, chunkSize);
                             builder.Append(decodedStr);
                             totalRecvd += chunkSize;
                         } while (handler.Available > 0);

                         var msg = builder.ToString().TrimEnd('\r', '\n');
                         Console.WriteLine(DateTime.Now.ToShortTimeString() + ": " + msg);
                         Console.WriteLine("Total of {0} bytes received.", totalRecvd);
                         if (msg.Equals(exitKeyword))
                         {
                             Console.WriteLine("Stop keyword received; closing session.");
                             break;
                         }
                         else
                         {
                             handler.Send(Encoding.UTF8.GetBytes(builder.ToString()));
                         }
                     }
                     handler.Send(Encoding.UTF8.GetBytes("Closing connection, bye-bye!"));
                     handler.Shutdown(SocketShutdown.Both);
                     handler.Close();
                 }
             }
             catch (Exception e) {
                 Console.WriteLine("Unable to start server: {0}. Exiting.", e.Message);
             }
        }
   }
}
