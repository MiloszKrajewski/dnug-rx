using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace HotVsCold
{
	class Program
	{
		static void Main(string[] args)
		{
			//Consumer("bad", BadObservable());
			//Consumer("defer", DeferObservable());
			//Consumer("cold", ColdObservable());
			//Consumer("cold.take2", ColdObservable().Take(2));
			//Consumer("enumerable", Generate("enumerable").ToObservable());
			Consumer("enumerable.take2", Generate("enumerable.take2").ToObservable().Take(2));

			Console.WriteLine("Press <enter>...");
			Console.ReadLine();
		}

		static void Consumer(string name, IObservable<int> observable)
		{
			Console.WriteLine("consumer({0}): subscribe", name);
			observable.Subscribe(
				n => Console.WriteLine("consumer({0}): {1}", name, n),
				_ => Console.WriteLine("consumer({0}): error"),
				() => Console.WriteLine("consumer({0}): complete", name));
		}

		static IEnumerable<int> Generate(string name)
		{
			for (var i = 1; i <= 5; i++)
			{
				Console.WriteLine("producer({0}): {1}", name, i);
				yield return i;
			}
			Console.WriteLine("producer({0}): done", name);
		}

		static void Producer(string name, IObserver<int> observer)
		{
			foreach (var i in Generate(name)) observer.OnNext(i);
			observer.OnCompleted();
		}

		static IObservable<int> BadObservable()
		{
			var subject = new Subject<int>();
			Producer("bad", subject);
			return subject;
		}

		static IObservable<int> DeferObservable()
		{
			return Observable.Defer(() => {
				var subject = new Subject<int>();
				Producer("defer", subject);
				return subject;
			});
		}

		static IObservable<int> ColdObservable()
		{
			return Observable.Create<int>(observer => {
				Producer("cold", observer);
				return () => { };
			});
		}
	}
}
