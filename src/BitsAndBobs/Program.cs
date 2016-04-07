using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitsAndBobs
{
	class Program
	{
		static void Main(string[] args)
		{
			IntProducerDemo();
			Console.WriteLine("Press <enter>...");
			Console.ReadLine();
		}

		static void IntProducerDemo()
		{
			var producer = new IntProducer();
			AttachGenericPrinter(producer.OnProduced);
			producer.ProduceMany(100);
		}

		static void AttachGenericPrinter<T>(EventHandler<T> handler)
		{
			handler += (s, e) => Console.WriteLine(e);
		}
	}

	public class IntProducer
	{
		public event EventHandler<int> OnProduced = (s, e) => { };

		public void ProduceMany(int limit)
		{
			for (int i = 0; i < limit; i++)
				OnProduced(this, i);
		}
	}
}
