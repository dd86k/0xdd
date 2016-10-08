using System;
using System.Collections.Generic;

//TODO: Sub-sub-menu rendering.*
//TODO: Consider making SelectItem exit the menu

/* New items to implement:
 * - File
 *   - Dump*
 *     - Formats
 *   - Recent files*
 * - View
 *   - Bytes per row...
 *   - Auto adjust to view
 *   - Offset view*
 *     - Bases
 *   - Byte group size*
 *     - 1, 2, 4, 8, 16
 *   - Refresh
 * - Tools
 *   - Preferences...
 * - ?
 *   - Check for updates
 */

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
        static bool inMenu = true;

        public static void Initialize()
        {
            MenuItem[] mainItems = {
                new MenuItem("File", null,
                    new MenuItem("Dump", () => {
                        Exit();
                        InfoPanel.Message("Dumping...");
                        Dumper.Dump(FilePanel.File.FullName, _0xdd.BytesPerRow, _0xdd.OffsetView);
                        InfoPanel.Message("Done");
                    }),
                    new MenuItem(),
                    new MenuItem("Exit", () => {
                        inMenu = false; _0xdd.Exit();
                    })
                ),/*
                new MenuItem("Edit", null,
                    new MenuItem("Test")
                ),*/
                new MenuItem("Search", null,
                    new MenuItem("Find byte...", () => {
                        Exit();
                        WindowSystem.PromptFindByte();
                    }),
                    new MenuItem("Find ASCII string...", () => {
                        Exit();
                        WindowSystem.PromptSearchString();
                    }),
                    new MenuItem(),
                    new MenuItem("Goto...", () => {
                        Exit();
                        WindowSystem.PromptGoto();
                    })
                ),
                new MenuItem("View", null,
                    new MenuItem("Offset view...", () => {
                        Exit();
                        WindowSystem.PromptOffset();
                    }),
                    new MenuItem(),
                    new MenuItem("Refresh", () => {
                        Exit();
                        FilePanel.Refresh();
                    })
                ),/*
                new MenuItem("Tools", null,
                    new MenuItem("Test")
                ),*/
                new MenuItem("?", null,
                    new MenuItem("About", () => {
                        Exit();
                        WindowSystem.GenerateWindow(
                            title: "About",
                            text:
$"{Program.Name}\nv{Program.Version}\nCopyright (c) 2015 guitarxhero",
                            width: 40,
                            height: 6,
                            centerText: true
                        );
                    })
                )
            };

            // Make an array for each, remember that arrays are REFERENCED.
            _pos = new int[mainItems.Length];
            _miw = new int[mainItems.Length];

            MenuItems = new List<MenuItem>(mainItems.Length);
            MenuItems.AddRange(mainItems);

            _barlength = 0;
            // Get menubar's length with items
            for (int i = 0; i < MenuItems.Count; ++i)
            {
                MenuItem item = MenuItems[i];

                _pos[i] = _barlength;

                _barlength += item.Text.Length + 2;

                int max = 0; // Get longuest string in each submenus
                for (int si = 0; si < item.Items.Count; si++)
                {
                    MenuItem subitem = MenuItems[i].Items[si];

                    if (!subitem.IsSeparator)
                    {
                        int len = subitem.Text.Length;

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

            // Select new item menubar item
            FocusMenuBarItem();

            // Select new submenu item
            FocusSubMenuItem();

            inMenu = true;
            while (inMenu)
                Entry();
        }

        static void Entry()
        {
            ConsoleKeyInfo ck = Console.ReadKey(true);

            _oy = _y;
            _ox = _x;

            switch (ck.Key)
            {
                case ConsoleKey.Escape:
                    Exit();
                    return;

                case ConsoleKey.UpArrow:
                    MoveUp();
                    break;

                case ConsoleKey.DownArrow:
                    MoveDown();
                    break;

                case ConsoleKey.LeftArrow:
                    MoveLeft();
                    Console.ResetColor();
                    OffsetPanel.Update();
                    FilePanel.Update();
                    DrawSubMenu();
                    break;

                case ConsoleKey.RightArrow:
                    MoveRight();
                    Console.ResetColor();
                    OffsetPanel.Update();
                    FilePanel.Update();
                    DrawSubMenu();
                    break;

                case ConsoleKey.Enter:
                    SelectItem();
                    break;
            }

            /*
             * This 'if' is there due to calling Exit() from the item's
             * Action, the stack pointer goes back to SelectItem, which
             * then used to call Update(). So this is a sanity check.
             */
            if (inMenu)
                Update();
        }

        /// <summary>
        /// Draw MenuBar
        /// </summary>
        public static void Draw()
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
                MenuItem item = MenuItems[_x].Items[i];

                Console.SetCursorPosition(x, y);

                if (item.IsSeparator)
                    Console.Write($"├{l}┤");
                else
                    Console.Write($"│ {item.Text.PadRight(_miw[_x])} │");
            }
             
            // Bottom wall
            Console.SetCursorPosition(x, y);
            Console.Write($"└{l}┘");
        }

        static void Update()
        {
            if (_x != _ox)
            {
                FocusMenuBarItem();

                _y = 0;

                if (_ox >= 0)
                {
                    UnfocusMenuBarItem();
                }
            }

            FocusSubMenuItem();

            // Unselect old submenu item
            if (_oy >= 0 && _x == _ox && _oy != _y)
            {
                UnfocusSubMenuItem();
            }
        }

        static void FocusMenuBarItem()
        {
            // Select new item menubar item
            MenuItem item = MenuItems[_x];
            ToggleSelectionColor();
            Console.SetCursorPosition(_pos[_x], 0);
            Console.Write($" {item.Text} ");
        }

        static void UnfocusMenuBarItem()
        {
            // Unselect old menubar selection
            MenuItem lastItem = MenuItems[_ox];
            ToggleMenuBarColor();
            Console.SetCursorPosition(_pos[_ox], 0);
            Console.Write($" {lastItem.Text} ");
        }

        static void FocusSubMenuItem()
        {
            // Select new submenu item
            MenuItem subitem = MenuItems[_x].Items[_y];
            ToggleSelectionColor();
            Console.SetCursorPosition(_pos[_x] + 1, _y + 2);
            Console.Write($" {subitem.Text.PadRight(_miw[_x])} ");
        }

        static void UnfocusSubMenuItem()
        {
            int ly = _oy + 2;
            MenuItem lastItem = MenuItems[_ox].Items[_oy];
            ToggleSubMenuColor();
            Console.SetCursorPosition(_pos[_ox] + 1, ly);
            Console.Write($" {lastItem.Text.PadRight(_miw[_ox])} ");
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

        /// <summary>
        /// Exit the menubar loop and clear all selections.
        /// </summary>
        static void Exit()
        {
            // Unselect old menubar selection
            UnfocusMenuBarItem();

            Console.ResetColor();
            FilePanel.Update();
            inMenu = false;
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
        public bool IsSeparator => Text == null;

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