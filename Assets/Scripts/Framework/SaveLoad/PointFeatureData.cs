using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PointFeatureData
{
    public int Id { get; set; }
    public int PointId { get; set; }
    public string DefName { get; set; }
    public string Label { get; set; }
}
