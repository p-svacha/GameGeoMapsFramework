using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class LineFeatureData
{
    public int Id { get; set; }
    public string DefName { get; set; }
    public List<int> PointIds { get; set; }
    public int RenderLayer { get; set; }
}
