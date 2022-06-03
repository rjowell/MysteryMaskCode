using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Purchasing;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using VoxelBusters.ReplayKit;
using UnityEngine.Video;
using TMPro;
using com.adjust.sdk;
using Newtonsoft.Json.Linq;
using System.Linq;
//using Firebase.Auth;

//Fields to load individual categories of masks.
[Serializable]
public class CategoryItem
{
    public string Name;
    public string Location;
    public string Price;
    public List<string> Names;
    public List<string> FolderNames;
    public string IOS_Store_ID;
    public string Android_Store_ID;
}

[Serializable]
public class CelebList
{
    public List<CategoryItem> Categories;
}

public class CelebHints
{
    public List<string> Hints;
}

public class PurchaseObject
{
    public string purchases;
}



public class MainCanvasScript : MonoBehaviour, IStoreListener
{

    WebClient wc;
    CelebList nameList;
    private static IStoreController storeController;
    public GameObject categoryButton, burgerMenu, eMailWindow, loginWindowErrorText, standbyLogo, timer, showDirectionsButton, restorePurchasesButton, eMailRestoreButton, menuBurgerButton, directionsPanel, directionsButton, recLogo, currentButton, topBar, mainScreen, ready321anim, hintObject, errorWindow, winLoseButton, maskSelectionPanel, categoryBackground, pictureContent, startButton, categoryContainer, categoryPanel, loadingWindow, startBlackPanel, startWindow, pictureFrameObject, pictureFrameContainer, videoPanel;
    public Image faceDetect, faceImage, maskImage;
    public GameObject greenFace, redFace;
    public TextMeshProUGUI topBarName, winLoseText, eMailField, eMailErrorText, eMailWindowText;
    List<GameObject> categoryButtons;
    public Button burgerMenuSubmitButton;
    public ARSessionOrigin arOrigin;
    public AudioSource faceWaitMusic, drumRoll, faceGuessMusic, loseSound, winSound, beepSound;
    ARHumanBodyManager bodyManager;
    NativeArray<XRHumanBodyPose2DJoint> joints;
    float[] lastFacePosition;
    string[,] questionList;
   

    public bool startButtonPressed, menuIsShowing, panelIsRestore,isPurchaseRestore;
    string currentFileName, currentCategory;
    VideoPlayer player;

    // Start is called before the first frame update


    

    //Controls Burger Menu
    public void showMenu()
    {
        menuIsShowing = !menuIsShowing;
        directionsButton.SetActive(menuIsShowing);
        restorePurchasesButton.SetActive(menuIsShowing);
        eMailRestoreButton.SetActive(menuIsShowing);
    }

    
        

    
    //Apple method to restore purchases.
    public void RestorePurchases()
    {
        isPurchaseRestore = true;
        extensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions(result => {
            if (result)
            {
                Debug.Log("Restore Success");

                // This does not mean anything was restored,
                // merely that the restoration process succeeded.
            }
            else
            {
                Debug.Log("Restore Failed");
                // Restoration failed.
            }
        });
    }

   

    void Start()
    {
        wc = new WebClient();
        player = videoPanel.transform.GetChild(1).gameObject.GetComponent<VideoPlayer>();
        
        menuIsShowing = false;
        categoryButtons = new List<GameObject>();
        player.isLooping = false;
        //Set action once video player has finished playing
        player.loopPointReached += (VideoPlayer vp) =>
        {
            CloseVideoWindow();
        };
        //Set up video replay
        ReplayKitManager.Initialise();
        ReplayKitManager.SetMicrophoneStatus(true);
        directionsButton.SetActive(menuIsShowing);

       
       //Set a player pref to ensure that Restore Purchases can only happen once.
       if(!PlayerPrefs.HasKey("purchased_categories"))
        {
            PlayerPrefs.SetString("purchased_categories", "");
        }

       if(PlayerPrefs.HasKey("Purchases_Restored")==true)
        {
            restorePurchasesButton.SetActive(false);
        }
       else
        {
            restorePurchasesButton.SetActive(menuIsShowing);
        }

        
        
       
        bodyManager = arOrigin.GetComponent<ARHumanBodyManager>();
        timer.GetComponent<TimerController>().SetTime(30);
        timer.GetComponent<TimerController>().timeIsUp += () => EndGame(false);
        recLogo.SetActive(false);


#if UNITY_IOS
        InitAdjust("7ffqlhe5pn28");
#endif

        lastFacePosition = new float[2];
        winLoseText.gameObject.SetActive(false);
       
    }

    public void CloseVideoWindow()
    {
        errorWindow.GetComponent<ErrorControllerScript>().ShowError("Do you want to save this video?", true, false);
    }

    private void InitAdjust(string adjustAppToken)
    {
        var adjustConfig = new AdjustConfig(
            adjustAppToken,
            AdjustEnvironment.Production, // AdjustEnvironment.Sandbox to test in dashboard
            true
        );
        adjustConfig.setLogLevel(AdjustLogLevel.Info); // AdjustLogLevel.Suppress to disable logs
        adjustConfig.setSendInBackground(true);
        new GameObject("Adjust").AddComponent<Adjust>(); // do not remove or rename
                                                         // Adjust.addSessionCallbackParameter("foo", "bar"); // if requested to set session-level parameters
                                                         //adjustConfig.setAttributionChangedDelegate((adjustAttribution) => {
                                                         //  Debug.LogFormat("Adjust Attribution Callback: ", adjustAttribution.trackerName);
                                                         //});
        Adjust.start(adjustConfig);
    }


    public void StartResetGame()
    {
        StartCoroutine(ResetGame());
    }

    public IEnumerator ResetGame()
    {
        ready321anim.SetActive(false);
        maskSelectionPanel.GetComponent<AudioSource>().Play();
        categoryPanel.GetComponent<Animator>().ResetTrigger("ShowCategories");
        startButtonPressed = false;
        categoryBackground.SetActive(true);
        categoryPanel.GetComponent<Animator>().SetTrigger("ShowCategories");
        //Wait until mask screen has reappeared
        yield return new WaitUntil(() => categoryPanel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("ShowCategories") == true);
        directionsButton.SetActive(true);
        burgerMenu.SetActive(true);
        menuIsShowing = true;
        //showMenu();
        videoPanel.GetComponent<Animator>().ResetTrigger("ShowPlayer");
        videoPanel.GetComponent<Animator>().SetTrigger("HidePlayer");
        videoPanel.GetComponent<Animator>().keepAnimatorControllerStateOnDisable = true;
        videoPanel.SetActive(false);

        player.Stop();
        player.url = "";
        timer.GetComponent<TimerController>().SetTime(30);
        maskSelectionPanel.GetComponent<Animator>().SetTrigger("ShowMaskSelection");
        

    }


    public void EndGame(bool winner)
    {
        StartCoroutine(EndGameSequence(winner));
    }

    public IEnumerator EndGameSequence(bool winner)
    {

        faceGuessMusic.Stop();
        if (winner == true)
        {
            winSound.Play();
        }
        else
        {
            loseSound.Play();
        }




        recLogo.SetActive(false);
        standbyLogo.SetActive(true);
        //winLoseButton.SetActive(false);
        timer.GetComponent<TimerController>().StartStopTimer(false);
        winLoseText.text = winner == true ? "YOU\nWIN" : "YOU\nLOSE";
        winLoseText.color = winner == true ? new Color(0f, 255f, 0f) : new Color(255f, 0f, 0f);   
        winLoseText.gameObject.SetActive(true);
        winLoseText.GetComponent<Animator>().SetTrigger("StartAnimation");

        yield return new WaitUntil(() => winLoseText.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("WinLoseAnimation") == true && winLoseText.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f);
        Application.onBeforeRender -= BodyBeforeRender;
        mainScreen.GetComponent<Animator>().SetTrigger("HideElements");
        maskImage.GetComponent<Animator>().SetTrigger("HideFace");
        //topBar.GetComponent<Animator>().SetTrigger("HideTopBar");
        //hintObject.GetComponent<Animator>().SetTrigger("HideHints");
        winLoseText.gameObject.SetActive(false);
        yield return new WaitUntil(() => mainScreen.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("MainScreenExit") == true && mainScreen.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.2f);
       
       

        videoPanel.SetActive(true);
        ReplayKitManager.StopRecording((fileName, error) => {

            currentFileName = fileName;
            //Debug.Log("File Name " + currentFileName);
            if (string.IsNullOrEmpty(error))
            {

                player.url = fileName;
                videoPanel.GetComponent<Animator>().SetTrigger("ShowPlayer");
                
               

            }
            else
            {
                errorWindow.GetComponent<ErrorControllerScript>().ShowError("Video Error", false, false);

            }


        });

    }


    public void StartGame()
    {

        
        StartCoroutine(StartSequence());
    }

    IEnumerator StartSequence()
    {

        startButtonPressed = true;
        greenFace.SetActive(false);
        redFace.SetActive(false);
        Debug.Log("step 1");
        if (errorWindow.GetComponent<ErrorControllerScript>().showedCaptureMessage == false)
        {
            errorWindow.GetComponent<ErrorControllerScript>().ShowError("In order to record video of the game, please click 'Allow' on the next window.", false, true);
            yield return new WaitUntil(() => errorWindow.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("ErrorWindowHiding") == true);
        }
        Debug.Log("step 2");
        faceWaitMusic.Stop();
        drumRoll.Play();
        ReplayKitManager.StartRecording();
        Debug.Log("step 3");
        yield return new WaitUntil(() => ReplayKitManager.IsRecording());
        //winLoseButton.SetActive(true);
        mainScreen.GetComponent<Animator>().SetTrigger("ShowElements");
        
        yield return new WaitUntil(() => mainScreen.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("MainScreenEnter") == true && mainScreen.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f);
        ready321anim.SetActive(true);
       
        yield return new WaitUntil(() => ready321anim.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Ready321Animation") && ready321anim.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f);
        ready321anim.GetComponent<Animator>().ResetTrigger("StartCountdown");
        ready321anim.SetActive(false);
        standbyLogo.SetActive(false);
        recLogo.SetActive(true);
        maskImage.GetComponent<Animator>().SetTrigger("ShowFace");
        timer.GetComponent<TimerController>().StartStopTimer(true);
        yield return new WaitUntil(() => maskImage.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("ShowFace") == true);
        drumRoll.Stop();
        faceGuessMusic.Play();
        Application.onBeforeRender += BodyBeforeRender;
    }


    // Update is called once per frame
    void Update()
    {
      
        joints = bodyManager.GetHumanBodyPose2DJoints(Allocator.Temp);

        if (!joints.IsCreated)
        {
            greenFace.SetActive(false);
            redFace.SetActive(true);

            return;
        }

        if (!startButtonPressed)
        {
           greenFace.SetActive(joints[0].tracked);
           redFace.SetActive(!joints[0].tracked);
        }
            
        
    }

    public void ShowCategoriesFromMaskMenu()
    {
        categoryBackground.SetActive(true);
        categoryPanel.GetComponent<Animator>().SetTrigger("ShowCategories");
    }

    //Function to download selected face from server and apply it to the Image texture
    IEnumerator SetFaceMaterial(string faceName)
    {
        loadingWindow.SetActive(true);
        burgerMenu.SetActive(false);
        maskSelectionPanel.GetComponent<Animator>().ResetTrigger("ShowMaskSelection");
        
        UnityWebRequest getPortrait = UnityWebRequestTexture.GetTexture("https://storage.googleapis.com/mask_game_data_1/" + currentCategory + "/" + faceName + "/face.png");
        yield return getPortrait.SendWebRequest();
       
        Texture2D celebFaceTexture = DownloadHandlerTexture.GetContent(getPortrait);
        maskImage.sprite = Sprite.Create(celebFaceTexture, new Rect(0, 0, celebFaceTexture.width, celebFaceTexture.height), new Vector2(0.5f, 0.5f));
        string json = "";
        try
        {
            json = wc.DownloadString("https://storage.googleapis.com/mask_game_data_1/" + currentCategory + "/" + faceName + "/hints.json");

        }
        catch (WebException we)
        {
            Debug.Log(we.Message);
            //TODO Show Error Window
        }
        
        hintObject.GetComponent<HintController>().SetHintText(JsonUtility.FromJson<CelebHints>(json).Hints.ToArray());

        loadingWindow.SetActive(false);
        //maskSelectionPanel.GetComponent<Animator>().ResetTrigger("ShowMaskSelection");
        maskSelectionPanel.GetComponent<Animator>().SetTrigger("HideMaskSelection");
        maskSelectionPanel.GetComponent<AudioSource>().Stop();
        faceWaitMusic.Play();
        directionsButton.SetActive(false);
        


    }

    public void StartSetFaceMaterial(string name, string folderName, Sprite celebPortrait)
    {
        
        faceImage.sprite = celebPortrait;
        topBarName.text = name;
       
        StartCoroutine(SetFaceMaterial(folderName));

    }

    public void StartButton()
    {
        Debug.Log("login button");
        
        StartCoroutine(StartButtonFunction());
    }

    // Start Button on home screen
    public IEnumerator StartButtonFunction()
    {

        yield return null;
        startButton.GetComponent<AudioSource>().Play();
        yield return new WaitUntil(()=> startButton.GetComponent<AudioSource>().time >= 1.0f);
        startButton.GetComponent<AudioSource>().Stop();
        loadingWindow.SetActive(true);
        StartCoroutine(LoadCategories());
        

        
        
    }

    

    IEnumerator StartButtonSequence()
    {
        
        yield return new WaitUntil(() => startWindow.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("StartScreenHide") == true && startWindow.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f);
        
    }



    public void StartPurchase(GameObject button)
    {
        currentButton = button;
        storeController.InitiatePurchase(button.GetComponent<CategoryButtonController>().identifier);
    }

    public bool SaveRecording()
    {
        
        NativeGallery.SaveVideoToGallery(System.IO.File.ReadAllBytes(player.url), "Mystery Mask", player.url);
        //Debug.Log("Record Ended");
        return true;
    }

   void BodyBeforeRender()
    {
     
        joints = bodyManager.GetHumanBodyPose2DJoints(Allocator.Temp);
        
        if(joints[0].position.x == 0 || joints[0].position.y == 0)
        {
            Debug.Log("Step 1a");
            maskImage.transform.position = new Vector3(Screen.width * lastFacePosition[0], Screen.height * lastFacePosition[1], 0);
            Debug.Log("Step 1b");
        }
        else
        {
            Debug.Log("Step 1c");
            lastFacePosition[0] = joints[0].position.x;
            Debug.Log("Step 1d");
            lastFacePosition[1] = joints[0].position.y > 0.73f?0.73f:joints[0].position.y;
            Debug.Log("Step 1e");
            maskImage.transform.position = new Vector3(Screen.width * joints[0].position.x, Screen.height * lastFacePosition[1], 0);
            Debug.Log("Step 1f");
        }
        
        //Debug.Log("Position "+joints[0].position);
    }



    public void Pause()
    {
        player.Pause();
    }

    public void Stop()
    {
        player.Stop();
    }

    public void Play()
    {
        player.Play();
    }



    public void StartLoadCategoryPics(string category, List<string> names, List<string> folders)
    {
        currentCategory = category;
        StartCoroutine(LoadCategoryPics(category,names,folders));
    }

    IEnumerator LoadCategoryPics(string category, List<string> names, List<string> folders)
    {
        yield return null;
        Debug.Log("Routine Started");
        
        loadingWindow.SetActive(true);
        if (pictureContent.transform.childCount != 0)
        {
            foreach (Transform child in pictureContent.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }


     

        GameObject currentFrames = null;
        for (int x = 0; x < names.Count; x++)
        {
            
            UnityWebRequest getPortrait = UnityWebRequestTexture.GetTexture("https://storage.googleapis.com/mask_game_data_1/" + category + "/" + folders[x] + "/portrait.png");
            yield return getPortrait.SendWebRequest();
            Texture2D currentPortrait = DownloadHandlerTexture.GetContent(getPortrait);
            if (x % 2 == 0)
            {
               
                currentFrames = Instantiate(pictureFrameObject, pictureFrameContainer.transform);
                currentFrames.GetComponent<FrameButtonScript>().leftName.text = names[x];
                currentFrames.GetComponent<FrameButtonScript>().leftFolderName = folders[x];
                currentFrames.GetComponent<FrameButtonScript>().leftMaskName = names[x];
                currentFrames.GetComponent<FrameButtonScript>().leftButton.image.sprite = Sprite.Create(currentPortrait, new Rect(0, 0, currentPortrait.width, currentPortrait.height), new Vector2(0.5f, 0.5f));
               Debug.Log("Left Done "+currentFrames.GetComponent<FrameButtonScript>().leftName.text);

            }
            if (x == folders.Count - 1 && x % 2 == 0)
            {
                Debug.Log("Done: " + names[x]);
                currentFrames.GetComponent<FrameButtonScript>().right.SetActive(false);
            }
            else
            {
                Debug.Log("Right: " + names[x]);
                currentFrames.GetComponent<FrameButtonScript>().rightName.text = names[x];
                currentFrames.GetComponent<FrameButtonScript>().rightFolderName = folders[x];
                currentFrames.GetComponent<FrameButtonScript>().rightMaskName = names[x];
                currentFrames.GetComponent<FrameButtonScript>().rightButton.image.sprite = Sprite.Create(currentPortrait, new Rect(0, 0, currentPortrait.width, currentPortrait.height), new Vector2(0.5f, 0.5f));
                Debug.Log("Right Done");
            }
        }
        GameObject bufferFrame = Instantiate(pictureFrameObject, pictureFrameContainer.transform);
        bufferFrame.GetComponent<FrameButtonScript>().left.SetActive(false);
        bufferFrame.GetComponent<FrameButtonScript>().right.SetActive(false);
        categoryPanel.GetComponent<Animator>().SetTrigger("HideCategories");
        
        
        yield return new WaitUntil(() => categoryPanel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.2f && categoryPanel.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("HideCategories"));
        categoryBackground.SetActive(false);
        loadingWindow.SetActive(false);
        maskSelectionPanel.GetComponent<Animator>().SetTrigger("ShowMaskSelection");
       
    }
        
        

    IEnumerator LoadCategories()
    {

        
        yield return null;
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        try
        {
            
            string json = wc.DownloadString("https://storage.googleapis.com/mask_game_data_1/list.json");
            
            nameList = JsonUtility.FromJson<CelebList>(json);
            foreach (CategoryItem item in nameList.Categories)
            {
                Debug.Log(item.Name +" "+item.IOS_Store_ID);
                builder.AddProduct(item.IOS_Store_ID, ProductType.NonConsumable);


            }

            UnityPurchasing.Initialize(this, builder);


        }
        catch (WebException we)
        {
            Debug.Log("Web Error " + we.Message);
            //currentClapboard.GetComponent<ErrorController>().ShowError("Connection Error.", false, false);

        }
    }

    IEnumerator GetCategoryPic(string currentCategory, GameObject button)
    {
        UnityWebRequest getPortrait = UnityWebRequestTexture.GetTexture("https://storage.googleapis.com/mask_game_data_1/Category_Pics/" + currentCategory+".png");
        yield return getPortrait.SendWebRequest();
        Texture2D celebFaceTexture = DownloadHandlerTexture.GetContent(getPortrait);
        button.GetComponent<Image>().sprite = Sprite.Create(celebFaceTexture, new Rect(0, 0, celebFaceTexture.width, celebFaceTexture.height), new Vector2(0.5f, 0.5f));
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log(error);
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs purchaseEvent)
    {
        if(isPurchaseRestore == false)
        {
            //Debug.Log("Purchase Success " + purchaseEvent.purchasedProduct.definition.id);
            currentButton.GetComponent<CategoryButtonController>().PurchaseStatus(true);
            
        }
        else
        {
            foreach (GameObject currentButton in categoryButtons)
            {

                if (currentButton.GetComponent<CategoryButtonController>().categoryName != "Free" && purchaseEvent.purchasedProduct.definition.id.Split('.')[1] == currentButton.GetComponent<CategoryButtonController>().identifier.Split('.')[1])
                {
                    currentButton.GetComponent<CategoryButtonController>().isPurchased = true;
                    currentButton.GetComponent<CategoryButtonController>().priceName.text = "";

                }
            }
            PlayerPrefs.SetInt("Purchases_Restored", 1);
            restorePurchasesButton.SetActive(false);
            errorWindow.GetComponent<ErrorControllerScript>().ShowError("Purchases Restored", false, false);
        }

        return PurchaseProcessingResult.Complete;

    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.Log("Purchase Failed");
        Debug.Log(failureReason);
    }


    IExtensionProvider extensionProvider;
        

    IEnumerator ShowCategories()
    {
        yield return new WaitUntil(()=> startWindow.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("StartScreenHide") == true && startWindow.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.5f);
        categoryPanel.GetComponent<Animator>().SetTrigger("ShowCategories");
        directionsPanel.GetComponent<Animator>().SetTrigger("ShowDirections");
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {

        extensionProvider = extensions;


        //extensionProvider.GetExtension<IAppleExtensions>().RestoreTransactions(result=> { });
       



        storeController = controller;
        foreach (CategoryItem item in nameList.Categories)
        {
            GameObject currentButton = Instantiate(categoryButton, categoryContainer.transform);
            categoryButtons.Add(currentButton);
            Debug.Log("Name Is " + item.Name);
            currentButton.GetComponent<CategoryButtonController>().categoryDisplayName.text = item.Name.ToUpper();
            currentButton.GetComponent<CategoryButtonController>().categoryName = item.Location;
            StartCoroutine(GetCategoryPic(item.Location,currentButton));
            currentButton.GetComponent<CategoryButtonController>().identifier = item.IOS_Store_ID;
            currentButton.GetComponent<CategoryButtonController>().folderNames = item.FolderNames;
            currentButton.GetComponent<CategoryButtonController>().maskNames = item.Names;
            Debug.Log("Load Done");
            if (storeController.products.WithID(item.IOS_Store_ID).hasReceipt == false && item.Name != "Free")
            {
                currentButton.GetComponent<CategoryButtonController>().isPurchased = false;
                currentButton.GetComponent<CategoryButtonController>().priceName.text = "$" + (Int32.Parse(item.Price) / 100).ToString() + "." + (Int32.Parse(item.Price) % 100).ToString();
            }
            else
            {
                currentButton.GetComponent<CategoryButtonController>().isPurchased = true;
                currentButton.GetComponent<CategoryButtonController>().priceName.text = "";
            }
        }
        loadingWindow.SetActive(false);
        startButton.SetActive(false);
        startBlackPanel.SetActive(false);
        startWindow.GetComponent<Animator>().SetTrigger("StartScreenLeave");
        StartCoroutine(ShowCategories());
        //categoryButtons.GetComponent<Animator>().SetTrigger("ShowCategories");
        //categoryScrollContent.GetComponent<Animator>().SetTrigger("ShowStars");
        //sidewalkImage.GetComponent<Animator>().SetTrigger("Drop");
        //StartCoroutine(showCategoryPics());
    }
}
