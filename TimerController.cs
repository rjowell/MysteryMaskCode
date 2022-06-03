// TIMER CONTROLLER
// Controls timer in main game


using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Used to fire "You Lose" graphic if player does not guess the mask in time. 
public delegate void TimerDone();

public class TimerController : MonoBehaviour
{

    //Text of timer
    public TextMeshProUGUI timerText;

    //Sounds for the various states 
    public AudioSource beep, win, lose;
    public event TimerDone timeIsUp;

    DateTime lastTime;
    int setTime;
    bool started;
    public bool timerDone;
    // Start is called before the first frame update
    void Start()
    {
        timerDone = false;
        
    }


   

    public void StartStopTimer(bool isStart)
    {
        started = isStart;

       
    }

    public void SetTime(int time)
    {
        setTime = time;
        timerText.text = setTime.ToString();
        lastTime = DateTime.Now;
    }

    // Update is called once per frame
    void Update()
    {
        //If timer reaches 0
        if (setTime == 0 && started)
        {
           
            timeIsUp.Invoke();


        }


    


        else if (Math.Abs(DateTime.Now.Second - lastTime.Second) >= 1 && started == true)
        {
            //Debug.Log("timer started");
            lastTime = DateTime.Now;
            setTime--;
            if (setTime <= 10)
            {
                //Time numbers will flash and 10 second countdown will sound.
                GetComponent<Animator>().SetTrigger("StartFlash");
                beep.Play();
            }
            timerText.text = setTime.ToString();


        }
    }
}
