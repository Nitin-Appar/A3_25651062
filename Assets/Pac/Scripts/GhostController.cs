using System.Collections.Generic;
using UnityEngine;

public sealed class GhostController : GridActor
{
    public int number;
    public bool Dead {get; private set;}
    public bool Frightened => !Dead&&game.ScaredRemaining>0;
    Vector2Int home, previousCell;
    List<Vector2Int> exit;
    int exitIndex;
    float deadTimer;
    int clockwiseCorner=4;
    public void ResetGhost()
    {
        home=game.ghostStarts[number-1];Snap(home);Dead=false;deadTimer=0;
        previousCell=home;facing=3;clockwiseCorner=4%game.clockwiseRoute.Length;
        exit=game.ghostMovementEnabled?game.Board.ExitRoute(home,number==1||number==3?game.topExit:game.bottomExit):new List<Vector2Int>();
        exitIndex=0;RefreshPose();
    }
    public void Eat()
    {
        if(Dead) return;
        Dead=true;Moving=false;deadTimer=3;DeathPose();
    }
    public void RefreshPose()
    {
        string state=Dead?"Dead":game.ScaredRemaining>0?
            (game.ScaredRemaining<=3?"Recovering":ScaredStates[facing]):WalkStates[facing];
        Pose(state,true);
    }
    public void Tick(float dt)
    {
        RefreshPose();
        if(!game.ghostMovementEnabled)
        {
            if(Dead) {deadTimer-=dt;if(deadTimer<=0) {Dead=false;RefreshPose();}}
            return;
        }
        float speed=game.playerSpeed*0.9f*(Dead||Frightened?0.5f:1);
        if(Dead)
        {
            // World-space straight-line motion at constant speed; no wall interactions.
            transform.position=Vector3.MoveTowards(transform.position,LevelGrid.World(home),speed*dt);
            if(Vector3.Distance(transform.position,LevelGrid.World(home))<0.0001f) ResetGhost();
            return;
        }
        float left=dt;
        while(left>0.000001f)
        {
            if(!Moving)
            {
                Vector2Int next;
                if(exitIndex<exit.Count) next=exit[exitIndex++];
                else next=Choose();
                if(next==Cell) throw new System.InvalidOperationException("ghost has no valid movement. Check the level map and ghost house exits.");
                previousCell=Cell;Begin(next);
            }
            Advance(ref left,speed);
        }
        RefreshPose();
    }
    Vector2Int Choose()
    {
        var candidates=new List<Vector2Int>();
        foreach(var d in LevelGrid.Directions)
        {var n=Cell+d;if(game.Board.Walkable(n,true)) candidates.Add(n);}
        if(candidates.Count>1) candidates.Remove(previousCell);
        if(candidates.Count==0) return Cell;
        if(number==4&&!Frightened)
        {
            // Visit the outside-corridor bends in clockwise order, including the side indents.
            // Forbid the previous cell on the first step; corridors may bend but cannot reverse.
            var corners=game.clockwiseRoute;
            if(Cell==corners[clockwiseCorner])clockwiseCorner=(clockwiseCorner+1)%corners.Length;
            var q=new Queue<Vector2Int>();var prev=new Dictionary<Vector2Int,Vector2Int>();
            q.Enqueue(Cell);prev[Cell]=Cell;var target=corners[clockwiseCorner];
            while(q.Count>0)
            {
                var p=q.Dequeue();if(p==target)break;
                foreach(var d in LevelGrid.Directions)
                {
                    var n=p+d;
                    if(prev.ContainsKey(n)||!game.Board.Walkable(n,true)||(p==Cell&&!candidates.Contains(n)))continue;
                    prev[n]=p;q.Enqueue(n);
                }
            }
            if(prev.ContainsKey(target)&&target!=Cell)
            {var n=target;while(prev[n]!=Cell)n=prev[n];return n;}
            throw new System.InvalidOperationException("Ghost 4 cannot reach its next perimeter cell. Check Clockwise Route in GameManager.");
        }
        if(Frightened||number==1||number==2)
        {
            bool away=Frightened||number==1;
            float current=(LevelGrid.World(Cell)-game.Player.transform.position).sqrMagnitude;
            var preferred=candidates.FindAll(p=>away?
                (LevelGrid.World(p)-game.Player.transform.position).sqrMagnitude>=current:
                (LevelGrid.World(p)-game.Player.transform.position).sqrMagnitude<=current);
            // No stopping/no reversal have priority if every valid forward move violates the distance preference.
            if(preferred.Count>0) candidates=preferred;
        }
        return candidates[Random.Range(0,candidates.Count)];
    }
}
