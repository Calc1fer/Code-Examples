using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*Low pass filter using the Butterworth model*/
public class LowPassFilter : MonoBehaviour
{
    //Variables - Customisable for the user
    private float cutoffFrequency = 10f;   //Hz
    private float sampleRate;       //Rate at which to sample audioData. Use the output samples in AudioSettings
    private float resonance;        //Influences the amplitude where the frequencies are cutoff, changing the shape of the filter. Set to default of 1
    private float c, a1, a2, a3, b1, b2;    //Coefficients which will influence the audio data samples passed into the filter

    private float x1, x2, y1, y2;
    
    //Function for setting the variables from external class
    public LowPassFilter(float cutoffFrequency, float resonance, float sampleRate)
    {
        this.cutoffFrequency = cutoffFrequency;
        this.resonance = resonance;
        this.sampleRate = sampleRate;
        
        CalculateCoefficients();
    }
    
    //Calculate the coefficients
    private void CalculateCoefficients()
    {
        float w0 = 2f * Mathf.PI * cutoffFrequency / sampleRate;    //Angular frequency - Speed at which a sinusoidal wave completes one cycle
        float alpha = Mathf.Sin(w0) / (2f * resonance);             //Related to the smoothness(lower value) or sharpness(higher value) of the filter

        float cosw0 = Mathf.Cos(w0);    //Cosine of the angular frequency. Will produce a value between -1 and 1.
        
        b1 = 1f - cosw0;
        b2 = (1f - cosw0) / 2f;
        a1 = -2f * cosw0;
        a2 = 1f - alpha;

        //Scale the calculated coefficients to normalise them
        //(Preserve the original signal level without causing any changes to the amplitude)
        c = 1f + alpha;
        a1 /= c;
        a2 /= c;
        b1 /= c;
        b2 /= c;
    }
    
    //Function for filtering
    public float Filter(float input)
    {
        //Second order difference calculation
        //Input - input audio sample data
        //Output - output audio sample data after filter application
        float output = b1 * input + b2 * x1 + b1 * x2 - a1 * y1 - a2 * y2;

        //x1, x2 - Previous input samples
        //y1, y2 - Previous output samples
        x2 = x1;
        x1 = input;
        y2 = y1;
        y1 = output;

        //return the filtered output
        return output;
    }
    
    //Setter for dynamically changing the level of filtering 
    public void SetCutoffFrequency(float val)
    {
        //Add or subtract a factor for determining level of filtering
        cutoffFrequency = val;
        
        //Recalculate coefficients to update the filtering data
        CalculateCoefficients();
    }
}
