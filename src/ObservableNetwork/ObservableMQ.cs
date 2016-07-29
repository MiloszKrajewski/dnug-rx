using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZMQ;

namespace ObservableNetwork
{
	public static class ObservableMQ
	{
		private static Context context = new Context();
		private static PollingThread poller = new PollingThread(context);

		public static IDisposable Serve<T>(IObservable<T> observable, int port, Func<T, byte[]> pickler)
		{
			var socket = context.Socket(SocketType.PUB);
			socket.Bind(Transport.TCP, string.Format("*:{0}", port));
			return observable.Subscribe(
				onNext : i => socket.Send(pickler(i)),
				onError : e => socket.Dispose(),
				onCompleted : () => socket.Dispose());
		}
	}
}
