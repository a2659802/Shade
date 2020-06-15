using System;
using UnityEngine;
using HutongGames.PlayMaker;
using On.HutongGames.PlayMaker.Actions;

namespace LordOfShade
{
    public class Deceleration : MonoBehaviour
    {
        public float deceleration;
        private Rigidbody2D _rb2d;
        private void Awake()
        {
            _rb2d = gameObject.GetComponent<Rigidbody2D>();
        }

        public void OnEnter()
        {
            this.DecelerateSelf();
        }
        
        public void OnFixedUpdate()
        {
            this.DecelerateSelf();
        }
        private void DecelerateSelf()
        {
            if (_rb2d == null)
            {
                return;
            }
            Vector2 velocity = _rb2d.velocity;
            if (velocity.x < 0f)
            {
                velocity.x *= deceleration;
                if (velocity.x > 0f)
                {
                    velocity.x = 0f;
                }
            }
            else if (velocity.x > 0f)
            {
                velocity.x *= deceleration;
                if (velocity.x < 0f)
                {
                    velocity.x = 0f;
                }
            }
            if (velocity.y < 0f)
            {
                velocity.y *= deceleration;
                if (velocity.y > 0f)
                {
                    velocity.y = 0f;
                }
            }
            else if (velocity.y > 0f)
            {
                velocity.y *= deceleration;
                if (velocity.y < 0f)
                {
                    velocity.y = 0f;
                }
            }
            _rb2d.velocity = velocity;
        }
    }
}