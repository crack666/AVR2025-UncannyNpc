using UnityEngine;

public class DemoSoldier : MonoBehaviour
{
    [SerializeField]
    private float rotationSpeed = -30;
    [SerializeField]
    private Transform camBaseTF;

    // Update is called once per frame
    void Update()
    {
        camBaseTF.Rotate(Vector3.up, Time.deltaTime * rotationSpeed);
    }
}
