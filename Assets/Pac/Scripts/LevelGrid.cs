using System;
using System.Collections.Generic;
using UnityEngine;

// Logical collision map. The existing LevelGenerator remains responsible for visuals.
public sealed class LevelGrid
{
    public static readonly Vector2Int[] Directions = {Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down};
    public readonly int Width, Height;
    public int Remaining { get; private set; }
    readonly GameManager game;
    readonly int[,] tiles;
    readonly bool[,] exterior;
    readonly Dictionary<Vector2Int, GameObject> pickups = new Dictionary<Vector2Int, GameObject>();
    public LevelGrid(GameManager owner, int[,] quadrant)
    {
        game = owner;
        int quadrantWidth = quadrant.GetLength(1), quadrantHeight = quadrant.GetLength(0);
        Width = quadrantWidth * 2; Height = quadrantHeight * 2 - 1;
        tiles = new int[Width,Height]; exterior = new bool[Width,Height];
        for(int y=0;y<Height;y++) for(int x=0;x<Width;x++)
        {
            int sx = x<quadrantWidth?x:Width-1-x;
            int sy = y<quadrantHeight?y:Height-1-y;
            tiles[x,y]=quadrant[sy,sx];
            if(tiles[x,y]==5 || tiles[x,y]==6) Remaining++;
        }
        // Outside black areas are not corridors. The tunnel row is a deliberate opening.
        var queue = new Queue<Vector2Int>();
        for(int y=0;y<Height;y++) for(int x=0;x<Width;x++)
            if((x==0||y==0||x==Width-1||y==Height-1) && y!=game.tunnelRow && tiles[x,y]==0)
            { exterior[x,y]=true; queue.Enqueue(new Vector2Int(x,y)); }
        while(queue.Count>0)
        {
            var p=queue.Dequeue();
            foreach(var d in Directions)
            {
                var n=p+d;
                if(InBounds(n)&&n.y!=game.tunnelRow&&!exterior[n.x,n.y]&&Tile(n)==0)
                { exterior[n.x,n.y]=true; queue.Enqueue(n); }
            }
        }
    }
    public bool InBounds(Vector2Int p) => p.x>=0&&p.x<Width&&p.y>=0&&p.y<Height;
    public int Tile(Vector2Int p) => InBounds(p)?tiles[p.x,p.y]:-1;
    public static Vector3 World(Vector2Int p) => new Vector3(p.x,-p.y,0);
    public static Vector2Int Cell(Vector3 p) => new Vector2Int(Mathf.RoundToInt(p.x),Mathf.RoundToInt(-p.y));
    public bool Walkable(Vector2Int p, bool ghost=false, bool leavingHouse=false)
    {
        if(!InBounds(p)||exterior[p.x,p.y]) return false;
        if(ghost&&p.y==game.tunnelRow&&(p.x==0||p.x==Width-1)) return false;
        if(game.house.Contains(p)&&!leavingHouse) return false;
        int t=Tile(p);
        return t==0||t==5||t==6||(t==8&&ghost&&leavingHouse);
    }
    public bool IsPortal(Vector2Int p) => p.y==game.tunnelRow&&(p.x==0||p.x==Width-1);
    public Vector2Int OtherPortal(Vector2Int p) => new Vector2Int(p.x==0?Width-1:0,p.y);
    public void BindVisuals(Transform root)
    {
        foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            var cell=Cell(renderer.transform.position);
            if(Tile(cell)!=5 && Tile(cell)!=6) continue;
            if(Vector3.Distance(renderer.transform.position,World(cell))>0.02f)
                throw new InvalidOperationException("Pickup is not aligned to its grid cell: " + renderer.name);
            if(pickups.ContainsKey(cell))
                throw new InvalidOperationException("More than one pickup renderer at " + cell);
            pickups[cell]=renderer.gameObject;
        }
        if(pickups.Count!=Remaining)
            throw new InvalidOperationException($"found {pickups.Count} pickup visuals but map has {Remaining}. Check generated hierarchy and grid origin before playing.");
    }
    public int Consume(Vector2Int p)
    {
        int t=Tile(p); if(t!=5&&t!=6) return 0;
        tiles[p.x,p.y]=0; Remaining--;
        if(pickups.TryGetValue(p,out var visual)) UnityEngine.Object.Destroy(visual);
        pickups.Remove(p); return t;
    }
    public List<Vector2Int> ExitRoute(Vector2Int from, Vector2Int goal)
    {
        var q=new Queue<Vector2Int>(); var prev=new Dictionary<Vector2Int,Vector2Int>();
        q.Enqueue(from); prev[from]=from;
        while(q.Count>0)
        {
            var p=q.Dequeue(); if(p==goal) break;
            foreach(var d in Directions)
            {
                var n=p+d;
                if(!prev.ContainsKey(n)&&(game.house.Contains(n)||n==goal)&&Walkable(n,true,true)) {prev[n]=p;q.Enqueue(n);}
            }
        }
        if(!prev.ContainsKey(goal)) throw new InvalidOperationException("ghost exit is unreachable. Check the ghost house bounds and exit cells against the generated map.");
        var path=new List<Vector2Int>();
        for(var p=goal;p!=from;p=prev[p]) path.Add(p);
        path.Reverse(); return path;
    }
}
