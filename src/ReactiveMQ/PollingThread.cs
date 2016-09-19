using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using ZMQ;

namespace ReactiveMQ
{
	public class PollingThread : IDisposable
	{
		private static readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

		private readonly Context _context;
		private readonly string _guid = Guid.NewGuid().ToString("N");
		private readonly Socket _poke;
		private readonly Socket _wake;

		private readonly Thread _thread;
		private readonly CancellationTokenSource _cancel;

		private readonly ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();
		private readonly IDictionary<Guid, PollItem> _polls = new Dictionary<Guid, PollItem>();
		private PollItem[] _items;

		public PollingThread(Context context)
		{
			_context = context;
			_poke = context.Socket(SocketType.PAIR);
			_wake = context.Socket(SocketType.PAIR);
			_wake.Bind(Transport.INPROC, _guid);
			_polls.Add(Guid.NewGuid(), PollAndIgnore(_wake));
			_poke.Connect(Transport.INPROC, _guid);
			_cancel = new CancellationTokenSource();
			_thread = new Thread(() => Loop(_cancel.Token)) { IsBackground = true };
			_thread.Start();
		}

		private static PollItem PollAndIgnore(Socket socket)
		{
			var poll = socket.CreatePollItem(IOMultiPlex.POLLIN);
			poll.PollInHandler += (s, e) => s.Recv();
			return poll;
		}

		public void Loop(CancellationToken cancel)
		{
			var interval = (long)(_interval.TotalMilliseconds * 1000);
			while (!cancel.IsCancellationRequested)
			{
				if (_items == null)
					_items = _polls.Values.ToArray();
				_context.Poll(_items, interval);
				Drain();
			}
		}

		private void Drain()
		{
			Action action;
			while (_actions.TryDequeue(out action)) action();
		}

		private void Enqueue(Action action)
		{
			_actions.Enqueue(action);
			_poke.Send();
		}

		public Action Poll(Socket socket, Action<byte[]> receive)
		{
			var guid = Register(socket, receive);
			return () => Unregister(guid);
		}

		private Guid Register(Socket socket, Action<byte[]> receive)
		{
			var guid = Guid.NewGuid();
			var poll = socket.CreatePollItem(IOMultiPlex.POLLIN | IOMultiPlex.POLLERR);
			poll.PollInHandler += (s, e) => receive(s.Recv());
			Enqueue(() => {
				_polls.Add(guid, poll);
				_items = null;
			});
			return guid;
		}

		private void Unregister(Guid guid)
		{
			Enqueue(() => {
				_polls.Remove(guid);
				_items = null;
			});
		}

		private void Cancel()
		{
			_cancel.Cancel();
			_poke.Send();
		}

		/// <summary>Performs application-defined tasks associated with 
		/// freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			Cancel();
			_thread.Join();
			_poke.Dispose();
			_wake.Dispose();
		}
	}
}
