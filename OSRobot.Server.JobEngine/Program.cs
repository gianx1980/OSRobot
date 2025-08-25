using System.Net.NetworkInformation;

Console.WriteLine("Hello world!");


Ping ping = new Ping();
PingReply reply = ping.Send("127.0.0.1", 1000);

Console.WriteLine($"{reply.Status} {reply.RoundtripTime} {reply.Address}");

Console.ReadLine();