using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class MapData
{
    public string Name { get; set; }
    public List<PointData> Points { get; set; }
    public List<LineFeatureData> LineFeatures { get; set; }
    public List<AreaFeatureData> AreaFeatures { get; set; }
}
