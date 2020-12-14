using UnityEngine;
using System;
using System.Collections.Generic;

public class Particles : MonoBehaviour
{
    #region Private Fields

    Vector3[] randPos;
    Dictionary<string, int> prevFrameConnections = new Dictionary<string, int>();
    Dictionary<string, int> currentFrameConnections = new Dictionary<string, int>();
    Transform allConnections;
    float radius = 0.3f;

    #endregion

    #region Public Fields

    [Header("Overlap Sphere settings (for particles connection lines generation)")]
    [Tooltip("Layer where to check Overlap Shpere between particles")]
    public LayerMask mask;

    [Header("Particles settings")]
    [Tooltip("Total number of particles to be generated")]
    public int numberOfParticles = 50;
    [Tooltip("Particle Prefab")]
    public GameObject particlePrefab;

    [Header("Movement Settings")]
    [Tooltip("Max distance before generating new rand position")]
    public float maxDistance = 0.2f;
    [Tooltip("Particles movement speed")]
    public float speed = 0.05f;

    // Using float instead of Vector2 for slightly better performance
    [Header("Moving range")]
    public float xRangeMin = -0.2f;
    public float xRangeMax = 0.2f;
    public float yRangeMin = -0.3f;
    public float yRangeMax = 0.3f;
    public float zRangeMin = -0.3f;
    public float zRangeMax = 0.3f;

    [Header("Line Settings")]
    [Tooltip("The prefab to use for the connection")]
    public LineRenderer connectionPrefab;
    public int maxNumberOfLines;

    [HideInInspector]
    public GameObject[] particles;
    [HideInInspector]
    public Renderer[] particlesRenderer;
    [HideInInspector]
    public List<LineRenderer> allLines = new List<LineRenderer>();

    #endregion

    #region MonoBehaviour Callbacks

    private void Start()
    {
        Init();
    }

    public void Update()
    {
        prevFrameConnections.Clear();
        foreach (KeyValuePair<string, int> item in currentFrameConnections)
            prevFrameConnections.Add(item.Key, item.Value);
        currentFrameConnections.Clear();
        DrawConnectingLines();
    }

    private void FixedUpdate()
    {
        UpdateParticlesMovement();
    }

    #endregion

    #region Private Methods

    private void Init()
    {
        // Setup containers
        particles = new GameObject[numberOfParticles];
        randPos = new Vector3[numberOfParticles];
        particlesRenderer = new Renderer[numberOfParticles];
        allConnections = new GameObject("AllConnections").transform;

        // Create and setup the particles
        for (int i = 0; i < numberOfParticles; i++)
        {
            particles[i] = Instantiate(particlePrefab, transform);
            particles[i].name = i.ToString();
            particlesRenderer[i] = particles[i].GetComponent<Renderer>();

            // Spawn particle at a random position within the range

            Vector3 tempPos = new Vector3(UnityEngine.Random.Range(xRangeMin, xRangeMax), UnityEngine.Random.Range(yRangeMin, yRangeMax), UnityEngine.Random.Range(zRangeMin, zRangeMax));
            particles[i].transform.localPosition = tempPos;
            randPos[i] = tempPos;
        }

        for (int i = 0; i < maxNumberOfLines; i++)
        {
            LineRenderer line = Instantiate(connectionPrefab, allConnections);
            line.gameObject.SetActive(false);
            line.positionCount = 2;
            line.useWorldSpace = false;
            allLines.Add(line);
        }
    }

    public void UpdateParticlesMovement()
    {
        for (int i = 0; i < numberOfParticles; i++)
        {
            // Lerp the particle position to the random position
            particles[i].transform.localPosition = Vector3.Lerp(particles[i].transform.localPosition, randPos[i], Time.deltaTime * speed);
            // When the particle get maxDistance close to the randomposition, generate a new one

            if (Vector3.Distance(particles[i].transform.localPosition, randPos[i]) <= maxDistance)
            {
               randPos[i] = new Vector3(UnityEngine.Random.Range(xRangeMin, xRangeMax), UnityEngine.Random.Range(yRangeMin, yRangeMax), UnityEngine.Random.Range(zRangeMin, zRangeMax));
            }
        }
    }

    private void DrawConnectingLines()
    {
        for (int i = 0; i < numberOfParticles; i++)
        {
            // Detect particles withing the radius of particles[i]
            Collider[] hitColliders = Physics.OverlapSphere(particles[i].transform.position, radius, mask);
            if (hitColliders.Length > 2)
            {
                // Only consider 2 points to avoid too many line between points
                hitColliders = SubArray(hitColliders, 0, 2);
            }

            // Cycle through all the particles that collided
            for (int j = 0; j < hitColliders.Length; j++)
            {
                // If the particle is colliding with itself, continue
                if (particles[i].name == hitColliders[j].name)
                    continue;

                string connectionName;
                if (Convert.ToInt32(particles[i].name) < Convert.ToInt32(hitColliders[j].name))
                    connectionName = particles[i].name + " - " + hitColliders[j].name;
                else
                    connectionName = hitColliders[j].name + " - " + particles[i].name;


                if (currentFrameConnections.TryGetValue(connectionName, out int p))
                    continue;

                if (prevFrameConnections.TryGetValue(connectionName, out int lineIndex))
                {
                    allLines[lineIndex].SetPosition(0, particles[i].transform.position);
                    allLines[lineIndex].SetPosition(1, hitColliders[j].transform.position);

                    float w = CalculateWidth(particles[i].transform.position, hitColliders[j].transform.position);
                    allLines[lineIndex].startWidth = w;
                    allLines[lineIndex].endWidth = w;

                    currentFrameConnections.Add(connectionName, lineIndex);
                }
                else
                {
                    if (FindFirstAvailableLine() == (maxNumberOfLines + 1))
                        break;

                    int index = FindFirstAvailableLine();
                    allLines[index].SetPosition(0, particles[i].transform.position);
                    allLines[index].SetPosition(1, hitColliders[j].transform.position);
                    float w = CalculateWidth(particles[i].transform.position, hitColliders[j].transform.position);
                    allLines[lineIndex].startWidth = w;
                    allLines[lineIndex].endWidth = w;
                    allLines[index].gameObject.SetActive(true);
                    currentFrameConnections.Add(connectionName, index);
                }
            }
        }

        // If the connection existed on the prev frame, and now doesn't exist anymore, destroy the connection
        Dictionary<string, int> diff = new Dictionary<string, int>();

        foreach (KeyValuePair<string, int> kvp in prevFrameConnections)
        {
            if (!currentFrameConnections.TryGetValue(kvp.Key, out int val))
            {
                diff[kvp.Key] = kvp.Value;
            }
        }
        foreach (KeyValuePair<string, int> item in diff)
        {
            allLines[item.Value].gameObject.SetActive(false);
        }
    }

    private int FindFirstAvailableLine()
    {
        for (int i = 0; i < allLines.Count; i++)
        {
            if (!allLines[i].gameObject.activeInHierarchy)
            {
                return i;
            }
        }
        return maxNumberOfLines + 1;
    }

    private float CalculateWidth(Vector3 pos1, Vector3 pos2)
    {
        float dist = Vector3.Distance(pos1, pos2);
        float res = (1 - (dist / radius));

        return 0.0005f*res;
    }

    #endregion

    #region Helper Methods

    // Keep only lenght elements of a given array starting at index.
    private T[] SubArray<T>(T[] data, int index, int length)
    {
        T[] result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }

    #endregion
}
