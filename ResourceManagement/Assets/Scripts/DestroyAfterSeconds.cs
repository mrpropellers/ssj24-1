using UnityEngine;

public class DestroyAfterSeconds : MonoBehaviour
{
    [SerializeField] private float seconds = 4;

    private void Awake()
    {
        Destroy(gameObject, seconds);
    }
}
