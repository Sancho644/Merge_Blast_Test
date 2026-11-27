using UnityEngine;

namespace MergeBlast
{
    public sealed class PortraitLayout : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private RectTransform _safeArea;
        [SerializeField] private Transform _background;
        [SerializeField] private Transform _leftWall;
        [SerializeField] private Transform _rightWall;
        [SerializeField] private Transform _topWall;
        [SerializeField] private Transform _floor;
        [SerializeField] private Transform _spawnArea;
        [SerializeField] private Transform _cannon;
        [SerializeField, Min(2f)] private float _arenaWidth = 7.2f;

        private Vector2Int _screenSize;
        private Rect _safeAreaPixels;

        public Rect ArenaBounds { get; private set; }
        public Rect SpawnBounds { get; private set; }

        private void OnEnable()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            RefreshIfNeeded();
        }

        private void Update()
        {
            RefreshIfNeeded();
        }

        private void RefreshIfNeeded()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            Rect safeArea = Screen.safeArea;
            if (size.x <= 0 || size.y <= 0 || safeArea.width <= 0 || safeArea.height <= 0)
                return;

            if (size == _screenSize && safeArea == _safeAreaPixels)
                return;

            _screenSize = size;
            _safeAreaPixels = safeArea;
            ApplyLayout(new Rect(safeArea.x / size.x, safeArea.y / size.y,
                safeArea.width / size.x, safeArea.height / size.y), (float)size.x / size.y);
        }

        public void ApplyLayout(Rect safeViewport, float aspect)
        {
            _safeArea.anchorMin = safeViewport.min;
            _safeArea.anchorMax = safeViewport.max;
            _safeArea.offsetMin = Vector2.zero;
            _safeArea.offsetMax = Vector2.zero;

            // Keep the playable width constant across phone aspect ratios.
            float viewportWidth = _arenaWidth / (safeViewport.width * 0.90f);
            float viewportHeight = viewportWidth / aspect;
            _camera.orthographicSize = viewportHeight * 0.5f;

            Vector2 cameraCenter = _camera.transform.position;
            float centerX = cameraCenter.x + (safeViewport.center.x - 0.5f) * viewportWidth;
            float bottom = cameraCenter.y + (safeViewport.yMin + safeViewport.height * 0.11f - 0.5f) * viewportHeight;
            float top = cameraCenter.y + (safeViewport.yMax - safeViewport.height * 0.15f - 0.5f) * viewportHeight;
            ArenaBounds = new Rect(centerX - _arenaWidth * 0.5f, bottom, _arenaWidth, top - bottom);

            const float thickness = 0.10f;
            Place(_background, centerX, ArenaBounds.center.y, ArenaBounds.width, ArenaBounds.height);
            Place(_leftWall, ArenaBounds.xMin - thickness * 0.5f, ArenaBounds.center.y, thickness, ArenaBounds.height);
            Place(_rightWall, ArenaBounds.xMax + thickness * 0.5f, ArenaBounds.center.y, thickness, ArenaBounds.height);
            Place(_topWall, centerX, top + thickness * 0.5f, ArenaBounds.width + thickness * 2f, thickness);
            Place(_floor, centerX, bottom - thickness * 0.5f, ArenaBounds.width + thickness * 2f, thickness);

            const float spawnInset = 0.75f;
            SpawnBounds = Rect.MinMaxRect(ArenaBounds.xMin + spawnInset, top - 1.75f,
                ArenaBounds.xMax - spawnInset, top - spawnInset);
            Place(_spawnArea, SpawnBounds.center.x, SpawnBounds.center.y, SpawnBounds.width, SpawnBounds.height);

            _cannon.position = new Vector3(
                Mathf.Clamp(_cannon.position.x, ArenaBounds.xMin + 0.45f, ArenaBounds.xMax - 0.45f),
                bottom + 0.45f, _cannon.position.z);
        }

        private static void Place(Transform item, float x, float y, float width, float height)
        {
            item.position = new Vector3(x, y, item.position.z);
            item.localScale = new Vector3(width, height, 1f);
        }
    }
}
