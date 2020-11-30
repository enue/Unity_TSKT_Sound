using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public class Decibel
    {
        static public float ToDecibel(float normalizedValue, float min = -80f)
        {
            // https://techblog.kayac.com/linear-vs-decibel
            var decibel = min;
            if (normalizedValue > 0f)
            {
                decibel = 20f * Mathf.Log10(normalizedValue);
                decibel = Mathf.Max(decibel, min);
            }
            return decibel;
        }
    }
}
