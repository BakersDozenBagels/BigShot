using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HarmonyLib;

public class NowsYourChanceToBeA : MonoBehaviour
{
    [SerializeField]
    private AudioSource _music;
    [SerializeField]
    private KMBombInfo _info;
    [SerializeField]
    private GameObject _canvas, _moveHint, _spaceHint;
    [SerializeField]
    private TextMesh _mainText, _leftText, _rightText;
    [SerializeField]
    private KMAudio _audio;
    [SerializeField]
    private Transform _heart;
    [SerializeField]
    private KMBombModule _module;
    [SerializeField]
    private RingScript _bulletRing;
    [SerializeField]
    private SpamtonBulletScript _singleBullet, _singleBullet2;

    private static readonly List<NowsYourChanceToBeA> _instances = new List<NowsYourChanceToBeA>();
    private static readonly Harmony _harmony = new Harmony("Ktane-Big-Shot");
    private static bool _isPatched, _allowLightsOn, _allowTimerOn, _allowActivate, _allowWidgets;
    private static object _mostRecentRoom;
    private static readonly List<object> _bombs = new List<object>(), _widgets = new List<object>();

#if UNITY_EDITOR
    private const float WAIT_TIME = 1f;
#else
    private const float WAIT_TIME = 20f;
#endif

    public Vector3 lp;
    public Vector3 HeartPos { get { return lp; } }

    private static bool AllowLightsOn
    {
        get
        {
            _instances.RemoveAll(i => i == null);
            if(_instances.Count == 0)
                return true;
            return _allowLightsOn;
        }
    }

    private static bool AllowTimerOn
    {
        get
        {
            _instances.RemoveAll(i => i == null);
            if(_instances.Count == 0)
                return true;
            return _allowTimerOn;
        }
    }

    public static bool AllowActivate
    {
        get
        {
            _instances.RemoveAll(i => i == null);
            if(_instances.Count == 0)
                return true;
            return _allowActivate;
        }
    }

    public static bool AllowWidgets
    {
        get
        {
            _instances.RemoveAll(i => i == null);
            if(_instances.Count == 0)
                return true;
            return _allowWidgets;
        }
    }

    private static int _idc;
    private readonly int _id = ++_idc;
    private bool _controller, _isSolved;

    private void Awake()
    {
        _instances.RemoveAll(n => n == null);
        if(_instances.Count == 0)
            _controller = true;
        _instances.Add(this);
    }

    private void Start()
    {
#if UNITY_EDITOR
        //_canvas.SetActive(true);
        _canvas.SetActive(false);
#else
        _canvas.SetActive(false);
#endif

        if(_controller)
        {
            _allowLightsOn = false;
            _allowTimerOn = false;
            _allowActivate = false;
            _allowWidgets = false;
            _bombs.Clear();
            _widgets.Clear();
            float vol = GameMusicControl.GameMusicVolume;
            GameMusicControl.GameMusicVolume = 0f;
            _music.volume = Mathf.Max(vol, 0.1f);
            _music.Play();
            _info.OnBombExploded += () => { _music.Stop(); GameMusicControl.GameMusicVolume = vol; };
#if !UNITY_EDITOR
            if(!_isPatched)
            {
                HarmonyMethod pre = new HarmonyMethod(GetType().Method("LightsPrefix"));
                _harmony.Patch(ReflectionHelper.FindTypeInGame("GameplayRoom").Method("ActivateCeilingLights"), prefix: pre);
                HarmonyMethod pre2 = new HarmonyMethod(GetType().Method("TimerPrefix"));
                _harmony.Patch(ReflectionHelper.FindTypeInGame("TimerComponent").Method("StartTimer"), prefix: pre2);
                HarmonyMethod pre3 = new HarmonyMethod(GetType().Method("ActivatePrefix"));
                _harmony.Patch(ReflectionHelper.FindTypeInGame("Bomb").Method("ActivateComponents"), prefix: pre3);
                HarmonyMethod pre4 = new HarmonyMethod(GetType().Method("WidgetPrefix"));
                _harmony.Patch(ReflectionHelper.FindTypeInGame("WidgetManager").Method("ActivateAllWidgets"), prefix: pre4);
                _isPatched = true;
            }
#endif
            if(KtaneVRChecker.VRC.IsEnabled)
            {
                _instances.RemoveAll(i => i == null);
                foreach(NowsYourChanceToBeA inst in _instances)
                    inst.Solve();
                _instances.Clear();
            }
            else
                StartCoroutine(RunGame());
        }
    }

    private IEnumerator RunGame()
    {
#if !UNITY_EDITOR
        yield return new WaitForSecondsRealtime(13.8f);

        _allowLightsOn = true;
        ReflectionHelper.FindTypeInGame("GameplayRoom").Method("ActivateCeilingLights").Invoke(_mostRecentRoom, new object[0]);

        System.Reflection.MethodInfo m = ReflectionHelper.FindTypeInGame("Bomb").Method("ActivateComponents");
        _allowActivate = true;
        foreach(object o in _bombs)
            m.Invoke(o, new object[0]);

        System.Reflection.MethodInfo w = ReflectionHelper.FindTypeInGame("WidgetManager").Method("ActivateAllWidgets");
        _allowWidgets = true;
        foreach(object o in _widgets)
        {
            w.Invoke(o, new object[0]);
        }
        yield return new WaitForSeconds(2f);
        Type t = ReflectionHelper.FindTypeInGame("GameplayState");
        IList bombList = t.GetProperty("Bombs", ReflectionHelper.Flags).GetValue(FindObjectOfType(t), new object[0]) as IList;
        _allowTimerOn = true;
        for(int i = 0; i < bombList.Count; i++)
        {

            object bomb = bombList[i];
            object timer = bomb.GetType().GetMethod("GetTimer").Invoke(bomb, new object[0]);
            timer.GetType().GetMethod("StartTimer").Invoke(timer, new object[0]);
        }
#endif

        int deal = -1;
        while(!_isSolved)
        {
            yield return new WaitForSeconds(WAIT_TIME);
            _instances.RemoveAll(c => c == null);
            yield return StartCoroutine(_instances.PickRandom().Attack(++deal));
            if(deal == 10)
            {
                _instances.RemoveAll(i => i == null);
                foreach(NowsYourChanceToBeA inst in _instances)
                    inst.Solve();
                _instances.Clear();
            }
        }

        yield break;
    }

    private void Solve()
    {
        _isSolved = true;
        _module.HandlePass();
        if(_controller)
            _music.Stop();
    }

    private IEnumerator Attack(int dealNum)
    {
        _canvas.SetActive(true);
        lp = _heart.localPosition = Vector3.zero;
        _audio.PlaySoundAtTransform("Attack", transform);
        bool leftCorrect = false;

        Vector3 left = new Vector3(-0.05f, 0f, 0f);
        float lastMove = Time.time, startTime = 0f;
        bool leftActive = false, rightActive = false;
        switch(dealNum)
        {
            case 0:
                _mainText.text = "I HAVE A VERY SPECIL\n[Deal] FOR YOU KID!";
                leftCorrect = UnityEngine.Random.Range(0, 2) == 0;
                (leftCorrect ? _leftText : _rightText).text = "TELL ME\nMORE";
                (leftCorrect ? _rightText : _leftText).text = "NOT\nINTERESTED";
                break;
            case 1:
                _moveHint.SetActive(true);
                _mainText.text = string.Empty;
                _leftText.text = string.Empty;
                _rightText.text = string.Empty;

                _bulletRing.gameObject.SetActive(true);
                break;
            case 2:
                _mainText.text = "I NEED A LITTLE\n[[Genorisity]]";
                leftCorrect = UnityEngine.Random.Range(0, 2) == 0;
                (leftCorrect ? _leftText : _rightText).text = "DON'T\nGIVE";
                (leftCorrect ? _rightText : _leftText).text = "GIVE\nMONEY";
                break;
            case 3:
                _moveHint.SetActive(true);
                _mainText.text = string.Empty;
                _leftText.text = string.Empty;
                _rightText.text = string.Empty;

                _singleBullet.gameObject.SetActive(true);
                break;
            case 4:
                _mainText.text = "W4NT TO BE JUST LIKE\nYOUR OLD PAL SPAMTON????";
                leftCorrect = UnityEngine.Random.Range(0, 2) == 0;
                (leftCorrect ? _leftText : _rightText).text = "TAKE\nDEAL";
                (leftCorrect ? _rightText : _leftText).text = "DON'T TAKE\nDEAL";
                break;
            case 5:
                _moveHint.SetActive(true);
                _mainText.text = string.Empty;
                _leftText.text = string.Empty;
                _rightText.text = string.Empty;

                _bulletRing.gameObject.SetActive(true);
                _singleBullet.gameObject.SetActive(true);
                break;
            case 6:
                _mainText.text = "THE LOW, LOW PRICE\nOF 1000 KROMER";
                leftCorrect = UnityEngine.Random.Range(0, 2) == 0;
                (leftCorrect ? _leftText : _rightText).text = "DON'T\nBUY";
                (leftCorrect ? _rightText : _leftText).text = "BUY\nINSURANCE";
                break;
            case 7:
                _moveHint.SetActive(true);
                _mainText.text = string.Empty;
                _leftText.text = string.Empty;
                _rightText.text = string.Empty;

                _bulletRing.gameObject.SetActive(true);
                _singleBullet.gameObject.SetActive(true);
                _singleBullet2.gameObject.SetActive(true);
                break;
            case 8:
                _mainText.text = "I JUST NEED\nYOUR [Account Details]";
                leftCorrect = UnityEngine.Random.Range(0, 2) == 0;
                (leftCorrect ? _leftText : _rightText).text = "REFUSE";
                (leftCorrect ? _rightText : _leftText).text = "GIVE ACCOUNT\nACCESS";
                break;
            case 9:
                _moveHint.SetActive(true);
                _mainText.text = string.Empty;
                _leftText.text = string.Empty;
                _rightText.text = string.Empty;

                _bulletRing.gameObject.SetActive(true);
                _singleBullet.gameObject.SetActive(true);
                _singleBullet2.gameObject.SetActive(true);
                break;
            case 10:
                _mainText.text = "WILL YOU TAKE\nTHE FINAL DEAL!?";
                leftCorrect = UnityEngine.Random.Range(0, 2) == 0;
                (leftCorrect ? _leftText : _rightText).text = "YES\nDEAL";
                (leftCorrect ? _rightText : _leftText).text = "NO\nDEAL";
                break;
            default:
                _mainText.text = "ERROR! GO LEFT";
                leftCorrect = true;
                _leftText.text = "YES";
                _rightText.text = "NO";
                break;
        }

        while(true)
        {
            if(Input.GetKey(KeyCode.W))
            {
                lp.y += 0.15f * Time.deltaTime;
                lp.y = Mathf.Min(lp.y, 0.088f);
                lastMove = Time.time;
                if(startTime == 0f)
                    startTime = Time.time;
                _moveHint.SetActive(false);
            }
            if(Input.GetKey(KeyCode.A))
            {
                lp.x -= 0.15f * Time.deltaTime;
                lp.x = Mathf.Max(lp.x, -0.088f);
                lastMove = Time.time;
                if(startTime == 0f)
                    startTime = Time.time;
                _moveHint.SetActive(false);
            }
            if(Input.GetKey(KeyCode.S))
            {
                lp.y -= 0.15f * Time.deltaTime;
                lp.y = Mathf.Max(lp.y, -0.088f);
                lastMove = Time.time;
                if(startTime == 0f)
                    startTime = Time.time;
                _moveHint.SetActive(false);
            }
            if(Input.GetKey(KeyCode.D))
            {
                lp.x += 0.15f * Time.deltaTime;
                lp.x = Mathf.Min(lp.x, 0.088f);
                lastMove = Time.time;
                if(startTime == 0f)
                    startTime = Time.time;
                _moveHint.SetActive(false);
            }

            _heart.localPosition = lp;

            if(_mainText.text != string.Empty)
            {
                if(Vector3.Distance(left, lp) < 0.02f)
                {
                    _leftText.color = Color.yellow;
                    leftActive = true;
                    if(Input.GetKeyDown(KeyCode.Space))
                    {
                        if(!leftCorrect)
                        {
                            _module.HandleStrike();
                        }
                        _canvas.SetActive(false);
                        yield break;
                    }
                }
                else
                {
                    _leftText.color = Color.white;
                    leftActive = false;
                }
                if(Vector3.Distance(-left, lp) < 0.02f)
                {
                    _rightText.color = Color.yellow;
                    rightActive = true;
                    if(Input.GetKeyDown(KeyCode.Space))
                    {
                        if(leftCorrect)
                        {
                            _module.HandleStrike();
                        }
                        _canvas.SetActive(false);
                        yield break;
                    }
                }
                else
                {
                    _rightText.color = Color.white;
                    rightActive = false;
                }

                if(Time.time - lastMove > 10f)
                {
                    if(!leftActive && !rightActive)
                        _moveHint.SetActive(true);
                    else
                        _spaceHint.SetActive(true);
                }
                else
                {
                    _spaceHint.SetActive(false);
                }
            }
            else
            {
                if(dealNum == 1 || dealNum == 5 || dealNum == 7 || dealNum == 9)
                    for(int i = 0; i < _bulletRing.transform.childCount; i++)
                        if(Vector3.Distance(_bulletRing.transform.GetChild(i).localPosition * _bulletRing.transform.localScale.x, lp) < 0.01f)
                            goto strike;
                if(dealNum == 3 || dealNum == 5 || dealNum == 7 || dealNum == 9)
                    if(startTime != 0f && _singleBullet.Dangerous && Vector3.Distance(_singleBullet.Position, lp) < 0.01f)
                        goto strike;
                if(dealNum == 7 || dealNum == 9)
                    if(startTime != 0f && _singleBullet2.Dangerous && Vector3.Distance(_singleBullet2.Position, lp) < 0.01f)
                        goto strike;


                if(startTime != 0f && Time.time - startTime >= 10f)
                    goto pass;
            }

            yield return null;
        }

        strike:
        _module.HandleStrike();
        pass:
        _bulletRing.gameObject.SetActive(false);
        _singleBullet.gameObject.SetActive(false);
        _singleBullet2.gameObject.SetActive(false);
        _canvas.SetActive(false);
        yield break;
    }

    private static bool LightsPrefix(object __instance)
    {
        _mostRecentRoom = __instance;
        return AllowLightsOn;
    }

    private static bool TimerPrefix()
    {
        return AllowTimerOn;
    }

    private static bool ActivatePrefix(object __instance)
    {
        if(!AllowActivate)
            _bombs.Add(__instance);
        return AllowActivate;
    }

    private static bool WidgetPrefix(object __instance)
    {
        if(!AllowWidgets)
            _widgets.Add(__instance);
        return AllowWidgets;
    }
}
