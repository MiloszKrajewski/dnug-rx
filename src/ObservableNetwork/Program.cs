using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ObservableNetwork
{
	class Program
	{
		static void Main(string[] args)
		{
		}
	}

	public class ObservableNetwork
	{
		public IObservable<TcpClient> Listen(int port)
		{
			return Observable.Create<TcpClient>(async (sink, token) => {
				var listener = new TcpListener(IPAddress.Any, port);
				listener.
				while (true) {
					var client = await listener.AcceptTcpClientAsync();
					sink.OnNext(client);
				}
			});
		};


		public IDisposable Serve<T>(IObservable<T> observable, int port)
		{
			var listener = new TcpListener(IPAddress.Any, port);
			listener.Acc
		}
	}
}
