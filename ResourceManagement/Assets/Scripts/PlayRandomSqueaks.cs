using System.Collections;
using UnityEngine;

public class PlayRandomSqueaks : MonoBehaviour
{
    [SerializeField] private GameObject squeakSfx;
    [SerializeField] private float avgTimeBetweenSqueaks;
    [SerializeField] private float varianceBetweenSqueaks;

    private void Start()
    {
        StartCoroutine(Squeak());
    }

    private IEnumerator Squeak()
    {
        while (true)
        {
            Instantiate(squeakSfx, transform.position, Quaternion.identity);
            yield return new WaitForSeconds(avgTimeBetweenSqueaks + Random.Range(0, varianceBetweenSqueaks));
        }
    }
}
