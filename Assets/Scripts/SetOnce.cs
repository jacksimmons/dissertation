/// <summary>
/// A simple class containing an attribute which cannot be set
/// after the constructor.
/// </summary>
/// <typeparam name="T"></typeparam>
public class SetOnce<T>
{
    public readonly T Value;


    public SetOnce(T value)
    {
        Value = value;
    }
}