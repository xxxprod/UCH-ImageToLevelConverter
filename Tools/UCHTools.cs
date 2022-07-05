using System;
using System.Linq;
using System.Windows.Media;
using System.Xml.Linq;
using UCH_ImageToLevelConverter.Model;

namespace UCH_ImageToLevelConverter.Tools;

public static class UCHTools
{
    public const int WallOffsets = 5;

    public static string CreateSnapshotXml(this BlockDataCollection blocks)
    {
        var activeBlocks = blocks
            .Where(a => a.Color.Value != new Color())
            .ToArray();

        var blockXmls = activeBlocks.Select<BlockData, object>((block, idx) => CreateBlockXml(blocks.Width, blocks.Height, block, idx)).ToArray();

        var minX = activeBlocks.Min(a => a.Left);
        var maxX = activeBlocks.Max(a => a.Right);
        var minY = activeBlocks.Min(a => a.Top);
        var maxY = activeBlocks.Max(a => a.Bottom);


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


        var saveFileContents = new XElement("scene", blockXmls.Concat(standardElements)
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
        int blockId;// = 40;
        var x = block.Left - width / 2;
        var y = height / 2 - block.Top;
        var rot = 0;

        if (block.Cells == 1)
            blockId = 40;
        else if (block.Cells == 2)
        {
            blockId = 41;
            if (block.Width == 1)
            {
                rot = 90;
                y -= 1;
            }
        }
        else if (block.Cells == 3)
        {
            blockId = 42;
            if (block.Height == 1)
                x += 1;
            else
            {
                rot = 90;
                y -= 1;
            }
        }
        else if (block.Cells == 4 && (block.Width == 1 || block.Height == 1))
        {
            blockId = 43;
            if (block.Height == 1)
                x += 1;
            else
            {
                rot = 90;
                y -= 2;
            }
        }
        else if (block.Cells == 8 && (block.Width == 1 || block.Height == 1))
        {
            blockId = 44;
            if (block.Height == 1)
                x += 4;
            else
            {
                rot = 90;
                y -= 3;
            }
        }
        else if (block.Cells == 16 && (block.Width == 1 || block.Height == 1))
        {
            blockId = 45;
            if (block.Height == 1)
                x += 7;
            else
            {
                rot = 90;
                y -= 8;
            }
        }
        else if (block.Cells == 8 && (block.Width == 2 || block.Height == 2))
        {
            blockId = 46;
            if (block.Height == 2)
                x += 1;
            else
            {
                rot = 90;
                y -= 2;
            }
        }
        else if (block.Cells == 4 && block.Width == 2)
            blockId = 47;
        else if (block.Cells == 16 && block.Width == 4)
        {
            blockId = 48;
            x += 1;
            y -= 1;
        }
        else if (block.Cells == 64)
        {
            blockId = 49;
            x += 3;
            y -= 3;
        }
        else if (block.Cells == 96)
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
        }
        else throw new NotSupportedException(
            $"Block with Height {block.Height} and Width {block.Width} is not supported");

        return new XElement("block",
            new XAttribute("sceneID", index),
            new XAttribute("blockID", blockId),
            new XAttribute("pX", x),
            new XAttribute("pY", y),
            new XAttribute("rZ", rot),
            new XAttribute("placeableID", 13 + index * 2),
            new XAttribute("colR", block.Color.Value.R / 512.0f),
            new XAttribute("colG", block.Color.Value.G / 512.0f),
            new XAttribute("colB", block.Color.Value.B / 512.0f)
        );
    }
}