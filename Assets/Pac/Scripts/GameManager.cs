using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class GameManager : MonoBehaviour
{
    [Header("Scene objects")]
    public LevelGenerator levelGenerator;
    public string generatedLevelRoot="Level01_Generated";
    public GameHUD hud;
    public CherryController cherry;
    public ParticleSystem dust, wallEffect, deathEffect;
    public AnimationClip playerDeathClip;
    [Header("Audio sources: Play On Awake off")]
    public AudioSource music, movement, sfx;
    public AudioClip introMusic, normalMusic, scaredMusic, deadMusic;
    public AudioClip moveSound, pelletSound, ghostEatSound, cherryEatSound, wallSound, deathSound;
    [Header("Enable when wiring the corresponding assessment band")]
    public bool scoringEnabled, ghostMovementEnabled;
    [Header("Grid cells: column, row; row increases down")]
    public Vector2Int playerStart = new Vector2Int(1,1);
    public Vector2Int[] ghostStarts = {new Vector2Int(12,14),new Vector2Int(13,14),new Vector2Int(14,14),new Vector2Int(15,14)};
    public RectInt house = new RectInt(10,12,8,5);
    public Vector2Int topExit=new Vector2Int(13,11), bottomExit=new Vector2Int(14,17);
    public int tunnelRow=14;
    public Vector2Int[] clockwiseRoute = {
        new Vector2Int(1,1), new Vector2Int(26,1), new Vector2Int(26,8), new Vector2Int(21,8),
        new Vector2Int(21,20), new Vector2Int(26,20), new Vector2Int(26,27), new Vector2Int(1,27),
        new Vector2Int(1,20), new Vector2Int(6,20), new Vector2Int(6,8), new Vector2Int(1,8)
    };
    public readonly float playerSpeed=5, cherrySpeed=4;
    const float hitRadius=.42f;
    bool innovation => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="InnovationScene";
    public bool IsPlaying => phase==Phase.Playing;
    public PacStudentController player;
    public GhostController[] ghosts;
    public LevelGrid Board {get;private set;}
    public PacStudentController Player => player;
    public int Score {get;private set;}
    public int Lives {get;private set;}=3;
    public float Elapsed {get;private set;}
    public float ScaredRemaining {get;private set;}
    public float Energy {get;private set;}=100;
    public float GuardRemaining {get;private set;}
    public float PlayerSpeed => playerSpeed*(sprinting?1.4f:1);
    enum Phase {Loading,Countdown,Playing,Dying,Over}
    Phase phase=Phase.Loading;
    bool sprinting;

    SpriteRenderer playerRenderer;
    Color originalPlayerColour;


    IEnumerator Start()
    {
        // The A3 generator executes Start first; wait one frame for its generated tile objects.
        yield return null;
        if(!levelGenerator || !player || !hud || !cherry || !music || !movement || !sfx || !dust)
            throw new System.InvalidOperationException("Complete the GameManager scene references in the Inspector.");
        if(!Camera.main)throw new System.InvalidOperationException("Tag the level camera MainCamera.");
        if(ghosts==null || ghosts.Length!=4)
            throw new System.InvalidOperationException("Assign exactly four ghosts, in numbered order.");
        Board=new LevelGrid(this,levelGenerator.GetLevelMap());
        var root=GameObject.Find(generatedLevelRoot);
        if(!root) throw new System.InvalidOperationException("Level01_Generated was not found. Check LevelGenerator and the generated root name.");
        Board.BindVisuals(root.transform);
        movement.loop=true;
        player.Setup(this);player.ResetPlayer();
        playerRenderer=player.GetComponent<SpriteRenderer>();originalPlayerColour=playerRenderer.color;
        for(int i=0;i<4;i++)
        {
            ghosts[i].number=i+1;ghosts[i].Setup(this);ghosts[i].ResetGhost();
            ghosts[i].Freeze();
        }
        hud.HideOverlay();hud.Refresh(this);cherry.Setup(this);
        if(scoringEnabled)
        {
            phase=Phase.Countdown;SetMusic(introMusic,false);FreezeActors();
            foreach(string text in new[]{"3","2","1","GO!"})
            {hud.ShowOverlay(text);yield return new WaitForSeconds(1);}
            hud.HideOverlay();
        }
        phase=Phase.Playing;SetMusic(normalMusic,true);Collect(player.Cell);
    }
    void Update()
    {
        if(phase==Phase.Loading)return;
        if(phase==Phase.Countdown) {cherry.Tick(Time.deltaTime);return;}
        if(phase==Phase.Dying) {Elapsed+=Time.deltaTime;hud.Refresh(this);return;}
        if(phase!=Phase.Playing)return;
        if(innovation&&Input.GetKeyDown(KeyCode.Space)&&Energy>=60&&GuardRemaining<=0)
        {Energy-=60;GuardRemaining=1f;}

        float remaining=Time.deltaTime;
        while(remaining>0.000001f&&phase==Phase.Playing)
        {
            float dt=Mathf.Min(remaining,1f/120f);remaining-=dt;
            if(scoringEnabled)Elapsed+=dt;
            ScaredRemaining=Mathf.Max(0,ScaredRemaining-dt);GuardRemaining=Mathf.Max(0,GuardRemaining-dt);
            sprinting=innovation&&Input.GetKey(KeyCode.LeftShift)&&Energy>0&&player.Moving;
            if(innovation)Energy=Mathf.Clamp(Energy+(sprinting?-28:12)*dt,0,100);
            player.BeginFrame();foreach(var ghost in ghosts)ghost.BeginFrame();
            player.Tick(dt);foreach(var ghost in ghosts)ghost.Tick(dt);cherry.Tick(dt);
            if(scoringEnabled) CheckCollisions();
            if(scoringEnabled&&Board.Remaining==0&&phase==Phase.Playing)EndGame();
        }
        if(player.Moving&&phase==Phase.Playing) {if(!dust.isPlaying)dust.Play();}
        else dust.Stop(true,ParticleSystemStopBehavior.StopEmitting);
        playerRenderer.color=GuardRemaining>0?Color.Lerp(originalPlayerColour,GameHUD.Lime,.65f):originalPlayerColour;
        if(phase==Phase.Playing)RefreshMusic();hud.Refresh(this);
    }
    void LateUpdate()
    {
        if(Board==null||!Camera.main)return;
        var cam=Camera.main;cam.rect=new Rect(0,.10f,1,.78f);cam.orthographic=true;
        cam.transform.position=new Vector3((Board.Width-1)*.5f,-(Board.Height-1)*.5f,-10);
        cam.orthographicSize=Mathf.Max(Board.Height*.5f+.5f,(Board.Width*.5f+.5f)/cam.aspect);
    }
    public void Collect(Vector2Int cell)
    {
        if(!scoringEnabled)return;
        int tile=Board.Consume(cell);
        if(tile==5)Score+=10;
        if(tile==6)
        {
            Score+=50;ScaredRemaining=10;
            foreach(var ghost in ghosts)ghost.RefreshPose();
        }
    }
    public void Bump(Vector3 at)
    {
        if(!scoringEnabled)return;
        Sfx(wallSound);GameEffects.Burst(wallEffect,at);
    }
    public void SetMoveSound(bool moving,bool eating)
    {
        if(!moving) {movement.Stop();return;}
        var clip=eating?pelletSound:moveSound;
        if(movement.clip!=clip||!movement.isPlaying) {movement.clip=clip;movement.Play();}
    }
    static bool Crossed(Vector3 a0,Vector3 a1,Vector3 b0,Vector3 b1,float radius)
    {
        var offset=a0-b0;var delta=(a1-a0)-(b1-b0);
        float t=delta.sqrMagnitude<0.0000001f?0:Mathf.Clamp01(-Vector3.Dot(offset,delta)/delta.sqrMagnitude);
        return (offset+t*delta).sqrMagnitude<=radius*radius;
    }
    void CheckCollisions()
    {
        if(cherry.Alive&&Crossed(player.Previous,player.transform.position,cherry.Previous,cherry.transform.position,hitRadius))
        {cherry.Despawn();Score+=100;Sfx(cherryEatSound);if(innovation)Energy=Mathf.Min(100,Energy+25);}
        foreach(var ghost in ghosts)
        {
            if(ghost.Dead||!Crossed(player.Previous,player.transform.position,ghost.Previous,ghost.transform.position,hitRadius))continue;
            if (innovation && GuardRemaining > 0f)
            {
                ghost.ResetGhost();
                Sfx(ghostEatSound);
            }
            else if (ghost.Frightened)
            {
                ghost.Eat();
                Score += 300;
                Sfx(ghostEatSound);
            }
            else
            {
                StartCoroutine(Die());
                return;
            }
        }
    }
    IEnumerator Die()
    {
        phase=Phase.Dying;Lives--;movement.Stop();music.Stop();dust.Stop();FreezeActors();player.DeathPose();
        GameEffects.Burst(deathEffect,player.transform.position);Sfx(deathSound);hud.Refresh(this);
        float duration=Mathf.Max(playerDeathClip?playerDeathClip.length:0,deathSound?deathSound.length:0);
        yield return new WaitForSeconds(duration);
        if(Lives<=0) {EndGame();yield break;}
        ScaredRemaining=0;GuardRemaining=0;sprinting=false;Energy=100;
        player.ResetPlayer();foreach(var ghost in ghosts)ghost.ResetGhost();
        // Preserve collected pellets, score, time and the existing cherry schedule.
        phase=Phase.Playing;RefreshMusic();
    }
    void FreezeActors() {player.Freeze();foreach(var ghost in ghosts)ghost.Freeze();}
    void EndGame()
    {
        if(phase==Phase.Over)return;
        phase=Phase.Over;FreezeActors();movement.Stop();dust.Stop();music.Stop();hud.ShowOverlay("GAME OVER");hud.Refresh(this);
        string key="PacStudent.A4.Level"+(innovation?2:1);
        int best=PlayerPrefs.GetInt(key+".Score",-1);float time=PlayerPrefs.GetFloat(key+".Time",float.MaxValue);
        if(Score>best||(Score==best&&Elapsed<time))
        {PlayerPrefs.SetInt(key+".Score",Score);PlayerPrefs.SetFloat(key+".Time",Elapsed);PlayerPrefs.Save();}
        StartCoroutine(ReturnToMenu());
    }
    IEnumerator ReturnToMenu()
    {yield return new WaitForSeconds(3);UnityEngine.SceneManagement.SceneManager.LoadScene("StartScene");}
    void RefreshMusic()
    {
        bool dead=false;foreach(var ghost in ghosts)if(ghost.Dead)dead=true;
        SetMusic(dead?deadMusic:ScaredRemaining>0?scaredMusic:normalMusic,true);
    }
    void SetMusic(AudioClip clip,bool loop)
    {if(music.clip==clip&&music.isPlaying)return;music.clip=clip;music.loop=loop;music.Play();}
    void Sfx(AudioClip clip) {if(clip)sfx.PlayOneShot(clip);}
}
