using UnityEngine;

public class Rotator : MonoBehaviour
{
    [SerializeField] private Vector3 eulerRotSpeed;

    private void Update()
    {
        transform.rotation *= Quaternion.Euler(eulerRotSpeed * Time.deltaTime);
    }
}
