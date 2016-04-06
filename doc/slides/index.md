- title :  Software Hollywood style: Don't call us, we'll call you
- description : Reactive Extensions in .NET
- author : Milosz Krajewski
- theme : beige
- transition : default

***

### Software Hollywood style:
### Don't call us, we'll call you

![Eveything is a stream](images/dog-stream.jpg)

#### gentle introduction to Reactive Extensions

***

### About me

- Milosz Krajewski
- BLOBAs @ Sepura
- first line of code written in ~1984
- C, C++, C#, SQL, Java
- (Iron)Python, F#, Scala, Kotlin

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
