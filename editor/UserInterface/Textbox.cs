﻿using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using BrewLib.Graphics;
using BrewLib.Graphics.Drawables;
using StorybrewEditor.UserInterface.Skinning.Styles;
using System;
using BrewLib.Util;

namespace StorybrewEditor.UserInterface
{
    public class Textbox : Widget, Field
    {
        private Label label;
        private Label content;
        private Sprite cursorLine;
        private bool hasFocus;
        private bool hovered;
        private bool hasCommitPending;

        private int cursorPosition;
        private int selectionStart;
        public int SelectionLeft
        {
            get { return Math.Min(selectionStart, cursorPosition); }
            set
            {
                if (selectionStart < cursorPosition)
                    selectionStart = value;
                else
                    cursorPosition = value;
            }
        }
        public int SelectionRight
        {
            get { return Math.Max(selectionStart, cursorPosition); }
            set
            {
                if (selectionStart > cursorPosition)
                    selectionStart = value;
                else
                    cursorPosition = value;
            }
        }
        public int SelectionLength => Math.Abs(cursorPosition - selectionStart);

        public override Vector2 MinSize => new Vector2(0, PreferredSize.Y);
        public override Vector2 MaxSize => new Vector2(0, PreferredSize.Y);
        public override Vector2 PreferredSize
        {
            get
            {
                var contentSize = content.PreferredSize;
                if (string.IsNullOrWhiteSpace(label.Text))
                    return new Vector2(Math.Max(contentSize.X, 200), contentSize.Y);

                var labelSize = label.PreferredSize;
                return new Vector2(Math.Max(labelSize.X, 200), labelSize.Y + contentSize.Y);
            }
        }

        public string LabelText { get { return label.Text; } set { label.Text = value; } }

        public string Value
        {
            get { return content.Text; }
            set
            {
                if (content.Text == value) return;
                SetValueSilent(value);

                if (hasFocus) hasCommitPending = true;
                OnValueChanged?.Invoke(this, EventArgs.Empty);
                if (!hasFocus) OnValueCommited?.Invoke(this, EventArgs.Empty);
            }
        }
        public object FieldValue
        {
            get { return Value; }
            set { Value = (string)value; }
        }

        public void SetValueSilent(string value)
        {
            content.Text = value ?? string.Empty;
            if (selectionStart > content.Text.Length)
                selectionStart = content.Text.Length;
            if (cursorPosition > content.Text.Length)
                cursorPosition = content.Text.Length;
        }

        private bool acceptMultiline;
        public bool AcceptMultiline
        {
            get { return acceptMultiline; }
            set
            {
                if (acceptMultiline == value) return;
                acceptMultiline = value;

                if (!acceptMultiline)
                    Value = Value.Replace("\n", "");
            }
        }
        public bool EnterCommits = true;

        public event EventHandler OnValueChanged;
        public event EventHandler OnValueCommited;

        public Textbox(WidgetManager manager) : base(manager)
        {
            cursorLine = new Sprite()
            {
                Texture = DrawState.WhitePixel,
                ScaleMode = ScaleMode.Fill,
                Color = Color4.White,
            };

            Add(content = new Label(manager)
            {
                AnchorFrom = BoxAlignment.BottomLeft,
                AnchorTo = BoxAlignment.BottomLeft,
            });
            Add(label = new Label(manager)
            {
                AnchorFrom = BoxAlignment.TopLeft,
                AnchorTo = BoxAlignment.TopLeft,
            });

            OnFocusChange += (sender, e) =>
            {
                if (hasFocus == e.HasFocus) return;
                if (hasFocus && hasCommitPending)
                {
                    OnValueCommited?.Invoke(this, EventArgs.Empty);
                    hasCommitPending = false;
                }

                hasFocus = e.HasFocus;
                RefreshStyle();
            };
            OnHovered += (sender, e) =>
            {
                hovered = e.Hovered;
                RefreshStyle();
            };
            OnKeyDown += (sender, e) =>
            {
                if (!hasFocus) return false;

                var editor = manager.ScreenLayerManager.GetContext<Editor>();
                switch (e.Key)
                {
                    case Key.Escape:
                        if (hasFocus)
                            manager.KeyboardFocus = null;
                        break;
                    case Key.BackSpace:
                        if (selectionStart > 0 && selectionStart == cursorPosition)
                            selectionStart--;
                        ReplaceSelection("");
                        break;
                    case Key.Delete:
                        if (selectionStart < Value.Length && selectionStart == cursorPosition)
                            cursorPosition++;
                        ReplaceSelection("");
                        break;
                    case Key.A:
                        if (editor.InputManager.ControlOnly)
                            SelectAll();
                        break;
                    case Key.C:
                        if (editor.InputManager.ControlOnly)
                            if (selectionStart != cursorPosition)
                                System.Windows.Forms.Clipboard.SetText(Value.Substring(SelectionLeft, SelectionLength), System.Windows.Forms.TextDataFormat.UnicodeText);
                            else
                                System.Windows.Forms.Clipboard.SetText(Value, System.Windows.Forms.TextDataFormat.UnicodeText);
                        break;
                    case Key.V:
                        if (editor.InputManager.ControlOnly)
                        {
                            var clipboardText = System.Windows.Forms.Clipboard.GetText(System.Windows.Forms.TextDataFormat.UnicodeText);
                            if (!AcceptMultiline)
                                clipboardText = clipboardText.Replace("\n", "");
                            ReplaceSelection(clipboardText);
                        }
                        break;
                    case Key.X:
                        if (editor.InputManager.ControlOnly)
                        {
                            if (selectionStart == cursorPosition)
                                SelectAll();

                            System.Windows.Forms.Clipboard.SetText(Value.Substring(SelectionLeft, SelectionLength), System.Windows.Forms.TextDataFormat.UnicodeText);
                            ReplaceSelection("");
                        }
                        break;
                    case Key.Left:
                        if (editor.InputManager.Shift)
                        {
                            if (cursorPosition > 0)
                                cursorPosition--;
                        }
                        else if (selectionStart != cursorPosition)
                            SelectionRight = SelectionLeft;
                        else if (cursorPosition > 0)
                            cursorPosition = --selectionStart;
                        break;
                    case Key.Right:
                        if (editor.InputManager.Shift)
                        {
                            if (cursorPosition < Value.Length)
                                cursorPosition++;
                        }
                        else if (selectionStart != cursorPosition)
                            SelectionLeft = SelectionRight;
                        else if (cursorPosition < Value.Length)
                            selectionStart = ++cursorPosition;
                        break;
                    case Key.Up:
                        cursorPosition = content.GetCharacterIndexAbove(cursorPosition);
                        if (!manager.ScreenLayerManager.GetContext<Editor>().InputManager.Shift)
                            selectionStart = cursorPosition;
                        break;
                    case Key.Down:
                        cursorPosition = content.GetCharacterIndexBelow(cursorPosition);
                        if (!editor.InputManager.Shift)
                            selectionStart = cursorPosition;
                        break;
                    case Key.Home:
                        cursorPosition = 0;
                        if (!editor.InputManager.Shift)
                            selectionStart = cursorPosition;
                        break;
                    case Key.End:
                        cursorPosition = Value.Length;
                        if (!editor.InputManager.Shift)
                            selectionStart = cursorPosition;
                        break;
                    case Key.Enter:
                    case Key.KeypadEnter:
                        if (AcceptMultiline && (!EnterCommits || editor.InputManager.Shift))
                            ReplaceSelection("\n");
                        else if (EnterCommits && hasCommitPending)
                        {
                            OnValueCommited?.Invoke(this, EventArgs.Empty);
                            hasCommitPending = false;
                        }
                        break;
                }
                return true;
            };
            OnKeyUp += (sender, e) =>
            {
                return hasFocus;
            };
            OnKeyPress += (sender, e) =>
            {
                if (!hasFocus) return false;
                ReplaceSelection(e.KeyChar.ToString());
                return true;
            };
            OnClickDown += (sender, e) =>
            {
                manager.KeyboardFocus = this;
                selectionStart = cursorPosition = content.GetCharacterIndexAt(Manager.Camera.FromScreen(new Vector2(e.X, e.Y)).Xy);
                return true;
            };
            OnDrag += (sender, e) =>
            {
                cursorPosition = content.GetCharacterIndexAt(Manager.Camera.FromScreen(new Vector2(e.X, e.Y)).Xy);
            };
        }

        protected override WidgetStyle Style => Manager.Skin.GetStyle<TextboxStyle>(BuildStyleName(hovered ? "hover" : null, hasFocus ? "focus" : null));

        protected override void ApplyStyle(WidgetStyle style)
        {
            base.ApplyStyle(style);
            var textboxStyle = (TextboxStyle)style;

            label.StyleName = textboxStyle.LabelStyle;
            content.StyleName = textboxStyle.ContentStyle;
        }

        protected override void DrawForeground(DrawContext drawContext, float actualOpacity)
        {
            base.DrawForeground(drawContext, actualOpacity);

            if (hasFocus)
            {
                if (cursorPosition != selectionStart)
                    content.ForTextBounds(SelectionLeft, SelectionRight, selectionBounds =>
                        cursorLine.Draw(drawContext, Manager.Camera, selectionBounds, actualOpacity * 0.2f));

                var bounds = content.GetCharacterBounds(cursorPosition);
                var position = new Vector2(bounds.Left, bounds.Top + bounds.Height * 0.2f);
                var scale = new Vector2(Manager.PixelSize, bounds.Height * 0.6f);
                cursorLine.Draw(drawContext, Manager.Camera, new Box2(position, position + scale), actualOpacity);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                cursorLine.Dispose();
            }
            cursorLine = null;

            base.Dispose(disposing);
        }

        protected override void Layout()
        {
            base.Layout();
            content.Size = new Vector2(Size.X, content.PreferredSize.Y);
            label.Size = new Vector2(Size.X, label.PreferredSize.Y);
        }

        public void SelectAll()
        {
            selectionStart = 0;
            cursorPosition = Value.Length;
        }

        public void ReplaceSelection(string text)
        {
            var left = SelectionLeft;
            var right = SelectionRight;

            var newValue = Value;
            if (left != right)
                newValue = newValue.Remove(left, right - left);
            newValue = newValue.Insert(left, text);

            Value = newValue;
            cursorPosition = selectionStart = SelectionLeft + text.Length;
        }
    }
}
