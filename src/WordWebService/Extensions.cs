using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordWebService
{
	public static class Extensions
	{
		public static IObservable<T> Observe<T>(this Task<T> task)
		{
			return Observable.FromAsync(() => task);
		}
	}
}
