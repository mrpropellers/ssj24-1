using System.Collections;
using UnityEngine;

public class PlayDingOnCamera : MonoBehaviour
{
    [SerializeField] private GameObject toSpawn;
    [SerializeField] private float seconds;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(seconds);
        GameObject cam = Camera.main.gameObject;
        Instantiate(toSpawn, cam.transform.position, Quaternion.identity, cam.transform);
    }
}
