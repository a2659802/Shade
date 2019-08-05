using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using ModCommon.Util;
using ModCommon;
using Modding;
using UnityEngine;
using UnityEngine.UI;
using Logger = Modding.Logger;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using System.IO;
using System;
using On;
using GlobalEnums;

namespace LordOfShade
{
    internal class Shade : MonoBehaviour
    {
        private readonly Dictionary<string, float> _fpsDict = new Dictionary<string, float> //Use to change animation speed
        {
        };

        private HealthManager _hm;
        private tk2dSpriteAnimator _anim;
        private PlayMakerFSM _control;
        private GameObject _target;
        private GameObject _slash;
        private Recoil _recoil;
        private BoxCollider2D _bc;
        private Rigidbody2D _rg;
        private int _life;
        private int _oldHP;
        private bool _attacked;


        private void Awake()
        {
            Log("Added Shade Lord Mono");
            _control = gameObject.LocateMyFSM("Shade Control");
            _target = HeroController.instance.gameObject;
            _anim = gameObject.GetComponent<tk2dSpriteAnimator>();
            _hm = gameObject.GetComponent<HealthManager>();
            _bc = gameObject.GetComponent<BoxCollider2D>();
            _rg = gameObject.GetComponent<Rigidbody2D>();
            _recoil = gameObject.GetComponent<Recoil>();
            _slash = gameObject.transform.Find("Slash").gameObject;
        }

        private void Start()
        {
            ModHooks.Instance.AttackHook += OnAttack;
            foreach (KeyValuePair<string, float> i in _fpsDict) _anim.GetClipByName(i.Key).fps = i.Value;
            _control.enabled = false;
            _hm.hp = _oldHP = 50;
            _life = 50;
            StartCoroutine(ShadeMovement());
            StartCoroutine(FaceHero());
        }

        IEnumerator InvulnTimed(float delay)
        {
            gameObject.GetComponent<BoxCollider2D>().enabled = false;
            yield return new WaitForSeconds(delay);
            gameObject.GetComponent<BoxCollider2D>().enabled = true;
        }

        private void OnAttack(AttackDirection dir)
        {
            if (dir == GlobalEnums.AttackDirection.normal)
            {
                _attacked = true;
            }
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            if (_oldHP > _hm.hp)
            {
                _hm.hp = _oldHP;
                _life--;
            }
        }

        IEnumerator ShadeMovementY()
        {
            while (_life > 0)
            {
                Vector2 pos = new Vector2(gameObject.transform.GetPositionX(), _target.transform.GetPositionY());
                gameObject.transform.position = Vector2.MoveTowards(gameObject.transform.position, pos, 10f * Time.fixedDeltaTime);
                yield return null;
            }
        }

        IEnumerator ShadeMovementX()
        {
            while (_life > 0)
            {
                float side = Mathf.Sign(_target.transform.GetPositionX() - gameObject.transform.GetPositionX());
                float dist = Mathf.Abs(_target.transform.GetPositionX() - gameObject.transform.GetPositionX());
                Vector2 newVel = new Vector2();
                if (dist < 4.8f) newVel = new Vector2(20f, 0f);
                else if (dist > 8.2f) newVel = new Vector2(-20f, 0f);
                _rg.velocity = Vector2.Lerp(_rg.velocity, newVel, 0.01f);
                yield return null;
            }
        }

        IEnumerator ShadeMovement()
        {
            float tim = 0;
            _anim.Play("Fly");
            while (tim < 3f)
            {
                float side = Mathf.Sign(gameObject.transform.GetPositionX() - _target.transform.GetPositionX());
                if (!_attacked)
                {
                    Vector2 pos = new Vector2(_target.transform.GetPositionX() + 5f * side, _target.transform.GetPositionY());
                    gameObject.transform.position = Vector2.MoveTowards(gameObject.transform.position, pos, 10f * Time.fixedDeltaTime);
                }
                else
                {
                    _bc.enabled = false;
                    _rg.velocity = new Vector2(-1f * side * 25f, 0f);
                    if (side > 0) yield return new WaitWhile(() => _target.transform.GetPositionX() - gameObject.transform.GetPositionX() < 5f * side);
                    else yield return new WaitWhile(() => _target.transform.GetPositionX() - gameObject.transform.GetPositionX() > 5f * side);
                    _bc.enabled = true;
                    _rg.velocity = new Vector2(0f, 0f);
                    _attacked = false;
                }
                tim += Time.fixedDeltaTime;
                yield return null;
            }
            //StartCoroutine(ShadeMovementX());
            //StartCoroutine(ShadeMovementY());
            StartCoroutine(ParryMe());
        }

        IEnumerator ParryMe()
        {
            var fsm = _slash.LocateMyFSM("nail_clash_tink");
            fsm.RemoveAction("Blocked Hit", 4);
            fsm.RemoveAction("Blocked Hit", 0);
            fsm.ChangeTransition("No Box Left", "FINISHED", "NailParryRecover");
            fsm.ChangeTransition("No Box Right", "FINISHED", "NailParryRecover");
            fsm.ChangeTransition("No Box Down", "FINISHED", "NailParryRecover");
            fsm.ChangeTransition("No Box Up", "FINISHED", "NailParryRecover");
            fsm.ChangeTransition("NailParryRecover", "FINISHED", "Detecting");
            while (true)
            {
                if (_attacked)
                {
                    _anim.Play("Slash Antic");
                    _slash.SetActive(true);
                    tk2dSpriteAnimator tk = _slash.GetComponent<tk2dSpriteAnimator>();
                    _slash.GetComponent<PolygonCollider2D>().enabled = true;
                    tk.Play("Slash Effect");
                    yield return null;
                    _anim.Play("Slash");
                    yield return new WaitForSeconds(0.083f);
                    yield return new WaitWhile(() => _anim.IsPlaying("Slash"));
                    _slash.GetComponent<PolygonCollider2D>().enabled = false;
                    _slash.SetActive(false);
                    _anim.Play("Slash CD");
                    yield return new WaitWhile(() => _anim.IsPlaying("Slash CD"));
                    _anim.Play("Fly");
                    _attacked = false;
                }
                yield return null;
            }
        }

        IEnumerator FaceHero()
        {
            while (_life>0)
            {
                float side = Mathf.Sign(gameObject.transform.GetPositionX() - _target.transform.GetPositionX());
                gameObject.transform.localScale.SetX(Mathf.Abs(gameObject.transform.localScale.x) * side);
                yield return null;
            }
        }

        bool FastApproximately(float a, float b, float threshold)
        {
            return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
        }
        /*
        float accelerationForce = 8f;
		float speedMax = 4f;
		float offsetX = 0f;
		float offsetY = 0f;
        GameObject objectA;
        GameObject objectB;
        string newAnimationClip = "TurnToFly";
		bool spriteFacesRight = false;
		bool everyFrame = true;
        bool resetFrame = true;
        bool playNewAnimation = true;
        tk2dSpriteAnimator _sprite;
        float xScale; 
        private void Start()
        {
            objectA = gameObject;
            objectB = HeroController.instance.gameObject;

            this._sprite = this.objectA.GetComponent<tk2dSpriteAnimator>();
            xScale = this.objectA.transform.localScale.x;
            if (this.xScale < 0f)
            {
                this.xScale *= -1f;
            }

            Log("Changing fps of animation");
            foreach (KeyValuePair<string, float> i in _fpsDict) _anim.GetClipByName(i.Key).fps = i.Value;

            try
            {

                Log("Setting health");
                _control.Fsm.GetFsmInt("HP").Value = 1800;

                Log("Set damage");
                gameObject.GetComponent<DamageHero>().damageDealt = 1;

                Log("Remove Friendly");
                _control.RemoveAction("Startle", 0);
                _control.RemoveAction("Idle", 0);

                Log("Stop it from waiting before attacking");
                _control.GetAction<WaitRandom>("Fly", 5).timeMin = 0f;
                _control.GetAction<WaitRandom>("Fly", 5).timeMin = 0f;

                Log("Adding choice between attacks");
                _control.RemoveTransition("Attack Choice", "SLASH");
                _control.RemoveTransition("Attack Choice", "FIREBALL");
                _control.RemoveAction("Attack Choice", 0);
                _control.InsertMethod("Attack Choice", 0, ChooseAttack);

                Log("Speeding up fireball time");
                _control.GetAction<Wait>("Cast Antic", 7).time = 0.15f;
                _control.GetAction<Wait>("Charge Wait", 0).time = 0f;
                _control.GetAction<Wait>("Cooldown", 5).time = 0f;
            }
            catch (Exception e)
            {
                Log(e);
            }
        }

        public void ChooseAttack()
        {
            var xH = HeroController.instance.transform.GetPositionX();
            var yH = HeroController.instance.transform.GetPositionY();
            var xS = gameObject.transform.GetPositionX();
            var yS = gameObject.transform.GetPositionY();

            //If y values are close
            Log("In here");
            if (Math.Abs(yS-yH) < 1f)
            {
                Log("Check if can fireball");
                if (Math.Abs(xH-xS) > 5f)
                {
                    Log("Use Fireball");
                    _control.SetState("Cast Antic");
                }
                
            }
            else
            {
                Log("Use Slash");
                _control.SetState("Slash Antic");
            }
        }

        private void Update()
        {
            try
            {
                Vector3 localScale = this.objectA.transform.localScale;
                if (this.objectA.transform.position.x < this.objectB.transform.position.x)
                {
                    if (this.spriteFacesRight)
                    {
                        if (localScale.x != xScale)
                        {
                            localScale.x = xScale;
                            if (this.resetFrame)
                            {
                                this._sprite.PlayFromFrame(0);
                            }
                            if (this.playNewAnimation)
                            {
                                this._sprite.Play(this.newAnimationClip);
                            }
                        }
                    }
                    else if (localScale.x != -xScale)
                    {
                        localScale.x = -this.xScale;
                        if (this.resetFrame)
                        {
                            this._sprite.PlayFromFrame(0);
                        }
                        if (this.playNewAnimation)
                        {
                            this._sprite.Play(this.newAnimationClip);
                        }
                    }
                }
                else if (this.spriteFacesRight)
                {
                    if (localScale.x != -xScale)
                    {
                        localScale.x = -xScale;
                        if (this.resetFrame)
                        {
                            this._sprite.PlayFromFrame(0);
                        }
                        if (this.playNewAnimation)
                        {
                            this._sprite.Play(this.newAnimationClip);
                        }
                    }
                }
                else if (localScale.x != this.xScale)
                {
                    localScale.x = this.xScale;
                    if (this.resetFrame)
                    {
                        this._sprite.PlayFromFrame(0);
                    }
                    if (this.playNewAnimation)
                    {
                        this._sprite.Play(this.newAnimationClip);
                    }
                }
                this.objectA.transform.localScale = new Vector3(localScale.x, this.objectA.transform.localScale.y, this.objectA.transform.localScale.z);

                Vector2 vector = new Vector2(target.transform.position.x + offsetX - gameObject.transform.position.x, target.transform.position.y + offsetY - gameObject.transform.position.y);
                vector = Vector2.ClampMagnitude(vector, 1f);
                vector = new Vector2(vector.x * accelerationForce, vector.y * accelerationForce);
                gameObject.GetComponent<Rigidbody2D>().AddForce(vector);
                Vector2 vector2 = gameObject.GetComponent<Rigidbody2D>().velocity;
                vector2 = Vector2.ClampMagnitude(vector2, speedMax);
                gameObject.GetComponent<Rigidbody2D>().velocity = vector2;
            }
            catch (System.Exception e)
            {
                Log(e);
            }
        }*/

        private void OnDestroy()
        {
            //tk2dSpriteDefinition def = gameObject.GetComponent<tk2dSprite>().GetCurrentSpriteDef();

            //def.material.mainTexture = _oldTex;

           // _lurkerChanged = false;
        }

        private static void Log(object obj)
        {
            Logger.Log("[Lord of Shade] " + obj);
        }
    }
}

