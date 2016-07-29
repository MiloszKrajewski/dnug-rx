using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZMQ;
using SocketType = ZMQ.SocketType;

namespace ObservableNetwork
{
	class Program
	{
		static void Main(string[] args)
		{
			var context = new Context();
			var thread = new PollingThread(context);

			var push = context.Socket(SocketType.PUSH);
			var pull = context.Socket(SocketType.PULL);
			pull.Bind(Transport.TCP, "*:8123");
			push.Connect(Transport.TCP, "127.0.0.1:8123");

			var obs = thread.Observe(pull);

			var y = obs
				.Select(Encoding.UTF8.GetString)
				.Subscribe(Console.WriteLine);

			var x = obs
				.Select(Encoding.UTF8.GetString)
				.Subscribe(Console.WriteLine);

			push.Send(Encoding.UTF8.GetBytes("Hello"));
			push.Send(Encoding.UTF8.GetBytes("World"));
			push.Send(Encoding.UTF8.GetBytes("Fresh"));

			Console.WriteLine("Press <enter>...");
			Console.ReadLine();

			x.Dispose();
			y.Dispose();

			Console.WriteLine("Press <enter>...");
			Console.ReadLine();

			thread.Dispose();
		}
	}
}
