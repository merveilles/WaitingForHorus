using System.Collections.Generic;

public class Pool<T> where T : class, new()
{
    readonly Stack<T> stack;

    public Pool() : this(0) { }
    public Pool(int size)
    {
        stack = new Stack<T>(size);
        Size = size;
    }

    public T Take()
    {
        return stack.Count > 0 ? stack.Pop() : new T();
    }

    public void Return(T item)
    {
        stack.Push(item);
    }

    int size;
    public int Size
    { 
        get { return size; }
        set
        {
            var left = value - size;
            if (left > 0)
                for (int i = 0; i < left; i++)
                    stack.Push(new T());

            size = value;
        }
    }

    public int Available
    {
        get { return stack.Count; }
    }
}
