using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Self contained canvas based level loader.
///
/// Awaits until load screen animation is done,
/// then async loads and switches to the level scene.
///
/// Within the level scene it is expected that there is a <see>LevelManager</see>
/// that takes control over.
///
/// The instance is not destructable, as it has to be present
/// across the scenes.
/// </summary>
[RequireComponent(typeof(Animator), typeof(Canvas))]
public class LoadScreen : MonoBehaviour
{
    public static LoadScreen Instance { get; private set; }

    #region Scenes
    private static readonly int LEVEL_SCENE_IDX = 1;
    #endregion

    #region States
    /// <summary>
    /// LevelId is set to this when loader fails to load requested level.
    ///
    /// Used in the Animator's OnLoadSceenDisappearAnimationDone callback.
    /// </summary>
    private static readonly int LEVEL_IDX_ABORTED = -100;
    /// <summary>
    /// LevelId is set to this on successful load.
    ///
    /// Used in the Animator's OnLoadSceenDisappearAnimationDone callback.
    /// </summary>
    private static readonly int LEVEL_IDX_OK = -1;
    #endregion

    private static readonly int animatorTriggerOpen = Animator.StringToHash("Open");
    private static readonly int animatorTriggerClose = Animator.StringToHash("Close");

    private Animator animator;
    private Canvas canvas;

    /// <summary>
    /// Level ID we are trying to load.
    /// </summary>
    private int levelId;

    /// <summary>
    /// Loaded and parsed level data.
    /// </summary>
    private Data.LevelData levelData;

    /// <summary>
    ///
    /// </summary> <summary>
    ///
    /// </summary>
    private void Awake()
    {
        // prevent mutliple instances
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
            // prevent destruction
            DontDestroyOnLoad(this);

            animator = GetComponent<Animator>();
            canvas = GetComponent<Canvas>();
        }
    }

    /// <summary>
    /// Coroutine that loads and switches to specified scene.
    /// </summary>
    /// <param name="sceneIdx">Scene index.</param>
    /// <returns></returns>
    private IEnumerator ChangeScene(int sceneIdx)
    {
        // artificial delay to allow sfx to finish
        // and give some time for animation
        yield return new WaitForSeconds(1);
        // load and switch the scene
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneIdx);
        while (!op.isDone)
        {
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// Initiates the level loading.
    /// </summary>
    /// <param name="id">Level ID >= 0</param>
    public void LoadLevel(int id)
    {
        levelId = id;
        canvas.enabled = true;
        animator.SetTrigger(animatorTriggerOpen);
    }

    /// <summary>
    /// Loads level data resource.
    ///
    /// Data is stored in a text file as JSON.
    /// Filename is the level id.
    ///
    /// </summary>
    /// <param name="id">Level ID.</param>
    /// <returns>Parsed level data.</returns>
    public Data.LevelData LoadLevelData(int id)
    {
        // Levels data is located at 'Assets/Resources/Level/<idx>.json'
        Data.LevelData ret = null;

        TextAsset levelFile = Resources.Load<TextAsset>("Level/" + id);
        try
        {
            ret = JsonUtility.FromJson<Data.LevelData>(levelFile.text);
            ret.id = id; // set level id
        }
        catch (Exception err)
        {
            Logger.Debug(err.Message);
        }
        finally
        {
            // free level file if it is not null
            // regardless of the outcome
            if (levelFile != null)
            {
                Resources.UnloadAsset(levelFile);
            }
        }

        return ret;
    }

    /// <summary>
    /// Callback from the Animator when animatorTriggerOpen ends.
    ///
    /// At this point the level loader covers the screen,
    /// continue with the scene load & switch.
    /// </summary>
    public void OnLoadSceenAppearAnimationDone()
    {
        if (levelId >= 0)
        {
            // load level data
            levelData = LoadLevelData(levelId);
            if (levelData == null)
            {
                // abort, hide load screen
                levelId = LEVEL_IDX_ABORTED;
                animator.SetTrigger(animatorTriggerClose);
                return;
            }

            // subsribe to the scene manager before starting
            // scene loading, that allows us to know
            // when the scene has loaded
            SceneManager.sceneLoaded += OnSceneLoaded;
            // start scene load
            StartCoroutine(ChangeScene(LEVEL_SCENE_IDX));
        }
    }

    /// <summary>
    /// SceneManager.sceneLoaded callback.
    /// </summary>
    /// <param name="scene">Loaded scene.</param>
    /// <param name="mode">Load scene mode.</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // as we are done with the loading
        // clear the index by setting ID to LEVEL_IDX_OK
        levelId = LEVEL_IDX_OK;

        // Initiate close animation
        animator.SetTrigger(animatorTriggerClose);
    }

    /// <summary>
    /// Callback from the Animator when animatorTriggerClose ends.
    /// However, because the close animation is reversed show
    /// animation the Animator triggers this callback multiple times.
    ///
    /// We differentiate open from close with the help of levelId.
    /// When loading a level scene the levelId will be >= 0.
    ///
    /// At this point the level loader close animation is done.
    /// Loaded scene is visible now.
    ///
    /// Pass control to the level scene (levelId is LEVEL_IDX_OK)
    /// or simply hide the loader canvas (levelId is LEVEL_IDX_ABORTED).
    /// </summary>
    public void OnLoadSceenDisappearAnimationDone()
    {
        if (levelId == LEVEL_IDX_OK)
        {
            // handle level loaded state
            canvas.enabled = false;
            // pass control to level manager
            LevelManager.Instance.OnLevelSceneLoaded(gameObject, levelData);
        }
        else if (levelId == LEVEL_IDX_ABORTED)
        {
            // handle level load aborted state
            canvas.enabled = false;
        }
    }
}
