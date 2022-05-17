// Permanent location - https://github.com/alexeidjango/ovsst

using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Lab1Client
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                // see https://stackoverflow.com/questions/616584/how-do-i-get-the-name-of-the-current-executable-in-c
                var executableName = AppDomain.CurrentDomain.FriendlyName;
                Console.WriteLine("Usage: {0} address port", executableName);
                Environment.Exit(-1);
            }

            var address = args[0];
            var port = args[1];
            
            try
            {    
                Console.Write("Trying to connect to {0}:{1}...", address, port);
                IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(address), Int32.Parse(port));
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                                      
                socket.Connect(ipPoint);
                Console.WriteLine("Connected.");
                Console.Write("> ");
                string message = Console.ReadLine();
                var bytesToSend = Encoding.Unicode.GetBytes(message);
                
                socket.Send(bytesToSend);
                
                StringBuilder builder = new StringBuilder();
                byte[] data = new byte[255];
                do {
                    socket.Receive(data);
                    // AM: technically, the below sucks: reason being that if the byte data is not aligned
                    // (and it most likely is) - we'll have nasty problems with multi-byte decoding. 
                    // Proper way would be to read the *entire* bytes payload, glue it together, and
                    // only then try to decode.
                    var decodedStr = Encoding.UTF8.GetString(data, 0, data.Length);
                    builder.Append(decodedStr);
                } while (socket.Available > 0);
 
                Console.WriteLine("Server responded with:\n {0}", builder);
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine("Unable to start connection: {0}", e.Message);
            }
        }
    }
}
