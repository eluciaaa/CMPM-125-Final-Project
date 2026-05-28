using System.Collections.Generic;
using UnityEngine;

public class ScoringZone : MonoBehaviour
{
    public List<GameObject> rocksInZone = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Rock"))
        {
            if (!rocksInZone.Contains(other.gameObject))
                rocksInZone.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Rock"))
        {
            rocksInZone.Remove(other.gameObject);
        }
    }

    private void Update()
    {
        // clean null/destroyed references every frame
        rocksInZone.RemoveAll(item => item == null);
    }
}