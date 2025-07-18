using Unity.Netcode;
using UnityEngine;


/// <summary>
/// Generic classes for the use of singletons.
/// There are three types of singletons:
/// 1.MonoBehaviour -> for the use of singletons to MonoBehaviours.
/// 2.NetWorkBehaviour -> for the use of singletons to NetworkBehaviours.
/// 3.Persistent -> when we need to make sure the object is not destroyed during the session. 
/// </summary>

public class Singleton<T> : MonoBehaviour where T : Component
{
    public static T Instance { get; private set; }

    public virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
        }

        else
        {
            Destroy(gameObject);
        }
    }
}

public class SingletonPersistent<T> : MonoBehaviour where T : Component
{
    public static T Instance { get; private set; }

    public virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

public class SingletonNetwork<T> : NetworkBehaviour where T : Component
{
    public static T Instance { get; private set; }

    public virtual void Awake()
    {
        if (Instance == null)
        {
            Instance = this as T;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
