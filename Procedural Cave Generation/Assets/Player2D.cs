using UnityEngine;

public class Player2D : MonoBehaviour 
{
    Rigidbody2D rigidbodyP;
    Vector2 velocity;
    public float speed = 30;

	// Use this for initialization
	void Start () 
    {
        rigidbodyP = GetComponent<Rigidbody2D>();
	}
	
	// Update is called once per frame
	void Update () 
    {
	    velocity = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized * speed;
	}

    void FixedUpdate()
    {
        rigidbodyP.MovePosition(rigidbodyP.position + velocity * Time.fixedDeltaTime);
    }
}
