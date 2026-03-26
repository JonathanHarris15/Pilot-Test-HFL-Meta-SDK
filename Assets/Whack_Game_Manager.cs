using UnityEngine;

public class Whack_Game_Manager : MonoBehaviour
{
    // Hand Objects
    [SerializeField]
    private GameObject left_hand;
    [SerializeField]
    private GameObject right_hand;

    // Set this in the Inspector to control the size of the colliders
    [SerializeField]
    private float colliderRadius = 0.02f;

    void Start()
    {
        // Initialize colliders for both hands, looking for the specific wrist names
        SetupHandColliders(left_hand, "L_Wrist");
        SetupHandColliders(right_hand, "R_Wrist");
    }

    private void SetupHandColliders(GameObject handRoot, string wristName)
    {
        if (handRoot == null) return;

        // Transform.Find looks for a direct child with the exact name
        Transform wristTransform = handRoot.transform.Find(wristName);

        if (wristTransform != null)
        {
            AddCollidersRecursively(wristTransform);
        }
        else
        {
            Debug.LogWarning($"Could not find {wristName} under {handRoot.name}. Check your hierarchy naming.");
        }
    }

    private void AddCollidersRecursively(Transform currentTransform)
    {
        // Check if a SphereCollider already exists to avoid duplicates
        SphereCollider sphereCollider = currentTransform.gameObject.GetComponent<SphereCollider>();

        if (sphereCollider == null)
        {
            // Add the component at runtime
            sphereCollider = currentTransform.gameObject.AddComponent<SphereCollider>();
        }

        // Apply the radius set in the inspector
        sphereCollider.radius = colliderRadius;

        // Loop through all immediate children and run this same function on them
        foreach (Transform child in currentTransform)
        {
            AddCollidersRecursively(child);
        }
    }

    void Update()
    {
        // Update logic here
    }
}