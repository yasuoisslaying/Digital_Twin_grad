using UnityEngine;

namespace SmartGuardTwin.Home
{
    /// <summary>
    /// Game-view camera with two modes:
    ///  - <b>3D orbit</b> (perspective, default): hold <b>left mouse</b> and drag to
    ///    rotate around the apartment, <b>scroll</b> to zoom.
    ///  - <b>Top-down</b> (orthographic): a floor-plan view like the paper's Figure 3.
    /// Press <b>V</b> to toggle between them.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public Vector3 target = Vector3.zero;

        float _yaw = -35f, _pitch = 38f, _dist = 9.5f;
        bool _topDown = false;
        Camera _cam;

        void Awake() { _cam = GetComponent<Camera>(); }
        void Start()
        {
            _dist = Mathf.Max(Config.HomeLayout.Width, Config.HomeLayout.Depth) * 1.25f;
            Apply();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.V)) { _topDown = !_topDown; Apply(); }
            if (_topDown) return;

            if (Input.GetMouseButton(0))
            {
                _yaw += Input.GetAxis("Mouse X") * 4f;
                _pitch = Mathf.Clamp(_pitch - Input.GetAxis("Mouse Y") * 3f, 8f, 85f);
            }
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
                _dist = Mathf.Clamp(_dist - scroll * 12f, 3f, 80f);

            ApplyOrbit();
        }

        void Apply()
        {
            if (_topDown)
            {
                _cam.orthographic = true;
                float aspect = _cam.aspect > 0.01f ? _cam.aspect : 1.6f;
                _cam.orthographicSize = Mathf.Max(Config.HomeLayout.Depth * 0.5f,
                                                  (Config.HomeLayout.Width * 0.5f) / aspect) + 0.6f;
                transform.position = new Vector3(target.x, 12f, target.z);
                transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                _cam.orthographic = false;
                _cam.fieldOfView = 55f;
                ApplyOrbit();
            }
        }

        void ApplyOrbit()
        {
            var rot = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.position = target + rot * (Vector3.back * _dist);
            transform.LookAt(target);
        }
    }
}
