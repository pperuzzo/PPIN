using OculusSampleFramework;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UniRx.Async;
using UnityEngine;

public enum PathCreationStep
{
    None = 0,
    MidPoint = 1,
    ShowingPath = 2
}

public class ProteinNetwork : MonoBehaviour
{
    #region Private Fields

    float[,] data = new float[10,10];
    Vertex[] vertices = new Vertex[10];
    List<Edge> edges = new List<Edge>();
    LineRenderer[] edgesRenderer = new LineRenderer[100];
    int numberOfEdges = 0;
    bool shouldLerpVertices = false;
    bool shouldUpdateEdges = false;
    PathCreationStep currentPathCreationStep = PathCreationStep.None;
    int midPoint = 0;
    int finalPoint = 0;
    List<Edge> currentPath = new List<Edge>();

    #endregion

    #region Public Fields

    public int threshold = 400;
    public DebugMatrix debugMatrix;
    public Transform vertexPrefab;
    public LineRenderer edgePrefab;
    public InfoUI infoUI;
    public GameObject menu;
    public PathUI pathUI;
    public GameObject loadingCanvas;
    public GameObject context;
    public TextMeshProUGUI currentProtein;
    public TextMeshProUGUI currentNumberOfEdges;

    #endregion

    #region MonoBehaviour Callbacks

    private void Start()
    {
        InitVertices();
        InitEdges();
    }

    private void Update()
    {
        if (shouldLerpVertices)
        {
            if (LerpVertices())
            {
                shouldLerpVertices = false;
                shouldUpdateEdges = true;
            }
        }

        if (shouldUpdateEdges)
            UpdateEdges();
    }

    private void OnDisable()
    {
        ResetNetwork();
    }

    #endregion

    #region Private Methods

    private void InitVertices()
    {
        for (int i = 0; i < 10; i++)
        {
            vertices[i] = new Vertex
            {
                transform = Instantiate(vertexPrefab, transform),
                x = Random.Range(0f, 1f),
                y = Random.Range(0f, 1f),
                z = (i == 0) ? 0 : Random.Range(-0.5f, 0.5f)
            };
            vertices[i].renderer = vertices[i].transform.GetComponent<Renderer>();
            vertices[i].buttonController = vertices[i].transform.GetComponentInChildren<ButtonController>();

            vertices[i].transform.gameObject.SetActive(false);
            vertices[i].buttonController.gameObject.name = i.ToString();
            vertices[i].buttonController.InteractableStateChanged.AddListener(ClickChanged);
            vertices[i].buttonController.InteractableStateChanged.AddListener(AddToPath);
            vertices[i].transform.localPosition = new Vector3(0.5f, 0.5f, 0);
        }
    }

    private void SetupVertices()
    {
        for (int i = 0; i < 10; i++)
        {
            vertices[i].transform.GetComponentInChildren<TextMeshProUGUI>().text = vertices[i].name;
            vertices[i].transform.gameObject.name = vertices[i].name;
            vertices[i].transform.gameObject.SetActive(true);
            if (i != 0)
                vertices[i].renderer.material.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
        }
    }

    private void InitEdges()
    {
        Transform edgesContainer = new GameObject("EdgesContainer").transform;
        for (int i = 0; i < 100; i++)
        {
            LineRenderer edge = Instantiate(edgePrefab, edgesContainer);
            edge.gameObject.SetActive(false);
            edge.positionCount = 2;
            edge.useWorldSpace = false;
            edgesRenderer[i] = edge;
        }
    }

    private void SetupEdges()
    {
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                if (i >= j && (data[i, j] != 0 || data[j, i] != 0))
                {
                    float s = (data[i, j] != 0) ? data[i, j] : data[j, i];
                    edges.Add(new Edge(edgesRenderer[numberOfEdges], vertices[i], vertices[j], s));
                    vertices[i].neighbours.Add(j);
                    vertices[j].neighbours.Add(i);

                    numberOfEdges++;
                }
            }
        }
    }

    private async UniTask<List<ProteinLink>> GetRowDataAsync(string id)
    {
        var query = GameManager.Instance.connection.Table<ProteinLink>().Where(pi => (pi.Protein1 == id)).OrderByDescending(pi => pi.Score);
        return await query.ToListAsync();
    }

    private void GetActualProteinInfo()
    {
        ProteinInfo result;
        for (int i = 0; i < 10; i++)
        {
            string id = vertices[i].id;
            result = GameManager.Instance.proteinsInfo.Where(pi => pi.Id == id).FirstOrDefault();

            if (result != null)
            {
                vertices[i].name = result.Name;
                vertices[i].size = result.Size;
                vertices[i].annotation = result.Annotation;
            }
            else
            {
                vertices[i].name = vertices[i].id;
                vertices[i].size = "";
                vertices[i].annotation = "";
            }
        }
    }

    private void CalculateSpringNetwork(int iterations = 50)
    {
        float W = 1f;
        float L = 1f;
        float area = W * L;
        float k = Mathf.Sqrt(area / 10);
        float t = W / 10;
        float dt = t / (iterations + 1);

        for (int i = 0; i < iterations; i++)
        {
            // Calculate repulsive force
            foreach (Vertex v in vertices)
            {
                v.dx = 0;
                v.dy = 0;
                foreach (Vertex u in vertices)
                {
                    if (!v.Equals(u))
                    {
                        float dx = v.x - u.x;
                        float dy = v.y - u.y;
                        float delta = Mathf.Sqrt(dx * dx + dy * dy);
                        if (delta != 0)
                        {
                            float d = RepulsiveForce(delta, k) / delta;
                            v.dx += dx * d;
                            v.dy += dy * d;
                        }
                    }
                }
            }

            // Calculate attractive forces
            foreach (Edge edge in edges)
            {
                Vertex v = edge.vertex1;
                Vertex u = edge.vertex2;
                float dx = v.x - u.x;
                float dy = v.y - u.y;
                float delta = Mathf.Sqrt(dx * dx + dy * dy);
                if (delta != 0)
                {
                    float d = AttractiveForce(delta, k) / delta;
                    float ddx = dx * d;
                    float ddy = dy * d;
                    v.dx += -ddx;
                    u.dx += +ddx;
                    v.dy += -ddy;
                    u.dy += +ddy;
                }
            }

            // Limit the max displacement to temperature t and prevent from being displace outside frame
            foreach (Vertex v in vertices)
            {
                float dx = v.dx;
                float dy = v.dy;
                float disp = Mathf.Sqrt(dx * dx + dy * dy);
                if (disp != 0)
                {
                    float d = Mathf.Min(disp, t)/disp;
                    float x = v.x + dx * d;
                    float y = v.y + dy * d;
                    x = Mathf.Min(W, Mathf.Max(0, x)) - W/2;
                    y = Mathf.Min(L, Mathf.Max(0, y)) - L/2;
                    v.x = (Mathf.Min(Mathf.Sqrt(W * W / 4 - y * y), Mathf.Max(-Mathf.Sqrt(W * W / 4 - y * y), x)) + W / 2);
                    v.y = (Mathf.Min(Mathf.Sqrt(L * L / 4 - x * x), Mathf.Max(-Mathf.Sqrt(L * L / 4 - x * x), y)) + L / 2);
                }
            }
            t -= dt;
        }
    }

    private float AttractiveForce(float x, float k)
    {
        return (Mathf.Pow(x, 2) / k);
    }

    private float RepulsiveForce(float x, float k)
    {
        return (Mathf.Pow(k, 2) / x);
    }

    private bool LerpVertices()
    {
        bool done = true;

        foreach (Vertex v in vertices)
        {
            if ((Mathf.Abs(v.transform.localPosition.x - v.x) >= 0.0001f) || 
                (Mathf.Abs(v.transform.localPosition.y - v.y) >= 0.0001f) ||
                (Mathf.Abs(v.transform.localPosition.z - v.z) >= 0.0001f))
            {
                v.transform.localPosition = new Vector3(
                    Mathf.Lerp(v.transform.localPosition.x, v.x, Time.deltaTime * 0.5f),
                    Mathf.Lerp(v.transform.localPosition.y, v.y, Time.deltaTime * 0.5f),
                    Mathf.Lerp(v.transform.localPosition.z, v.z, Time.deltaTime * 0.5f)
                    );
                done = false;
            }
        }
        UpdateEdges();
        return done;
    }

    private void UpdateEdges()
    {
        int test = 0;
        foreach (Edge e in edges)
        {
            e.renderer.SetPosition(0, e.vertex1.transform.position);
            e.renderer.SetPosition(1, e.vertex2.transform.position);
            e.renderer.startWidth = e.score * 0.0010f;
            e.renderer.endWidth = e.score * 0.0010f;
            e.renderer.gameObject.SetActive(true);
            test++;
        }
    }

    private void ClickChanged(InteractableStateArgs obj)
    {
        if (GameManager.Instance.currentState == StateType.Path)
            return;

        bool inActionState = obj.NewInteractableState == InteractableState.ActionState;
        if (inActionState)
        {
            Vertex v = vertices[System.Int32.Parse(obj.Interactable.gameObject.name)];
            infoUI.UpdateUI(v);
        }

        //_toolInteractingWithMe = obj.NewInteractableState > InteractableState.Default ?
        //  obj.Tool : null;
    }

    private void AddToPath(InteractableStateArgs obj)
    {
        if (GameManager.Instance.currentState != StateType.Path)
            return;

        bool inActionState = obj.NewInteractableState == InteractableState.ActionState;
        if (inActionState)
        {
            switch (currentPathCreationStep)
            {
                case PathCreationStep.None:
                    midPoint = System.Int32.Parse(obj.Interactable.gameObject.name);
                    if (midPoint == 0)
                        return;
                    pathUI.ChangeMidProtein(vertices[midPoint].name);
                    currentPathCreationStep++;
                    break;
                case PathCreationStep.MidPoint:
                    finalPoint = System.Int32.Parse(obj.Interactable.gameObject.name);
                    if (midPoint == 0)
                        return;
                    pathUI.ChangeFinalProtein(vertices[finalPoint].name);
                    currentPathCreationStep++;
                    break;
                case PathCreationStep.ShowingPath:
                    break;
            }
        }
    }

    private bool BFS(int src, int dest, int[] pred, int[] dist)
    {
        // Queue of vertices whose adjacency list is to be scanned as per normal BFS algorithm
        List<int> queue = new List<int>();

        // Stores whether ith vertex is reached at least once in the BFS
        bool[] visited = new bool[10];

        // Initially all vertices are unvisited so v[i] for all i is false
        // Also no path is constructed yet, so we set dist[i] = infinity
        for (int i = 1; i < 10; i++)
        {
            visited[i] = false;
            dist[i] = int.MaxValue;
            pred[i] = -1;
        }

        // Set visited and dist for the source vertex
        visited[src] = true;
        dist[src] = 0;
        queue.Add(src);

        // BFS implementation
        while (queue.Count != 0)
        {
            int u = queue[0];
            queue.RemoveAt(0);

            for (int i = 1; i < vertices[u].neighbours.Count(); i++)
            {
                if (visited[vertices[u].neighbours[i]] == false)
                {
                    visited[vertices[u].neighbours[i]] = true;
                    dist[vertices[u].neighbours[i]] = dist[u] + 1;
                    pred[vertices[u].neighbours[i]] = u;
                    queue.Add(vertices[u].neighbours[i]);

                    // If we found our destination, return
                    if (vertices[u].neighbours[i] == dest)
                        return true;
                }
            }
        }
        return false;
    }

    #endregion

    #region Public Methods

    public async void DisplayNetwork(string id)
    {
        ResetPath();
        context.SetActive(false);
        loadingCanvas.SetActive(true);
        await GetData(id);
        SetupVertices();
        SetupEdges();
        CalculateSpringNetwork();
        menu.SetActive(true);
        loadingCanvas.SetActive(false);

        currentProtein.text = vertices[0].name;
        currentNumberOfEdges.text = numberOfEdges.ToString();
        context.SetActive(true);

        shouldLerpVertices = true;
    }

    public async void Recenter(string id)
    {
        ResetPath();
        context.SetActive(false);
        ResetNetwork();
        loadingCanvas.SetActive(true);
        await GetData(id);
        SetupVertices();
        SetupEdges();
        CalculateSpringNetwork();
        loadingCanvas.SetActive(false);

        currentProtein.text = vertices[0].name;
        currentNumberOfEdges.text = numberOfEdges.ToString();
        context.SetActive(true);

        shouldLerpVertices = true;
    }

    public void ResetNetwork()
    {
        shouldLerpVertices = false;
        shouldUpdateEdges = false;
        numberOfEdges = 0;
        ResetScale();
        ResetTransform();

        System.Array.Clear(data, 0, data.Length);
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].transform.gameObject.SetActive(false);
            vertices[i].transform.localPosition = new Vector3(0.5f, 0.5f, 0);
        }
        for (int i = 0; i < edges.Count; i++)
            if (edges[i].renderer != null)
                edges[i].renderer.gameObject.SetActive(false);
        edges.Clear();
    }

    public async UniTask GetData(string id)
    {
        var query = GameManager.Instance.connection.Table<ProteinLink>().Where(pi => (pi.Protein1 == id)).OrderByDescending(pi => pi.Score);

        List<ProteinLink> result = await query.ToListAsync();

        // Add current protein.
        vertices[0].id = id;
        for (int i = 1; i < 10; i++)
        {
            vertices[i].id = result[i - 1].Protein2;
        }

        // Fill first row and first colum, we already have the data from the prev query.
        for (int i = 1; i < 10; i++)
        {
            data[0, i] = result[i].Score / 1000f;
            data[i, 0] = result[i].Score / 1000f;
        }

        // Fill the rest of the body
        for (int i = 1; i < 10; i++)
        {
            List<ProteinLink> res = await GetRowDataAsync(vertices[i].id);
            // If res contains the protein and the score is > threshold (400), then add it, otherwise 0
            for (int j = 1; j < 10; j++)
            {
                if (vertices[i].id == vertices[j].id)
                    continue;
                ProteinLink match = res.Where(p => p.Protein2 == vertices[j].id && p.Score > threshold).FirstOrDefault();
                if (match != null)
                {
                    data[i, j] = (match.Score/1000f);
                }
            }
        }
        GetActualProteinInfo();
        debugMatrix.DrawMatrix(vertices, data);
    }

    public void IncreaseScale()
    {
        transform.parent.localScale = new Vector3(transform.parent.localScale.x + 0.1f, transform.parent.localScale.y + 0.1f, transform.parent.localScale.z + 0.1f);
    }

    public void DecreaseScale()
    {
        if (Mathf.Abs(transform.parent.localScale.x - 0f) <= 0.1f || Mathf.Abs(transform.parent.localScale.y - 0f) <= 0.1f || Mathf.Abs(transform.parent.localScale.z - 0f) <= 0.1f)
            return;

        transform.parent.localScale = new Vector3(transform.parent.localScale.x - 0.1f, transform.parent.localScale.y - 0.1f, transform.parent.localScale.z - 0.1f);
    }

    public void ResetScale()
    {
        transform.parent.localScale = Vector3.one;
    }

    public void ResetTransform()
    {
        transform.localPosition = new Vector3(-0.5f, -0.5f, 0);
        transform.rotation = Quaternion.identity;
        transform.localEulerAngles = Vector3.zero;
    }

    // https://www.geeksforgeeks.org/shortest-path-unweighted-graph/
    public void GetShortestPath()
    {
        if (shouldLerpVertices)
            return;

        // pred[i] = the predecessor of i
        // dist[i] = distance of i from s
        int[] pred = new int[10];
        int[] dist = new int[10];


        if (!BFS(midPoint, finalPoint, pred, dist))
        {
            Debug.LogError("Given source and destination are not connected");
            return;
        }

        // List to store path
        List<int> path = new List<int>(10);

        int crawl = finalPoint;
        path.Add(crawl);

        while (pred[crawl] != -1)
        {
            path.Add(pred[crawl]);
            crawl = pred[crawl];
        }

        path.Add(0);
        Edge e = null;
        for (int i = path.Count - 1; i >= 0; i--)
        {
            if (i != 0)
            {
                e = edges.Where(x => (x.vertex1.id == vertices[path[i]].id && x.vertex2.id == vertices[path[i - 1]].id) || (x.vertex1.id == vertices[path[i - 1]].id && x.vertex2.id == vertices[path[i]].id)).FirstOrDefault();

                if (e != null)
                {
                    e.renderer.material.color = Color.yellow;
                    currentPath.Add(e);
                }
                //Debug.Log(path[i] + " " + path[i - 1]);
            }
        }

        GameManager.Instance.currentState = StateType.Default;

        //for (int i = path.Count - 1; i >= 0; i--)
        //{
        //    Debug.Log(path[i] + " ");
        //}
        //Debug.Log("Shortest path length is: " + dist[finalPoint]);
        //Debug.Log("Path is: ");
        //for (int i = path.Count - 1; i >= 0; i--)
        //{
        //    Debug.Log(path[i] + " ");
        //}
    }

    public void ResetPath()
    {
        pathUI.ChangeMidProtein("");
        pathUI.ChangeFinalProtein("");
        for (int i = 0; i < currentPath.Count; i++)
        {
            currentPath[i].renderer.material.color = Color.white;
        }
        currentPath.Clear();
        currentPathCreationStep = PathCreationStep.None;
    }

    #endregion
}

public class Vertex
{
    public string id;
    public string name;
    public string size;
    public string annotation;
    public Renderer renderer;
    public Transform transform;
    public List<int> neighbours = new List<int>();
    public ButtonController buttonController;
    public float x;
    public float y;
    public float z;
    public float dx;
    public float dy;
}

public class Edge 
{
    public Edge(LineRenderer aRenderer, Vertex aVertex1, Vertex aVertex2, float aScore)
    {
        renderer = aRenderer;
        vertex1 = aVertex1;
        vertex2 = aVertex2;
        score = aScore;
    }

    public LineRenderer renderer;
    public Vertex vertex1;
    public Vertex vertex2;
    public float score;
}
