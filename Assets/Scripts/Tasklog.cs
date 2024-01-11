using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tasklog 
{
    float inittimestamp = 0;
    float endtimestamp = 0;
    Vector3 precision;
    public int numberErrors = 0;
    public  Tasklog(float inittimestamp)
    {
        this.inittimestamp = inittimestamp;
    }

    public Tasklog(float inittimestamp, float endtimestamp, Vector3 precision)
    {
        this.inittimestamp = inittimestamp;
        this.endtimestamp = endtimestamp;
        this.precision = precision;
    }

    public float Inittimestamp { get => inittimestamp; set => inittimestamp = value; }
    public float Endtimestamp { get => endtimestamp; set => endtimestamp = value; }
    public Vector3 Precision { get => precision; set => precision = value; }

}
