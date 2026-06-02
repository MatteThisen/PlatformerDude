using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class SplineBoxBehaviour : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;

    [SerializeField] private Instruments thisInstrument;
    [SerializeField] private Notes thisNote;

    private enum Instruments
    {
        None,
        StringSolo
    }

    private enum Notes
    {
        None,
        A2,
        AS2,
        B2,
        C2,
        CS2,
        D2,
        DS2,
        E2,
        F2,
        FS2,
        G2,
        GS2,
        A3,
        AS3,
        B3,
        C3,
        CS3,
        D3,
        DS3,
        E3,
        F3,
        FS3,
        G3,
        GS3,
        A4,
        AS4,
        B4,
        C4,
        CS4,
        D4,
        DS4,
        E4,
        F4,
        FS4,
        G4,
        GS4,
        A5,
        AS5,
        B5,
        C5,
        CS5,
        D5,
        DS5,
        E5,
        F5,
        FS5,
        G5,
        GS5,
        A6,
        AS6,
        B6,
        C6,
        CS6,
        D6,
        DS6,
        E6,
        F6,
        FS6,
        G6,
        GS6
    }

    /*public List<AK.Wwise.Event> StartEvents = new List<AK.Wwise.Event> { };
    public List<AK.Wwise.Event> StopEvents = new List<AK.Wwise.Event> { };*/

    private void Awake()
    {
        float splineLength = splineContainer.CalculateLength();
    }
    
   /* public void PlayNote()
    {
        int noteIndex = (int)thisNote;

        if (noteIndex < StartEvents.Count)
        {
            StartEvents[noteIndex].Post(gameObject);
            Debug.Log($"Played note {thisNote} on {thisInstrument}");
        }
    }

    public void StopNote()
    {
        int noteIndex = (int)thisNote;
        if (noteIndex < StopEvents.Count)
        {
            StopEvents[noteIndex].Post(gameObject);
            Debug.Log($"Stopped note {thisNote} on {thisInstrument}");
        }
    }*/
}
