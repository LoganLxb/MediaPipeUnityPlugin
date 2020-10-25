using System;
using System.IO;
using System.Collections;

using Mediapipe;
using UnityEngine;

using Directory = System.IO.Directory;

public class SceneDirector : MonoBehaviour {
  [SerializeField] bool useGPU = true;

  GameObject webCamScreen;
  GameObject graphPrefab;
  GameObject graphContainer;
  SidePacket sidePacket;
  Coroutine graphRunner;
  GpuResources gpuResources;
  GlCalculatorHelper gpuHelper;

  const int MAX_WAIT_FRAME = 50;

  bool IsAssetLoaded = false;
  bool IsAssetLoadFailed = false;

  void OnEnable() {
    var nameForGlog = Path.Combine(Application.dataPath, "MediaPipePlugin");
    var logDir = Path.Combine(Application.persistentDataPath, "Logs", "MediaPipe");

    if (!Directory.Exists(logDir)) {
      Directory.CreateDirectory(logDir);
    }

    UnsafeNativeMethods.InitGoogleLogging(nameForGlog, logDir);
  }

  async void Start() {
    webCamScreen = GameObject.Find("WebCamScreen");

    if (useGPU) {
      gpuResources = GpuResources.Create().ConsumeValue();

      gpuHelper = new GlCalculatorHelper();
      gpuHelper.InitializeForTest(gpuResources);
    }

    #if UNITY_EDITOR
      var resourceManager = LocalAssetManager.Instance;
    #else
      var resourceManager = AssetBundleManager.Instance;
    #endif

    ResourceUtil.InitializeResourceManager(resourceManager);

    try {
      await resourceManager.LoadAllAssetsAsync();
      IsAssetLoaded = true;
    } catch (Exception e) {
      Debug.LogError(e);
      IsAssetLoadFailed = true;
    }
  }

  void OnDisable() {
    UnsafeNativeMethods.ShutdownGoogleLogging();
  }

  public void ChangeWebCamDevice(WebCamDevice? webCamDevice) {
    webCamScreen.GetComponent<WebCamScreenController>().ResetScreen(webCamDevice);
  }

  public void ChangeGraph(GameObject graphPrefab) {
    StopGraph();
    this.graphPrefab = graphPrefab;
    StartGraph();
  }

  void StartGraph() {
    if (graphRunner != null) {
      return;
    }

    graphRunner = StartCoroutine(RunGraph());
  }

  void StopGraph() {
    if (graphRunner != null) {
      StopCoroutine(graphRunner);
      graphRunner = null;
    }

    if (graphContainer != null) {
      Destroy(graphContainer);;
    }
  }

  IEnumerator RunGraph() {
    var webCamScreenController = webCamScreen.GetComponent<WebCamScreenController>();
    var waitFrame = MAX_WAIT_FRAME;

    yield return new WaitWhile(() => {
      waitFrame--;

      var isGraphPrefabPresent = graphPrefab != null;
      var isWebCamPlaying = webCamScreenController.isPlaying;

      if (!isGraphPrefabPresent && waitFrame % 10 == 0) {
        Debug.Log($"Waiting for a graph");
      }

      if (!isWebCamPlaying && waitFrame % 10 == 0) {
        Debug.Log($"Waiting for a WebCamDevice");
      }

      return (!isGraphPrefabPresent || !isWebCamPlaying) && waitFrame > 0;
    });

    if (graphPrefab == null) {
      Debug.LogWarning("No graph is set. Stopping...");
      yield break;
    }
    
    if (!webCamScreenController.isPlaying) {
      Debug.LogWarning("WebCamDevice is not working. Stopping...");
      yield break;
    }

    if (!IsAssetLoaded && !IsAssetLoadFailed) {
      Debug.Log("Waiting for assets to be loaded...");
    }

    yield return new WaitUntil(() => IsAssetLoaded || IsAssetLoadFailed);

    if (IsAssetLoadFailed) {
      Debug.LogError("Failed to load assets. Stopping...");
      yield break;
    }

    graphContainer = Instantiate(graphPrefab);
    var graph = graphContainer.GetComponent<IDemoGraph<TextureFrame>>();

    if (useGPU) {
      graph.Initialize(gpuResources, gpuHelper);
    } else {
      graph.Initialize();
    }

    sidePacket = new SidePacket();
    graph.StartRun(sidePacket).AssertOk();

    while (true) {
      yield return new WaitForEndOfFrame();

      if (!webCamScreenController.isPlaying) {
        Debug.LogWarning("WebCam is not working");
        break;
      }

      var nextFrameRequest = webCamScreenController.RequestNextFrame();
      yield return nextFrameRequest;

      var nextFrame = nextFrameRequest.textureFrame;

      // webCamScreenController.DrawScreen(nextFrame);

      graph.PushInput(nextFrame).AssertOk();
      graph.RenderOutput(webCamScreenController, nextFrame);

      webCamScreenController.OnReleaseFrame(nextFrame);
    }
  }
}
