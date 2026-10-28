using UnityEngine;

public sealed class PacStudentController : GridActor
{
    // These persist until another key is pressed; input is applied only at a tile centre.
    public Vector2Int lastInput;
    public Vector2Int currentInput;
    Vector2Int bumpedDirection;
    bool bumped;
    bool waitingForInput=true;
    public void ResetPlayer()
    {
        Snap(game.playerStart);currentInput=Vector2Int.zero;waitingForInput=true;
        facing=0;bumped=false;Pose("WalkRight",false);
    }
    void Update()
    {
        if(game==null || !game.IsPlaying)return;
        Vector2Int input=Vector2Int.zero;
        if(Input.GetKeyDown(KeyCode.W)) input=Vector2Int.down;
        if(Input.GetKeyDown(KeyCode.A)) input=Vector2Int.left;
        if(Input.GetKeyDown(KeyCode.S)) input=Vector2Int.up;
        if(Input.GetKeyDown(KeyCode.D)) input=Vector2Int.right;
        if(input!=Vector2Int.zero) {lastInput=input;waitingForInput=false; if(input!=bumpedDirection) bumped=false;}
    }
    public void Tick(float dt)
    {
        if(waitingForInput)return;
        float left=dt;
        while(left>0.000001f)
        {
            if(!Moving)
            {
                Vector2Int chosen=Vector2Int.zero;
                if(lastInput!=Vector2Int.zero&&game.Board.Walkable(Cell+lastInput)) chosen=lastInput;
                else if(currentInput!=Vector2Int.zero&&game.Board.Walkable(Cell+currentInput)) chosen=currentInput;
                if(chosen==Vector2Int.zero)
                {
                    if(lastInput!=Vector2Int.zero&&!bumped)
                    {
                        bumped=true;bumpedDirection=currentInput!=Vector2Int.zero?currentInput:lastInput;
                        game.Bump(transform.position+new Vector3(bumpedDirection.x,-bumpedDirection.y,0)*0.45f);
                    }
                    break;
                }
                bumped=false;currentInput=chosen;Begin(Cell+chosen);
            }
            bool arrived=Advance(ref left,game.PlayerSpeed);
            if(arrived)
            {
                if(game.Board.IsPortal(Cell))
                {
                    Snap(game.Board.OtherPortal(Cell));
                    // Do not sweep across the entire maze when resolving collision after a teleport.
                    Previous=transform.position;
                }
                game.Collect(Cell);
            }
        }
        Pose(WalkStates[facing],Moving);
        game.SetMoveSound(Moving,game.Board.Tile(destination)==5||game.Board.Tile(destination)==6);
    }
}
