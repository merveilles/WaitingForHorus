using UnityEngine;

public class BulletScript : MonoBehaviour 
{
    public static int BulletCollisionLayers
    {
        get
        {
            return (1<<LayerMask.NameToLayer("Default")) |
                   (1<<LayerMask.NameToLayer("Player Hit"));
        }
    }

    public GameObject bulletCasingPrefab;
    public float speed = 900;
	public float lifetime = 2;
    public int damage = 1;
    public float areaOfEffect = 0;
    public float recoil = 0;
    public float homing = 0;
	public float accelerationSpeed = 0.1f;
    public Transform target;
	public float TrailAlpha = 0.5f;
    bool dead;
	float acceleration = 1.0f;
	float randomBrightness = 1.0f;

    public NetworkPlayer Player { get; set; }
	
	public LayerMask layerMask; //make sure we aren't in this layer 
	public float skinWidth = 0.1f; //probably doesn't need to be changed 
 
	private float minimumExtent; 
	private float partialExtent; 
	private float sqrMinimumExtent; 
	private Vector3 previousPosition; 
	private Rigidbody myRigidbody; 
	
    public void OnNetworkInstantiate( NetworkMessageInfo info )
    {
		/*if( Network.isServer )
		{
			foreach( NetworkView nv in GetComponents<NetworkView>() )
				foreach( NetworkPlayer np in Network.connections )
			   	 nv.SetScope( np, true );
		}*/
		
		randomBrightness = RandomHelper.Between( 0.125f, 1.0f );
    }

    public void Awake()
    {
		// Auxillary Collision Testing
		myRigidbody = rigidbody; 
		previousPosition = myRigidbody.position; 
		minimumExtent = Mathf.Min(Mathf.Min(collider.bounds.extents.x, collider.bounds.extents.y), collider.bounds.extents.z); 
		partialExtent = minimumExtent * (1.0f - skinWidth); 
		sqrMinimumExtent = minimumExtent * minimumExtent; 
		
        GameObject casing = (GameObject)
            Instantiate(bulletCasingPrefab, transform.position, transform.rotation);
        casing.rigidbody.AddRelativeForce(
            new Vector3(1 + Random.value, Random.value + 1, 0),
            ForceMode.Impulse);
        casing.rigidbody.AddTorque(
            5 * new Vector3(-0.5f-Random.value, -Random.value*0.1f, -0.5f-Random.value),
            ForceMode.Impulse);
        casing.rigidbody.useGravity = true;
    }

    bool DoDamageTo( Transform t )
    {
        HealthScript health = t.GetComponent<HealthScript>();
        // err, kinda lame, this is so that the collider can be
        // directly inside the damaged object rather than on it,
        // useful when the damage collider is different from the
        // real collider
        if( health == null && t.parent != null )
           health = t.parent.GetComponent<HealthScript>();

        if( health != null )
        {
            if (health.networkView.owner != Player ) // No Friendly Fire
			{
				if( networkView.isMine ) 
					audio.Play(); //Hitreg Sound
                health.DoDamage(damage, Player);
				
				return true;
			}
        }
		
        return false;
    }
	
	void DoRecoil( Vector3 point, bool playerWasHit )
	{
        Collider[] colliders = Physics.OverlapSphere( point, 15, ( 1 << LayerMask.NameToLayer("Player Hit") ) );
        foreach( Collider c in colliders )
        {
            var hitReceiver = c.gameObject.GetComponent<PlayerHitReceiver>();
            if (hitReceiver == null) continue;
            // Recoil is applied per-client, locally
            if (!hitReceiver.Player.gameObject.networkView.isMine) continue;

            var playerTransform = hitReceiver.Player.gameObject.transform;
            Vector3 positionDifference = playerTransform.position - point;
            // is there a function to do both of these at once? kinda dumb
            Vector3 impulseDirection = positionDifference.normalized;
            float impulseDistance = positionDifference.magnitude;

            var dist = Mathf.Max( impulseDistance, 0.5f );

            var impulse = impulseDirection * ( 45 / dist );
            if( impulse.y > 0 ) 
				impulse.y *= 2.25f;
            else 
				impulse.y = 0;

            if( playerWasHit )
                impulse *= 10;

            hitReceiver.Player.AddRecoil(impulse);
        }
	}
	
	void Collide( Transform trans, Vector3 point, Vector3 normal ) 
	{
		bool playerWasHit = DoDamageTo( trans );
		if( recoil > 0 ) 
			DoRecoil( point, playerWasHit );
		
        if( playerWasHit )
			EffectsScript.ExplosionHit( point, Quaternion.LookRotation( normal ) );
        else
			EffectsScript.Explosion( point, Quaternion.LookRotation( normal ) );

		dead = true;
		Destroy( rigidbody );
		renderer.enabled = false;
	}
	
	public void OnCollisionEnter( Collision collision ) 
	{
		BulletScript hitBullet = collision.gameObject.GetComponent<BulletScript>();
        if( !dead && hitBullet == null )
			Collide( collision.transform, collision.contacts[0].point, collision.contacts[0].normal );
	}
	
	public void OnCollisionStay( Collision collision ) 
	{
		BulletScript hitBullet = collision.gameObject.GetComponent<BulletScript>();
        if( !dead && hitBullet == null )
			Collide( collision.transform, collision.contacts[0].point, collision.contacts[0].normal );
	}

    public void Update()
    {
        if( !dead )
        {
			acceleration += accelerationSpeed * Time.deltaTime;
            float distance = speed * Time.deltaTime;
			distance *= acceleration;

            transform.position += transform.forward * distance;

            // homing
            if( target != null && homing > 0 )
            {
                //Debug.Log("Is homing @ " + homing);
                var lookVec = (target.position - transform.position).normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookVec),
                                                      Mathf.Clamp01(homing * Time.deltaTime * 9));
            }
        }

	    var o = randomBrightness * lifetime / 2f * 0.75f;
        GetComponent<TrailRenderer>().material.SetColor( "_TintColor", new Color( o, o, o, TrailAlpha ) );

	    // max lifetime
		lifetime -= Time.deltaTime;
		if( lifetime <= 0 )
		{
			if( networkView.isMine )
			{
				//Network.RemoveRPCs( networkView.viewID );
				Network.Destroy( gameObject );
			}
		}
	}

    public void FixedUpdate() 
	{ 
		if( dead ) return;
		
		//have we moved more than our minimum extent? 
		Vector3 movementThisStep = myRigidbody.position - previousPosition; 
		float movementSqrMagnitude = movementThisStep.sqrMagnitude;
		
		if( movementSqrMagnitude > sqrMinimumExtent ) 
		{ 
			float movementMagnitude = Mathf.Sqrt(movementSqrMagnitude);
			RaycastHit hitInfo; 
		
			//check for obstructions we might have missed 
			if( Physics.Raycast(previousPosition, movementThisStep, out hitInfo, movementMagnitude, layerMask.value) ) 
			{
				myRigidbody.position = hitInfo.point - (movementThisStep/movementMagnitude)*partialExtent;
				Collide( hitInfo.transform, hitInfo.point, hitInfo.normal );
			} 
		
			previousPosition = myRigidbody.position;
		}
	}
}
