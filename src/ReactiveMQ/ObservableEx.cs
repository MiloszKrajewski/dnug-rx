using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace System.Reactive.Linq
{
	public static class ObservableEx
	{
		public static IObservable<T[]> Pairwise<T>(this IObservable<T> observable)
		{
			return Observable.Create<T[]>(observer => {
				var prev = default(T);
				var initialized = false;
				return observable.Subscribe(
					next => {
						if (initialized) observer.OnNext(new[] { prev, next });
						initialized = true;
						prev = next;
					},
					observer.OnError,
					observer.OnCompleted);
			});
		}

		public static IObservable<U> SelectLatest<T, U>(
			this IObservable<T> observable,
			Func<T, CancellationToken, Task<U>> selector)
		{
			return observable
				.Select(item => Observable.FromAsync(token => selector(item, token)))
				.Switch();
		}
	}
}
