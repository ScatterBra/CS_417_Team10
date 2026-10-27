using System;
using UnityEngine;

// Trusted adapters derive from this and emit only after their subsystem's real success.
// Reconnecting a consumer must not reset an in-progress source session.
public abstract class DEF_PrerequisiteSource : MonoBehaviour
{
    public abstract DEF_ProgressionState.Prerequisite Kind { get; }
    public event Action<DEF_PrerequisiteSource, int, string> Completed;
    protected int Session { get; private set; }
    string completedReceipt;
    public virtual void BeginSession(int session) { Session = session; completedReceipt = null; }
    public void ResumeSession(int session, Action<DEF_PrerequisiteSource,int,string> consumer)
    {
        if (Session != session) BeginSession(session);
        if (isActiveAndEnabled && Session > 0 && !string.IsNullOrEmpty(completedReceipt)) consumer(this,Session,completedReceipt);
    }
    protected void Publish(string receipt) { if (isActiveAndEnabled && Session > 0) { completedReceipt = receipt; Completed?.Invoke(this, Session, receipt); } }
}

