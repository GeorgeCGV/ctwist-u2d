using System;
using System.Collections;
using Model;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

/// <summary>
/// Self-contained canvas based level loader.
///
/// Waits until load screen animation is done,
/// then async loads and switches to the level scene.
///
/// Within the level scene it is expected that there is a <see>LevelManager</see>
/// that takes control over.
///
/// The instance is not destructible, as it has to be present
/// across the scenes.
/// </summary>
[RequireComponent(typeof(Animator), typeof(Canvas))]
public class LevelLoader : MonoBehaviour
{
    public static LevelLoader Instance { get; private set; }

    #region Scenes

    private const int LevelSceneIdx = 1;

    #endregion
        
    #region States

    /// <summary>
    /// LevelId is set to this when loader fails to load requested level.
    ///
    /// Used in the Animator's OnLoadScreenDisappearAnimationDone callback.
    /// </summary>
    private const int LevelIdxAborted = -100;

    /// <summary>
    /// LevelId is set to this on successful load.
    ///
    /// Used in the Animator's OnLoadScreenDisappearAnimationDone callback.
    /// </summary>
    private const int LevelIdxOk = -1;

    #endregion

    private static readonly int AnimatorTriggerOpen = Animator.StringToHash("Open");
    private static readonly int AnimatorTriggerClose = Animator.StringToHash("Close");

    private Animator _animator;
    private Canvas _canvas;

    /// <summary>
    /// Level ID we are trying to load.
    /// </summary>
    private int _levelId;

    /// <summary>
    /// Loaded and parsed level data.
    /// </summary>
    private LevelData _levelData;

    /// <summary>
    /// Setups as a singleton and grabs required references.
    /// </summary>
    private void Awake()
    {
        // prevent multiple instances
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            // prevent destruction
            DontDestroyOnLoad(this);

            _animator = GetComponent<Animator>();
            _canvas = GetComponent<Canvas>();
        }
    }

    /// <summary>
    /// Coroutine that loads and switches to specified scene.
    /// </summary>
    /// <param name="sceneIdx">Scene index.</param>
    /// <returns></returns>
    private static IEnumerator ChangeScene(int sceneIdx)
    {
        // artificial delay to allow sfx to finish
        // and give some time for animation
        yield return new WaitForSeconds(1);
        // load and switch the scene
        SceneManager.LoadSceneAsync(sceneIdx);
    }
        
    /// <summary>
    /// Loads level data resource.
    /// </summary>
    /// <remarks>
    /// Data is stored in a text file as JSON, <c>Resources/Level/[ID].json</c>.
    /// Filename is the level id.
    /// </remarks>
    /// <param name="id">Level ID.</param>
    /// <returns>Parsed level data or <c>null</c>.</returns>
    private static LevelData LoadLevelData(int id)
    {
        // Levels data is located at 'Assets/Resources/Level/<idx>.json'
        LevelData ret;

        TextAsset levelFile = Resources.Load<TextAsset>("Level/" + id);
        try
        {
            ret = JsonUtility.FromJson<LevelData>(levelFile.text);
            ret.ID = id; // set level id
            ret.ParseInternal();
        }
        catch (Exception err)
        {
            // regardless of the content, on error reset to null
            ret = null;
            Logger.Debug(err.Message);
        }
        finally
        {
            // regardless of the outcome
            // free level asset if it is not null
            if (levelFile != null)
            {
                Resources.UnloadAsset(levelFile);
            }
        }

        return ret;
    }
        
    /// <summary>
    /// SceneManager.sceneLoaded callback.
    /// </summary>
    /// <remarks>
    /// <see cref="SceneManager.sceneLoaded"/>
    /// </remarks>
    /// <param name="scene">Loaded scene.</param>
    /// <param name="mode">Load scene mode.</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // as we are done with the loading
        // clear the index by setting ID to LEVEL_IDX_OK
        _levelId = LevelIdxOk;

        // Initiate close animation
        _animator.SetTrigger(AnimatorTriggerClose);
    }

    /// <summary>
    /// Callback for the Animator when <see cref="AnimatorTriggerOpen"/> ends.
    /// </summary>
    /// <remarks>
    /// At this point the level loader covers the screen,
    /// continue with the scene load & switch.
    /// </remarks>
    private void OnLoadScreenAppearAnimationDone()
    {
        if (_levelId >= 0)
        {
            // load level data
            _levelData = LoadLevelData(_levelId);
            if (_levelData == null)
            {
                // abort, hide load screen
                _levelId = LevelIdxAborted;
                _animator.SetTrigger(AnimatorTriggerClose);
                return;
            }

            // subscribe to the scene manager before starting
            // scene loading, that allows us to know
            // when the scene has loaded
            SceneManager.sceneLoaded += OnSceneLoaded;
            // start scene load
            StartCoroutine(ChangeScene(LevelSceneIdx));
        }
    }

    /// <summary>
    /// Callback for the Animator when <see cref="AnimatorTriggerClose"/> ends.
    /// </summary>
    /// <remarks>
    /// <para>
    /// However, because the close animation is reversed show
    /// animation the Animator triggers this callback multiple times.
    /// </para>
    /// <para>
    /// We differentiate open from close with the help of level ID.
    /// When loading a level scene the level ID will be >= 0.
    /// </para>
    /// <para>>
    /// At this point the level loader close animation is done.
    /// Loaded scene is visible now.
    /// </para>
    /// <para>
    /// Pass control to the level scene (level ID is <see cref="LevelIdxOk"/>)
    /// or simply hide the loader canvas (level ID is <see cref="LevelIdxAborted"/>).
    /// </para>
    /// </remarks>
    private void OnLoadScreenDisappearAnimationDone()
    {
        if (_levelId == LevelIdxOk)
        {
            // handle level loaded state
            _canvas.enabled = false;
            // pass control to level manager
            LevelManager.Instance.OnLevelSceneLoaded(_levelData);
        }
        else if (_levelId == LevelIdxAborted)
        {
            // handle level load aborted state
            _canvas.enabled = false;
        }
    }
        
    /// <summary>
    /// Initiates the level loading.
    /// </summary>
    /// <param name="id">Level ID >= 0</param>
    public void LoadLevel(int id)
    {
        Assert.IsTrue(id >= 0, "Invalid level id");
        _levelId = id;
        _canvas.enabled = true;
        _animator.SetTrigger(AnimatorTriggerOpen);
    }
}