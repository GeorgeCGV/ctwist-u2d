using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Class <c>LoadScreen</c> awaits until load screen animation is done,
/// then async loads and switches to the level scene.
/// Within the level scene it is expected that there is a <see>LevelManager</see>
/// that takes the control over.
/// </summary>
[RequireComponent(typeof(Animator), typeof(Canvas))]
public class LoadScreen : MonoBehaviour
{
    public static LoadScreen Instance { get; private set; }

    private static int ABORTED_SCEME_IDX = -100;
    private static int LOADED_SCEME_IDX = -1;

    private static int animatorTriggerOpen = Animator.StringToHash("Open");
    private static int animatorTriggerClose = Animator.StringToHash("Close");

    private int scene_idx;
    private Data.LevelData levelData;

    private Animator animator;
    private Canvas canvas;

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

    protected IEnumerator ChangeScene(int idx)
    {
        // artificial delay to allow sfx to finish
        // and give some time for animation
        yield return new WaitForSeconds(1);
        // load and switch the scene
        AsyncOperation op = SceneManager.LoadSceneAsync(idx);
        while (!op.isDone)
        {
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void LoadLevel(int idx = 1)
    {
        scene_idx = idx;
        canvas.enabled = true;
        animator.SetTrigger(animatorTriggerOpen);
    }

    public void OnLoadSceenAppearAnimationDone()
    {
        if (scene_idx > 0)
        {
            // load level data
            levelData = LoadLevelData(scene_idx - 1);
            if (levelData == null)
            {
                // abort, hide load screen
                scene_idx = ABORTED_SCEME_IDX;
                animator.SetTrigger(animatorTriggerClose);
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(ChangeScene(scene_idx));
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        Logger.Debug("Scene Loaded Completely!");

        // as we are done with the loading
        // clear the index
        scene_idx = LOADED_SCEME_IDX;
        AudioManager.Instance.StopMusic();
        animator.SetTrigger(animatorTriggerClose);
    }

    public void OnLoadSceenDisappearAnimationDone()
    {
        // the event is fired at the first animation frame
        // when load screen appears.
        // To handle that we will check if we are currently
        // loading the level; when loading a level scene
        // the scene idx won't be LOADED_SCEME_IDX.
        if (scene_idx == LOADED_SCEME_IDX)
        {
            canvas.enabled = false;
            // pass control to level manager
            LevelManager.Instance.OnLevelSceneLoaded(gameObject, levelData);
        }
        else if (scene_idx == ABORTED_SCEME_IDX)
        {
            // failed to load the scene
            canvas.enabled = false;
        }
    }

    public Data.LevelData LoadLevelData(int level)
    {
        // Levels data is located at 'Assets/Resources/Level/<idx>.json'
        Data.LevelData ret = null;

        TextAsset levelFile = Resources.Load<TextAsset>("Level/" + level);
        try
        {
            ret = JsonUtility.FromJson<Data.LevelData>(levelFile.text);
        }
        catch (Exception err)
        {
            Logger.Debug(err.Message);
        }
        finally
        {
            if (levelFile != null)
            {
                Resources.UnloadAsset(levelFile);
            }
        }

        return ret;
    }
}
