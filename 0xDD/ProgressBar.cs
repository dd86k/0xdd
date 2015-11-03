using System;

namespace _0xdd
{
    /// <summary>
    /// Sraightforward TUI-oriented progress bar implementation.
    /// </summary>
    class ProgressBar
    {
        enum ProgressBarStyle
        {
            Continuous,
            Marquee
        }

        /// <summary>
        /// Constructs a new <see cref="ProgressBar"/> at the current position
        /// with a maximum value of 100.
        /// </summary>
        internal ProgressBar()
            : this(Console.CursorTop, Console.CursorLeft, 100)
        { }

        /// <summary>
        /// Constructs a new <see cref="ProgressBar"/> at the current position.
        /// </summary>
        /// <param name="pMaximumValue">Maximum value.</param>
        internal ProgressBar(int pMaximumValue)
            : this(Console.CursorTop, Console.CursorLeft, pMaximumValue)
        { }

        /// <summary>
        /// Constructs a new <see cref="ProgressBar"/>.
        /// </summary>
        /// <param name="pTopPosition">Top position (Y).</param>
        /// <param name="pLeftPosition">Left position (X).</param>
        /// <param name="pMaximumValue">Maximum value.</param>
        internal ProgressBar(int pTopPosition, int pLeftPosition, int pMaximumValue)
        {
            TopPosition = pTopPosition;
            LeftPosition = pLeftPosition;

            Width = Console.WindowWidth;
            //Height = 1;

            Value = 0;
            MaximumValue = pMaximumValue;

            BeginChar = '[';
            ProgressChar = '=';
            EndChar = ']';
        }

        /// <summary>
        /// If the <see cref="ProgressBar"/> has been initiated on screen.
        /// </summary>
        internal bool Initiated
        {
            get; private set;
        }

        /// <summary>
        /// Gets or sets the Y position.
        /// </summary>
        internal int TopPosition
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the X position.
        /// </summary>
        internal int LeftPosition
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the total width.
        /// </summary>
        internal int Width
        {
            get; set;
        }

        /*
        internal int Height
        {
            get; set;
        }
        */

        int _value;
        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        internal int Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (value <= MaximumValue)
                {
                    _value = value;
                }
                else
                {
                    _value = _maximumValue;
                }

                Update();
            }
        }

        int _maximumValue;
        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        internal int MaximumValue
        {
            get
            {
                return _maximumValue;
            }
            set
            {
                if (value >= _value)
                {
                    _maximumValue = value;
                }
                else
                {
                    _maximumValue = _value;
                }

                Update();
            }
        }

        /// <summary>
        /// Gets or sets the current text on the progress bar.
        /// Unused.
        /// </summary>
        internal string Text
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the progress bar's beginning character.
        /// </summary>
        internal char BeginChar
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the progress bar's progress character.
        /// </summary>
        internal char ProgressChar
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the progress bar's ending character.
        /// </summary>
        internal char EndChar
        {
            get; set;
        }

        /// <summary>
        /// Last number of generated characters, used to skip
        /// a render if the number is the same (especially in
        /// very frequent updates).
        /// </summary>
        int LastNumberOfCharacters
        {
            get; set;
        }


        void Initiate()
        {
            Console.SetCursorPosition(LeftPosition, TopPosition);
            Console.Write(BeginChar);
            Console.SetCursorPosition(LeftPosition + Width - 1, TopPosition);
            Console.Write(EndChar);

            Initiated = true;
        }


        void Update()
        {
            if (!Initiated)
                Initiate();

            int NumberOfCharsToPrint = (Value * (Width - 2)) / MaximumValue;

            if (LastNumberOfCharacters != NumberOfCharsToPrint)
            {
                Console.SetCursorPosition(LeftPosition + 1, TopPosition);
                Console.Write(new string(ProgressChar, NumberOfCharsToPrint));
                LastNumberOfCharacters = NumberOfCharsToPrint;
            }
        }


        void Clear()
        {
            Console.SetCursorPosition(LeftPosition, TopPosition);
            Console.Write(new string(' ', Width));
        }


        void Refresh()
        {
            Clear();
            Update();
        }

        /// <summary>
        /// Increment the current value.
        /// </summary>
        /// <param name="pValue">Number to increment the value.</param>
        internal void Increment(int pValue)
        {
            Value += pValue;
        }
    }
}
