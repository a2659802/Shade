using System;
using UnityEngine;

namespace LordOfShade
{
    public class MoveToPos : MonoBehaviour
    {
        public Vector2 pos;
        public float speed;
        private Vector3 lastPos;

        private void Awake()
        {
            lastPos = new Vector3(0f,0f,0f);
        }

        private void FixedUpdate()
        {
            float step =  speed * Time.deltaTime; // calculate distance to move
            transform.position = Vector2.MoveTowards(transform.position, pos, step);

            // Check if the position of the cube and sphere are approximately equal.
            if (Vector3.Distance(transform.position, pos) < 0.001f ||
                Vector3.Distance(transform.position, lastPos) < step - 0.01)
            {
                Destroy(this);
            }

            lastPos = transform.position;
        }
    }
}