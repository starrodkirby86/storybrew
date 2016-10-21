﻿using OpenTK;
using StorybrewEditor.UserInterface;
using StorybrewEditor.Util;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace StorybrewEditor.Graphics.Text
{
    public class TextLayout
    {
        private List<string> textLines = new List<string>();
        public IEnumerable<string> TextLines => textLines;

        private List<TextLayoutLine> lines = new List<TextLayoutLine>();
        public IEnumerable<TextLayoutLine> Lines => lines;

        private Vector2 size;
        public Vector2 Size => size;

        public IEnumerable<TextLayoutGlyph> Glyphs
        {
            get
            {
                foreach (var line in lines)
                    foreach (var glyph in line.Glyphs)
                        yield return glyph;
            }
        }

        public IEnumerable<TextLayoutGlyph> VisibleGlyphs
        {
            get
            {
                foreach (var line in lines)
                    foreach (var glyph in line.Glyphs)
                        if (!glyph.Glyph.IsEmpty)
                            yield return glyph;
            }
        }

        public TextLayout(string text, TextFont font, UiAlignment alignment, StringTrimming trimming, int maxWidth)
        {
            textLines = LineBreaker.Split(text, maxWidth, c => font.GetGlyph(c).Width);

            if (text.Length > 0)
            {
                var glyphIndex = 0;
                var width = 0.0f;
                var height = 0.0f;
                foreach (var textLine in textLines)
                {
                    var line = new TextLayoutLine(this, height, alignment, lines.Count == 0);
                    foreach (var c in textLine)
                        line.Add(font.GetGlyph(c), glyphIndex++);
                    lines.Add(line);

                    width = Math.Max(width, line.Width);
                    height += line.Height;
                }
                var lastLine = lines[lines.Count - 1];
                if (lastLine.GlyphCount == 0) height += font.LineHeight;
                lastLine.Add(new FontGlyph(null, 0, font.LineHeight), glyphIndex++);

                size = new Vector2(width, height);
            }
            else
            {
                var line = new TextLayoutLine(this, 0, alignment, lines.Count == 0);
                line.Add(new FontGlyph(null, 0, font.LineHeight), 0);
                lines.Add(line);
                size = new Vector2(0, font.LineHeight);
            }
        }

        public TextLayoutGlyph GetGlyph(int index)
        {
            foreach (var line in lines)
            {
                if (index < line.GlyphCount)
                    return line.GetGlyph(index);
                index -= line.GlyphCount;
            }
            throw new IndexOutOfRangeException();
        }

        public int GetCharacterIndexAt(Vector2 position)
        {
            var index = 0;
            foreach (var line in lines)
            {
                var lineMatches = position.Y < line.Position.Y + line.Height;
                foreach (var glyph in line.Glyphs)
                {
                    if (lineMatches && position.X < glyph.Position.X + glyph.Glyph.Width * 0.5f)
                        return index;

                    index++;
                }
                if (lineMatches)
                    return index - 1;
            }
            return index - 1;
        }

        public void ForTextBounds(int startIndex, int endIndex, Action<Box2> action)
        {
            var index = 0;
            foreach (var line in lines)
            {
                var topLeft = Vector2.Zero;
                var bottomRight = Vector2.Zero;
                var hasBounds = false;
                foreach (var layoutGlyph in line.Glyphs)
                {
                    if (!hasBounds && startIndex <= index)
                    {
                        topLeft = layoutGlyph.Position;
                        hasBounds = true;
                    }
                    if (index < endIndex)
                        bottomRight = layoutGlyph.Position + layoutGlyph.Glyph.Size;
                    index++;
                }
                if (hasBounds)
                    action(new Box2(topLeft, bottomRight));
            }
        }
    }

    public class TextLayoutLine
    {
        private List<TextLayoutGlyph> glyphs = new List<TextLayoutGlyph>();
        public IEnumerable<TextLayoutGlyph> Glyphs => glyphs;
        public int GlyphCount => glyphs.Count;

        private TextLayout layout;
        private float y;
        private UiAlignment alignment;
        private bool advance;

        private int width;
        public int Width => width;

        private int height;
        public int Height => height;

        public Vector2 Position => new Vector2(
            alignment.HasFlag(UiAlignment.Left) ? 0 :
                alignment.HasFlag(UiAlignment.Right) ?
                    layout.Size.X - width :
                    layout.Size.X * 0.5f - width * 0.5f,
            y);

        public TextLayoutLine(TextLayout layout, float y, UiAlignment alignment, bool advanceOnEmptyGlyph)
        {
            this.layout = layout;
            this.y = y;
            this.alignment = alignment;
            advance = advanceOnEmptyGlyph;
        }

        public void Add(FontGlyph glyph, int glyphIndex)
        {
            if (!glyph.IsEmpty) advance = true;

            glyphs.Add(new TextLayoutGlyph(this, glyph, glyphIndex, width));
            if (advance) width += glyph.Width;
            height = Math.Max(height, glyph.Height);
        }

        public TextLayoutGlyph GetGlyph(int index)
            => glyphs[index];
    }

    public class TextLayoutGlyph
    {
        private TextLayoutLine line;
        private float x;

        private FontGlyph glyph;
        public FontGlyph Glyph => glyph;

        private int index;
        public int Index => index;

        public Vector2 Position
        {
            get
            {
                var linePosition = line.Position;
                return new Vector2(linePosition.X + x, linePosition.Y);
            }
        }

        public TextLayoutGlyph(TextLayoutLine line, FontGlyph glyph, int index, float x)
        {
            this.line = line;
            this.glyph = glyph;
            this.index = index;
            this.x = x;
        }
    }
}