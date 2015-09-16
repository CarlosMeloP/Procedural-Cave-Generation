using UnityEngine;

public class Player : MonoBehaviour 
{
    Rigidbody rigidbodyP;
    Vector3 velocity;
    public float speed = 30;

	// Use this for initialization
	void Start () 
    {
        rigidbodyP = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () 
    {
	    velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * speed;
	}

    void FixedUpdate()
    {
        rigidbodyP.MovePosition(rigidbodyP.position + velocity * Time.fixedDeltaTime);
    }
}
