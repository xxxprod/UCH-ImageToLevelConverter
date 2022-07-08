using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UCH_ImageToLevelConverter.Model;

[JsonConverter(typeof(StringEnumConverter))]
public enum Layer
{
    [EnumMember(Value = "SkyBackground")]
    SkyBackground = -10,
    [EnumMember(Value = "Distant Background")]
    DistantBackground = -9,
    [EnumMember(Value = "Background 4")]
    Background4 = -7,
    [EnumMember(Value = "Background 3")]
    Background3 = -6,
    [EnumMember(Value = "Background 2")]
    Background2 = -5,
    [EnumMember(Value = "Background 1")]
    Background1 = -4,
    [EnumMember(Value = "Main Background")]
    MainBackground = -1,
    [EnumMember(Value = "Default")]
    Default = 0,
    [EnumMember(Value = "Player")]
    Player = 3,
    [EnumMember(Value = "Effects")]
    Effects = 4,
    [EnumMember(Value = "Foreground Background")]
    Foreground = 5
}