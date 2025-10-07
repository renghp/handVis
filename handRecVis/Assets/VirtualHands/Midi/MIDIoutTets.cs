using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Linq;
using System.Text;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;

using System.Threading;


public class MIDIoutTets : MonoBehaviour
{
    public NoteOnEvent e1;
    public NoteOffEvent e2;
    public OutputDevice keyboard;
    private static Playback _playback;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("running midi out");
        keyboard =  OutputDevice.GetByName("Nord Electro 5 MIDI"); 
        //keyboard.EventSent += OnEventSent;

        e2 = new NoteOffEvent((SevenBitNumber)60, (SevenBitNumber)60);

        var midiFile = MidiFile.Read("./Assets/midi-files/under-the-sea.mid");
        _playback = midiFile.GetPlayback(keyboard);
        _playback.NotesPlaybackStarted += OnNotesPlaybackStarted;
        _playback.Start();

        SpinWait.SpinUntil(() => !_playback.IsRunning);

        Console.WriteLine("Playback stopped or finished.");

        keyboard.Dispose();
        _playback.Dispose(); 
    }

    private static void OnNotesPlaybackStarted(object sender, NotesEventArgs e)
    {
        //if (e.Notes.Any(n => n.NoteName == Melanchall.DryWetMidi.MusicTheory.NoteName.B))
        //    _playback.Stop();
    } 


    private void OnEventSent(object sender, MidiEventSentEventArgs e)
    {
        var midiDevice = (MidiDevice)sender;
        Debug.Log($"Event sent to '{midiDevice.Name}' at {DateTime.Now}: {e.Event}");
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)){
            _playback.Stop();
        }
        
    }
}
