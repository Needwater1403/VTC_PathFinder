using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using DG.Tweening;
using UnityEngine.Animations;
using UnityEngine.Timeline;
using Vector3 = UnityEngine.Vector3;

public class PathMarker
{
    public MapLocation location;
    public float G;
    public float H;
    public float F;
    public GameObject marker;
    public PathMarker parent;
    
    public PathMarker(MapLocation l, float g, float h, float f, GameObject m, PathMarker p)
    {
        location = l;
        G = g;
        H = h;
        F = f;
        marker = m;
        parent = p;
    }

    public override bool Equals(object obj)
    {
        if ( obj == null || GetType() != obj.GetType()) return false;
        else return location.Equals(((PathMarker)obj).location);
    }

    public override int GetHashCode()
    {
        return 0;
    }
}

public class AstarPath : MonoBehaviour
{
    public static AstarPath Instance;
    #region Parameters

    public Maze maze;
    public Material closedMaterial;
    public Material opendMaterial;

    public List<PathMarker> open = new List<PathMarker>();
    public List<PathMarker> closed = new List<PathMarker>();

    [Header("Prefabs")]
    public GameObject npc;
    public GameObject start;
    public GameObject end;
    public GameObject path;

    private PathMarker goalNode;
    private PathMarker startNode;

    private PathMarker lastPos;
    private bool done = false;
    private bool startMove;
    private bool canMove = true;
    private bool startSearch;
    private bool canSearch = true;

    private GameObject _npc;
    private Animator _npcAnim;
    #endregion

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    private void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A) && canSearch)
        {
            BeginSearch();
        }
        if (!canSearch && !done)
        {
            Search(lastPos);
        }
        if (Input.GetKeyDown(KeyCode.S) && done && canMove)
        {
            GetPath();
        }
        if (startMove)
        {
            StartCoroutine(Move(.2f));
        }
    }

    private static void RemoveAllMarkers()
    {
        var markers = GameObject.FindGameObjectsWithTag("marker");
        foreach (var m in markers)
        {
            Destroy(m);
        }
    }

   
    private void BeginSearch()
    {
        canSearch = false;
        RemoveAllMarkers();
        var locations = new List<MapLocation>();
        for (var z = 0; z < maze.depth - 1; z++)
        {
            for (var x = 0; x < maze.width - 1; x++)
            {
                locations.Add(new MapLocation(x,z));
            }
        }
        
        locations.Shuffle();
        var i = 0;
        var startLocation = new Vector3(locations[i].x * maze.scale, 0, locations[i].z * maze.scale);
        if(_npc==null)
        {
            var a = false;
            while (!a)
            {
                for (var j = 0; j < maze.wallList.Count; j++)
                {
                    if (maze.wallList[j].position == startLocation)
                    {
                        i++;
                        startLocation = new Vector3(locations[i].x * maze.scale, 0, locations[i].z * maze.scale);
                        break;
                    }

                    if (j == maze.wallList.Count - 1) a = true;
                }
            }

            startNode = new PathMarker(new MapLocation(locations[i].x, locations[i].z), 0, 0, 0,
                Instantiate(start, startLocation, Quaternion.identity), null);
            _npc = Instantiate(npc, startLocation, Quaternion.identity);
            _npcAnim = _npc.GetComponentInChildren<Animator>();
            i++;
        }
        else
        {
            startLocation = _npc.transform.position;
            startNode = new PathMarker(new MapLocation((int)startLocation.x/maze.scale, (int)startLocation.z/maze.scale), 0, 0, 0,
                Instantiate(start, startLocation, Quaternion.identity), null);
        }

        var endLocation = new Vector3(locations[i].x * maze.scale, 0, locations[i].z * maze.scale);
        var b = false;
        while (!b)
        {
            for (var j = 0; j < maze.wallList.Count; j++)
            {
                if (maze.wallList[j].position == endLocation || startLocation == endLocation)
                {
                    i++;
                    endLocation = new Vector3(locations[i].x * maze.scale, 0, locations[i].z * maze.scale);
                    break;
                }
                if (j == maze.wallList.Count - 1) b = true;
            }
        }
        goalNode = new PathMarker(new MapLocation(locations[i].x, locations[i].z), 0, 0, 0,
            Instantiate(end, endLocation, Quaternion.identity), null);
        
        open.Clear();
        closed.Clear();
        open.Add(startNode);
        lastPos = startNode;
        done = false;
        UIManager.Instance.SetText("Press <color=red>S</color> to move");
    }

    private void Search(PathMarker pos)
    {
        if (pos.Equals(goalNode))
        {
            done = true;
            canMove = true;
            return;
        }

        foreach (var dir in maze.directions)
        {
            var neighbourLocation = pos.location + dir;
            if (maze.map[neighbourLocation.x, neighbourLocation.z] == 1)
            {
                continue;
            }

            if (neighbourLocation.x < 1 || neighbourLocation.x >= maze.width - 1 ||
                neighbourLocation.z < 1 || neighbourLocation.x >= maze.depth - 1)
            {
                continue;
            }

            if (IsClosed(neighbourLocation))
            {
                continue;
            }

            float G = Vector2.Distance(pos.location.ToVector(), neighbourLocation.ToVector()) + pos.G;
            float H = Vector2.Distance(pos.location.ToVector(), goalNode.location.ToVector()) + pos.H;
            float F = G + H;
            GameObject marker = Instantiate(path,
                new Vector3(neighbourLocation.x * maze.scale, 0, neighbourLocation.z * maze.scale),
                Quaternion.identity);

            var texts = marker.GetComponentsInChildren<TextMesh>();
            texts[0].text = "G: " + G.ToString("0.00");
            texts[0].text = "F: " + G.ToString("0.00");
            texts[0].text = "H: " + G.ToString("0.00");
            if(!UpdateMarker(neighbourLocation, G, H, F, pos))
                open.Add(new PathMarker(neighbourLocation, G, H, F, marker, pos));
        }

        open = open.OrderBy(x => x.F).ThenBy(x => x.H).ToList<PathMarker>();
        var pm = open.ElementAt(0);
        closed.Add(pm);
        open.RemoveAt(0);
        pm.marker.GetComponent<Renderer>().material = closedMaterial;
        lastPos = pm;
    }

    private bool UpdateMarker(MapLocation pos, float g, float h, float f, PathMarker path)
    {
        foreach (var p in open.Where(p => p.location.Equals(pos)))
        {
            p.F = f;
            p.G = g;
            p.H = h;
            p.parent = path;
            return true;
        }

        return false;
    }

    private bool IsClosed(MapLocation marker)
    {
        return closed.Any(p => p.location.Equals(marker));
    }

    private List<Vector3> movePath = new List<Vector3>();
    private List<GameObject> pathList = new List<GameObject>();
    private void GetPath()
    {
        RemoveAllMarkers();
        canMove = true;
        bool abc = false;
        var begin = lastPos;
        while (!startNode.Equals(begin) && begin != null)
        {
            Vector3 a = new Vector3(begin.location.x * maze.scale, 0, begin.location.z * maze.scale);
            pathList.Add(Instantiate(abc==false?end:path, a,
                Quaternion.identity));
            movePath.Add(a);
            begin = begin.parent;
            abc = true;
        }
        pathList.Add(Instantiate(path, new Vector3(startNode.location.x * maze.scale, 0, startNode.location.z * maze.scale),
            Quaternion.identity));
        movePath.Add(new Vector3(startNode.location.x * maze.scale, 0, startNode.location.z * maze.scale));
        movePath.Reverse();
        pathList.Reverse();
        startMove = true;
    }

    private IEnumerator Move(float moveTime)
    {
        startMove = false;
        canMove = false;
        _npcAnim.SetTrigger("Fly");
        for (var index = 0; index < movePath.Count; index++)
        {
            var time = moveTime;
            var t = movePath[index];
            var dir = t - _npc.transform.position;
            var rotate = Vector3.Angle(dir, _npc.transform.forward) >= 10;
            if(rotate)
            {
                time = moveTime + .1f;
                _npc.transform.DOLookAt(t, .1f);
            }
            _npc.transform.DOMove(t, moveTime);
            if (index == movePath.Count - 1)
            {
                RemoveAllMarkers();
                UIManager.Instance.SetText("Press <color=red>A</color> to randomize <color=yellow>new destination</color>");
                _npcAnim.SetTrigger("Bounce");
                startMove = false;
                canSearch = true;
                movePath = new List<Vector3>();
                yield return null;
            }
            yield return new WaitForSeconds(time);
        }
    }
}
