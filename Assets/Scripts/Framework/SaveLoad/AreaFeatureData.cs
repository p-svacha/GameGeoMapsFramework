using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class AreaFeatureData
{
    public int Id { get; set; }
    public string DefName { get; set; }
    public string Name { get; set; }
    public List<int> PointIds { get; set; }
    public int RenderLayer { get; set; }
}
