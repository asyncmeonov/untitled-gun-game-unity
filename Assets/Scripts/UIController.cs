using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }
    public bool IsCameraAttached { get => _isCameraAttached; set => _isCameraAttached = value; }

    [SerializeField] TextMeshProUGUI _scoreText;
    [SerializeField] TextMeshProUGUI _copCounter;
    [SerializeField] Slider _tweakSlider;
    [SerializeField] Image _fireIcon;
    [SerializeField] Image _flashImg;
    [SerializeField] AudioEvent _zoomWooshSfx;
    [SerializeField] AudioEvent _revZoomWooshSfx;
    [SerializeField] GameObject _postProcessor;

    [SerializeField] AudioMixer _mixer;


    [Header("Menu Screens")]
    [SerializeField] GameObject _mainMenu;
    [SerializeField] GameObject _endGameMenu;
    [SerializeField] GameObject _creditsMenu;
    [SerializeField] GameObject _optionsMenu;
    [SerializeField] GameObject _leaderBoardMenu;
    [SerializeField] GameObject _inGameHUD;
    [SerializeField] GameObject _saveScoreMenu;

    private PostProcessVolume _postProcessVol;

    private bool _isCameraAttached;

    private string _leaderboardPath = "Assets/leaderboard.txt";



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        _flashImg.gameObject.SetActive(true);
        _flashImg.color = new Color(1, 1, 1, 0);
        _fireIcon.enabled = false;
        _tweakSlider.value = 0;
        _postProcessVol = _postProcessor.GetComponent<PostProcessVolume>();
        _isCameraAttached = false;

        _mainMenu.SetActive(true);
        StartCoroutine(DefocusBackground());
    }

    void Update()
    {
        if (IsCameraAttached)
        {
            _tweakSlider.value = PlayerController.Instance.TweakValue;
            _scoreText.text = GameController.Instance.Score.ToString().PadLeft(4, '0');
            _copCounter.text = MobSpawnerController.Instance.MobCount.ToString().PadLeft(2, '0');
        }

    }


    public void IsPlayerTweakingUI(bool isTweaking)
    {
        if (isTweaking)
        {
            _fireIcon.enabled = true;
            StartCoroutine(FlashBang());
        }
        else
        {
            GameController.Instance.PlayMainTheme();
            _revZoomWooshSfx.Play(null);
            _fireIcon.enabled = false;
         }    
    }


    public IEnumerator FlashBang()
    {
        _zoomWooshSfx.Play(null);
        GameController.Instance.PlayTweakTheme();
        _flashImg.color = new Color(1, 1, 1, 1);
        yield return new WaitForSeconds(0.25f);
        while (_flashImg.color.a > 0)
        {
            _flashImg.color = new Color(1, 1, 1, Mathf.MoveTowards(_flashImg.color.a, 0f, 0.001f));
        }
    }

    public void ShowEndGameScreen(bool isVictory)
    {
        IsCameraAttached = false;
        StartCoroutine(DefocusBackground());
        CloseAllMenusAndDialogs();
        _endGameMenu.SetActive(true);
        TextMeshProUGUI scoreDisplay = GameObject.FindGameObjectWithTag("end_game_score_field").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI scoreHeader = GameObject.FindGameObjectWithTag("score_header_field").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI scoreSubheader = GameObject.FindGameObjectWithTag("score_subheader_field").GetComponent<TextMeshProUGUI>();

        scoreSubheader.text = "score";

        try
        {
            if (isVictory)
            {
                scoreHeader.text = "Congratulations!";
                scoreSubheader.text = "You snorted all the cocaine in the city without being caught by the police!";
            }
            else
            {
                if (GetLeaderboardFromFile().Max(p => p.Item2) < GameController.Instance.Score)
                {
                    scoreSubheader.text = "new high score";
                }
            }

        }
        catch (InvalidOperationException e)
        {
            Debug.Log("No previous scores found. " + e.Message);
            scoreSubheader.text = "new high score";
        }
        finally
        {
            scoreDisplay.text = _scoreText.text;
        }


    }

    public void ShowSaveScoreScreen()
    {
        StartCoroutine(DefocusBackground());
        CloseAllMenusAndDialogs();
        _saveScoreMenu.SetActive(true);
        TextMeshProUGUI scoreDisplay = GameObject.FindGameObjectWithTag("end_game_score_field").GetComponent<TextMeshProUGUI>();
        scoreDisplay.text = _scoreText.text;
    }

    public void ShowCreditsScreen()
    {
        StartCoroutine(DefocusBackground());
        CloseAllMenusAndDialogs();
        _creditsMenu.SetActive(true);

    }

    public void ShowOptionsScreen()
    {
        CloseAllMenusAndDialogs();
        _optionsMenu.SetActive(true);

    }

    public void SetMusicVolume(float value)
    {
        _mixer.SetFloat("MusicVol", Mathf.Log10(value) * 20);
    }

    public void SetSoundEffectsVolume(float value)
    {
        _mixer.SetFloat("SFXVol", Mathf.Log10(value) * 20);
        _zoomWooshSfx.Play(null);
    }

    public void ShowLeaderBoardScreen()
    {
        StartCoroutine(DefocusBackground());
        CloseAllMenusAndDialogs();
        _leaderBoardMenu.SetActive(true);
        TextMeshProUGUI leaderboardList = GameObject.FindGameObjectWithTag("leaderboard_list").GetComponent<TextMeshProUGUI>();
        List<Tuple<string, int>> leaderboard = GetLeaderboardFromFile();
        List<Tuple<string, int>> topTen = leaderboard.OrderBy(s => s.Item2).Reverse().Take(10).ToList();

        leaderboardList.text = "";

        foreach (var item in topTen)
        {
            string row = item.Item2.ToString().PadLeft(4, '0') + " - " + item.Item1 + "\n";
            leaderboardList.text += row;
        }

    }

    public void WriteHighscoreToLeaderboard()
    {
        GameObject inputField = GameObject.FindGameObjectWithTag("highscore_player_name_field");
        string playerName = inputField.GetComponent<TMP_InputField>().text;
        // Write to file
        File.AppendAllText(_leaderboardPath, String.Format("{0}|{1}\n", playerName, GameController.Instance.Score));
        ShowLeaderBoardScreen();
    }

    private List<Tuple<string, int>> GetLeaderboardFromFile()
    {
        List<Tuple<string, int>> leaderboard = new List<Tuple<string, int>>();
        string line;
        try
        {
            StreamReader sr = new StreamReader(_leaderboardPath);
            while ((line = sr.ReadLine()) != null)
            {
                string player = line.Substring(0, line.LastIndexOf('|'));
                string score = line.Substring(line.LastIndexOf('|') + 1);
                Tuple<string, int> entry = new Tuple<string, int>(player, int.Parse(score));
                leaderboard.Add(entry);
            }
            sr.Close();
            return leaderboard;
        }
        catch (Exception e)
        {
            Debug.Log("Exception reading file: " + e.Message);
            return leaderboard;
        }
    }

    public void ExitToMainMenu()
    {
        StartCoroutine(DefocusBackground());
        _isCameraAttached = false;
        CloseAllMenusAndDialogs();
        _mainMenu.SetActive(true);
    }

    public void StartGame()
    {
        StartCoroutine(FocusBackground());
        CloseAllMenusAndDialogs();
        _inGameHUD.SetActive(true);
        _isCameraAttached = true;
        GameController.Instance.StartGame();
    }

    private IEnumerator DefocusBackground()
    {
        yield return null;
        while (_postProcessVol.weight < 1)
        {
            _postProcessVol.weight = Mathf.MoveTowards(_postProcessVol.weight, 1f, 0.01f);
        }
    }
    private IEnumerator FocusBackground()
    {
        yield return null;
        while (_postProcessVol.weight > 0)
        {
            _postProcessVol.weight = Mathf.MoveTowards(_postProcessVol.weight, 0f, 0.01f);
        }
    }

    private void CloseAllMenusAndDialogs()
    {
        _endGameMenu.SetActive(false);
        _inGameHUD.SetActive(false);
        _creditsMenu.SetActive(false);
        _mainMenu.SetActive(false);
        _saveScoreMenu.SetActive(false);
        _leaderBoardMenu.SetActive(false);
        _optionsMenu.SetActive(false);
    }
}
