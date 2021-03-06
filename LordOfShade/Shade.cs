﻿using System.Collections;
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
using Random = System.Random;

namespace LordOfShade
{
    internal class Shade : MonoBehaviour
    {
        private readonly Dictionary<string, float> _fpsDict =
            new Dictionary<string, float> //Use to change animation speed
            {
                {"Retreat Start", 20},
                {"Quake Start",25},
                {"Quake Land",25},
                {"Retreat End",25}
            };

        private List<Action> _actions;
        private Dictionary<Action, int> _reps;

        private Dictionary<Action, int> _maxReps;
        private HealthManager _hm;
        private tk2dSpriteAnimator _anim;
        private PlayMakerFSM _control;
        private GameObject _target;
        private GameObject _slash;
        private GameObject _ring;
        private GameObject _screamBlst;
        private GameObject _screamHit;
        private GameObject _quakeBlst;
        private GameObject _quakeHit;
        private GameObject _quakePill;
        private Recoil _recoil;
        private ParticleSystem _shadePartic;
        private ParticleSystem _reformPartic;
        private ParticleSystem _retPartic;
        private ParticleSystem _chargePartic;
        private ParticleSystem _quakePartic;
        private ParticleSystem _wavePartic;
        private BoxCollider2D _bc;
        private Rigidbody2D _rb;
        private MeshRenderer _mr;
        //private int _life;
        private int mana = 33*6;
        private Action _currAtt;
        private float side;
        //private bool _hitShade;
        private bool __hit_shade;
        private bool hitShade
        {
            set => __hit_shade = value;
            get
            {
                if(__hit_shade)
                {
                    __hit_shade = false;
                    return true;
                }
                return __hit_shade;
            }
        }
        private bool _hitFloor;
        private bool _attacking;
        private float Extra_Time = 0;
        private const float Sword_Time = 0.5f;
        private const float Spell_Time = 1.2f;
        public int maxHp = 10;
        //private bool _tping;
        private void Awake()
        {
            Log("Added Shade Lord Mono");
            _control = gameObject.LocateMyFSM("Shade Control");
            _target = HeroController.instance.gameObject;
            _anim = gameObject.GetComponent<tk2dSpriteAnimator>();
            _hm = gameObject.GetComponent<HealthManager>();
            _bc = gameObject.GetComponent<BoxCollider2D>();
            _rb = gameObject.GetComponent<Rigidbody2D>();
            _recoil = gameObject.GetComponent<Recoil>();
            _mr = gameObject.GetComponent<MeshRenderer>();
            _slash = gameObject.transform.Find("Slash").gameObject;

            FixThings();

            
        }

        private IEnumerator Start()
        {
            //boss level setup
            if(BossSceneController.Instance!=null)
            {
                if(BossSceneController.Instance.BossLevel < 1) // 调谐
                {
                    maxHp = 800;
                    mana = 0;
                    var _dream_ctrl = gameObject.LocateMyFSM("Dreamnail Kill");
                    _dream_ctrl.InsertMethod("Die", 0, () => {
                        HeroController.instance?.AddMPCharge(99);
                    }
                    );
                }
                else if(BossSceneController.Instance.BossLevel < 2)
                {
                    maxHp = 1000;
                    mana = 33 * 6;
                    var _dream_ctrl = gameObject.LocateMyFSM("Dreamnail Kill");
                    _dream_ctrl.InsertMethod("Die", 0, () => {
                        HeroController.instance?.AddMPCharge(33);
                    }
                    );
                }
                else
                {
                    maxHp = 1200;
                    mana = 33;
                    var _dream_ctrl = gameObject.LocateMyFSM("Dreamnail Kill");
                    _dream_ctrl.InsertMethod("Die", 0, () => {
                        HeroController.instance?.AddMPCharge(33);
                    }
                    );
                }
                
            }


            On.HealthManager.Hit += OnHit;
            On.HealthManager.Die += HealthManager_Die;
            ModHooks.Instance.TakeHealthHook += OnHitPlayer;
            //transform.position = transform.position - new Vector3(2f, 4f, 0f);
            foreach (KeyValuePair<string, float> i in _fpsDict) _anim.GetClipByName(i.Key).fps = i.Value;
            _control.GetAction<IntCompare>("Friendly?", 1).integer2.Value = 10;
            Log("Remove Friendly");
            //_control.Fsm.GetFsmFloat("Max Roam").Value = 200f;
            //Log($"FIx Roam:{_control.Fsm.GetFsmFloat("Max Roam").Value}");
            //_control.RemoveAction("Startle", 0);
            //_control.RemoveAction("Idle", 0);

            //_control.enabled = true;
            yield return null;
            _hm.hp = maxHp;
            //_life = 10;


            _actions = new List<Action>()
            {
                Fireball,
                GroundPound,
                Shriek,
                Slash,
                Heal,
                Copy,
                SummonGate,
                
            };
            _maxReps = new Dictionary<Action, int>()
            {
                {Fireball, 2},
                {GroundPound, 1},
                {Shriek, 1},
                {Slash, 3},
                {Heal, 1},
                {Copy,1 },
                {SummonGate,2 }
            };
            _reps = new Dictionary<Action, int>()
            {
                {Fireball, 0},
                {GroundPound, 0},
                {Shriek, 0},
                {Slash, 0},
                {Heal, 0},
                {Copy, 0 },
                {SummonGate,0 },
            };
            yield return new WaitWhile(() => _control.ActiveStateName != "Idle");
            _control.SetState("Startle");
            yield return new WaitWhile(() => _control.ActiveStateName != "Fly");
            _anim.Play("Fly");
            _control.enabled = false;
            //yield return new WaitForSeconds(0.3f);
            yield return null;
            
            _attacking = false;
            StartCoroutine(AttackChooser());
            
            //Teleport(_target)
            /*while (true)
            {
                yield return new WaitWhile(() => !Input.GetKey(KeyCode.R));
                GameObject ball = Instantiate(LordOfShade.preloadedGO["ball"]);
                ball.SetActive(true);
                Vector3 tmp = ball.transform.localScale;
                float sig = Mathf.Sign(_target.transform.localScale.x);
                ball.transform.position = _target.transform.position + new Vector3(0f, 0f, 0f);
                ball.transform.localScale = new Vector3(sig * -2f * tmp.x, tmp.y * 2f, tmp.z);
                Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
                rb.velocity = new Vector2(sig * 30f, 0f);
                yield return null;
            }*/
        }

        private void HealthManager_Die(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            if (self == _hm)
            {
                GameManager.instance.StartCoroutine(EndBattle());
                return;
            }

            orig(self, attackDirection, attackType, ignoreEvasion);
        }




        private IEnumerator EndBattle()
        {
            _control.SetState("Killed");
            yield return new WaitForSeconds(2.5f);
            BossSceneController.Instance.EndBossScene();
            yield break;
        }

        private void Update()
        {
            side = FaceHero();
        }

        private void FixThings()
        {
            Vector3 tmp = _slash.transform.localScale;
            Vector3 tmp2 = _slash.transform.localPosition;

            side = -1;
            _slash.transform.localScale = new Vector3(tmp.x * 1.7f, tmp.y * 1.35f, tmp.z);
            _slash.transform.localPosition = new Vector3(tmp2.x - 0.5f, tmp2.y, tmp2.z);

            _ring = transform.Find("Shade Cast Ring").gameObject;
            _screamBlst = transform.Find("Scream Blast").gameObject;
            _screamHit = transform.Find("Scream Hit").gameObject;
            Vector3 tmp3 = _screamBlst.transform.localScale;
            Vector3 tmp4 = _screamHit.transform.localScale;
            _screamBlst.transform.localScale = new Vector3(tmp3.x * 2f, tmp3.y * 2.5f, tmp3.z);
            _screamHit.transform.localScale = new Vector3(tmp4.x * 2f, tmp4.y * 2.5f, tmp4.z);
            _shadePartic = transform.Find("Shade Particles").gameObject.GetComponent<ParticleSystem>();
            _retPartic = transform.Find("Retreat Particles").gameObject.GetComponent<ParticleSystem>();
            _chargePartic = transform.Find("Charge Particles").gameObject.GetComponent<ParticleSystem>();
            _quakePartic = transform.Find("Quake Particles").gameObject.GetComponent<ParticleSystem>();
            _reformPartic = transform.Find("Reform Particles").gameObject.GetComponent<ParticleSystem>();
            _wavePartic = transform.Find("Particle Wave").gameObject.GetComponent<ParticleSystem>();
            _quakeBlst = transform.Find("Quake Blast").gameObject;
            _quakeHit = transform.Find("Quake Hit").gameObject;
            _quakePill = transform.Find("Quake Pillar").gameObject;
            tmp = _quakeBlst.transform.localScale;
            tmp2 = _quakeHit.transform.localScale;
            tmp3 = _quakePill.transform.localScale;
            _quakeBlst.transform.localScale = new Vector3(tmp.x * 2.5f, tmp.y * 2.5f, tmp.z);
            _quakeHit.transform.localScale = new Vector3(tmp2.x * 2.5f, tmp2.y * 2.5f, tmp2.z);
            _quakePill.transform.localScale = new Vector3(tmp3.x * 2.5f, tmp3.y * 2f, tmp3.z);
            _quakeBlst.transform.localPosition += new Vector3(0f, 3f, 0f);
            _quakeHit.transform.localPosition += new Vector3(0f, 3f, 0f);
            _quakePill.transform.localPosition += new Vector3(0f, 2.5f, 0f);
        }

        private Random _rand = new Random();

        public class IdleSeconds : CustomYieldInstruction
        {
            private float waitTime;
            private Shade _s;
            public IdleSeconds(float time,Shade s)
            {
                _s = s;
                this.waitTime = Time.realtimeSinceStartup + time;
            }
            public override bool keepWaiting
            {
                get
                {
                    return ((Time.realtimeSinceStartup < this.waitTime) && !_s.hitShade);
                }
            }
        }

        private IEnumerator AttackChooser()
        {
            if (_attacking)
                yield break;
            //_attacking = false;
            //yield return new WaitForSeconds(_currAtt == Slash ? Sword_Time : Spell_Time);
            yield return new IdleSeconds((_currAtt == Slash ? Sword_Time : Spell_Time)+Extra_Time, this);
            Extra_Time = 0;
            if (_attacking)
                yield break;
            _attacking = true;
            yield return new WaitForSeconds(0.2f);
            //yield return new WaitWhile(() => _tping);
            //yield return new WaitWhile(() => _attacking);
            //_attacking = true;
            _currAtt = Slash;
            Vector2 hPos = _target.transform.position;
            Vector2 shadePos = transform.position;
            if (mana > 33 &&
                (_rand.Next(5) < 3 || _reps[Slash] > _maxReps[Slash]))
            {
                if (_hm.hp<maxHp/2 && _reps[Heal] < _maxReps[Heal] && _rand.Next(5) < 4)
                {
                    _currAtt = Heal;
                }
                else if(_rand.Next(10)>7 && _hm.hp>maxHp/3 &&_reps[Copy] < _maxReps[Copy])
                {
                    _currAtt = Copy;
                }
                // If player is near
                else if (FastApproximately(hPos.x, shadePos.x, 5.5f))
                {
                    if (hPos.y - shadePos.y > 2f) _currAtt = Shriek;
                    if (hPos.y - shadePos.y < -1f && _rand.Next(3) > 0 ) _currAtt = GroundPound;
                }
                else
                {
                    int tmp = _rand.Next(6);
                    if (tmp < 3) _currAtt = Fireball;
                    else if (tmp == 4) _currAtt = SummonTendrils;
                    else _currAtt = GroundPound;
                }
            }
            else if(mana > 11 && _rand.Next(10)>5 && _maxReps[SummonGate]>_reps[SummonGate])
            {
                _currAtt = SummonGate;
            }
            else if(_rand.Next(10)>6 && !FastApproximately(hPos.x,shadePos.x,3f))
            {
                _currAtt = Dash;
            }
            
            foreach (var i in _actions)
            {
                _reps[i] = (i == _currAtt) ? _reps[i] + 1 : 0;
            }

            Log("Doing attack: " + _currAtt.Method.Name);
            
            _currAtt();
        }
        private void Dash()
        {
            IEnumerator DoDash()
            {
                //yield return null;
                //_wavePartic.Play();
                yield return new TeleportV2(new Vector2(transform.position.x, _target.transform.position.y), 30, this);
                ParticleSystem.EmissionModule em = _shadePartic.emission;
                em.rate = 100;
                do
                {
                    _wavePartic.Play();
                    yield return new WaitForSeconds(0.8f);
                    
                    Vector2 v = _target.transform.position - transform.position;
                    v = Vector2.ClampMagnitude(v * 1000, 40);
                    _rb.velocity = v;
                    yield return new WaitForSeconds(0.7f);
                    _rb.velocity = Vector2.zero;
                } while (_rand.Next(10) > 7);
                em.rate = 0;
                _attacking = false;
                StartCoroutine(AttackChooser());
            }
            StartCoroutine(DoDash());
        }
        private void SummonTendrils()
        {
            IEnumerator DoSummon()
            {
                yield return null;
                bool right = _rand.Next(2) > 0;
                float floor = 6.4f;
                float summonY = floor + 10.2f - 9f;
                float summonX = right ? 55 : 35;
                summonX += UnityEngine.Random.Range(-3, 3);

                yield return new TeleportV2(new Vector2(summonX + (right ? -1 : 1)*4, summonY), 20, this);

                var em2 = _chargePartic.emission;
                _anim.Play("Cast Charge");
                em2.rate = 150;
                yield return new WaitForSeconds(1f);

                do
                {
                    var tendrils = Instantiate(LordOfShade.preloadedGO["tendrils"]);
                    tendrils.transform.localScale = new Vector3(1.5f, 2.5f, 1);
                    tendrils.transform.position = new Vector3(summonX, summonY);
                    tendrils.transform.Find("Extra Box").gameObject.AddComponent<DamageOnce>();
                    tendrils.AddComponent<DamageOnce>();
                    tendrils.SetActive(true);
                    var fsm = tendrils.LocateMyFSM("Control");
                    fsm.InsertMethod("Still Close?", 0, () => fsm.SetState("Idle"));
                    fsm.SetState("Emerge");
                    Destroy(tendrils, 3);
                    yield return new WaitForSeconds(0.5f);
                    summonX += (right ? -1 : 1) * 8;
                } while (summonX >= 25 && summonX <= 65  &&_rand.Next(6) > 0);
                //Extra_Time = 1.5f;
                yield return new TeleportV2(new Vector2(40, 10), 10, this);
                em2.rate = 0;
                _anim.Play("Fly");

                _attacking = false;
                StartCoroutine(AttackChooser());
            }
            StartCoroutine(DoSummon());
        }
        private class DamageOnce : MonoBehaviour
        {

            private void OnTriggerEnter2D(Collider2D collision)
            {
                IEnumerator DisableSelf()
                {
                    yield return new WaitForEndOfFrame();
                    Destroy(gameObject);
                }
                if (collision.gameObject.layer == (int)GlobalEnums.PhysLayers.HERO_BOX)
                {
                    StartCoroutine(DisableSelf());
                }
            }

        }
        private void SummonGate()
        {
            IEnumerator DoSummon()
            {
                float summonY = 7f;
                float moveSpeed = 7f;
                var em2 = _chargePartic.emission;
                var gate = LordOfShade.preloadedGO["gate"];

                em2.rate = 50;
                //yield return new TeleportV2(new Vector2(31, 10), 20, this);
                _anim.Play("Cast Charge");
                yield return new WaitForSeconds(0.2f);
                gate = Instantiate(gate);
                gate.SetActive(true);
                gate.transform.position = new Vector3(30f, summonY);
                gate.AddComponent<Rigidbody2D>().velocity = new Vector2(moveSpeed, 0);
                gate.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                Destroy(gate, 5);
                //yield return new TeleportV2(new Vector2(UnityEngine.Random.Range(40, 60), 8f),30, this);
                em2.rate = 0;
                _attacking = false;
                Extra_Time = -1f;
                _anim.Play("Fly");
                StartCoroutine(AttackChooser());
            }
            IEnumerator DoSummonV()
            {
                float summonY = 18f;
                float moveSpeed = 5.5f;
                var em2 = _chargePartic.emission;
                var gate = LordOfShade.preloadedGO["gate"];

                em2.rate = 50;
                //yield return new TeleportV2(new Vector2(31, 10), 20, this);
                _anim.Play("Cast Charge");
                yield return new WaitForSeconds(0.2f);
                gate = Instantiate(gate);
                gate.transform.localScale += new Vector3(0, 2, 0);
                gate.transform.eulerAngles = new Vector3(0, 0, 90);
                gate.SetActive(true);
                gate.transform.position = new Vector3(60f, summonY);
                gate.AddComponent<Rigidbody2D>().velocity = new Vector2(0, -1*moveSpeed);
                gate.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                Destroy(gate, 1.5f);
                //yield return new TeleportV2(new Vector2(UnityEngine.Random.Range(40, 60), 8f),30, this);
                em2.rate = 0;
                _attacking = false;
                Extra_Time = -1f;
                _anim.Play("Fly");
                StartCoroutine(AttackChooser());
            }
            mana -= 11;
            if (_rand.Next(2) > 0)
            {
                StartCoroutine(DoSummonV());
            }
            else
            {
                StartCoroutine(DoSummon());
            }
            
        }

        private void GroundPound()
        {
            IEnumerator DoGroundPound()
            {
                int _layer = gameObject.layer;
                ParticleSystem.EmissionModule em = _shadePartic.emission;
                ParticleSystem.EmissionModule em2 = _chargePartic.emission;
                //ParticleSystem.EmissionModule em2 = _chargePartic.emission;
                //em2.rate = 50;
                //_chargePartic.transform.localScale *= 2f;
                _anim.Play("Cast Antic");
                em2.rate = 50;
                yield return new WaitForSeconds(0.2f);
                em2.rate = 0;
                //em2.rate = 0;
                gameObject.layer = (int)PhysLayers.ENEMY_ATTACK;
                //Log($"Quake1");
                Vector2 pos = _target.transform.position;
                pos.y = transform.position.y < 14 ? 14 : transform.position.y;
                //Log($"Quake1-2");
                _bc.enabled = false;
                if (!FastApproximately(_target.transform.position.x, transform.position.x, 5f))
                {
                    
                    //Log($"Quake1-3");
                    //Teleport(pos, 50f);
                    yield return new TeleportV2(pos, 40, this);
                    //yield return new WaitWhile(()=>_tping);
                    _anim.Play("Quake Antic");
                    yield return new WaitForSeconds(0.18f);
                }
                else
                {
                    _anim.Play("Quake Antic");
                    _rb.velocity = new Vector2(0, 15f);
                    em2.rate = 100;
                    yield return new WaitForSeconds(0.2f);
                    em2.rate = 0;
                }
                //Log($"Quake1-4");
                //yield return new WaitWhile(() => _attacking);
                //Log($"Quake2");
                //_attacking = true;
                
                
                
                //_rb.velocity = new Vector2(0f, 30f);
                
                em.rate = 0;
                
                em2.rate = 50;
                //gameObject.AddComponent<Deceleration>().deceleration = 0.85f;
               // yield return new WaitForSeconds(0.1f);
                //Destroy(dec);
                em.rate = 20;
                em2.rate = 0;
                _anim.Play("Quake Start");
                GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
                yield return new WaitWhile(() => _anim.IsPlaying("Quake Start"));
                _anim.Play("Quake");
                _rb.velocity = new Vector2(0f, -60f);
                _bc.enabled = true;
                _hitFloor = false;
                yield return new WaitWhile(() => !_hitFloor && _rb.velocity.y<-50f);
                gameObject.layer = _layer;
                if(!_hitFloor)
                {
                    _rb.velocity = new Vector2(0f, 0f);
                    _mr.enabled = false;
                    pos = _target.transform.position + new Vector3(side * UnityEngine.Random.Range(6f, 10f), UnityEngine.Random.Range(0f, 6f), 0f);
                    yield return new WaitForSeconds(0.2f);
                    yield return new TeleportV2(pos, 30, this);
                    em.rate = 20;
                    _mr.enabled = true;
                    _anim.Play("Retreat End");
                    yield return new WaitWhile(() => _anim.IsPlaying("Retreat End"));
                    _bc.enabled = true;
                    _anim.Play("Fly");
                    _attacking = false;
                    StartCoroutine(AttackChooser());
                    yield break;
                }
                _hitFloor = false;
                _rb.velocity = new Vector2(0f, 0f);
                GameCameras.instance.cameraShakeFSM.SendEvent("BigShake");
                _anim.Play("Quake Land");
                
                em.rate = 0;
                _reformPartic.Play();
                _quakePartic.Play();
                _quakeBlst.SetActive(true);
                _quakeHit.SetActive(true);
                _quakePill.SetActive(true);
                yield return new WaitWhile(() => _anim.IsPlaying("Quake Land"));
                
                if (mana>=33) //extra spell
                {
                    
                    if (_rand.Next(10) > 6 && FastApproximately(_target.transform.position.x, transform.position.x, 6))
                    {
                        em2.rate = 50;
                        _anim.Play("Scream Antic");
                        yield return new WaitForSeconds(0.8f);

                        _anim.Play("Scream");
                        _screamBlst.SetActive(true);
                        _screamHit.SetActive(true);
                        em2.rate = 0;
                        GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                        yield return new WaitForSeconds(0.5f);
                        _anim.Play("Scream End");
                        mana -= 11;
                    }
                    else if(_rand.Next(10)>6 && Vector2.Distance(_target.transform.position, transform.position) >= 5)
                    {
                        em2.rate = 50;
                        _anim.Play("Cast Charge");
                        yield return new WaitForSeconds(0.8f);
                        float sig = 1;

                        for(int i=0;i<2;i++)
                        {
                            GameObject ball = Instantiate(LordOfShade.preloadedGO["ball"]);
                            ball.SetActive(true);
                            Vector3 tmp = ball.transform.localScale;
                            ball.transform.position = transform.position;
                            ball.transform.localScale = new Vector3(sig * 1.5f * tmp.x, tmp.y * 1.5f, tmp.z);
                            Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
                            rb.velocity = new Vector2(sig * 30f, 0f);

                            //yield return new WaitForSeconds(0.5f);
                            sig *= -1f;
                        }
                        if(_rand.Next(2)>0)
                        {
                            for (int i = 0; i < 2; i++)
                            {
                                yield return new WaitForSeconds(0.5f);
                                GameObject ball = Instantiate(LordOfShade.preloadedGO["ball"]);
                                ball.SetActive(true);
                                Vector3 tmp = ball.transform.localScale;
                                ball.transform.position = transform.position;
                                ball.transform.localScale = new Vector3(sig * tmp.x, tmp.y, tmp.z);
                                Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
                                rb.velocity = new Vector2(sig * 30f, 0f);
                                sig *= -1f;
                            }
                            mana -= 11;
                        }

                        em2.rate = 0;
                        mana -= 11;
                    }

                }
                _mr.enabled = false;
                pos = _target.transform.position + new Vector3(side * UnityEngine.Random.Range(6f, 10f), UnityEngine.Random.Range(0f, 6f), 0f) ;
                yield return new WaitForSeconds(0.2f);
                yield return new TeleportV2(pos, 30, this);
                //Teleport(pos, 30f);
                //yield return new WaitWhile(() => _tping);
                //yield return new WaitWhile(() => _attacking);
                _attacking = true;
                _reformPartic.Stop();
                em.rate = 20;
                _mr.enabled = true;
                _anim.Play("Retreat End");
                yield return new WaitWhile(() => _anim.IsPlaying("Retreat End"));
                _bc.enabled = true;
                _anim.Play("Fly");
                //Log($"Quake3");
                _attacking = false;
                StartCoroutine(AttackChooser());
            }
            mana-=33;
            StartCoroutine(DoGroundPound());
        }

        private void Shriek()
        {
            IEnumerator DoShriek()
            {
                Deceleration dec = gameObject.AddComponent<Deceleration>();
                dec.deceleration = 0.85f;
                _anim.Play("Scream Antic");
                ParticleSystem.EmissionModule em = _shadePartic.emission;
                em.rate = 0;
                ParticleSystem.EmissionModule em2 = _chargePartic.emission;
                em2.rate = 50;
                yield return new WaitForSeconds(0.2f);
                Destroy(dec);
                _rb.velocity = Vector2.zero;
                yield return new WaitForSeconds(0.2f);
                _anim.Play("Scream");
                _screamBlst.SetActive(true);
                _screamHit.SetActive(true);
                em2.rate = 0;
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                yield return new WaitForSeconds(0.5f);
                em.rate = 20;
                _anim.Play("Scream End");
                yield return new WaitWhile((() => _anim.IsPlaying("Scream End")));
                _anim.Play("Fly");
                _attacking = false;
                Extra_Time = 0.5f;
                StartCoroutine(AttackChooser());
            }

            mana-=33;
            StartCoroutine(DoShriek());
        }

        private void Heal()
        {
            IEnumerator DoHeal()
            {
                Vector3 tmpVec = _target.transform.position;
                Vector2 pos = new Vector2(tmpVec.x > 45f ? 30f : 60f, 7f);
                yield return new TeleportV2(pos, 45, this);
                //Teleport(pos, 45f);
                //yield return new WaitWhile(() => _tping);
                //yield return new WaitWhile(() => _attacking);

                _rb.velocity = Vector2.zero;
                //_rb.velocity = new Vector2(-side * 30f, 20f);
                ParticleSystem.EmissionModule em2 = _chargePartic.emission;
                em2.rate = 50;
                _anim.Play("Cast Antic");
                float time = 0.5f;
                hitShade = false;
                while (time > 0f && !hitShade)
                {
                    _rb.velocity = new Vector2(_rb.velocity.x * 0.8f, _rb.velocity.y * 0.8f);
                    yield return new WaitForEndOfFrame();
                    //time -= Time.fixedDeltaTime;
                    time -= Time.deltaTime;
                }
                if (time>0f)
                {
                    em2.rate = 0;
                    _anim.Play("Fly");
                    _attacking = false;
                    StartCoroutine(AttackChooser());
                    yield break;
                }
                _rb.velocity = Vector2.zero;
                em2.rate = 50;
                _anim.Play("Cast Charge");
                time = 1.0f;
                while (time > 0f && !hitShade)
                {
                    yield return new WaitForEndOfFrame();
                    //time -= Time.fixedDeltaTime;
                    time -= Time.deltaTime;
                }
                if (time<=0.01f)
                {
                    GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                    //_life += 3;
                    _hm.hp += (int)(maxHp * 0.3);
                    Log("Heal Success");
                }
                //_hitShade = false;
                em2.rate = 0;
                _anim.Play("Fly");
                _attacking = false;
                Extra_Time = -0.5f;
                StartCoroutine(AttackChooser());
            }

            mana-=33;
            StartCoroutine(DoHeal());
        }
        private void Copy()
        {
            IEnumerator DoCopy()
            {
                
                Vector2 pos = new Vector2(UnityEngine.Random.Range(40,50), 16f);
                yield return new TeleportV2(pos, 20, this);
                _rb.velocity = Vector2.zero;

                ParticleSystem.EmissionModule em2 = _chargePartic.emission;
                em2.rate = 50;
                _anim.Play("Cast Antic");
                float time = 0.5f;
                hitShade = false;
                while (time > 0f && !hitShade)
                {
                    _rb.velocity = new Vector2(_rb.velocity.x * 0.8f, _rb.velocity.y * 0.8f);
                    yield return new WaitForEndOfFrame();
                    time -= Time.deltaTime;
                }
                if (time > 0f)
                {
                    em2.rate = 0;
                    _anim.Play("Fly");
                    _attacking = false;
                    StartCoroutine(AttackChooser());
                    yield break;
                }
                _rb.velocity = Vector2.zero;
                em2.rate = 50;
                _anim.Play("Cast Charge");
                time = 0.7f;
                while (time > 0f && !hitShade)
                {
                    yield return new WaitForEndOfFrame();
                    time -= Time.deltaTime;
                }
                if (time <= 0.01f)
                {
                    GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
                    var _cpy = Instantiate(LordOfShade.preloadedGO["shade"]);
                    var bc = _cpy.GetComponent<BoxCollider2D>();
                    bc.enabled = false;
                    _cpy.SetActive(true);
                    _cpy.transform.localScale *= 0.8f;
                    _cpy.transform.position = transform.position + Vector3.right * 3f;
                    _cpy.AddComponent<Shade>().Invoke("ChildDestroy", 15);
                    
                    Log("Copy Success");
                    mana = mana > 0 ? -mana : mana;
                    yield return null;
                    yield return null;
                    _cpy.GetComponent<HealthManager>().hp = 9999;
                    yield return new WaitForSeconds(0.2f);
                    bc.enabled = true;
                }
                //_hitShade = false;
                em2.rate = 0;
                _anim.Play("Fly");
                _attacking = false;
                StartCoroutine(AttackChooser());
            }
            
            StartCoroutine(DoCopy());
        }
        private void Fireball()
        {
            IEnumerator DoFire()
            {
                ParticleSystem.EmissionModule em = _shadePartic.emission;
                em.rate = 900f;
                FlyMoveTo fm = gameObject.AddComponent<FlyMoveTo>();
                fm.enabled = false;
                fm.target = _target;
                fm.distance = 12;
                fm.speedMax = 12;
                fm.acceleration = 0.75f;
                fm.targetsHeight = true;
                fm.height = 0;
                fm.enabled = true;
                yield return new WaitWhile(() =>
                    !FastApproximately(_target.transform.position.x, transform.position.x, 12f));
                yield return new WaitWhile(() =>
                    !FastApproximately(_target.transform.position.y, transform.position.y, 0.4f));
                ParticleSystem.EmissionModule em2 = _chargePartic.emission;
                em2.rate = 50;
                _anim.Play("Cast Antic");
                Destroy(fm);
                float time = 0.08f;
                while (time > 0f)
                {
                    _rb.velocity = new Vector2(_rb.velocity.x * 0.8f, _rb.velocity.y * 0.8f);
                    time -= Time.fixedDeltaTime;
                    yield return null;
                }
                yield return new WaitForSeconds(0.05f);
                _rb.velocity = Vector2.zero;
                em.rate = 20;
                em2.rate = 0;
                _anim.Play("Cast Charge");
                GameObject effect = Instantiate(_ring);
                effect.SetActive(true);
                Vector3 tmp = effect.transform.localScale;
                effect.transform.position = transform.position + new Vector3(side * 1.664f, 0.12f, -0.112f);
                effect.transform.localScale = new Vector3(tmp.x * 2 * side, tmp.y * 2, tmp.z);
                yield return new WaitForSeconds(0.10f);
                GameObject ball = Instantiate(LordOfShade.preloadedGO["ball"]);
                ball.SetActive(true);
                ball.GetComponent<DamageHero>().damageDealt = 2;
                tmp = ball.transform.localScale;
                ball.transform.position = transform.position;
                ball.transform.localScale = new Vector3(side * 2f, tmp.y * 2f, tmp.z);
                Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
                rb.velocity = new Vector2(side * 30f, 0f);
                GameCameras.instance.cameraShakeFSM.SendEvent("AverageShake");
                _anim.Play("Cast");
                _rb.velocity = new Vector2(side * -10, 0);
                yield return null;
                while (_anim.IsPlaying("Cast"))
                {
                    _rb.velocity = new Vector2(_rb.velocity.x * 0.9f, _rb.velocity.y * 0.9f);
                    yield return null;
                }
                _anim.Play("Fly");
                time = 0.1f;
                while (time > 0f)
                {
                    _rb.velocity = new Vector2(_rb.velocity.x * 0.9f, _rb.velocity.y * 0.9f);
                    time -= Time.fixedDeltaTime;
                    yield return null;
                }
                _attacking = false;

                StartCoroutine(AttackChooser());
            }
            mana-=33;
            StartCoroutine(DoFire());
        }

        private void Slash()
        {
            IEnumerator DoSlash()
            {
                FlyMoveTo fm = gameObject.AddComponent<FlyMoveTo>();
                fm.enabled = false;
                fm.target = _target;
                fm.distance = 3;
                fm.speedMax = 12;
                fm.acceleration = 0.75f;
                fm.targetsHeight = true;
                fm.height = 0;
                fm.enabled = true;
                yield return new WaitWhile(() =>
                    Vector2.Distance(_target.transform.position, transform.position) > 5f);
                Log("ATTACKING TIME");
                _anim.Play("Slash Antic");
                yield return new WaitWhile(() => _anim.IsPlaying("Slash Antic"));
                Destroy(fm);
                //float dir = FaceHero();
                _rb.velocity = new Vector2(8f * side, 0f);
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
                _attacking = false;
                StartCoroutine(AttackChooser());
            }
            //FaceHero();
            StartCoroutine(DoSlash());
        }

        private void Teleport(Vector2 pos, float speed)
        {
            IEnumerator tp()
            {
                //Log($"TP to {pos} begin");
                //_tping = true;
                _bc.enabled = false;
                _anim.Play("Retreat Start");
                Deceleration dec = gameObject.AddComponent<Deceleration>();
                dec.deceleration = 0.85f;
                yield return new WaitWhile(() => _anim.IsPlaying("Retreat Start"));
                Destroy(dec);
                _rb.velocity = Vector2.zero;
                MoveToPos mp = gameObject.AddComponent<MoveToPos>();
                mp.pos = pos;
                mp.speed = speed;
                ParticleSystem.EmissionModule em = _shadePartic.emission;
                em.enabled = false;
                ParticleSystem.EmissionModule em2 = _retPartic.emission;
                em2.enabled = true;
                yield return new WaitWhile(() => mp != null);
                _anim.Play("Retreat End");
                em.enabled = true;
                em2.enabled = false;
                yield return new WaitWhile(() => _anim.IsPlaying("Retreat End"));
                _anim.Play("Fly");
                _bc.enabled = true;
                _attacking = false;
                //_tping = false;
                //Log($"TP to {pos} finished");
                Extra_Time = Spell_Time * -1f;
                StartCoroutine(AttackChooser());
            }

            StartCoroutine(tp());
        }
        public class TeleportV2 : CustomYieldInstruction
        {
            private MoveToPos mp;
            private Shade _s;
            public TeleportV2(Vector2 pos,float speed,Shade s)
            {
                _s = s;
                _s._bc.enabled = false;
                _s._anim.Play("Retreat Start");
                //Deceleration dec = _s.gameObject.AddComponent<Deceleration>();
                //dec.deceleration = 0.85f;
                //yield return new WaitWhile(() => _s._anim.IsPlaying("Retreat Start"));
                //Destroy(dec);
                _s._rb.velocity = Vector2.zero;
                mp = _s.gameObject.AddComponent<MoveToPos>();
                mp.pos = pos;
                mp.speed = speed;
                ParticleSystem.EmissionModule em = _s._shadePartic.emission;
                em.enabled = false;
                ParticleSystem.EmissionModule em2 = _s._retPartic.emission;
                em2.enabled = true;
               
            }
            public override bool keepWaiting
            {
                get
                {
                    if (mp == null)
                    {
                        ParticleSystem.EmissionModule em = _s._shadePartic.emission;
                        ParticleSystem.EmissionModule em2 = _s._retPartic.emission;
                        _s._anim.Play("Retreat End");
                        em.enabled = true;
                        em2.enabled = false;
                        //yield return new WaitWhile(() => _s._anim.IsPlaying("Retreat End"));
                        _s._anim.Play("Fly");
                        _s._bc.enabled = true;
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }
        private void OnHit(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitinstance)
        {
            if (self == _hm)
            {
                if (!_attacking && _rand.Next(2)>0)
                {
                    _attacking = true;
                    //float offset = FaceHero();
                    Vector2 tmp = _target.transform.position;
                    Vector2 pos = new Vector2(tmp.x + side * 8.5f, tmp.y);
                    Teleport(pos, 20f);
                }
                hitShade = true;
                Log("Hit shade");
                //hitinstance.Multiplier = 0;
                /*if (_life < 0)
                {
                    //_control.SetState("Killed");
                    hitinstance.Multiplier = 100;
                    Log("shade will die");
                }
                Log(_life);
                _life--;*/
                mana += 11;
                if(BossSceneController.Instance?.BossLevel == 2)
                {
                    mana += 33;
                }
            }
            orig(self, hitinstance);
        }

        private int OnHitPlayer(int damage)
        {
            if (_currAtt == Slash)
            {
                mana+=33;
            }

            return damage;
        }

        private float FaceHero()
        {
            float sign = Mathf.Sign(_target.transform.position.x - transform.position.x) * -1f;
            Vector3 tmp = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(tmp.x) * sign, tmp.y, tmp.z);
            return sign * -1f;
        }

        /*IEnumerator ParryMe()
        {
            var fsm = _slash.LocateMyFSM("nail_clash_tink");
            fsm.RemoveAction("Blocked Hit", 4);
            fsm.RemoveAction("Blocked Hit", 0);
            fsm.ChangeTransition("No Box Left", "FINISHED", "NailParryRecover");
            fsm.ChangeTransition("No Box Right", "FINISHED", "NailParryRecover");
            fsm.ChangeTransition("No Box Down", "FINISHED", "NailParryRecover");
            fsm.ChangeTransition("No Box Up", "FINISHED", "NailParryRecover");
            fsm.ChangeTransition("NailParryRecover", "FINISHED", "Detecting");
            
        }*/

        private void OnCollisionEnter2D(Collision2D other)
        {
            //_hitFloor = other.gameObject.name == "Chunk 0 1";
            _hitFloor = (other.gameObject.layer == (int)PhysLayers.TERRAIN);
        }

        bool FastApproximately(float a, float b, float threshold)
        {
            return ((a - b) < 0 ? ((a - b) * -1) : (a - b)) <= threshold;
        }
        private void OnDestroy()
        {

            Log("Shade Destroy");
        }
        private void ChildDestroy()
        {
            if (_hm.hp > 3000)
            {
                _control.SetState("Killed");
            }
        }
        private static void Log(object obj)
        {
            Logger.Log("[Lord of Shade] " + obj);
        }


    }
}

