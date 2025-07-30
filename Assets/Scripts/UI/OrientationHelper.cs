using UnityEngine;

public static class OrientationHelper
{
    public static void SetPortraitOrientation()
    {
        Debug.Log("Setting Portrait orientation");
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = true;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }
    
    public static void SetLandscapeOrientation()
    {
        Debug.Log("Setting Landscape orientation");
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
    }
}