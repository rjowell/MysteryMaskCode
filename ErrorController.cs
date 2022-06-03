using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ErrorControllerScript : MonoBehaviour
{

    public Button yesButton, noButton, okButton;
    public TextMeshProUGUI errorMessage;
    public bool isSave, showedCaptureMessage, isCapPermission;

    // Start is called before the first frame update


    public void DismissError()
    {
        GetComponent<Animator>().SetTrigger("HideError");
        if (isCapPermission == true)
        {
            showedCaptureMessage = true;
        }
        if (isSave == true)
        {
            Debug.Log("It's Not True");
            isSave = false;
            transform.parent.gameObject.GetComponent<MainCanvasScript>().StartResetGame();
            //GetComponent<Animator>().SetTrigger("showClapBoard");
        }
    }

    public void ShowError(string message,bool isSaveVideo,bool isCapturePermission)
    {
        errorMessage.text = message;
        isCapPermission = isCapturePermission;
        isSave = isSaveVideo;
        yesButton.gameObject.SetActive(isSaveVideo);
        noButton.gameObject.SetActive(isSaveVideo);
        okButton.gameObject.SetActive(!isSaveVideo);
        GetComponent<Animator>().SetTrigger("ShowError");
    }

    public void StartSaveVideo()
    {
        isSave = true;
        StartCoroutine(SaveVideo());
    }

    public IEnumerator SaveVideo()
    {
        Debug.Log("Save Started");
        yield return new WaitUntil(() => gameObject.transform.parent.GetComponent<MainCanvasScript>().SaveRecording() == true);
        Debug.Log("Video Saves");
        errorMessage.text = "Video Saved To Your Library";
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        okButton.gameObject.SetActive(true);
    }

    public void SaveVideoComplete()
    {
        errorMessage.text = "Video Saved To Your Library";

        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
        okButton.gameObject.SetActive(true);
    }

    void Start()
    {
        showedCaptureMessage = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
