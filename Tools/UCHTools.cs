using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using UCH_ImageToLevelConverter.Model;

namespace UCH_ImageToLevelConverter.Tools;

public static class UCHTools
{
    public const int WallOffsets = 5;

    public static string CreateSnapshotXml(this BlockDataCollection blocks)
    {
        var activeBlocks = blocks
            .Where(a => a.Color.Value != BlockData.EmptyColor)
            .ToArray();

        var blockXmls = activeBlocks.Select<BlockData, object>((block, idx) => CreateBlockXml(blocks.Width, blocks.Height, block, idx)).ToArray();

        var minX = activeBlocks.Any() ? activeBlocks.Min(a => a.Left) : 0;
        var maxX = activeBlocks.Any() ? activeBlocks.Max(a => a.Right) : 0;
        var minY = activeBlocks.Any() ? activeBlocks.Min(a => a.Top) : 0;
        var maxY = activeBlocks.Any() ? activeBlocks.Max(a => a.Bottom) : 0;


        var standardElements = new[]
        {
            new XElement("moved",
                new XAttribute("placeableID", 7),
                new XAttribute("path", "DeathPit"),
                new XAttribute("pY", minY - blocks.Height / 2 - 4 - WallOffsets)),
            new XElement("moved",
                new XAttribute("placeableID", 9),
                new XAttribute("path", "LeftWall"),
                new XAttribute("pX", minX - blocks.Width / 2 - 5 - WallOffsets),
                new XAttribute("rZ", 270)),
            new XElement("moved",
                new XAttribute("placeableID", 8),
                new XAttribute("path", "Ceiling"),
                new XAttribute("pY", maxY - blocks.Height / 2 + 6 + WallOffsets),
                new XAttribute("rZ", 180)),
            new XElement("moved",
                new XAttribute("placeableID", 6),
                new XAttribute("path", "RightWall"),
                new XAttribute("pX", maxX - blocks.Width / 2 + 5 + WallOffsets),
                new XAttribute("rZ", 90)),
            new XElement("block",
                new XAttribute("sceneID", blockXmls.Length + 5),
                new XAttribute("blockID", 39),
                new XAttribute("pX", maxX - blocks.Width / 2 + WallOffsets -1),
                new XAttribute("pY", minY - blocks.Height / 2 - WallOffsets + 1),
                new XAttribute("placeableID", 2)),
            new XElement("moved",
                new XAttribute("placeableID", 11),
                new XAttribute("path", "StartPlank"),
                new XAttribute("pX", minX - blocks.Width / 2 - WallOffsets + 1.5),
                new XAttribute("pY", minY - blocks.Height / 2 - WallOffsets + 1))
        };


        var saveFileContents = new XElement("scene", blockXmls
            .Concat(standardElements)
            .Concat(new[]
            {
                new XAttribute("levelSceneName", "BlankLevel"),
                new XAttribute("saveFormatVersion", 1),
                new XAttribute("customLevelBackground", 2),
                new XAttribute("customLevelMusic", 10),
                new XAttribute("customLevelAmbience", 10)
            })
        ).ToString(SaveOptions.DisableFormatting);
        return saveFileContents;
    }

    private static XElement CreateBlockXml(int width, int height, BlockData block, int index)
    {
        int blockId;
        var x = block.Left - width / 2;
        var y = height / 2 - block.Top;
        var rot = 0;

        switch (block.Cells)
        {
            case 1:
                blockId = 40;
                break;
            case 2:
                {
                    blockId = 41;
                    if (block.Width == 1)
                    {
                        rot = 90;
                        y -= 1;
                    }

                    break;
                }
            case 3:
                {
                    blockId = 42;
                    if (block.Height == 1)
                        x += 1;
                    else
                    {
                        rot = 90;
                        y -= 1;
                    }

                    break;
                }
            case 4 when (block.Width == 1 || block.Height == 1):
                {
                    blockId = 43;
                    if (block.Height == 1)
                        x += 1;
                    else
                    {
                        rot = 90;
                        y -= 2;
                    }

                    break;
                }
            case 8 when (block.Width == 1 || block.Height == 1):
                {
                    blockId = 44;
                    if (block.Height == 1)
                        x += 4;
                    else
                    {
                        rot = 90;
                        y -= 3;
                    }

                    break;
                }
            case 16 when (block.Width == 1 || block.Height == 1):
                {
                    blockId = 45;
                    if (block.Height == 1)
                        x += 7;
                    else
                    {
                        rot = 90;
                        y -= 8;
                    }

                    break;
                }
            case 8 when (block.Width == 2 || block.Height == 2):
                {
                    blockId = 46;
                    if (block.Height == 2)
                        x += 1;
                    else
                    {
                        rot = 90;
                        y -= 2;
                    }

                    break;
                }
            case 4 when block.Width == 2:
                blockId = 47;
                break;
            case 16 when block.Width == 4:
                blockId = 48;
                x += 1;
                y -= 1;
                break;
            case 64:
                blockId = 49;
                x += 3;
                y -= 3;
                break;
            case 96:
                {
                    blockId = 50;
                    if (block.Width == 6)
                    {
                        x += 2;
                        y -= 7;
                    }
                    else
                    {
                        rot = 90;
                        x += 7;
                        y -= 3;
                    }

                    break;
                }
            default:
                throw new NotSupportedException(
                    $"Block with Height {block.Height} and Width {block.Width} is not supported");
        }

        var attributes = new List<object>
        {
            new XAttribute("sceneID", index),
            new XAttribute("blockID", block.Layer.Value == Layer.Normal ?blockId:9000+blockId),
            new XAttribute("pX", x),
            new XAttribute("pY", y),
            new XAttribute("rZ", rot),
            new XAttribute("placeableID", 13 + index * 2),
            new XAttribute("colR", block.Color.Value.R / 512.0f),
            new XAttribute("colG", block.Color.Value.G / 512.0f),
            new XAttribute("colB", block.Color.Value.B / 512.0f)
        };

        if (block.Layer.Value != Layer.Normal)
        {
            var jsonString = new ModBlockInfo(block.Layer, 1).ToJsonString();
            var xAttribute = new XAttribute("overrideName", jsonString);
            attributes.Add(xAttribute);
        }

        return new XElement("block", attributes);
    }

    public class ModBlockInfo
    {
        public const string ModBlockTag = "[ModBlock]";
        public const string StartMarker = "mbi::";
        public const string EndMarker = "::mbi";

        public ModBlockInfo(Layer layer, double alpha)
        {
            Layer = layer switch
            {
                Model.Layer.Background => "Main Background",
                Model.Layer.Foreground => "Effects",
                _ => throw new NotSupportedException($"Layer '{layer}' not supported")
            };
            Alpha = alpha;
        }

        [JsonProperty("layer")]
        public string Layer { get; }

        [JsonProperty("alpha")]
        public double Alpha { get; }

        public string ToJsonString()
        {
            return $"{ModBlockTag} {StartMarker}{JsonConvert.SerializeObject(this)}{EndMarker}";
        }
    }
}