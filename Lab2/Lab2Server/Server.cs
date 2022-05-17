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
                     StringBuilder builder = new StringBuilder();
                     byte[] data = new byte[255];
                     int totalRecvd = 0;
                     int chunkSize = 0;
                     do {
                         chunkSize = handler.Receive(data);
                         
                         // AM: technically, the below sucks: reason being that if the byte data is not aligned
                         // (and it most likely is) - we'll have nasty problems with multi-byte decoding. 
                         // Proper way would be to read the *entire* bytes payload, glue it together, and
                         // only then try to decode.
                         var decodedStr = Encoding.UTF8.GetString(data, 0, data.Length);
                         builder.Append(decodedStr);
                         totalRecvd += chunkSize;
                     } while (handler.Available > 0);
                     
                     Console.WriteLine(DateTime.Now.ToShortTimeString() + ": " + builder.ToString());
                     Console.WriteLine("Total of {0} bytes received.", totalRecvd);

                     var response = String.Format(
                         "Received by server: {0}", builder.ToString());
                     handler.Send(Encoding.UTF8.GetBytes(builder.ToString()));
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
