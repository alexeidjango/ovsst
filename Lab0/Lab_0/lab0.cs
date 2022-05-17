// Permanent location - https://github.com/alexeidjango/ovsst

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Lab0
{
    class Program
    {
        static void Main()
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
            
            // get own machine's hostname
            var myHost = Dns.GetHostName();
            Console.WriteLine("My local machine (hostname: {0})\n------------", myHost);
            
            // go over ethernet and wifi interfaces and print their IPs
            var myEnInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && 
                             (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || 
                              ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                .OrderBy(ni => ni.Name);

            foreach (var enInterface in myEnInterfaces) {
                var ipProps = enInterface.GetIPProperties();
                // we only want interfaces with some real IPs assigned
                var uniCastEntries = ipProps.UnicastAddresses
                    .Where(a => new[] {
                        AddressFamily.InterNetworkV6, AddressFamily.InterNetwork
                    }.Contains(a.Address.AddressFamily));
                if (!uniCastEntries.Any())
                {
                    continue;
                }
                Console.WriteLine("{0} ({1})", enInterface.Name, enInterface.NetworkInterfaceType);
                
                foreach (var uniCastEntry in uniCastEntries)
                {
                    Console.WriteLine("  {0} ", uniCastEntry.Address);
                }

                if (ipProps.DnsAddresses.Count() != 0)
                {
                    Console.Write("  DNS: ");
                    foreach (var dnsAddress in ipProps.DnsAddresses)
                    {
                        Console.Write("{0} ", dnsAddress);
                    }
                    Console.WriteLine();
                }
               
                Console.WriteLine();
            }
            Console.WriteLine();

            // get host info for a given address
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