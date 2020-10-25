using Mediapipe;
using System.Collections.Generic;
using UnityEngine;

public class HandTrackingGraph : DemoGraph {
  private const string handednessStream = "handedness";
  private OutputStreamPoller<ClassificationList> handednessStreamPoller;
  private ClassificationListPacket handednessPacket;

  private const string handRectStream = "hand_rect";
  private OutputStreamPoller<NormalizedRect> handRectStreamPoller;
  private NormalizedRectPacket handRectPacket;

  private const string handLandmarksStream = "hand_landmarks";
  private OutputStreamPoller<NormalizedLandmarkList> handLandmarksStreamPoller;
  private NormalizedLandmarkListPacket handLandmarksPacket;

  private const string palmDetectionsStream = "palm_detections";
  private OutputStreamPoller<List<Detection>> palmDetectionsStreamPoller;
  private DetectionVectorPacket palmDetectionsPacket;

  private const string palmDetectionsPresenceStream = "palm_detections_presence";
  private OutputStreamPoller<bool> palmDetectionsPresenceStreamPoller;
  private BoolPacket palmDetectionsPresencePacket;

  public override Status StartRun(SidePacket sidePacket) {
    handednessStreamPoller = graph.AddOutputStreamPoller<ClassificationList>(handednessStream).ConsumeValue();
    handednessPacket = new ClassificationListPacket();

    handRectStreamPoller = graph.AddOutputStreamPoller<NormalizedRect>(handRectStream).ConsumeValue();
    handRectPacket = new NormalizedRectPacket();

    handLandmarksStreamPoller = graph.AddOutputStreamPoller<NormalizedLandmarkList>(handLandmarksStream).ConsumeValue();
    handLandmarksPacket = new NormalizedLandmarkListPacket();

    palmDetectionsStreamPoller = graph.AddOutputStreamPoller<List<Detection>>(palmDetectionsStream).ConsumeValue();
    palmDetectionsPacket = new DetectionVectorPacket();

    palmDetectionsPresenceStreamPoller = graph.AddOutputStreamPoller<bool>(palmDetectionsPresenceStream).ConsumeValue();
    palmDetectionsPresencePacket = new BoolPacket();

    return graph.StartRun(sidePacket);
  }

  public override void RenderOutput(WebCamScreenController screenController, TextureFrame textureFrame) {
    var handTrackingValue = FetchNextHandTrackingValue();
    RenderAnnotation(screenController, handTrackingValue);

    var texture = screenController.GetScreen();
    texture.SetPixels32(textureFrame.GetPixels32());

    texture.Apply();
  }

  private HandTrackingValue FetchNextHandTrackingValue() {
    var handedness = FetchNextHandedness();
    var handLandmarks = FetchNextHandLandmarks();
    var handRect = FetchNextHandRect();

    if (!FetchNextPalmDetectionsPresence()) {
      return new HandTrackingValue(handedness, handLandmarks, handRect);
    }

    var palmDetections = FetchNextPalmDetections();

    return new HandTrackingValue(handedness, handLandmarks, handRect, palmDetections);
  }

  private ClassificationList FetchNextHandedness() {
    return FetchNext(handednessStreamPoller, handednessPacket, handednessStream);
  }

  private NormalizedRect FetchNextHandRect() {
    return FetchNext(handRectStreamPoller, handRectPacket, handRectStream);
  }

  private NormalizedLandmarkList FetchNextHandLandmarks() {
    return FetchNext(handLandmarksStreamPoller, handLandmarksPacket, handLandmarksStream);
  }

  private bool FetchNextPalmDetectionsPresence() {
    return FetchNext(palmDetectionsPresenceStreamPoller, palmDetectionsPresencePacket, palmDetectionsPresenceStream);
  }

  private List<Detection> FetchNextPalmDetections() {
    return FetchNextVector(palmDetectionsStreamPoller, palmDetectionsPacket, palmDetectionsStream);
  }

  private void RenderAnnotation(WebCamScreenController screenController, HandTrackingValue value) {
    // NOTE: input image is flipped
    GetComponent<HandTrackingAnnotationController>().Draw(
      screenController.transform, value.Handedness, value.HandLandmarkList, value.HandRect, value.PalmDetections, true);
  }
}
