using System.Collections.Generic;
using UnityEngine;

public class ProjectileTurret : MonoBehaviour
{
    [SerializeField] float projectileSpeed = 1;
    [SerializeField] Vector3 gravity = new Vector3(0, -9.8f, 0);
    [SerializeField] LayerMask targetLayer;
    [SerializeField] GameObject crosshair;
    [SerializeField] float baseTurnSpeed = 3;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] GameObject gun;
    [SerializeField] Transform turretBase;
    [SerializeField] Transform barrelEnd;
    [SerializeField] bool useLowAngle;

    // Trajectory pointer
    [SerializeField] LineRenderer trajectory;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        TrackMouse();
        TurnBase();
        RotateGun();     //Angle to rotate

        // For the trajectory prediction
        TrajectoryLine();

        if (Input.GetButtonDown("Fire1"))
            Fire();
    }

    void Fire()
    {
        GameObject projectile = Instantiate(projectilePrefab, barrelEnd.position, gun.transform.rotation);
        projectile.GetComponent<Rigidbody>().velocity = projectileSpeed * barrelEnd.transform.forward;
    }

    void TrackMouse()
    {
        Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(cameraRay, out hit, 1000, targetLayer))
        {
            crosshair.transform.forward = hit.normal;
            crosshair.transform.position = hit.point + (hit.normal * 0.1f);
            //Debug.Log("hit ground");
        }
    }

    void TurnBase()
    {
        Vector3 directionToTarget = (crosshair.transform.position - turretBase.transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
        turretBase.transform.rotation = Quaternion.Slerp(turretBase.transform.rotation, lookRotation, Time.deltaTime * baseTurnSpeed);
    }

    void RotateGun()
    {
        float? angle = CalculateTrajectory(crosshair.transform.position, useLowAngle);
        if (angle != null)
            gun.transform.localEulerAngles = new Vector3(360f - (float)angle, 0, 0);
        
    }

    float? CalculateTrajectory(Vector3 target, bool useLow)
    {
        Vector3 targetDir = target - barrelEnd.position;

        float y = targetDir.y;
        targetDir.y = 0;

        float x = targetDir.magnitude;

        float v = projectileSpeed;
        float v2 = Mathf.Pow(v, 2);
        float v4 = Mathf.Pow(v, 4);
        float g = gravity.y;
        float x2 = Mathf.Pow(x, 2);

        float underRoot = v4 - (g * ((g * x2) + (2 * y * v2)));

        if (underRoot >= 0)
        {
            float root = Mathf.Sqrt(underRoot);
            float highAngle = v2 + root;
            float lowAngle = v2 - root;

            if (useLow)
                return Mathf.Atan2(lowAngle, g * x) * Mathf.Rad2Deg;
            else
                return Mathf.Atan2(highAngle, g * x) * Mathf.Rad2Deg;
        }
        else
            return null;
    }

    public void TrajectoryLine()
    {
        List<Vector3> points = new List<Vector3>();
        Vector3 initialPosition = barrelEnd.position;
        Vector3 initialVelocity = projectileSpeed * barrelEnd.forward;

        float timeIncrement = 0.1f;
        float time = 5f;

        // Trajectory line prediction over time
        for (float t = 0; t < time; t += timeIncrement)
        {
            // Kinematic equation
            Vector3 displacement = initialPosition + (initialVelocity * t) + (0.5f * -gravity * t * t);

            // Stop if collision with surface
            if (points.Count > 0)
            {
                Vector3 lastPoint = points[points.Count - 1];
                if (Physics.Raycast(lastPoint, (displacement - lastPoint).normalized, out RaycastHit hit, (displacement - lastPoint).magnitude, targetLayer))
                {
                    points.Add(hit.point);
                    break;
                }
            }

            // Add position to the trajectory points
            points.Add(displacement);

            // Add trajectory points to line renderer
            trajectory.positionCount = points.Count;
            trajectory.SetPositions(points.ToArray());
        }
    }
}
