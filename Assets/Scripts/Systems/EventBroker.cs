using System;
using System.Collections.Generic;

public static class EventBroker
{
    private static readonly Dictionary<Type, Delegate> eventTable = new();

    public static void Subscribe<T>(Action<T> listener)
    {
        if (eventTable.TryGetValue(typeof(T), out var existingDelegate))
            eventTable[typeof(T)] = Delegate.Combine(existingDelegate, listener);
        else
            eventTable[typeof(T)] = listener;
    }

    public static void Unsubscribe<T>(Action<T> listener)
    {
        if (eventTable.TryGetValue(typeof(T), out var existingDelegate))
        {
            var currentDel = Delegate.Remove(existingDelegate, listener);
            if (currentDel == null)
                eventTable.Remove(typeof(T));
            else
                eventTable[typeof(T)] = currentDel;
        }
    }

    public static void Publish<T>(T eventData)
    {
        if (eventTable.TryGetValue(typeof(T), out var del))
            (del as Action<T>)?.Invoke(eventData);
    }

    public static void Clear()
    {
        eventTable.Clear();
    }
}
