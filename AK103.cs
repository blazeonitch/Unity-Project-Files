using UnityEngine;

public class AK103 : Gun
{
    private void Awake()
    {
        gunData.isAutomatic = true; 
    }

    public override void Shoot()
    {
        RaycastHit hit;
        Vector3 target = Vector3.zero;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, gunData.shootingRange, gunData.targetLayerMask))
        {
            Debug.Log(gunData.gunName + " hit " + hit.collider.name);
            target = hit.point;
        }
        else
        {
            target = cameraTransform.position + cameraTransform.forward * gunData.shootingRange;
        }

        StartCoroutine(BulletFire(target, hit));
    }
}
