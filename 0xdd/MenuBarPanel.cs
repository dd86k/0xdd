using System;
using System.Collections.Generic;

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
//TODO: Sub-sub-menu rendering.*

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
            X, Y, oldY = -1, oldX = -1;
        static bool inMenu = true;

        public static void Initialize()
        {
            MenuItem[] mainItems = {
                new MenuItem("File",
                    new MenuItem("Dump", () => {
                        Exit();
                        InfoPanel.Message("Dumping...");
                        Dumper.Dump(FilePanel.File.FullName, MainApp.BytesPerRow, MainApp.OffsetView);
                        InfoPanel.Message("Done");
                    }),
                    new MenuItem(),
                    new MenuItem("Exit", () => {
                        inMenu = false; MainApp.Exit();
                    })
                ),/*
                new MenuItem("Edit", null,
                    new MenuItem("Test")
                ),*/
                new MenuItem("Search",
                    new MenuItem("Find byte...", () => {
                        Exit();
                        Dialog.PromptFindByte();
                    }),
                    new MenuItem("Find ASCII string...", () => {
                        Exit();
                        Dialog.PromptSearchString();
                    }),
                    new MenuItem(),
                    new MenuItem("Goto...", () => {
                        Exit();
                        Dialog.PromptGoto();
                    })
                ),
                new MenuItem("View",
                    new MenuItem("Offset view...", () => {
                        Exit();
                        Dialog.PromptOffset();
                    }),
                    new MenuItem(),
                    new MenuItem("File info", () => {
                        Exit();
                        InfoPanel.DisplayFileInfo();
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
#if DEBUG
                new MenuItem("Debug",
                    new MenuItem("Show Test Window", () => {
                        Exit();
                        new Window("Test", new Control[] {
                            new Label("Hello World!")
                        }).Show();
                    }),
                    new MenuItem("Goto", () => {
                        Exit();
                        new Window("Goto", new Control[] {
                            new Label("Hello World!", 1, 1),
                            new Button("OK", 12, 3, action: () => { MainApp.Goto(0xdd); })
                        }).Show();
                    }),
                    new MenuItem("Preferences...", () => {
                        Exit();
                        new Window("Test", 50, 6, new Control[] {
                            new Label("Setting 1:", 1, 1),
                            new Button("OK", 12, 3)
                        }).Show();
                    })
                ),
#endif
                new MenuItem("?",
                    new MenuItem("About", () => {
                        Exit();
                        Dialog.GenerateWindow(
                            title: "About",
                            text:
$"{Program.Name} v{Program.Version}\nCopyright (c) 2015-2017 dd86k",
                            width: 36,
                            height: 5
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
                ReadKey();
        }

        static void ReadKey()
        {
            ConsoleKeyInfo ck = Console.ReadKey(true);

            oldY = Y;
            oldX = X;

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
                    OffsetPanel.Draw();
                    FilePanel.Update();
                    DrawSubMenu();
                    break;

                case ConsoleKey.RightArrow:
                    MoveRight();
                    Console.ResetColor();
                    OffsetPanel.Draw();
                    FilePanel.Update();
                    DrawSubMenu();
                    break;

                case ConsoleKey.Spacebar:
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
            string l = new string('─', _miw[X] + 2);
            int x = _pos[X];
            int y = 1;

            ToggleSubMenuColor();

            // Top wall
            Console.SetCursorPosition(x, y++);
            Console.Write($"┌{l}┐");

            for (int i = 0; i < MenuItems[X].Items.Count; ++i, ++y)
            {
                MenuItem item = MenuItems[X].Items[i];

                Console.SetCursorPosition(x, y);

                if (item.IsSeparator)
                    Console.Write($"├{l}┤");
                else
                    Console.Write($"│ {item.Text.PadRight(_miw[X])} │");
            }
             
            // Bottom wall
            Console.SetCursorPosition(x, y);
            Console.Write($"└{l}┘");
        }

        static void Update()
        {
            if (X != oldX)
            {
                FocusMenuBarItem();

                Y = 0;

                if (oldX >= 0)
                {
                    UnfocusMenuBarItem();
                }
            }

            FocusSubMenuItem();

            // Unselect old submenu item
            if (oldY >= 0 && X == oldX && oldY != Y)
            {
                UnfocusSubMenuItem();
            }
        }

        static void FocusMenuBarItem()
        {
            // Select new item menubar item
            MenuItem item = MenuItems[X];
            ToggleSelectionColor();
            Console.SetCursorPosition(_pos[X], 0);
            Console.Write($" {item.Text} ");
        }

        static void UnfocusMenuBarItem()
        {
            // Unselect old menubar selection
            MenuItem lastItem = MenuItems[oldX];
            ToggleMenuBarColor();
            Console.SetCursorPosition(_pos[oldX], 0);
            Console.Write($" {lastItem.Text} ");
        }

        static void FocusSubMenuItem()
        {
            // Select new submenu item
            MenuItem subitem = MenuItems[X].Items[Y];
            ToggleSelectionColor();
            Console.SetCursorPosition(_pos[X] + 1, Y + 2);
            Console.Write($" {subitem.Text.PadRight(_miw[X])} ");
        }

        static void UnfocusSubMenuItem()
        {
            int ly = oldY + 2;
            MenuItem lastItem = MenuItems[oldX].Items[oldY];
            ToggleSubMenuColor();
            Console.SetCursorPosition(_pos[oldX] + 1, ly);
            Console.Write($" {lastItem.Text.PadRight(_miw[oldX])} ");
        }

        static void MoveUp()
        {
            --Y;

            if (Y < 0)
                Y = MenuItems[X].Items.Count - 1;

            while (MenuItems[X].Items[Y].Text == null)
            {
                --Y;

                if (Y < 0)
                    Y = MenuItems[X].Items.Count - 1;
            }
        }

        static void MoveDown()
        {
            ++Y;

            if (Y >= MenuItems[X].Items.Count)
                Y = 0;

            while (MenuItems[X].Items[Y].Text == null)
            {
                ++Y;

                if (Y >= MenuItems[X].Items.Count)
                    Y = 0;
            }
        }

        static void MoveLeft()
        {
            --X;

            if (X < 0)
                X = MenuItems.Count - 1;

            if (Y >= MenuItems[X].Items.Count)
            {
                Y = MenuItems[X].Items.Count - 1;
            }
        }

        static void MoveRight()
        {
            ++X;

            if (X >= MenuItems.Count)
                X = 0;

            if (Y >= MenuItems[X].Items.Count)
            {
                Y = MenuItems[X].Items.Count - 1;
            }
        }

        static void SelectItem()
        {
            MenuItems[X].Items[Y].Action?.Invoke();
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
            OffsetPanel.Initialize();
            inMenu = false;
        }

        static void ToggleMenuBarColor()
        {
            ToggleSubMenuColor();
        }

        static void ToggleSubMenuColor()
        {
            Console.ForegroundColor = Console.ForegroundColor.Invert();
            Console.BackgroundColor = Console.ForegroundColor.Invert();
        }

        static void ToggleSelectionColor()
        {
            Console.ResetColor();
            /*Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;*/
        }
    }

    class MenuItem
    {
        public List<MenuItem> Items { get; }
        public Action Action { get; }
        public string Text { get; }
        public bool IsSeparator => Text == null;

        public MenuItem() : this(null) { }

        public MenuItem(string text, params MenuItem[] items) : this(text, null, items) { }

        public MenuItem(string text, Action action, params MenuItem[] items)
        {
            Text = text;
            Action = action;

            Items = items.Length == 0 ? null : new List<MenuItem>(items);
        }
    }
}