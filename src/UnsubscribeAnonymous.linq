<Query Kind="Program" />

public class Producer {
	public event EventHandler<int> OnProduced;

	public void ProduceMany(int limit) {
		for (int i = 0; i < limit; i++) 
			OnProduced(this, i);
	}
}

void Main() {
	var producer = new Producer();
	producer.OnProduced += (s, e) => Console.WriteLine(e);
	producer.OnProduced -= (s, e) => Console.WriteLine(e);
	producer.ProduceMany(100);
}