using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.LogFormat("lalala: {0}", LightProbeProxyVolume.isFeatureSupported);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
