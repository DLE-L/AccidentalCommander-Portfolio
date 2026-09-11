using System.Collections.Generic;
using Lizzo.PV.Gameplay.World;
using UnityEngine;

public interface IVisibilityCullTarget
{
    void OnVisibilityEnter(CameraVisibilityZone zone);
    void OnVisibilityExit(CameraVisibilityZone zone);
}

public interface IWorldVisibilityQuery
{
    bool ContainsWorldPosition(Vector3 worldPosition);
}

public sealed class CameraVisibilityZone : MonoBehaviour, IWorldVisibilityQuery
{
    const float VIEW_MARGIN_WORLD_UNITS = 1.25f;

    readonly Dictionary<IVisibilityCullTarget, int> _overlapCounts = new Dictionary<IVisibilityCullTarget, int>();
    Camera _camera;
    [SerializeField] BoxCollider2D _zoneCollider;
    [SerializeField] Rigidbody2D _body;
    float _lastWidth = -1.0f;
    float _lastHeight = -1.0f;


    public bool Setup(Camera camera)
    {
        _camera = camera;
        if (!ValidateAuthoredReferences())
            return false;

        RefreshFromCamera();
        return true;
    }

    public void RefreshFromCamera()
    {
        if (_camera == null || _camera.orthographic == false)
            return;

        float height = _camera.orthographicSize * 2.0f + VIEW_MARGIN_WORLD_UNITS * 2.0f;
        float width = _camera.orthographicSize * 2.0f * _camera.aspect + VIEW_MARGIN_WORLD_UNITS * 2.0f;
        if (Mathf.Abs(width - _lastWidth) < 0.001f && Mathf.Abs(height - _lastHeight) < 0.001f)
            return;

        _zoneCollider.offset = Vector2.zero;
        _zoneCollider.size = new Vector2(width, height);
        _lastWidth = width;
        _lastHeight = height;
    }

    public bool ContainsWorldPosition(Vector3 worldPosition)
    {
        if (_zoneCollider == null)
            RefreshFromCamera();

        if (_zoneCollider == null)
            return true;

        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        Vector2 offset = _zoneCollider.offset;
        Vector2 halfSize = _zoneCollider.size * 0.5f;
        return localPosition.x >= offset.x - halfSize.x
            && localPosition.x <= offset.x + halfSize.x
            && localPosition.y >= offset.y - halfSize.y
            && localPosition.y <= offset.y + halfSize.y;
    }

    public void Forget(IVisibilityCullTarget target)
    {
        if (target == null)
            return;

        _overlapCounts.Remove(target);
    }

    void OnDisable()
    {
        _overlapCounts.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        IVisibilityCullTarget target = ResolveTarget(other);
        if (target == null)
            return;

        if (_overlapCounts.TryGetValue(target, out int count))
        {
            _overlapCounts[target] = count + 1;
            return;
        }

        _overlapCounts[target] = 1;
        target.OnVisibilityEnter(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        IVisibilityCullTarget target = ResolveTarget(other);
        if (target == null)
            return;

        if (_overlapCounts.TryGetValue(target, out int count) == false)
            return;

        count--;
        if (count > 0)
        {
            _overlapCounts[target] = count;
            return;
        }

        _overlapCounts.Remove(target);
        target.OnVisibilityExit(this);
    }

    bool ValidateAuthoredReferences()
    {
        if (_body == null || _zoneCollider == null)
        {
            Debug.LogError("[CameraVisibilityZone] Authored Rigidbody2D and BoxCollider2D references are required.", this);
            return false;
        }

        if (_body.bodyType != RigidbodyType2D.Kinematic || !_zoneCollider.isTrigger)
        {
            Debug.LogError("[CameraVisibilityZone] Rigidbody2D must be Kinematic and BoxCollider2D must be a trigger.", this);
            return false;
        }

        return true;
    }

    static IVisibilityCullTarget ResolveTarget(Collider2D collider)
    {
        if (collider == null)
            return null;

        VisibilityCullProbe probe = collider.GetComponent<VisibilityCullProbe>();
        return probe == null ? null : probe.Target;
    }
}
