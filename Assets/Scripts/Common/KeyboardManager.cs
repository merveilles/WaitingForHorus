using System;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardManager : MonoBehaviour, IKeyboard
{
    static KeyboardManager instance;
    public static IKeyboard Instance 
    {
        get 
        {
            if (instance == null) 
            {
                instance = FindObjectOfType(typeof (KeyboardManager)) as KeyboardManager;
                if (instance == null)
                    throw new InvalidOperationException("No instance in scene!");
            }
            return instance;
        }
    }
    void OnApplicationQuit() 
    {
        instance = null;
    }
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    readonly Dictionary<KeyCode, TimedButtonState> keyStates = new Dictionary<KeyCode, TimedButtonState>(KeyCodeEqualityComparer.Default);

    readonly List<KeyCode> registeredKeys = new List<KeyCode>();

    public void RegisterKey(KeyCode key)
    {
        if (!registeredKeys.Contains(key))
            registeredKeys.Add(key);
    }

    public TimedButtonState GetKeyState(KeyCode key)
    {
        TimedButtonState state;
        if (!keyStates.TryGetValue(key, out state))
            state = new TimedButtonState();

        return state;
    }

    void Update()
    {
        var dt = Time.deltaTime;

        foreach (var key in registeredKeys)
        {
            TimedButtonState state;
            bool down = Input.GetKey(key);

            if (keyStates.TryGetValue(key, out state))
            {
                var nextState = state.NextState(down, dt);
                if (nextState != state)
                {
                    keyStates.Remove(key);
                    keyStates.Add(key, state.NextState(down, dt));
                }
            }
            else
                keyStates.Add(key, state.NextState(down, dt));
        }
    }
}

public interface IKeyboard
{
    void RegisterKey(KeyCode key);
    TimedButtonState GetKeyState(KeyCode key);
}

public class KeyCodeEqualityComparer : IEqualityComparer<KeyCode>
{
    public static readonly KeyCodeEqualityComparer Default = new KeyCodeEqualityComparer();
    public bool Equals(KeyCode x, KeyCode y) { return x == y; }
    public int GetHashCode(KeyCode obj) { return (int)obj; }
}
public class KeyCodeComparer : IComparer<KeyCode>
{
    public static readonly KeyCodeComparer Default = new KeyCodeComparer();
    public int Compare(KeyCode x, KeyCode y)
    {
        if (x < y) return -1;
        if (x > y) return 1;
        return 0;
    }
}