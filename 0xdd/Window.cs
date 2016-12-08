using System;
using System.Collections.Generic;

namespace _0xdd
{
    public abstract class Control
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public ConsoleColor Foreground { get; set; } = ConsoleColor.Black;
        public ConsoleColor Background { get; set; } = ConsoleColor.Gray;

        public abstract void Draw(int wX = 0, int wY = 0);
        //public abstract void Enter();
    }

    public class Window : Control
    {
        public Window()
        {

        }

        public Window(string title)
            : this(title, 30, 12)
        {

        }

        public Window(string title, params Control[] items)
            : this(title, 30, 12, items)
        {

        }

        public Window(string title, int width, int height, params Control[] items)
        {
            Title = title;
            Width = width;
            Height = height;
            Controls = items;

            X = (Console.WindowWidth / 2) - (width / 2);
            Y = (Console.WindowHeight / 2) - (height / 2);
        }

        public string Title { get; set; }
        public Control[] Controls { get; set; }

        /// <summary>
        /// Show the Window with its controls on-screen.
        /// </summary>
        /// <param name="enter">Enter the message loop.</param>
        public void Show(bool enter = true)
        {
            Draw();
            DrawChildren();

            /*if (enter)
                Enter();*/
        }

        public override void Draw(int x = 0, int y = 0)
        {
            Console.SetCursorPosition(X, Y);

            Console.ForegroundColor = Foreground;
            Console.BackgroundColor = Background;

            string line = new string(' ', Width);

            if (Title != null)
            {
                Console.BackgroundColor = ConsoleColor.Gray;

                Console.Write(Title.Center(Width));

                Console.BackgroundColor = Background;
            }
            else
                Console.Write(line);

            int th = Y + Height;
            for (int cy = Y + 1; cy < th; ++cy)
            {
                Console.SetCursorPosition(X, cy);
                Console.Write(line);
            }

            Console.ResetColor();
        }

        void DrawChildren()
        {
            int y = Title != null ? Y + 1 : Y;

            foreach (Control c in Controls)
                c.Draw(X, y);
        }

        /// <summary>
        /// Enter the message loop, which goes around the control collection.
        /// </summary>
        /*public void Enter()
        {
            if (Controls != null)
            {
                int ci = 0; // Control index.
                int nc = Controls.Length;

                // Put console cursor off and on here?

                bool n = true;
                while (n)
                {
                    Controls[ci < nc ? ++ci : ci = 0].Enter();
                }
            }
        }*/
    }
    
    class Label : Control
    {
        public Label() : this(null, 0, 0)
        {

        }

        public Label(string text) : this(text, 0, 0)
        {

        }

        public Label(string text, int x, int y)
        {
            if (text != null)
            {
                Text = text;
                Width = Text.Length;
            }
            X = x;
            Y = y;
            Height = 1;
        }

        public string Text { get; set; }
        
        public override void Draw(int wx, int wy)
        {
            if (Text != null)
            {
                Console.ForegroundColor = Foreground;
                Console.BackgroundColor = Background;

                Console.SetCursorPosition(wx + X, wy + Y);
                Console.Write(Text);

                Console.ResetColor();
            }
        }

        /*public override void Enter()
        {

        }*/
    }

    class Button : Control
    {
        public Button(string text)
            : this(text, 1, 1, text.Length + 2, 1, null)
        {

        }

        public Button(string text, int x, int y)
            : this(text, x, y, text.Length + 2, 1, null)
        {

        }

        public Button(string text, int x, int y, int width, int height)
            : this(text, x, y, width, height, null)
        {

        }

        public Button(string text, int x = 0, int y = 0, int width = 6, int height = 1, Action action = null)
        {
            Background = ConsoleColor.White;

            Text = text;
            X = x;
            Y = y;
            Width = width;
            Height = height;

            Action = action;
        }

        public string Text { get; set; }
        public Action Action { get; set; }

        public override void Draw(int wX = 0, int wY = 0)
        {
            if (Text != null)
            {
                Console.ForegroundColor = Foreground;
                Console.BackgroundColor = Background;

                Console.SetCursorPosition(wX + X, wY + Y);
                Console.Write(Text.Center(Width));

                Console.ResetColor();
            }
        }

        /*public override void Enter()
        {
            throw new NotImplementedException();
        }*/
    }
}
