using System;
using System.Linq;
using System.Text;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

class MIDIDevice : MonoBehaviour, KeyboardVisualizer.KeyboardDataProvider
{
    public interface MidiDataProvider
     {
          List<HandSequence.SerializableNoteEvent> GetMidiData();
     }

     public HashSet<int> GetNotesDown(){
        return notesDown;
     }

    private static IInputDevice _inputDevice;
    public HashSet<int> notesDown;

    public event Action<NoteEvent> OnNoteUpdate;

    void Start()
    {
        notesDown = new HashSet<int>();
        Debug.Log("Looking for MIDI device, change midi device name in this file");

        var devices = InputDevice.GetAll();
        foreach (var device in devices)
        {
            Debug.Log(device.Name);
        }
        _inputDevice = InputDevice.GetByName("Nord Electro 5 MIDI");
        _inputDevice.EventReceived += OnEventReceived;
        _inputDevice.StartEventsListening();

        Debug.Log("Input device is listening for events");
        //Console.ReadKey();

        //Debug.Log("sa");
    }

    private void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
    {
        var midiDevice = (MidiDevice)sender;
        var thisNoteEvent = e.Event;
        var number = (int)((NoteEvent)thisNoteEvent).NoteNumber;
        if(thisNoteEvent.EventType == MidiEventType.NoteOn){
            //Debug.Log("add number");
            notesDown.Add(number);
            OnNoteUpdate?.Invoke((NoteEvent)thisNoteEvent);
        }

        if(thisNoteEvent.EventType == MidiEventType.NoteOff){
            //Debug.Log("remove number");
            notesDown.Remove(number);
            OnNoteUpdate?.Invoke((NoteEvent)thisNoteEvent);
        }

        //Debug.Log($"Notes Down: [{string.Join(", ", notesDown)}]");


        //Debug.Log($"Event received from '{midiDevice.Name}' at {DateTime.Now}: {number}");
    }

    void OnApplicationQuit(){
        (_inputDevice as IDisposable)?.Dispose();
    }

}
