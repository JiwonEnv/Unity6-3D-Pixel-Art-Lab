using UnityEngine;

namespace PixelArtLab
{
    // All artwork is generated locally; no extracted game assets are used.
    public class PixelArtLab : MonoBehaviour
    {
        public Camera worldCamera;
        public Transform hero;
        public int renderHeight = 360;
        public bool pixelated = true;
        RenderTexture buffer;
        float yaw = 42, zoom = 15;
        Vector3 focus = new Vector3(0, 1, 0);
        GUIStyle title, caption, small;
        bool help = true;

        void OnEnable() { if (Application.isPlaying) UpdateCamera(); }
        void Update()
        {
            if (!worldCamera) return;
            if (Input.GetKeyDown(KeyCode.Tab)) pixelated = !pixelated;
            if (Input.GetKeyDown(KeyCode.H)) help = !help;
            if (Input.GetKeyDown(KeyCode.R)) { yaw = 42; zoom = 15; focus = new Vector3(0,1,0); }
            if (Input.GetKey(KeyCode.Q)) yaw -= Time.deltaTime * 45;
            if (Input.GetKey(KeyCode.E)) yaw += Time.deltaTime * 45;
            zoom = Mathf.Clamp(zoom - Input.mouseScrollDelta.y, 7, 23);
            Vector3 forward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            focus += (right * Input.GetAxisRaw("Horizontal") + forward * Input.GetAxisRaw("Vertical")) * Time.deltaTime * 7;
            UpdateCamera();
            foreach (var sprite in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                sprite.transform.rotation = Quaternion.LookRotation(worldCamera.transform.forward, Vector3.up);
        }
        void UpdateCamera()
        {
            worldCamera.transform.position = focus + Quaternion.Euler(34, yaw, 0) * new Vector3(0,0,-30);
            worldCamera.transform.LookAt(focus);
            worldCamera.orthographicSize = zoom;
            int height = renderHeight, width = Mathf.RoundToInt(height * (float)Screen.width / Screen.height);
            if (pixelated && (buffer == null || buffer.width != width || buffer.height != height))
            {
                if (buffer) { buffer.Release(); Destroy(buffer); }
                buffer = new RenderTexture(width, height, 24) { filterMode = FilterMode.Point, antiAliasing = 1 };
                buffer.Create();
            }
            worldCamera.targetTexture = pixelated ? buffer : null;
        }
        void OnDisable() { if (worldCamera) worldCamera.targetTexture = null; if (buffer) { buffer.Release(); Destroy(buffer); } }
        void OnGUI()
        {
            if (!Application.isPlaying) return;
            if (pixelated && buffer) GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height), buffer, ScaleMode.StretchToFill);
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, normal = { textColor = new Color(.94f,.86f,.65f) } };
                caption = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(.68f,.77f,.73f) } };
                small = new GUIStyle(caption) { fontSize = 13 };
            }
            GUI.color = new Color(.035f,.065f,.08f,.92f);
            GUI.DrawTexture(new Rect(24,24,370,91), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(42,33,340,22), "UNITY 6  /  3D PIXEL ART STUDY", caption);
            GUI.Label(new Rect(40,55,345,36), "3D PIXEL ART LAB", title);
            GUI.Label(new Rect(42,91,340,20), "STUDY 01    •    THE AMBER CROSSING", caption);
            if (help)
            {
                GUI.color = new Color(.035f,.065f,.08f,.9f);
                GUI.DrawTexture(new Rect(24,Screen.height-82,605,58), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(40,Screen.height-73,580,22), "WASD / Arrows  Pan    Q E  Orbit    Wheel  Zoom    R  Reset", small);
                GUI.Label(new Rect(40,Screen.height-51,580,22), "TAB  Pixel / Native    H  Hide guide    •    " + (pixelated ? "360p · POINT SAMPLING" : "NATIVE RESOLUTION"), caption);
            }
        }
    }
}
