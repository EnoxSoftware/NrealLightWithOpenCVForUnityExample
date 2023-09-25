using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastLaser : MonoBehaviour
{
    public float _lineWidthMultiplier = 0.01f;
    public Material _laserMaterial;

    public void shootLaserFrom(Vector3 from, Vector3 direction, float length, Material mat = null)
    {
        LineRenderer lr = new GameObject().AddComponent<LineRenderer>();
        lr.transform.parent = transform;
        lr.widthMultiplier = _lineWidthMultiplier;

        lr.material = mat == null ? _laserMaterial : mat;

        Vector3 to = from + length * direction;

        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
    }
}
