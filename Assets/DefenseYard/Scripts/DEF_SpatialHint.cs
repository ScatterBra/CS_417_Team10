using UnityEngine;
using UnityEngine.UI;

// A real world-space canvas: the text stays next to the mechanic that caused it.
public class DEF_SpatialHint : MonoBehaviour
{
    public Text label;
    public Transform anchor;
    public Vector3 worldPoint;
    public float seconds = 4f;
    float expires;
    MED_WorldPanel recordingPanel;
    public void Show(Transform target, Vector3 point, string message)
    {
        anchor = target; worldPoint = point; label.text = message;
        expires = Time.unscaledTime + seconds;
        if(DEF_Progression.Current!=null&&DEF_Progression.Current.workshopRecording)
        {
            label.GetComponentInParent<Canvas>().enabled=false;
            recordingPanel=MED_WorldPanel.Create("Readable defense hint",new Vector2(1.4f,.8f),1.1f,.85f,new Color(.65f,1,.85f),new Color(.1f,.85f,.6f));
            recordingPanel.Root.transform.SetParent(transform,false);
            int split=message.IndexOf('\n');recordingPanel.SetText(split<0?"DEFENSE":message.Substring(0,split),split<0?message:message.Substring(split+1));
            recordingPanel.Body.enableAutoSizing=true;recordingPanel.Body.fontSizeMin=.75f;recordingPanel.Body.fontSizeMax=.85f;recordingPanel.Body.overflowMode=TMPro.TextOverflowModes.Truncate;
        }
    }
    void LateUpdate()
    {
        if (Time.unscaledTime >= expires) { Destroy(gameObject); return; }
        var cam = Camera.main;
        if(recordingPanel!=null){var setup=FindAnyObjectByType<DEF_WorkshopRecordingSetup>();var rig=setup!=null?setup.xrOrigin.GetComponent<Unity.XR.CoreUtils.XROrigin>():null;if(rig!=null)cam=rig.Camera;}
        if (cam == null) return;
        Vector3 point = anchor != null ? anchor.position : worldPoint;
        transform.position = point + Vector3.up * 1.8f;
        transform.rotation = cam.transform.rotation;
        // Keep world text legible from a defense window without growing with screen resolution.
        float distance = Vector3.Distance(transform.position, cam.transform.position);
        if(recordingPanel!=null)
        {
            transform.rotation=Quaternion.LookRotation(transform.position-cam.transform.position);
            transform.localScale=Vector3.one;recordingPanel.Root.SetActive(distance<=8f);return;
        }
        float scale = Mathf.Clamp(distance * 0.0014f, 0.004f, 0.022f);
        transform.localScale = Vector3.one * scale;
    }
    void OnDestroy(){recordingPanel?.Destroy();}
}
