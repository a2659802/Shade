using UnityEngine;
using HutongGames.PlayMaker;

namespace LordOfShade
{
    public class FlyMoveTo : MonoBehaviour
    {
	    public GameObject target;
	    public float distance;
	    public float speedMax;
	    public float acceleration;
	    public bool targetsHeight;
	    public float height;
	    private float distanceAway;
	    private Rigidbody2D _rb;
	    
	    public  void Awake()
	    {
		    _rb = GetComponent<Rigidbody2D>();
	    }

		public void FixedUpdate()
		{
			this.DoBuzz();
		}

		private void DoBuzz()
		{
			this.distanceAway = Mathf.Sqrt(Mathf.Pow(transform.position.x - target.transform.position.x, 2f)
			                               + Mathf.Pow(transform.position.y - target.transform.position.y, 2f));
			Vector2 velocity = _rb.velocity;
			if (distanceAway > distance)
			{
				if (transform.position.x < target.transform.position.x)
				{
					velocity.x += acceleration;
				}
				else
				{
					velocity.x -= acceleration;
				}
				if (!targetsHeight)
				{
					if (transform.position.y < target.transform.position.y)
					{
						velocity.y += acceleration;
					}
					else
					{
						velocity.y -= acceleration;
					}
				}
			}
			else
			{
				if (transform.position.x < target.transform.position.x)
				{
					velocity.x -= acceleration;
				}
				else
				{
					velocity.x += acceleration;
				}
				if (!this.targetsHeight)
				{
					if (transform.position.y < target.transform.position.y)
					{
						velocity.y -= acceleration;
					}
					else
					{
						velocity.y += acceleration;
					}
				}
			}
			if (targetsHeight)
			{
				if (transform.position.y < target.transform.position.y + height)
				{
					velocity.y += acceleration;
				}
				if (transform.position.y > target.transform.position.y + height)
				{
					velocity.y -= acceleration;
				}
			}
			if (velocity.x > this.speedMax)
			{
				velocity.x = this.speedMax;
			}
			if (velocity.x < -this.speedMax)
			{
				velocity.x = -this.speedMax;
			}
			if (velocity.y > this.speedMax)
			{
				velocity.y = this.speedMax;
			}
			if (velocity.y < -this.speedMax)
			{
				velocity.y = -this.speedMax;
			}
			_rb.velocity = velocity;
		}
    }
}