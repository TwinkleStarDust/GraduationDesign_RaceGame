/*
Copyright (c) 2021 Omar Duarte
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
    public class ReplacerSettings : CircleToolBase, ISelectionBrushTool, IModifierTool, IPaintToolSettings
    {
        [SerializeField] private bool _keepTargetSize = false;
        [SerializeField] private bool _maintainProportions = false;

        public enum PositionMode { CENTER, PIVOT, ON_SURFACE }
        [SerializeField] private PositionMode _positionMode = PositionMode.CENTER;

        [SerializeField] private bool _sameParentasTarget = true;
        public bool keepTargetSize
        {
            get => _keepTargetSize;
            set
            {
                if (_keepTargetSize == value) return;
                _keepTargetSize = value;
                DataChanged();
            }
        }
        public bool maintainProportions
        {
            get => _maintainProportions;
            set
            {
                if (_maintainProportions == value) return;
                _maintainProportions = value;
                DataChanged();
            }
        }

        public PositionMode positionMode
        {
            get => _positionMode;
            set
            {
                if (_positionMode == value) return;
                _positionMode = value;
                DataChanged();
            }
        }
        public bool sameParentAsTarget
        {
            get => _sameParentasTarget;
            set
            {
                if (_sameParentasTarget == value) return;
                _sameParentasTarget = value;
                DataChanged();
            }
        }

        #region MODIFIER TOOL
        [SerializeField] private ModifierToolSettings _modifierTool = new ModifierToolSettings();
        public ReplacerSettings() => _modifierTool.OnDataChanged += DataChanged;
        public ModifierToolSettings.Command command { get => _modifierTool.command; set => _modifierTool.command = value; }
        public bool modifyAllButSelected
        {
            get => _modifierTool.modifyAllButSelected;
            set => _modifierTool.modifyAllButSelected = value;
        }

        public bool onlyTheClosest
        {
            get => _modifierTool.onlyTheClosest;
            set => _modifierTool.onlyTheClosest = value;
        }
        public bool outermostPrefabFilter
        {
            get => _modifierTool.outermostPrefabFilter;
            set => _modifierTool.outermostPrefabFilter = value;
        }
        #endregion

        #region PAINT TOOL
        [SerializeField] private PaintToolSettings _paintTool = new PaintToolSettings();
        public Transform parent { get => _paintTool.parent; set => _paintTool.parent = value; }
        public bool overwritePrefabLayer
        {
            get => _paintTool.overwritePrefabLayer;
            set => _paintTool.overwritePrefabLayer = value;
        }
        public int layer { get => _paintTool.layer; set => _paintTool.layer = value; }
        public bool autoCreateParent { get => _paintTool.autoCreateParent; set => _paintTool.autoCreateParent = value; }
        public bool setSurfaceAsParent { get => _paintTool.setSurfaceAsParent; set => _paintTool.setSurfaceAsParent = value; }
        public bool createSubparentPerPalette
        {
            get => _paintTool.createSubparentPerPalette;
            set => _paintTool.createSubparentPerPalette = value;
        }
        public bool createSubparentPerTool
        {
            get => _paintTool.createSubparentPerTool;
            set => _paintTool.createSubparentPerTool = value;
        }
        public bool createSubparentPerBrush
        {
            get => _paintTool.createSubparentPerBrush;
            set => _paintTool.createSubparentPerBrush = value;
        }
        public bool createSubparentPerPrefab
        {
            get => _paintTool.createSubparentPerPrefab;
            set => _paintTool.createSubparentPerPrefab = value;
        }
        public bool overwriteBrushProperties
        {
            get => _paintTool.overwriteBrushProperties;
            set => _paintTool.overwriteBrushProperties = value;
        }
        public BrushSettings brushSettings => _paintTool.brushSettings;

        #endregion

        public override void Copy(IToolSettings other)
        {
            var otherReplacerSettings = other as ReplacerSettings;
            if (otherReplacerSettings == null) return;
            base.Copy(other);
            _modifierTool.Copy(otherReplacerSettings);
            _paintTool.Copy(otherReplacerSettings._paintTool);
            _keepTargetSize = otherReplacerSettings._keepTargetSize;
            _maintainProportions = otherReplacerSettings._maintainProportions;
            _positionMode = otherReplacerSettings._positionMode;
            _sameParentasTarget = otherReplacerSettings._sameParentasTarget;
        }

        public override void DataChanged()
        {
            base.DataChanged();
            BrushstrokeManager.ClearReplacerDictionary();
            BrushstrokeManager.UpdateBrushstroke();
        }
    }

    [System.Serializable]
    public class ReplacerManager : ToolManagerBase<ReplacerSettings> { }
    #endregion

    #region PWBIO
    public static partial class PWBIO
    {
        private class ReplacerPaintStrokeItem : PaintStrokeItem
        {
            public Transform target = null;

            public ReplacerPaintStrokeItem(Transform target, GameObject prefab, Vector3 position, Quaternion rotation,
                Vector3 scale, int layer, Transform parent, Transform surface, bool flipX, bool flipY,
                int index = -1) : base(prefab, position, rotation, scale, layer, parent, surface, flipX, flipY, index)
            {
                this.target = target;
            }
        }
        private static System.Collections.Generic.List<Renderer> _replaceRenderers
            = new System.Collections.Generic.List<Renderer>();
        private static bool _replaceAllSelected = false;
        private static void ReplacerMouseEvents()
        {
            var settings = ReplacerManager.settings;
            if (Event.current.button == 0 && !Event.current.alt
                && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag))
            {
                Replace();
                Event.current.Use();
            }
            if (Event.current.button == 1)
            {
                if (Event.current.type == EventType.MouseDown && (Event.current.control || Event.current.shift))
                {
                    _pinned = true;
                    _pinMouse = Event.current.mousePosition;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp) _pinned = false;
            }
        }

        private static void ReplacerDuringSceneGUI(UnityEditor.SceneView sceneView)
        {
            if (PaletteManager.selectedBrushIdx < 0) return;
            if (_replaceAllSelected)
            {
                var targets = SelectionManager.topLevelSelection;
                BrushstrokeManager.UpdateReplacerBrushstroke(clearDictionary: true, targets);
                _paintStroke.Clear();
                _replaceAllSelected = false;
                ReplacePreview(sceneView.camera, ReplacerManager.settings, targets);
                Replace();
                return;
            }
            ReplacerMouseEvents();

            var mousePos = Event.current.mousePosition;
            if (_pinned) mousePos = _pinMouse;
            var mouseRay = UnityEditor.HandleUtility.GUIPointToWorldRay(mousePos);

            var center = mouseRay.GetPoint(_lastHitDistance);
            if (MouseRaycast(mouseRay, out RaycastHit mouseHit, out GameObject collider,
                float.MaxValue, -1, true, true))
            {
                _lastHitDistance = mouseHit.distance;
                center = mouseHit.point;
                PWBCore.UpdateTempCollidersIfHierarchyChanged();
            }
            DrawCircleTool(center, sceneView.camera, new Color(0f, 0f, 0, 1f), ReplacerManager.settings.radius);
            DrawPreview(center, mouseRay, sceneView.camera);
        }
        public static void ResetReplacer()
        {
            foreach (var renderer in _replaceRenderers)
            {
                if (renderer == null) continue;
                renderer.enabled = true;
            }
            _replaceRenderers.Clear();
            _paintStroke.Clear();
            BrushstrokeManager.ClearReplacerDictionary();
        }

        private static Transform _replaceSurface = null;

        private static void ReplacePreview(Camera camera, ReplacerSettings settings, GameObject[] targets)
        {
            BrushstrokeManager.UpdateReplacerBrushstroke(false, targets);
            var brushstroke = BrushstrokeManager.brushstroke;
            foreach (var strokeItem in brushstroke)
            {
                var target =  BrushstrokeManager.GetReplacerTargetFromStrokeItem(strokeItem);
                if (target == null) continue;
                var prefab = strokeItem.settings.prefab;
                var itemPosition = strokeItem.tangentPosition;
                var itemRotation = Quaternion.Euler(strokeItem.additionalAngle);
                var scaleMult = strokeItem.scaleMultiplier;
                var itemScale = Vector3.Scale(prefab.transform.localScale, scaleMult);
                
                var layer = settings.overwritePrefabLayer ? settings.layer : target.gameObject.layer;
                Transform parentTransform = target.parent;

                if (!settings.sameParentAsTarget)
                    parentTransform = GetParent(settings, prefab.name, create: false, _replaceSurface);

                _paintStroke.Add(new ReplacerPaintStrokeItem(target, prefab, itemPosition,
                    itemRotation * prefab.transform.rotation,
                    itemScale, layer, parentTransform, null, false, false));
                var rootToWorld = Matrix4x4.TRS(itemPosition, itemRotation, scaleMult)
                    * Matrix4x4.Translate(-prefab.transform.position);
                PreviewBrushItem(prefab, rootToWorld, layer, camera, false, false, strokeItem.flipX, strokeItem.flipY);
            }
        }

        private static GameObject[] Replace()
        {
            const string COMMAND_NAME = "Replace";
            foreach (var renderer in _replaceRenderers) renderer.enabled = true;
            _replaceRenderers.Clear();
            var settings = ReplacerManager.settings;
            var newObjects = new System.Collections.Generic.HashSet<GameObject>();
            foreach (ReplacerPaintStrokeItem item in _paintStroke)
            {
                if (item.prefab == null) continue;
                var target = item.target.gameObject;
                if (target == null) continue;
                if (settings.outermostPrefabFilter)
                {
                    var nearestRoot = UnityEditor.PrefabUtility.GetNearestPrefabInstanceRoot(target);
                    if (nearestRoot != null) target = nearestRoot;
                }
                else
                {
                    var parent = target.transform.parent.gameObject;
                    if (parent != null)
                    {
                        GameObject outermost = null;
                        do
                        {
                            outermost = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(target);
                            if (outermost == null) break;
                            if (outermost == target) break;
                            UnityEditor.PrefabUtility.UnpackPrefabInstance(outermost,
                                UnityEditor.PrefabUnpackMode.OutermostRoot, UnityEditor.InteractionMode.UserAction);
                        } while (outermost != parent);
                    }
                }
                var type = UnityEditor.PrefabUtility.GetPrefabAssetType(item.prefab);
                GameObject obj = type == UnityEditor.PrefabAssetType.NotAPrefab ? GameObject.Instantiate(item.prefab)
                    : (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab
                    (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(item.prefab)
                    ? item.prefab : UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(item.prefab));
                if (settings.overwritePrefabLayer) obj.layer = settings.layer;
                obj.transform.SetPositionAndRotation(item.position, item.rotation);
                obj.transform.localScale = item.scale;
                var root = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
                PWBCore.AddTempCollider(obj);
                AddPaintedObject(obj);
                newObjects.Add(obj);
                if (!LineManager.instance.ReplaceObject(target, obj))
                    if (!ShapeManager.instance.ReplaceObject(target, obj))
                        TilingManager.instance.ReplaceObject(target, obj);

                var tempColliders = PWBCore.GetTempColliders(target);
                if (tempColliders != null)
                    foreach (var tempCollider in tempColliders) UnityEditor.Undo.DestroyObjectImmediate(tempCollider);
                UnityEditor.Undo.DestroyObjectImmediate(target);
                UnityEditor.Undo.RegisterCreatedObjectUndo(obj, COMMAND_NAME);
                Transform parentTransform = item.parent;
                if (settings.sameParentAsTarget)
                {
                    if (root != null) UnityEditor.Undo.SetTransformParent(root.transform, parentTransform, COMMAND_NAME);
                    else UnityEditor.Undo.SetTransformParent(obj.transform, parentTransform, COMMAND_NAME);
                }
                else
                {
                    parentTransform = GetParent(settings, item.prefab.name, create: true, _replaceSurface);
                    UnityEditor.Undo.SetTransformParent(obj.transform, parentTransform, COMMAND_NAME);
                }
            }
            _paintStroke.Clear();
            BrushstrokeManager.ClearReplacerDictionary();
            return newObjects.ToArray();
        }

        public static void ReplaceAllSelected() => _replaceAllSelected = true;

        private static GameObject[] GetReplacerTargets(Vector3 center, Ray mouseRay, Camera camera)
        {
            var targetsSet = new System.Collections.Generic.HashSet<GameObject>();
            GetCircleToolTargets(mouseRay, camera, ReplacerManager.settings, ReplacerManager.settings.radius, targetsSet);
            var toReplace = targetsSet.ToArray();
            _paintStroke.Clear();
            var result = new System.Collections.Generic.HashSet<GameObject>();
            for (int i = 0; i < toReplace.Length; ++i)
            {
                var obj = toReplace[i];
                var isChild = false;
                foreach (var listed in toReplace)
                {
                    if (obj.transform.IsChildOf(listed.transform) && listed != obj)
                    {
                        isChild = true;
                        break;
                    }
                }
                if (isChild) continue;
                result.Add(obj);
            }
            return result.ToArray();
        }

        private static void DrawPreview(Vector3 center, Ray mouseRay, Camera camera)
        {
            foreach (var renderer in _replaceRenderers)
            {
                if (renderer == null) continue;
                renderer.enabled = true;
            }
            
            var targets = GetReplacerTargets(center, mouseRay, camera);
            _replaceRenderers.Clear();
            foreach ( var obj in targets)
                _replaceRenderers.AddRange(obj.GetComponentsInChildren<Renderer>().Where(r => r.enabled == true));
            ReplacePreview(camera, ReplacerManager.settings, targets);
            foreach (var renderer in _replaceRenderers) renderer.enabled = false;
        }
    }
    #endregion
}