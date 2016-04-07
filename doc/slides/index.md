- title : Reactive Extensions, the answer to the ultimate question of life, the universe, and everything
- description : a gentle introduction to Reactive Extensions
- author : Milosz Krajewski
- theme : beige
- transition : default

***

## Reactive Extensions

![Eveything is a stream](images/dog-stream.jpg)

### the answer to the ultimate question of life, the universe, and everything

***

### About me

- Milosz Krajewski
- BLOBAs @ Sepura
- first line of code written in ~1984
- C, C++, C#, SQL, Java, Delphi
- (Iron)Python, F#, Scala, Kotlin

***

## Observer Pattern

![Observer Pattern](images/observer-wikipedia.png)

the GoF version in Java...

---

...and converted to C#

```csharp
public interface IObserver<T> {
    void OnNotification(T item); // Notify
}

public interface ISubject<T> {
    void Subscribe(IObserver<T> observer); // registerObserver
    void Unsubscribe(IObserver<T> observer); // unregisterObserver
    void Notify(T item); // notifyObservers
}
```

***

## .NET events are GoF type subjects

```csharp
// Subject
public event Action<T> OnNotification; // = _ => {};

// Subject.Subscribe(Observer)
this.OnNotification += OnNotificationHandler;

// Subject.Unsubscribe(Observer)
this.OnNotification -= OnNotificationHandler;

// Subject.Notify(item)
this.OnNotification(item);

// Observer.OnNotification(item)
public void OnNotificationHandler(T item) { ... }
```

***

### What's wrong with .NET events?

---

* not first-class citizens

***

### From GoF to Rx

```csharp
public interface IObserver<T> {
    void OnNotification(T item);
}
```

```csharp
public interface IObserver<T> {
    void OnNotification(T|void|Exception item);
}
```

```csharp
public interface IObserver<T> {
    void OnNext(T item);
    void OnComplete();
    void OnError(Exception error);
}
```

---

```csharp
public interface ISubject<T> {
    void Subscribe(IObserver<T> observer);
    void Unsubscribe(IObserver<T> observer);
    void Notify(T item);
}
```

```csharp
public interface IObservable<T> {
    void Subscribe(IObserver<T> observer);
    void Unsubscribe(IObserver<T> observer);
}

public interface ISubject<T>: IObservable<T> {
    void Notify(T item);
}
```

---

```csharp
public interface IObservable<T> {
    void Subscribe(IObserver<T> observer);
    void Unsubscribe(IObserver<T> observer);
}
```

```csharp
public interface IObservable<T> {
    IDisposable Subscribe(IObserver<T> observer);
}
```

---

```csharp
public interface ISubject<T>: IObservable<T> {
    void Notify(T item);
}
```

```csharp
public interface ISubject<T>: IObservable<T> {
    void Notify(T|void|Exception item);
}
```

```csharp
public interface ISubject<T>: IObservable<T> {
    void NotifyNext(T item);
    void NotifyComplete();
    void NotifyError(Exception error);
}
```

---

```csharp
public interface ISubject<T>: IObservable<T> {
    void NotifyNext(T item);
    void NotifyComplete();
    void NotifyError(Exception error);
}
```

but signatures of those methods are identical to

```csharp
public interface IObserver<T> {
    void OnNext(T item);
    void OnComplete();
    void OnError(Exception error);
}
```

therefore

```csharp
public interface ISubject<T>: IObservable<T>, IObserver<T> { }
```

---

```csharp
public interface IObserver<T> {
    void OnNext(T item);
    void OnComplete();
    void OnError(Exception error);
}

public interface IObservable<T> {
    IDisposable Subscribe(IObserver<T> observer);
}

public interface ISubject<T>: IObservable<T>, IObserver<T> { }
```

***

Observables are like .NET events<br>
(or the other way around)

```csharp
public class IntProducer
{
    public event EventHandler<int> OnProduced; // Observable
}
```

```csharp
public class IntFactory
{
    public IntFactory()
    {
        var producer = new IntProducer();
        producer.OnProduced += HandleProduced; // Register
        // ...
        producer.OnProduced -= HandleProduced; // Unregister
    }

    // Observer
    public void HandleProduced(object sender, int item)
    {
        Console.WriteLine("Received: {0}", item);
    }
}
```

---

```csharp
public class IntProducer
{
    public event EventHandler<int> OnProduced = (s, e) => { };

    public void ProduceMany(int limit)
    {
        for (int i = 0; i < limit; i++)
            OnProduced(this, i);
    }
}
```

---

```csharp
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
```
---

```
error CS0070: The event 'BitsAndBobs.IntProducer.OnProduced' can only appear on the left hand side of += or -= (except when used from within the type 'BitsAndBobs.IntProducer')
```

***

So what is `Observable<T>`?

Some say it is:

* dual ('opposite') to `Enumerable<T>`
* similar to `Task<T>`
* also an `EventHandler<T>`

---

### Duality

> Function **$f(x)$** is dual to **$g(x)$** if **$g(f(x)) = x$**

for example:

**$f(x) = x\times7$** and **$g(x) = x/7$** are dual.

---

```csharp
int Parse(string);
string Render(int);
```

```csharp
byte[] Serialize<T>(T);
T Deserialize<T>(byte[]);
```

```csharp
void Consume<T>(T);
T Produce<T>(void);
```
---

```csharp
interface IEnumerable<T> {
    IEnumerator<T> GetEnumerator();
}

interface IEnumerable<T> {
    IEnumerator<T> GetEnumerator(void);
}
```

---

```csharp
interface IEnumerator<T> {
    bool MoveNext();
    T Current { get; }
}
```

```csharp
interface IEnumerator<T> {
    bool|Exception MoveNext(void);
    T GetCurrent(void);
}
```

```csharp
interface IEnumerator<T> {
    T|void|Exception GetNext(void);
    // Either[Option[T], Exception]
}
```

---

### Magic!

![magic hat](images/magic-hat.jpg)

* Swap inputs and outputs
* Use opposite terms

---

```csharp
interface IEnumerable<T> {
    IEnumerator<T> GetEnumerator(void);
}
```

```csharp
interface IObservable<T> {
    void SetObserver(IObserver<T>);
}
```

---

```csharp
interface IEnumerator<T> {
    T|void|Exception GetNext(void);
}

interface IObserver<T> {
    void PutNext(T|void|Exception);
}
```

---

```csharp
interface IObservable<T> {
    void SetObserver(IObserver<T>);
}
```

```csharp
interface IObservable<T> {
    void Subscribe(IObserver<T>);
}
```

---

```csharp
interface IObserver<T> {
    void PutNext(T|void|Exception);
}
```

```csharp
interface IObserver<T> {
    void OnNext(T);
    void OnComplete(void);
    void OnError(Exception);
}
```

|                    | One       | Many            |
|-------------------:|:---------:|:---------------:|
| **Synchronously**  | `T`       | `Enumerable<T>` |
| **Asynchronously** | `Task<T>` | `Observable<T>` |

***

### http://reactivex.io

**Bart de Smet** @ DevCamp 2010<br>
"Rx: Curing your asynchronous programming blues"<br>
http://bit.ly/1qsXfsx

**Mike Taulty** @ DevDays 2011<br>
"Reactive Extensions for .NET for the Rest of Us"<br>
http://bit.ly/1PSoukV
