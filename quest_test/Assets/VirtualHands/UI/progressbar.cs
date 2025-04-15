using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class progressbar : MonoBehaviour
{
    private float _fullWidth;
    private float _startPos;

    private GameObject _textLeftGo;
    private GameObject _textRightGo;

    private float _debugProgress;

    public void SetTextLeft(string text)
    {
        Debug.Log("updating text: " + text);
        if (_textLeftGo != null)
        {
            TextMesh tm = _textLeftGo.GetComponent<TextMesh>();
            if (tm != null)
            {
                tm.text = text;
            }
        }
    }

    public void SetTextRight(string text)
    {
        Debug.Log("updating text: " + text);
        if (_textRightGo != null)
        {
            TextMesh tm = _textRightGo.GetComponent<TextMesh>();
            if (tm != null)
            {
                tm.text = text;
            }
        }
    }

    void Start()
    {
        
    }

    public void Inititalize()
    {
        _textLeftGo = transform.parent.Find("Status").gameObject;
        _textRightGo = transform.parent.Find("Speed").gameObject;
        _fullWidth = transform.localScale.z;
        _startPos = transform.localPosition.z;
        _debugProgress = 0.0f;
        
    }

    public void UpdateProgress(float progress)
    {
        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, progress * _fullWidth);
        float zpos = (_startPos + (_fullWidth*10.0f) / 2.0f) - progress * ((_fullWidth*10.0f) / 2.0f);
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, zpos);
    }

    void FixedUpdate()
    {
        /*_debugProgress += 0.001f;
        if (_debugProgress >= 1.0f)
        {
            _debugProgress = 0.0f;
        }

        UpdateProgress(_debugProgress);
        */
    }
}
