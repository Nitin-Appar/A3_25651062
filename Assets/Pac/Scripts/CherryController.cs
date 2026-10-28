using UnityEngine;

public sealed class CherryController : MonoBehaviour
{
    GameManager game;
    SpriteRenderer visual;
    public GameObject cherryPrefab;
    Vector3 start,end;
    float progress,wait=5;
    public bool Alive {get;private set;}
    public Vector3 Previous {get;private set;}
    public void Setup(GameManager owner)
    {
        game=owner;
        if(!cherryPrefab)throw new System.InvalidOperationException(" ");

    }
    public void Tick(float dt)
    {
        Previous=transform.position;
        if(!Alive) {wait-=dt;if(wait<=0) Spawn();return;}
        progress+=dt*game.cherrySpeed/Vector3.Distance(start,end);
        transform.position=Vector3.Lerp(start,end,progress);
        var centre=new Vector3((game.Board.Width-1)*.5f,-(game.Board.Height-1)*.5f,0);
        var vp=Camera.main.WorldToViewportPoint(transform.position);
        if(progress>=1||(Vector3.Dot(transform.position-centre,end-start)>0&&(vp.x<-.05f||vp.x>1.05f||vp.y<-.05f||vp.y>1.05f))) Despawn();
    }
    void Spawn()
    {
        var centre=new Vector3((game.Board.Width-1)*0.5f,-(game.Board.Height-1)*0.5f,0);
        float angle=Random.Range(0,Mathf.PI*2);
        var dir=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
        float edge=Mathf.Min((game.Board.Width*0.5f+1)/Mathf.Max(0.0001f,Mathf.Abs(dir.x)),
            (game.Board.Height*0.5f+1)/Mathf.Max(0.0001f,Mathf.Abs(dir.y)));
        start=centre+dir*edge;
        var cam=Camera.main;
        float cameraEdge=Mathf.Max(cam.orthographicSize,cam.orthographicSize*cam.aspect)*2+game.Board.Width+game.Board.Height;
        end=centre-dir*cameraEdge;
        transform.position=start;Previous=start;progress=0;Alive=true;
        visual=Instantiate(cherryPrefab,transform).GetComponent<SpriteRenderer>();visual.name="BonusCherry";
        visual.transform.localPosition=Vector3.zero;visual.gameObject.SetActive(true);
    }
    public void Despawn() {Alive=false;if(visual)Destroy(visual.gameObject);visual=null;wait=5;}
    public void ResetCherry() {Despawn();}
}
