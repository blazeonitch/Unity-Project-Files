using UnityEngine;
using System.Collections;

public class RecoilSystem : MonoBehaviour
{
   [Header("Recoil Settings")]
   [SerializeField] private Vector3 recoilRotation = new Vector3(0, 0.01f, -0.01f); // Rotation recoil (X = Kickback)
   [SerializeField] private Vector3 recoilPosition = new Vector3(0f, 0f, -0.01f); // Position recoil (Z = Pushback)
   [SerializeField] private float recoilSpeed = 10f; // Speed of recoil effect
   [SerializeField] private float returnSpeed = 5f; // Speed of returning to normal
   [SerializeField] private InputMappings inputMappings;

   private Vector3 originalPosition;
   private Quaternion originalRotation;
   private Vector3 currentRotation;
   private Vector3 currentPosition;

   private void Start()
   {
       originalRotation = transform.localRotation;
       originalPosition = transform.localPosition;
       currentRotation = originalRotation.eulerAngles;
       currentPosition = originalPosition;
   }

   public void ApplyRecoil()
   {
       StopAllCoroutines();
       StartCoroutine(DoRecoil());
   }

 
   private IEnumerator DoRecoil()
   {
       float t = 0;

       Vector3 adjustedRecoilRotation = inputMappings.AimAction.ReadValue<float>() > 0.5f ? recoilRotation * 0.5f : recoilRotation;
       Vector3 adjustedRecoilPosition = inputMappings.AimAction.ReadValue<float>() > 0.5f ? recoilPosition * 0.5f : recoilPosition;

       while (t < 1)
       {
           t += Time.deltaTime * recoilSpeed;
           currentRotation = Vector3.Lerp(currentRotation, originalRotation.eulerAngles + adjustedRecoilRotation, t);
           currentPosition = Vector3.Lerp(currentPosition, originalPosition + adjustedRecoilPosition, t);

           transform.localRotation = Quaternion.Euler(currentRotation);
           transform.localPosition = currentPosition;

           yield return null;
       }

       t = 0;
       while (t < 1)
       {
           t += Time.deltaTime * returnSpeed;
           currentRotation = Vector3.Lerp(currentRotation, originalRotation.eulerAngles, t);
           currentPosition = Vector3.Lerp(currentPosition, originalPosition, t);

           transform.localRotation = Quaternion.Euler(currentRotation);
           transform.localPosition = currentPosition;

           yield return null;
       }

       transform.localRotation = originalRotation;
       transform.localPosition = originalPosition;
   }
}

 