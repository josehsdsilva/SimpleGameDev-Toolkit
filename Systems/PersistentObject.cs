namespace SimpleGameDev
{
using UnityEngine;

/// <summary>
/// Persists this GameObject and all children across scene loads.
/// Attach to any root GameObject.
/// </summary>
public class PersistentObject : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}

}
