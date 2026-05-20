using UnityEngine;

public class LabProcessManager : MonoBehaviour 
{
    public LithoState currentState = LithoState.Idle;
    public float bakeTimer = 0f;
    public float targetBakeTime = 60f;
}

public enum LithoState 
{ 
    Idle, 
    PreBaking, 
    ReadyForExposure, 
    Exposed, 
    PostBaking, 
    Developing 
}