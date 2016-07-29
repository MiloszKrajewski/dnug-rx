using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace WordWebService
{
	public static class ObservableEx
	{
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
