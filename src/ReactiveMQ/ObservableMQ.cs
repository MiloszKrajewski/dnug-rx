using System;
using System.Net;
using System.Reactive.Linq;
using ZMQ;
using SocketType = ZMQ.SocketType;

namespace ReactiveMQ
{
	public static class ObservableMQ
	{
		private static readonly Context Context = new Context();
		private static readonly PollingThread Poller = new PollingThread(Context);
		private static readonly byte[] AllSubjects = new byte[0];

		public static IDisposable Publish(IObservable<byte[]> observable, int port)
		{
			var socket = Context.Socket(SocketType.PUB);
			socket.Bind(Transport.TCP, $"*:{port}");
			return observable.Subscribe(
				d => socket.Send(d),
				e => socket.Dispose(),
				() => socket.Dispose());
		}

		public static IObservable<byte[]> Subscribe(IPAddress address, int port)
		{
			var socket = Context.Socket(SocketType.SUB);
			socket.Connect(Transport.TCP, $"{address}:{port}");
			socket.Subscribe(AllSubjects);
			return Observable.Create<byte[]>(
				emitter => Poller.Poll(socket, emitter.OnNext)
			).Publish().RefCount();
		}
	}
}
