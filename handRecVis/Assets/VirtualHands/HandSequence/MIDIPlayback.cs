using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;



public class MIDIPlayback : MonoBehaviour
{
    private MIDIDevice.MidiDataProvider _midiDataProvider;
    private static OutputDevice  _outputDevice;
    // Start is called before the first frame update
    void Start()
    {
        if(_midiDataProvider == null){
            var foundDataProvider = SearchMidiDataProvider();
            if (foundDataProvider != null)
            {
                _midiDataProvider = foundDataProvider;
                if (_midiDataProvider is MonoBehaviour mb)
                {
                    Debug.Log($"found mididataprovider");
                }
            }else{
                Debug.LogWarning("didn't find a midi data provider for playback"); 
            }
        }
       
        _outputDevice = OutputDevice.GetByName("Nord Electro 5 MIDI");
    }

    void Update()
    {
        var data = _midiDataProvider.GetMidiData();
        if(data != null && data.Count != 0){
            foreach(var e in data){
                var ev = e.ToNoteEvent();
                _outputDevice.SendEvent(e.ToNoteEvent());
            }
        }
    }

    internal MIDIDevice.MidiDataProvider SearchMidiDataProvider()
    {
        var oldProviders = gameObject.GetComponentsInParent<MIDIDevice.MidiDataProvider>();
        foreach (var dataProvider in oldProviders)
        {
            return dataProvider;
        }

        return null;
    }

    void OnApplicationQuit(){
        _outputDevice?.Dispose();
    }
}
