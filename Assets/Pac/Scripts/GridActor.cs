using UnityEngine;

public abstract class GridActor : MonoBehaviour
{
    public Vector2Int Cell {get; protected set;}
    public Vector3 Previous {get; protected set;}
    public bool Moving {get; protected set;}
    protected Vector2Int destination;
    protected Vector3 from;
    protected float progress;
    protected GameManager game;
    protected Animator anim;
    protected int facing;
    string playing;
    protected static readonly string[] WalkStates={"WalkRight","WalkDown","WalkLeft","WalkUp"};
    protected static readonly string[] ScaredStates={"ScaredRight","ScaredDown","ScaredLeft","ScaredUp"};
    void Awake()
    {
        anim=GetComponent<Animator>();
        if(anim)anim.speed=0;
    }
    public void Setup(GameManager owner)
    {
        game=owner; anim=GetComponent<Animator>();
        if(!anim || !anim.runtimeAnimatorController)
            throw new System.InvalidOperationException("Assign a gameplay Animator Controller to " + name);
    }
    protected void Snap(Vector2Int p)
    { Cell=p;destination=p;transform.position=LevelGrid.World(p);Previous=transform.position;Moving=false;progress=0; }
    protected void Begin(Vector2Int target)
    {
        var d=target-Cell;
        facing=d.x>0?0:d.y>0?1:d.x<0?2:3;
        destination=target;from=transform.position;progress=0;Moving=true;
    }
    protected bool Advance(ref float seconds,float speed)
    {
        float used=Mathf.Min(seconds,(1-progress)/speed);
        progress=Mathf.Min(1,progress+used*speed);seconds-=used;
        transform.position=Vector3.Lerp(from,LevelGrid.World(destination),progress);
        if(progress<0.999999f) return false;
        transform.position=LevelGrid.World(destination);Cell=destination;Moving=false;return true;
    }
    protected void Pose(string state,bool running)
    {
        anim.speed=running?1:0;
        if(playing!=state)
        { anim.Play("Base Layer."+state,0,0);anim.Update(0);playing=state; }
    }
    public void Freeze() {if(anim) anim.speed=0;}
    public void BeginFrame() {Previous=transform.position;}
    public void DeathPose() {playing=null;Pose("Dead",true);}
}
