using UnityEngine;
using UnityEngine.UI;

public class GiftFall : MonoBehaviour
{
    public float fallSpeed = 2.0f;
    public GameObject sparkleEffect;
    public Text messageText;

    private bool hasLanded = false;

    void Start()
    {
        messageText.text = "Raksha Bandhan Gift Coming...";
    }

    void Update()
    {
        if (!hasLanded)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;

            if (transform.position.y <= 0.5f)
            {
                hasLanded = true;
                Instantiate(sparkleEffect, transform.position, Quaternion.identity);
                messageText.text = "Gift Arrived! 💝";
            }
        }
    }
}
