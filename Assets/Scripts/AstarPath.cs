using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEngine.Animations;
using UnityEngine.Timeline;

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
    #region Parameters

    public Maze maze;
    public Material closedMaterial;
    public Material opendMaterial;

    public List<PathMarker> open = new List<PathMarker>();
    public List<PathMarker> closed = new List<PathMarker>();

    public GameObject start;
    public GameObject end;
    public GameObject path;

    private PathMarker goalNode;
    private PathMarker startNode;

    private PathMarker lastPos;
    private bool done = false;

    #endregion
    
    void Start()
    {
        BeginSearch();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            
        }
        if (!done)
        {
            Search(lastPos);
        }
        if (Input.GetKeyDown(KeyCode.D) && done)
        {
            GetPath();
        }
    }

    private void RemoveAllMarkers()
    {
        GameObject[] markers = GameObject.FindGameObjectsWithTag("marker");
        foreach (var m in markers)
        {
            Destroy(m);
        }
    }
    
    private void BeginSearch()
    {
        done = false;
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
        var startLocation = new Vector3(locations[0].x * maze.scale, 0, locations[0].z* maze.scale);
        startNode = new PathMarker(new MapLocation(locations[0].x, locations[0].z), 0, 0, 0,
            Instantiate(start, startLocation, Quaternion.identity), null);
        
        var endLocation = new Vector3(locations[1].x * maze.scale, 0, locations[1].z * maze.scale);
        goalNode = new PathMarker(new MapLocation(locations[1].x, locations[1].z), 0, 0, 0,
            Instantiate(end, endLocation, Quaternion.identity), null);
        
        open.Clear();
        closed.Clear();
        open.Add(startNode);
        lastPos = startNode;
    }

    private void Search(PathMarker pos)
    {
        if (pos.Equals(goalNode))
        {
            done = true;
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
        PathMarker pm = open.ElementAt(0);
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

    private void GetPath()
    {
        RemoveAllMarkers();
        var begin = lastPos;
        while (!startNode.Equals(begin) && begin != null)
        {
            Instantiate(path, new Vector3(begin.location.x * maze.scale, 0, begin.location.z * maze.scale),
                Quaternion.identity);
            begin = begin.parent;
        }
        Instantiate(path, new Vector3(startNode.location.x * maze.scale, 0, startNode.location.z * maze.scale),
            Quaternion.identity);
    }
}
