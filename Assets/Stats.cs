using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats : MonoBehaviour
{
    public static Stats stats; //A static reference to self that other objects can easily use to access the variables here.

    public Dictionary<string, int> gameStats;


    // Awake is like Start but even earlier.
    void Awake()
    {
        //If this is the first copy of Stats, set up a self-reference that other objects can use. Otherwise, destroy self to ensure there is only one.
        if (stats == null)
        {
            stats = this;
        }
        else //if (stats != this) //The sample code I found has the extra "if" on the "else", but is it really necessary...?
        {
            Destroy(gameObject);
            return;
        }

        ResetObject();


        //Don't destroy on load. That's kinda what "persistant" means.
        DontDestroyOnLoad(gameObject);
    }

    public void ResetObject()
    {
        gameStats = new Dictionary<string, int>(256);
    }

    //Short for "increment stat", this adds 1 to a stat.
    public void IncStat(string stat)
    {
        AddToStat(stat, 1);
    }

    //Increases the value of a stat, and creates it if it doesn't exist yet.
    public void AddToStat(string stat, int addBy)
    {
        if (gameStats.ContainsKey(stat))
        {
            gameStats[stat] += addBy;
        }
        else
        {
            gameStats.Add(stat, addBy);
        }
    }
}
