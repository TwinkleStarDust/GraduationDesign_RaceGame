/*
Copyright(c) 2021 Omar Duarte
Unauthorized copying of this file, via any medium is strictly prohibited.
Writen by Omar Duarte, 2021.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using UnityEngine;
using System.Linq;

namespace PluginMaster
{
    #region DATA & SETTINGS
    [System.Serializable]
    public struct WallCellSize
    {
        [SerializeField] private string _name;
        [SerializeField] private float _size;
        public WallCellSize(string name, float size)
        {
            _name = name;
            _size = size;
        }
        public string name { get => _name; set => _name = value; }
        public float size { get => _size; set => _size = value; }
    }
    [System.Serializable]
    public class WallSettings : ModularToolBase, ISerializationCallbackReceiver
    {
        [SerializeField] private bool _autoCalculateAxes = true;
        public bool autoCalculateAxes
        {
            get => _autoCalculateAxes;
            set
            {
                if (_autoCalculateAxes == value) return;
                _autoCalculateAxes = value;
                OnDataChanged();
            }
        }

        public override void Copy(IToolSettings other)
        {
            base.Copy(other);
            var otherWallSettings = other as WallSettings;
            if (otherWallSettings == null) return;
            _autoCalculateAxes = otherWallSettings.autoCalculateAxes;
        }
        #region SIZES
        
        [SerializeField] private WallCellSize[] _sizes = null;
        private const string DEFAULT_SIZE_NAME = "Default";
        [SerializeField] private string _selectedSizeName = DEFAULT_SIZE_NAME;
        private System.Collections.Generic.Dictionary<string, float> _sizesDictionary
            = new System.Collections.Generic.Dictionary<string, float>() { { DEFAULT_SIZE_NAME, 1 } };
        public string selectedSizeName
        {
            get => _selectedSizeName;
            set
            {
                if (_selectedSizeName == value) return;
                _selectedSizeName = value;
                var newSize = moduleSize;
                AxesUtils.SetAxisValue(ref newSize, WallManager.wallLenghtAxis, _sizesDictionary[selectedSizeName]);
                moduleSize = newSize;
                DataChanged();
            }
        }
        public void SetSize(float value)
        {
            AxesUtils.SetAxisValue(ref _moduleSize, WallManager.wallLenghtAxis, value);
            DataChanged();
        }

        public void SaveSize(string name)
        {
            var size = AxesUtils.GetAxisValue(moduleSize, WallManager.wallLenghtAxis);
            if (_sizesDictionary.ContainsKey(name))
                _sizesDictionary[name] = size;
            else _sizesDictionary.Add(name, size);
            _selectedSizeName = name;
            DataChanged();
        }
        
        public string[] GetSizesNames() => _sizesDictionary.Keys.ToArray();
        public void DeleteSelectedSize()
        {
            _sizesDictionary.Remove(_selectedSizeName);
            selectedSizeName = DEFAULT_SIZE_NAME;
        }
        public int GetIndexOfSize(string name) => _sizesDictionary.Keys.Select((key, index) => new { key, index })
            .FirstOrDefault(pair => pair.key == name)?.index ?? -1;
        public int GetIndexOfSelectedSize() => GetIndexOfSize(selectedSizeName);
        public string GetSizeAt(int index) => _sizesDictionary.Keys.ElementAt(index);
        public void SelectSize(int index) => selectedSizeName = GetSizeAt(index);
        public void ResetSize()
        {
            var newSize = moduleSize;
            AxesUtils.SetAxisValue(ref newSize, WallManager.wallLenghtAxis, _sizesDictionary[selectedSizeName]);
            moduleSize = newSize;
            DataChanged();
        }
        #endregion
        public void OnBeforeSerialize()
        {
            _sizes = _sizesDictionary.Select(pair => new WallCellSize(pair.Key, pair.Value)).ToArray();
        }

        public void OnAfterDeserialize()
        {
            if (_sizes == null || _sizes.Length == 0) return;
            _sizesDictionary = _sizes.ToDictionary(origin => origin.name, origin => origin.size);
        }
    }
    [System.Serializable]
    public class WallManager : ToolManagerBase<WallSettings>
    {
        public enum ToolState
        {
            FIRST_WALL_PREVIEW,
            EDITING
        }
        public static ToolState state { get; set; } = ToolState.FIRST_WALL_PREVIEW;
        public static float wallThickness { get; set; } = 0.1f;
        public static float wallLength { get; set; } = 1f;
        public static AxesUtils.Axis wallLenghtAxis { get; set; } = AxesUtils.Axis.X;
        public static Vector3 startPoint { get; set; } = Vector3.zero;
        public static Vector3 endPoint { get; set; } = Vector3.zero;
        public static bool halfTurn { get; set; } = false;
    }
    #endregion
    public static partial class PWBIO
    {
        #region HANDLERS
        private static void WallInitializeOnLoad()
        {
            WallManager.settings.OnDataChanged += OnWallSettingsChanged;
            BrushSettings.OnBrushSettingsChanged += UpdateWallSettingsOnBrushChanged;
            SnapManager.settings.OnGridOriginChange += OnWallGridOriginChange;
        }

        private static void SetSnapStepToWallCellSize()
        {
            var cellSize = WallManager.settings.moduleSize + WallManager.settings.spacing;
            if (WallManager.settings.autoCalculateAxes)
            {
                WallManager.settings.SetUpwardAxis(AxesUtils.SignedAxis.UP);
                if (cellSize.x >= cellSize.z)
                {
                    WallManager.wallLenghtAxis = AxesUtils.Axis.X;
                    WallManager.settings.SetForwardAxis(AxesUtils.SignedAxis.FORWARD);
                }
                else
                {
                    WallManager.wallLenghtAxis = AxesUtils.Axis.Z;
                    WallManager.settings.SetForwardAxis(AxesUtils.SignedAxis.RIGHT);
                }
                WallManager.wallThickness = Mathf.Min(cellSize.x, cellSize.z);
                WallManager.wallLength = Mathf.Max(cellSize.x, cellSize.z);
            }
            else
            {
                WallManager.wallLenghtAxis = AxesUtils.Axis.X;
                WallManager.wallThickness = cellSize.z;
                WallManager.wallLength = cellSize.x;
            }
            cellSize.x = cellSize.z = WallManager.wallLength;
            SnapManager.settings.step = cellSize;
            UnityEditor.SceneView.RepaintAll();
        }

        private static void OnWallSettingsChanged()
        {
            repaint = true;
            BrushstrokeManager.UpdateWallBrushstroke(WallManager.wallLenghtAxis, cellsCount: 1,
                setNextIdx: false, deleteMode: false);
            SetSnapStepToWallCellSize();
        }

        public static void UpdateWallSettingsOnBrushChanged()
        {
            if (ToolManager.tool != ToolManager.PaintTool.WALL) return;
            WallManager.halfTurn = false;
            WallManager.settings.UpdateCellSize();
            SetSnapStepToWallCellSize();
            WallManager.state = WallManager.ToolState.FIRST_WALL_PREVIEW;
        }

        public static void OnWallGridOriginChange()
        {
            if (ToolManager.tool != ToolManager.PaintTool.WALL) return;
            repaint = true;
            BrushstrokeManager.UpdateWallBrushstroke(WallManager.wallLenghtAxis, cellsCount: 1,
                setNextIdx: false, deleteMode: false);
            SetSnapStepToWallCellSize();
        }
        #endregion

        public static void OnWallEnabled()
        {
            SnapManager.settings.radialGridEnabled = false;
            SnapManager.settings.gridOnY = true;
            SnapManager.settings.visibleGrid = true;
            SnapManager.settings.lockedGrid = true;
            SnapManager.settings.snappingOnX = true;
            SnapManager.settings.snappingOnZ = true;
            SnapManager.settings.snappingEnabled = true;
            UpdateWallSettingsOnBrushChanged();
            SnapManager.settings.DataChanged(repaint: true);
            WallManager.state = WallManager.ToolState.FIRST_WALL_PREVIEW;
            WallManager.halfTurn = false;
        }

        private static Vector3 _wallEnd = Vector3.zero;
        private static void WallToolDuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            if (PaletteManager.selectedBrush == null) return;
            var mousePos2D = Event.current.mousePosition;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos2D);
            var mousePos3D = Vector3.zero;
            var localMousePos3D = Vector3.zero;
            AxesUtils.Axis axis;
            int cellsCount = 1;
            bool rotateHalfTurn;
            if (GridRaycast(mouseRay, out RaycastHit gridHit))
            {
                mousePos3D = WallManager.state == WallManager.ToolState.FIRST_WALL_PREVIEW
                    ? SnapWallPosition(gridHit.point, out axis, out rotateHalfTurn, out localMousePos3D)
                    : SnapWallPosition(WallManager.startPoint, gridHit.point,
                        out axis, out cellsCount, out rotateHalfTurn, out localMousePos3D);
            }
            else return;

            if (WallInput(mousePos3D, axis, cellsCount)) return;

            switch (WallManager.state)
            {
                case WallManager.ToolState.FIRST_WALL_PREVIEW:
                    if (_modularDeleteMode) PreviewDeleteSingleWall(sceneView.camera, axis, mousePos3D);
                    else PreviewFirstWall(sceneView.camera, mousePos3D, axis, rotateHalfTurn);
                    break;
                case WallManager.ToolState.EDITING:
                    if (_modularDeleteMode)
                    {
                        if (cellsCount == 1) PreviewDeleteSingleWall(sceneView.camera, axis, mousePos3D);
                        else PreviewDeleteWall(sceneView.camera, axis, mousePos3D);
                    }
                    else PreviewWall(sceneView.camera, axis, rotateHalfTurn);
                    break;
            }
            WallInfoText(sceneView, localMousePos3D, cellsCount);
        }

        private static void WallInfoText(UnityEditor.SceneView sceneView, Vector3 localMousePos3D, int cellsCount)
        {
            if (!PWBCore.staticData.showInfoText) return;
            var localX = Mathf.RoundToInt(localMousePos3D.x / SnapManager.settings.step.x);
            if (localX >= 0) ++localX;
            var localZ = Mathf.RoundToInt(localMousePos3D.z / SnapManager.settings.step.z);
            if (localZ >= 0) ++localZ;
            var labelTexts = new string[] { $"Position: (X: {localX}, Z: {localZ})", $"Size: {cellsCount}"};
            InfoText.Draw(sceneView, labelTexts);

        }
        private static bool WallInput(Vector3 mousePos3D, AxesUtils.Axis axis, int cellsCount)
        {
            if ((Event.current.type == EventType.KeyUp || Event.current.type == EventType.KeyDown))
            {
                if (Event.current.control && !Event.current.alt && !Event.current.shift) _modularDeleteMode = true;
                else if (_modularDeleteMode && (!Event.current.control || Event.current.alt || Event.current.shift))
                {
                    _modularDeleteMode = false;
                    WallManager.state = WallManager.ToolState.FIRST_WALL_PREVIEW;
                    return true;
                }
            }

            if (Event.current.button == 0)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    WallManager.state = WallManager.ToolState.EDITING;
                    WallManager.endPoint = WallManager.startPoint = mousePos3D;
                    BrushstrokeManager.UpdateWallBrushstroke(axis, cellsCount: 1, setNextIdx: false, _modularDeleteMode);
                    return true;
                }
                if (WallManager.state == WallManager.ToolState.EDITING)
                {
                    if (Event.current.type == EventType.MouseDrag)
                    {
                        WallManager.endPoint = mousePos3D;
                        if (_wallEnd != WallManager.endPoint)
                            BrushstrokeManager.UpdateWallBrushstroke(axis, cellsCount, setNextIdx: true, _modularDeleteMode);
                    }
                    else if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseMove)
                    {
                        var paintStrokeCount = _paintStroke.Count;
                        WallManager.endPoint = mousePos3D;
                        if (_modularDeleteMode)
                            DeleteWall();
                        else Paint(WallManager.settings);
                        WallManager.state = WallManager.ToolState.FIRST_WALL_PREVIEW;
                        if (paintStrokeCount == 1)
                            BrushstrokeManager.UpdateWallBrushstroke(axis, cellsCount: 1, setNextIdx: true, deleteMode: false);
                        return true;
                    }
                }
                _wallEnd = WallManager.endPoint;
            }
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
            {
                WallManager.state = WallManager.ToolState.FIRST_WALL_PREVIEW;
                BrushstrokeManager.UpdateWallBrushstroke(axis, cellsCount: 1, setNextIdx: true, deleteMode: false);
                return true;
            }

            if (PWBSettings.shortcuts.wallHalfTurn.Check())
            {
                WallManager.halfTurn = !WallManager.halfTurn;
                WallManager.settings.UpdateCellSize();
                SetSnapStepToWallCellSize();
                WallManager.state = WallManager.ToolState.FIRST_WALL_PREVIEW;
                BrushstrokeManager.UpdateWallBrushstroke(WallManager.wallLenghtAxis, cellsCount: 1,
                    setNextIdx: false, deleteMode: false);
                return true;
            }
            return false;
        }

        private static void PreviewFirstWall(Camera camera, Vector3 mousePos3D,
            AxesUtils.Axis axis, bool rotateHalfTurn)
        {
            if (BrushstrokeManager.brushstroke.Length == 0) return;
            var strokeItem = BrushstrokeManager.brushstroke[0].Clone();
            if (strokeItem.settings == null)
            {
                BrushstrokeManager.UpdateWallBrushstroke(axis, cellsCount: 1, setNextIdx: false, deleteMode: false);
                return;
            }
            var prefab = strokeItem.settings.prefab;
            if (prefab == null) return;
            _previewData.Clear();
            _paintStroke.Clear();
            var toolSettings = WallManager.settings;
            var itemRotation = Quaternion.Euler(strokeItem.additionalAngle);
            itemRotation *= Quaternion.Inverse(prefab.transform.rotation);
            if (rotateHalfTurn) itemRotation *= Quaternion.AngleAxis(180, toolSettings.upwardAxis);
            var previewRotation = itemRotation;
            if (axis != WallManager.wallLenghtAxis) previewRotation *= Quaternion.AngleAxis(90, toolSettings.upwardAxis);

            var cellCenter = mousePos3D;
            var halfCellSize = toolSettings.moduleSize / 2;

            var nearbyObjects = new System.Collections.Generic.List<GameObject>();
            boundsOctree.GetColliding(cellCenter, halfCellSize, SnapManager.settings.rotation,
                itemRotation, nearbyObjects);
            if (nearbyObjects.Count > 0)
            {
                bool checkNextItem = false;
                foreach (var obj in nearbyObjects)
                {
                    if (obj == null) continue;
                    if (!obj.activeInHierarchy) continue;
                    var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                    var centerDistance = (objCenter - cellCenter).magnitude;
                    if (centerDistance > WallManager.wallThickness * 0.9999) continue;
                    if (PaletteManager.selectedPalette.ContainsSceneObject(obj))
                    {
                        checkNextItem = true;
                        break;
                    }
                }
                if (checkNextItem) return;
            }

            var scaleMult = strokeItem.scaleMultiplier;

            var centerToPivot = GetCenterToPivot(prefab, scaleMult, itemRotation);
            var itemPosition = cellCenter + centerToPivot;

            var previewCenterToPivot = GetCenterToPivot(prefab, scaleMult, previewRotation);
            var previewItemPosition = cellCenter + previewCenterToPivot;

            var translateMatrix = Matrix4x4.Translate(Quaternion.Inverse(itemRotation) * -prefab.transform.position);
            var rootToWorld = Matrix4x4.TRS(itemPosition, itemRotation, scaleMult) * translateMatrix;
            var previewRootToWorld = Matrix4x4.TRS(previewItemPosition, previewRotation, scaleMult) * translateMatrix;
            var layer = toolSettings.overwritePrefabLayer ? toolSettings.layer : prefab.layer;

            PreviewBrushItem(prefab, previewRootToWorld, layer, camera,
                redMaterial: false, reverseTriangles: false, flipX: false, flipY: false);

            _previewData.Add(new PreviewData(prefab, previewRootToWorld, layer, flipX: false, flipY: false));
            var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
            Transform parentTransform = toolSettings.parent;
            var paintItem = new PaintStrokeItem(prefab, itemPosition, itemRotation,
                itemScale, layer, parentTransform, surface: null, flipX: false, flipY: false);
            _paintStroke.Add(paintItem);
        }

        private static void PreviewWall(Camera camera, AxesUtils.Axis axis, bool rotateHalfTurn)
        {
            BrushstrokeItem[] brushstroke = null;
            if (PreviewIfBrushtrokestaysTheSame(out brushstroke, camera, forceUpdate: _paintStroke.Count == 0)) return;
            if (BrushstrokeManager.brushstroke.Length == 0) return;
            _previewData.Clear();
            _paintStroke.Clear();
            var toolSettings = WallManager.settings;
            var halfCellSize = toolSettings.moduleSize / 2;
            for (int i = 0; i < brushstroke.Length; ++i)
            {
                var strokeItem = brushstroke[i];
                if (strokeItem.settings == null) return;
                var prefab = strokeItem.settings.prefab;
                if (prefab == null) return;
                var scaleMult = strokeItem.scaleMultiplier;
                var itemRotation = Quaternion.Euler(strokeItem.additionalAngle);
                if (rotateHalfTurn) itemRotation *= Quaternion.AngleAxis(180, toolSettings.upwardAxis);
                if (WallManager.halfTurn)
                    itemRotation *= Quaternion.AngleAxis(180, toolSettings.upwardAxis);
                var cellCenter = strokeItem.tangentPosition;
                var centerToPivot = GetCenterToPivot(prefab, scaleMult, itemRotation);
                var itemPosition = cellCenter + centerToPivot;

                var nearbyObjects = new System.Collections.Generic.List<GameObject>();
                boundsOctree.GetColliding(cellCenter, halfCellSize, SnapManager.settings.rotation,
                    itemRotation, nearbyObjects);
                if (nearbyObjects.Count > 0)
                {
                    bool checkNextItem = false;
                    foreach (var obj in nearbyObjects)
                    {
                        if (obj == null) continue;
                        if (!obj.activeInHierarchy) continue;
                        var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                        var centerDistance = (objCenter - cellCenter).magnitude;
                        if (centerDistance > WallManager.wallThickness * 0.9999) continue;
                        if (PaletteManager.selectedPalette.ContainsSceneObject(obj))
                        {
                            checkNextItem = true;
                            break;
                        }
                    }
                    if (checkNextItem) continue;
                }

                var translateMatrix = Matrix4x4.Translate(Quaternion.Inverse(itemRotation) * -prefab.transform.position);
                var rootToWorld = Matrix4x4.TRS(itemPosition, itemRotation, scaleMult) * translateMatrix;
                var layer = toolSettings.overwritePrefabLayer ? toolSettings.layer : prefab.layer;
                PreviewBrushItem(prefab, rootToWorld, layer, camera,
                    redMaterial: false, reverseTriangles: false, flipX: false, flipY: false);
                _previewData.Add(new PreviewData(prefab, rootToWorld, layer, flipX: false, flipY: false));
                var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
                Transform parentTransform = toolSettings.parent;
                var paintItem = new PaintStrokeItem(prefab, itemPosition, itemRotation,
                    itemScale, layer, parentTransform, surface: null, flipX: false, flipY: false);
                _paintStroke.Add(paintItem);
            }
        }

        private static void PreviewDeleteSingleWall(Camera camera, AxesUtils.Axis axis, Vector3 position)
        {
            if (BrushstrokeManager.brushstroke.Length == 0) return;
            var strokeItem = BrushstrokeManager.brushstroke[0].Clone();
            if (strokeItem.settings == null)
            {
                BrushstrokeManager.UpdateWallBrushstroke(axis, cellsCount: 1, setNextIdx: false, deleteMode: true);
                return;
            }
            var itemRotation = Quaternion.Euler(strokeItem.additionalAngle);
            if (axis == AxesUtils.Axis.Z) itemRotation *= Quaternion.Euler(0f, 90f, 0f);
            var TRS = Matrix4x4.TRS(position, itemRotation, WallManager.settings.moduleSize);
            Graphics.DrawMesh(cubeMesh, TRS, transparentRedMaterial2, 0, camera);
            _wallDeleteStroke.Clear();
            _wallDeleteStroke.Add(new Pose(position, itemRotation));
        }
        private static System.Collections.Generic.HashSet<Pose> _wallDeleteStroke
            = new System.Collections.Generic.HashSet<Pose>();
        private static void PreviewDeleteWall(Camera camera, AxesUtils.Axis axis, Vector3 position)
        {
            if (BrushstrokeManager.brushstroke.Length == 0) return;
            var brushstroke = BrushstrokeManager.brushstroke;
            var toolSettings = WallManager.settings;
            _wallDeleteStroke.Clear();
            for (int i = 0; i < brushstroke.Length; ++i)
            {
                var strokeItem = brushstroke[i];
                var itemPosition = strokeItem.tangentPosition;
                var itemRotation = Quaternion.Euler(strokeItem.additionalAngle);
                var rootToWorld = Matrix4x4.TRS(itemPosition, itemRotation, WallManager.settings.moduleSize);
                Graphics.DrawMesh(cubeMesh, rootToWorld, transparentRedMaterial2, layer: 0, camera);
                _wallDeleteStroke.Add(new Pose(itemPosition, itemRotation));
            }
        }

        private static void DeleteWall()
        {
            if (_wallDeleteStroke.Count == 0) return;
            var toolSettings = WallManager.settings;
            var toBeDeleted = new System.Collections.Generic.HashSet<GameObject>();
            var halfCellSize = toolSettings.moduleSize / 2;
            foreach (var cellPose in _wallDeleteStroke)
            {
                var nearbyObjects = new System.Collections.Generic.List<GameObject>();
                boundsOctree.GetColliding(cellPose.position, halfCellSize,
                    SnapManager.settings.rotation, cellPose.rotation, nearbyObjects);
                if (nearbyObjects.Count == 0) continue;
                foreach (var obj in nearbyObjects)
                {
                    if (obj == null) continue;
                    if (!obj.activeInHierarchy) continue;
                    var objCenter = BoundsUtils.GetBoundsRecursive(obj.transform).center;
                    var centerDistance = (objCenter - cellPose.position).magnitude;
                    if (centerDistance > WallManager.wallThickness * 0.999) continue;
                    if (PaletteManager.selectedPalette.ContainsSceneObject(obj)) toBeDeleted.Add(obj);
                }
            }
            void EraseObject(GameObject obj)
            {
                if (obj == null) return;
                var root = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(obj);
                if (root != null) obj = root;
                PWBCore.DestroyTempCollider(obj.GetInstanceID());
                UnityEditor.Undo.DestroyObjectImmediate(obj);
            }
            foreach (var obj in toBeDeleted) EraseObject(obj);
        }
    }
}