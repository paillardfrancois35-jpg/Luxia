namespace Luxia.App.Services;

/// <summary>Observateur réduit à une action (abonnement aux changements de propriété Avalonia).</summary>
/// <typeparam name="T">Type observé.</typeparam>
internal sealed class Observer<T>(Action<T> onNext) : IObserver<T>
{
    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(T value) => onNext(value);
}
