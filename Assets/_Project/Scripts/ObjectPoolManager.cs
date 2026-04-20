using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    private static ObjectPoolManager instance;
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private Dictionary<GameObject, GameObject> instanceToPrefab = new Dictionary<GameObject, GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static ObjectPoolManager EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

#if UNITY_2023_1_OR_NEWER
        instance = FindAnyObjectByType<ObjectPoolManager>();
#else
        instance = FindObjectOfType<ObjectPoolManager>();
#endif
        
        if (instance != null)
        {
            return instance;
        }

        GameObject go = new GameObject("ObjectPoolManager");
        instance = go.AddComponent<ObjectPoolManager>();
        DontDestroyOnLoad(go);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        return EnsureInstance().SpawnInternal(prefab, position, rotation);
    }

    public static void ReturnToPool(GameObject instance)
    {
        if (instance == null) return;
        
        if (ObjectPoolManager.instance != null)
        {
            ObjectPoolManager.instance.ReturnInternal(instance);
        }
        else
        {
            Destroy(instance);
        }
    }

    private GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new Queue<GameObject>();
        }

        GameObject obj;
        if (pools[prefab].Count > 0)
        {
            obj = pools[prefab].Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation);
            instanceToPrefab[obj] = prefab;
            
            // Чтобы объекты не захламляли свалку рута, можно складывать их в пул
            obj.transform.SetParent(transform);
        }

        return obj;
    }

    private void ReturnInternal(GameObject instance)
    {
        if (!instanceToPrefab.TryGetValue(instance, out GameObject prefab))
        {
            Destroy(instance);
            return;
        }

        instance.SetActive(false);
        if (!pools.ContainsKey(prefab))
        {
            pools[prefab] = new Queue<GameObject>();
        }
        pools[prefab].Enqueue(instance);
    }
}
