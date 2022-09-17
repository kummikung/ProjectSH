using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    public float followSpeed ;
    public float damping ;

    public float X_OffSet;
    public float Y_OffSet;

    public Transform CameraTarget;

    public Vector3 Offset;

    private Vector3 velocity = Vector3.zero;

    private void Update()
    {
        Vector3 newPos = new Vector3(CameraTarget.position.x - X_OffSet, CameraTarget.position.y + Y_OffSet, -10f);
        transform.position = Vector3.Slerp(transform.position, newPos, followSpeed * Time.deltaTime);

        Vector3 movePos = CameraTarget.position + Offset;
        transform.position = Vector3.SmoothDamp(transform.position, movePos, ref velocity, damping);
    }

    private void FixedUpdate()
    {

    }
}
