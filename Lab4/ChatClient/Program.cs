// Permanent location - https://github.com/alexeidjango/ovsst

using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Lab4Client
{
    class Program
    {
        private static Socket _socket;
        
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

            var name = "";
            Console.Write("Please introduce yourself: ");
            name = Console.ReadLine();
            while (name.Trim() == "")
            {
                Console.Write("\n> ");
                name = Console.ReadLine();
            }

            Console.Clear();
            Console.WriteLine("Trying to connect to {0}:{1}...\n", address, port);

            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(address), Int32.Parse(port));
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                _socket.Connect(ipPoint);
                sendMessage(string.Format("/join {0}", name));
                new ConnectionWorker(_socket);
                
                while (true)
                {
                    string message = Console.ReadLine();
                    removePreviousLine();

                    try {
                        sendMessage(message);
                    } catch(Exception e) {
                        Console.WriteLine("Connection closed by the server: {0}", e.Message);
                        break;
                    }
                }
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
                Console.WriteLine("Connection closed.");
            }
            catch(Exception e)
            {
                Console.WriteLine("Unable to connect to server: {0}. Exiting.", e.Message);
            }
            Environment.Exit(0);
        }
        private static void sendMessage(string message) {
            var bytesToSend = Encoding.UTF8.GetBytes(message);
            _socket.Send(bytesToSend);
        }

        private static void removePreviousLine(int lineCount = 1)
        // output "\e[A\e[K" to console to remove previous line.
        // tested and proven to work in bash, as well as in Rider's built-in console
        {
            byte[] eraseSequence = {0x1B, Convert.ToByte('['), Convert.ToByte('A'), 0x1B, 
                Convert.ToByte('['), Convert.ToByte('K')};
            for (int i = 0; i < lineCount; ++i) {
                Console.Write(Encoding.ASCII.GetString(eraseSequence));
            }
        }
    }
    
    class ConnectionWorker
        {
            private Thread thread = null;
            public ConnectionWorker(Socket handler) 
            {
                thread = new Thread(this.func);
                thread.Start(handler); 
            }
            
            void func(object hnd)
            {
                var handler = (Socket) hnd;
                while (true)
                {
                    try {
                        StringBuilder builder = new StringBuilder();
                        byte[] data = new byte[255];
                        var totalRecvd = 0;
                        do {
                            var br = handler.Receive(data);
                            // AM: technically, the below sucks: reason being that if the byte data is not aligned
                            // (and it most likely is) - we'll have nasty problems with multi-byte decoding. 
                            // Proper way would be to read the *entire* bytes payload, glue it together, and
                            // only then try to decode.
                            var decodedStr = Encoding.UTF8.GetString(data, 0, data.Length);
                            builder.Append(decodedStr);
                            totalRecvd += br;
                        } while (handler.Available > 0);
                        
                        if (totalRecvd == 0) {
                            Console.WriteLine("Server has closed the connection, exiting.");
                            break;
                        }
                        Console.WriteLine(builder);
                    }
                    catch (Exception e) {
                        Console.WriteLine("Unable to read from server, exiting.");
                        break;
                    }
                }
                // if we're terminating the thread - we must terminate application as whole
                Environment.Exit(0);
            }
        }
}
