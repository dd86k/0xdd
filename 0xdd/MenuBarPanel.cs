using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _0xdd
{
    static class MenuBarPanel
    {
        // MenuBar's items. Maybe turn this into an array?
        static List<MenuItem> MenuItems;
        // Item Positions of the items in the menu bar and
        // the Maximum Items Width of the items.
        static int[] _pos, _miw;
        // The menu bar length with items, to ease filling the gap.
        static int _barlength = 0,
            // X and Y position in the menu, including old positions.
            _x, _y, _oy = -1, _ox = -1;

        public static void Initialize()
        {
            MenuItem[] items = {
                    new MenuItem("File", null,
                        new MenuItem("Message", () => { InfoPanel.Message("Test"); }),
                        new MenuItem(),
                        new MenuItem("Test")
                    ),
                    new MenuItem("Edit", null,
                        new MenuItem("Test")
                    ),
                    new MenuItem("Search", null,
                        new MenuItem("Test")
                    ),
                    new MenuItem("View", null,
                        new MenuItem("Test")
                    ),
                    new MenuItem("Options", null,
                        new MenuItem("Test")
                    ),
                    new MenuItem("?", null,
                        new MenuItem("About", () => { InfoPanel.Message($"{Program.ProjectName} v{Program.Version} by guitarhero"); })
                    )
                };

            // Make an array for each, arrays are REFERENCED.
            _pos = new int[items.Length];
            _miw = new int[items.Length];

            MenuItems = new List<MenuItem>(items.Length);
            MenuItems.AddRange(items);

            // Get menubar's length with items
            for (int i = 0; i < MenuItems.Count; ++i)
            {
                _pos[i] = _barlength;

                _barlength += $" {MenuItems[i].Text} ".Length;

                int max = 0; // Get longuest string in each submenus
                for (int si = 0; si < MenuItems[i].Items.Count; si++)
                {
                    if (MenuItems[i].Items[si].Text != null)
                    {
                        int len = MenuItems[i].Items[si].Text.Length;

                        if (len > max)
                            max = len;
                    }
                }
                _miw[i] = max;
            }

            Draw();
        }

        /// <summary>
        /// Enter the readkey loop.
        /// </summary>
        public static void Enter()
        {
            Update();
            DrawSubMenu();
            while (Entry()) ;
        }

        static bool Entry()
        {
            ConsoleKeyInfo ck = Console.ReadKey(true);

            _oy = _y;
            _ox = _x;

            switch (ck.Key)
            {
                case ConsoleKey.Escape:
                    // Unselect old menubar selection
                    MenuItem lastItem = MenuItems[_ox];
                    ToggleMenuBarColor();
                    Console.SetCursorPosition(_pos[_ox], 0);
                    Console.Write($" {lastItem.Text} ");

                    Console.ResetColor();
                    MainPanel.Update();
                    return false;

                case ConsoleKey.UpArrow:
                    MoveUp();
                    Update();
                    break;

                case ConsoleKey.DownArrow:
                    MoveDown();
                    Update();
                    break;

                case ConsoleKey.LeftArrow:
                    MoveLeft();
                    Console.ResetColor();
                    OffsetPanel.Update();
                    MainPanel.Update();
                    DrawSubMenu();
                    break;

                case ConsoleKey.RightArrow:
                    MoveRight();
                    Console.ResetColor();
                    OffsetPanel.Update();
                    MainPanel.Update();
                    DrawSubMenu();
                    break;

                case ConsoleKey.Enter:
                    SelectItem();
                    return true;
            }

            Update();

            return true;
        }

        /// <summary>
        /// Draw MenuBar
        /// </summary>
        static void Draw()
        {
            ToggleMenuBarColor();
            Console.SetCursorPosition(0, 0);

            for (int i = 0; i < MenuItems.Count; ++i)
                Console.Write($" {MenuItems[i].Text} ");

            Console.Write(new string(' ', Console.WindowWidth - _barlength));
            Console.ResetColor();
        }

        /// <summary>
        /// Draw a sub menu.
        /// </summary>
        static void DrawSubMenu()
        {
            string l = new string('─', _miw[_x] + 2);
            int x = _pos[_x];
            int y = 1;

            ToggleSubMenuColor();

            // Top wall
            Console.SetCursorPosition(x, y++);
            Console.Write($"┌{l}┐");

            for (int i = 0; i < MenuItems[_x].Items.Count; ++i, ++y)
            {
                Console.SetCursorPosition(x, y);

                if (MenuItems[_x].Items[i].Text != null)
                    Console.Write($"│ {MenuItems[_x].Items[i].Text.PadRight(_miw[_x])} │");
                else
                    Console.Write($"├{l}┤");
            }

            // Bottom wall
            Console.SetCursorPosition(x, y);
            Console.Write($"└{l}┘");
        }

        static void Update()
        {
            if (_x != _ox)
            {
                // Select new item menubar item
                MenuItem item = MenuItems[_x];
                ToggleSelectionColor();
                Console.SetCursorPosition(_pos[_x], 0);
                Console.Write($" {item.Text} ");

                // Unselect old menubar selection
                if (_ox >= 0)
                {
                    MenuItem lastItem = MenuItems[_ox];
                    ToggleMenuBarColor();
                    Console.SetCursorPosition(_pos[_ox], 0);
                    Console.Write($" {lastItem.Text} ");
                }
            }

            // Select new submenu item
            MenuItem subitem = MenuItems[_x].Items[_y];
            ToggleSelectionColor();
            Console.SetCursorPosition(_pos[_x] + 1, _y + 2);
            Console.Write($" {subitem.Text.PadRight(_miw[_x])} ");

            // Unselect old submenu item
            if (_oy >= 0 && _x == _ox && _oy != _y)
            {
                int ly = _oy + 2;
                MenuItem lastItem = MenuItems[_ox].Items[_oy];
                ToggleSubMenuColor();
                Console.SetCursorPosition(_pos[_ox] + 1, ly);
                Console.Write($" {lastItem.Text.PadRight(_miw[_ox])} ");
            }
        }

        static void MoveUp()
        {
            _y--;

            if (_y < 0)
                _y = MenuItems[_x].Items.Count - 1;

            while (MenuItems[_x].Items[_y].Text == null)
            {
                _y--;

                if (_y < 0)
                    _y = MenuItems[_x].Items.Count - 1;
            }
        }

        static void MoveDown()
        {
            _y++;

            if (_y >= MenuItems[_x].Items.Count)
                _y = 0;

            while (MenuItems[_x].Items[_y].Text == null)
            {
                _y++;

                if (_y >= MenuItems[_x].Items.Count)
                    _y = 0;
            }
        }

        static void MoveLeft()
        {
            _x--;
            if (_x < 0)
                _x = MenuItems.Count - 1;

            if (_y >= MenuItems[_x].Items.Count)
            {
                _y = MenuItems[_x].Items.Count - 1;
            }
        }

        static void MoveRight()
        {
            _x++;
            if (_x >= MenuItems.Count)
                _x = 0;

            if (_y >= MenuItems[_x].Items.Count)
            {
                _y = MenuItems[_x].Items.Count - 1;
            }
        }

        static void SelectItem()
        {
            MenuItems[_x].Items[_y].Action?.Invoke();
        }

        static void ToggleMenuBarColor()
        {
            ToggleSubMenuColor();
        }

        static void ToggleSubMenuColor()
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Gray;
        }

        static void ToggleSelectionColor()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
        }
    }

    class MenuItem
    {
        public List<MenuItem> Items { get; }
        public Action Action { get; }
        public string Text { get; }

        public MenuItem() : this(null, null) { }

        public MenuItem(string text) : this(text, null) { }

        public MenuItem(string text, Action action, params MenuItem[] items)
        {
            Text = text;
            Action = action;

            Items = items.Length == 0 ? null : new List<MenuItem>(items);
        }
    }
}
