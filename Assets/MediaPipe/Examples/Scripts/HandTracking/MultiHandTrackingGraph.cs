using Mediapipe;
using System.Collections.Generic;
using UnityEngine;

public class MultiHandTrackingGraph : DemoGraph {
  private const string multiHandLandmarksStream = "multi_hand_landmarks";
  private OutputStreamPoller<List<NormalizedLandmarkList>> multiHandLandmarksStreamPoller;
  private NormalizedLandmarkListVectorPacket multiHandLandmarksPacket;

  private const string multiPalmDetectionsStream = "multi_palm_detections";
  private OutputStreamPoller<List<Detection>> multiPalmDetectionsStreamPoller;
  private DetectionVectorPacket multiPalmDetectionsPacket;

  private const string multiPalmRectsStream = "multi_palm_rects";
  private OutputStreamPoller<List<NormalizedRect>> multiPalmRectsStreamPoller;
  private NormalizedRectVectorPacket multiPalmRectsPacket;

  private const string multiPalmDetectionsPresenceStream = "multi_palm_detections_presence";
  private OutputStreamPoller<bool> multiPalmDetectionsPresenceStreamPoller;
  private BoolPacket multiPalmDetectionsPresencePacket;

  public override Status StartRun(SidePacket sidePacket) {
    multiHandLandmarksStreamPoller = graph.AddOutputStreamPoller<List<NormalizedLandmarkList>>(multiHandLandmarksStream).ConsumeValue();
    multiHandLandmarksPacket = new NormalizedLandmarkListVectorPacket();

    multiPalmDetectionsStreamPoller = graph.AddOutputStreamPoller<List<Detection>>(multiPalmDetectionsStream).ConsumeValue();
    multiPalmDetectionsPacket = new DetectionVectorPacket();

    multiPalmRectsStreamPoller = graph.AddOutputStreamPoller<List<NormalizedRect>>(multiPalmRectsStream).ConsumeValue();
    multiPalmRectsPacket = new NormalizedRectVectorPacket();

    multiPalmDetectionsPresenceStreamPoller = graph.AddOutputStreamPoller<bool>(multiPalmDetectionsPresenceStream).ConsumeValue();
    multiPalmDetectionsPresencePacket = new BoolPacket();

    return graph.StartRun(sidePacket);
  }

  public override void RenderOutput(WebCamScreenController screenController, TextureFrame textureFrame) {
    var multiHandTrackingValue = FetchNextMultiHandTrackingValue();
    RenderAnnotation(screenController, multiHandTrackingValue);

    var texture = screenController.GetScreen();
    texture.SetPixels32(textureFrame.GetPixels32());

    texture.Apply();
  }

  private MultiHandTrackingValue FetchNextMultiHandTrackingValue() {
    var multiHandLandmarks = FetchNextMultiHandLandmarks();

    if (!FetchNextMultiPalmDetectionsPresence()) {
      return new MultiHandTrackingValue(multiHandLandmarks);
    }

    var multiPalmRects = FetchNextMultiPalmRects();
    var multiPalmDetections = FetchNextMultiPalmDetections();

    return new MultiHandTrackingValue(multiHandLandmarks, multiPalmDetections, multiPalmRects);
  }

  private List<NormalizedLandmarkList> FetchNextMultiHandLandmarks() {
    return FetchNextVector(multiHandLandmarksStreamPoller, multiHandLandmarksPacket, multiHandLandmarksStream);
  }

  private List<Detection> FetchNextMultiPalmDetections() {
    return FetchNextVector(multiPalmDetectionsStreamPoller, multiPalmDetectionsPacket, multiPalmDetectionsStream);
  }

  private List<NormalizedRect> FetchNextMultiPalmRects() {
    return FetchNextVector(multiPalmRectsStreamPoller, multiPalmRectsPacket, multiPalmRectsStream);
  }

  private bool FetchNextMultiPalmDetectionsPresence() {
    return FetchNext(multiPalmDetectionsPresenceStreamPoller, multiPalmDetectionsPresencePacket, multiPalmDetectionsPresenceStream);
  }

  private void RenderAnnotation(WebCamScreenController screenController, MultiHandTrackingValue value) {
    // NOTE: input image is flipped
    GetComponent<MultiHandTrackingAnnotationController>().Draw(
      screenController.transform, value.MultiHandLandmarks, value.MultiPalmDetections, value.MultiPalmRects, true);
  }
}
