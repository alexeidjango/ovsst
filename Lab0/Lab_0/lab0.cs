using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Lab0
{
    class Program
    {
        static void Main(string[] args)
        {
            // Print special addresses
            var specialAddresses = new Dictionary<string, IPAddress> {
                {"Loopback", IPAddress.Loopback},
                {"IPV6 Loopback", IPAddress.IPv6Loopback},
                {"Broadcast", IPAddress.Broadcast},
                {"None", IPAddress.None},
                {"IPV6 None", IPAddress.IPv6None},
                {"Any", IPAddress.Any},
                {"IPV6 Any", IPAddress.IPv6Any},
            };
            Console.WriteLine("\nSpecial addresses:\n------------");
            foreach (var entry in specialAddresses) {
                Console.WriteLine("{0}: {1}", entry.Key, entry.Value);
            }
            Console.WriteLine();
            
            // get own machine's info
            AddressFamily[] internalAddrFamilies = {
                AddressFamily.InterNetworkV6, AddressFamily.InterNetwork
            };
            var myHost = Dns.GetHostName();
            var myLocalIpAddresses = Dns.GetHostAddresses(myHost)
                .Where(a => internalAddrFamilies.Contains(a.AddressFamily))
                .OrderBy(a => a.AddressFamily);
            Console.WriteLine("My local machine (hostname: {0})\n------------", myHost);
            foreach (var entry in myLocalIpAddresses) {
                Console.WriteLine("{0}: {1}", entry.AddressFamily, entry.ToString());
            }
            Console.WriteLine();

            // get hostinfo for a given address
            const string hostName = "unn.ru";
            try {
                var hostInfo = Dns.GetHostEntry(hostName);
                Console.WriteLine("Host info for {0}\n------------", hostName);
                foreach (var entry in hostInfo.AddressList)
                {
                    Console.WriteLine(entry.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to resolve host {0}: {1}", hostName, e.Message);
            }
            

            Console.ReadLine();
        }
    }
}