namespace Carubbi.DataSinks;

public interface IDataSink<T>
{
    Task ProcessAsync(T data);
    Task CompleteAsync();
}
