using System;
using System.Collections.Generic;

namespace RpgGame
{
    class Program
    {
        static void Main(string[] args)
        {
            Game game = new Game();
            game.Start();
        }
    }
    public abstract class MapObject
    {
        public abstract char Symbol { get; }
        public abstract ConsoleColor Color { get; }
        public bool IsConsumed { get; set; } = false;
        public abstract bool OnEnter(Player player, Game game);
    }

    public class Enemy : MapObject
    {
        public string UnitType;
        public string Name;
        public int Health;
        public int Damage;
        public bool IsHostile;
        public Enemy(string unitType, string name, int health, int damage, bool isHostile = true)
        {
            UnitType = unitType;
            Name = name;
            Health = health;
            Damage = damage;
            IsHostile = isHostile;
        }
        public override char Symbol => UnitType == "BigBoss" ? 'B' : UnitType == "Monster" ? 'M' : 'A';
        public override ConsoleColor Color => UnitType == "BigBoss" ? ConsoleColor.Red : UnitType == "Monster" ? ConsoleColor.DarkRed : ConsoleColor.Yellow;
        public override bool OnEnter(Player player, Game game)
        {
            Console.WriteLine($"{Name} ({UnitType}) HP:{Health} DMG:{Damage}");
            if (!IsHostile)
            {
                Console.WriteLine("Hunt? (C/V)");
                if (Char.ToUpper(Console.ReadKey(true).KeyChar) != 'C')
                {
                    Console.WriteLine("Skip");
                    return true;
                }
            }
            while (Health > 0 && player.Health > 0)
            {
                Console.WriteLine("(Z) Attack, (X) Run");
                char action = Char.ToUpper(Console.ReadKey(true).KeyChar);
                if (action == 'Z')
                {
                    int heroDamage = player.Strength + game.random.Next(5);
                    Console.WriteLine($"Hit {heroDamage}");
                    Health -= heroDamage;
                    if (Health <= 0)
                    {
                        Console.WriteLine("Enemy defeated");
                        if (UnitType == "Monster")
                            player.Add("Gold", 10);
                        if (UnitType == "Animal")
                            player.Add("Meat", 5);
                        IsConsumed = true;
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Enemy hits {Damage}");
                        player.Health -= Damage;
                        if (player.Health <= 0)
                        {
                            Console.WriteLine("You died");
                            return false;
                        }
                    }
                }
                else if (action == 'X')
                {
                    if (game.random.Next(100) < player.Agility * 5)
                    {
                        Console.WriteLine("Escaped");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Failed to escape");
                        Console.WriteLine($"Enemy hits {Damage}");
                        player.Health -= Damage;
                        if (player.Health <= 0)
                        {
                            Console.WriteLine("You died");
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }

    public class Resource : MapObject
    {
        public string Category;
        public string Type;
        public int StepsRequired;
        public int StepsDone = 0;
        public int Yield;
        public Resource(string category, string type, int stepsRequired, int yield)
        {
            Category = category;
            Type = type;
            StepsRequired = stepsRequired;
            Yield = yield;
        }
        public override char Symbol => Category == "Gold" ? 'G' : Category == "Wood" ? 'W' : 'T';
        public override ConsoleColor Color => Category == "Gold" ? ConsoleColor.Yellow : Category == "Wood" ? ConsoleColor.DarkYellow : ConsoleColor.Cyan;
        public override bool OnEnter(Player player, Game game)
        {
            Console.WriteLine($"{Type} ({Category}) [{StepsDone}/{StepsRequired}]");
            Console.WriteLine("Harvest? (C/V)");
            if (Char.ToUpper(Console.ReadKey(true).KeyChar) == 'C')
            {
                StepsDone++;
                if (StepsDone >= StepsRequired)
                {
                    Console.WriteLine("Harvest complete");
                    player.Add(Type, Yield);
                    IsConsumed = true;
                }
            }
            return true;
        }
    }

    public class Shop : MapObject
    {
        public List<ShopItem> Items = new List<ShopItem>();
        public Shop()
        {
            Items.Add(new ShopItem("Sword", new Dictionary<string, int> { { "Gold", 20 }, { "Wood", 5 } }, ("Strength", 5)));
            Items.Add(new ShopItem("Potion", new Dictionary<string, int> { { "Gold", 10 } }, ("Health", 20)));
        }
        public override char Symbol => 'S';
        public override ConsoleColor Color => ConsoleColor.Blue;
        public override bool OnEnter(Player player, Game game)
        {
            Console.WriteLine("Shop");
            bool shopping = true;
            while (shopping)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    Console.WriteLine($"{i + 1}: {Items[i].Name} - {Items[i].PriceString()}");
                }
                Console.WriteLine("0: Exit");
                string input = Console.ReadLine();
                if (int.TryParse(input, out int choice))
                {
                    if (choice == 0)
                        shopping = false;
                    else if (choice > 0 && choice <= Items.Count)
                    {
                        ShopItem item = Items[choice - 1];
                        if (player.CanAfford(item.Cost))
                        {
                            player.Purchase(item);
                            Console.WriteLine("Bought " + item.Name);
                        }
                        else
                            Console.WriteLine("Not enough");
                    }
                }
            }
            return true;
        }
    }

    public class ShopItem
    {
        public string Name;
        public Dictionary<string, int> Cost;
        public (string stat, int boost) StatBoost;
        public ShopItem(string name, Dictionary<string, int> cost, (string, int) statBoost)
        {
            Name = name;
            Cost = cost;
            StatBoost = statBoost;
        }
        public string PriceString()
        {
            List<string> parts = new List<string>();
            foreach (var kvp in Cost)
                parts.Add($"{kvp.Value}{kvp.Key}");
            return string.Join(",", parts);
        }
    }

    public class Player
    {
        public int X, Y;
        public int Health;
        public int Strength;
        public int Agility;
        public Dictionary<string, int> Inventory = new Dictionary<string, int>();
        public void Add(string item, int amount)
        {
            if (Inventory.ContainsKey(item))
                Inventory[item] += amount;
            else
                Inventory[item] = amount;
        }
        public bool CanAfford(Dictionary<string, int> cost)
        {
            foreach (var kvp in cost)
            {
                if (!Inventory.ContainsKey(kvp.Key) || Inventory[kvp.Key] < kvp.Value)
                    return false;
            }
            return true;
        }
        public void Purchase(ShopItem item)
        {
            foreach (var kvp in item.Cost)
            {
                Inventory[kvp.Key] -= kvp.Value;
            }
            switch (item.StatBoost.stat)
            {
                case "Strength":
                    Strength += item.StatBoost.boost;
                    break;
                case "Health":
                    Health += item.StatBoost.boost;
                    break;
            }
        }
    }

    public class Game
    {
        public int GridWidth = 30;
        public int GridHeight = 30;
        public MapObject[,] Map;
        public Player player;
        public Random random = new Random();
        public bool gameOver = false;
        public bool gameWin = false;

        public void Start()
        {
            InitializeLevel();

            while (!gameOver)
            {
                DrawMap();
                DisplayStats();
                Console.Write("Move (arrow) or Q: ");
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Q)
                {
                    gameOver = true;
                    break;
                }
                ProcessInput(key.Key);
                if (player.Health <= 0)
                {
                    gameOver = true;
                    Console.WriteLine("Game Over!");
                    break;
                }
                if (IsBossDefeated())
                {
                    Console.WriteLine("Boss defeated!");
                    gameWin = true;
                    break;
                }
            }
            if (gameWin)
            {
                Console.WriteLine("You Win!");
            }
        }

        public void InitializeLevel()
        {
            Map = new MapObject[GridHeight, GridWidth];
            if (player == null)
                player = new Player() { X = 0, Y = 0, Health = 100, Strength = 10, Agility = 10 };
            else
            {
                player.X = 0;
                player.Y = 0;
            }
            int bossX, bossY;
            do { bossX = random.Next(GridHeight); bossY = random.Next(GridWidth); }
            while (bossX == 0 && bossY == 0);
            Map[bossX, bossY] = new Enemy("BigBoss", "Dragon", 150, 30);

            for (int i = 0; i < GridHeight; i++)
            {
                for (int j = 0; j < GridWidth; j++)
                {
                    if (i == 0 && j == 0)
                        continue;
                    if (Map[i, j] != null)
                        continue;
                    double roll = random.NextDouble();
                    if (roll < 0.10)
                    {
                        int health = 40;
                        int damage = 10;
                        Map[i, j] = new Enemy("Monster", "Goblin", health, damage);
                    }
                    else if (roll < 0.15)
                    {
                        int health = 30;
                        int damage = 5;
                        Map[i, j] = new Enemy("Animal", "Wolf", health, damage, false);
                    }
                    else if (roll < 0.30)
                    {
                        double r2 = random.NextDouble();
                        if (r2 < 0.4)
                        {
                            if (random.Next(2) == 0)
                                Map[i, j] = new Resource("Gold", "Coin", 1, 1);
                            else
                                Map[i, j] = new Resource("Gold", "Nugget", 2, 3);
                        }
                        else if (r2 < 0.8)
                        {
                            if (random.Next(2) == 0)
                                Map[i, j] = new Resource("Wood", "Bush", 2, 2);
                            else
                                Map[i, j] = new Resource("Wood", "Tree", 3, 5);
                        }
                        else
                        {
                            Map[i, j] = new Resource("Treasure", "Diamond", 3, 1);
                        }
                    }
                    else if (roll < 0.32)
                    {
                        Map[i, j] = new Shop();
                    }
                }
            }
        }

        public void DrawMap()
        {
            Console.Clear();
            for (int i = 0; i < GridHeight; i++)
            {
                for (int j = 0; j < GridWidth; j++)
                {
                    if (player.X == i && player.Y == j)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("@ ");
                        Console.ResetColor();
                    }
                    else if (Map[i, j] != null)
                    {
                        Console.ForegroundColor = Map[i, j].Color;
                        Console.Write(Map[i, j].Symbol + " ");
                        Console.ResetColor();
                    }
                    else
                        Console.Write(". ");
                }
                Console.WriteLine();
            }
        }

        public void DisplayStats()
        {
            Console.WriteLine($"HP:{player.Health} Str:{player.Strength} Agi:{player.Agility}");
            Console.Write("Inventory: ");
            foreach (var item in player.Inventory)
                Console.Write($"{item.Key}:{item.Value} ");
            Console.WriteLine();
        }

        public void ProcessInput(ConsoleKey key)
        {
            int newX = player.X, newY = player.Y;
            switch (key)
            {
                case ConsoleKey.UpArrow: newX--; break;
                case ConsoleKey.DownArrow: newX++; break;
                case ConsoleKey.LeftArrow: newY--; break;
                case ConsoleKey.RightArrow: newY++; break;
                default:
                    Console.WriteLine("Unknown command.");
                    return;
            }
            if (newX < 0 || newX >= GridHeight || newY < 0 || newY >= GridWidth)
            {
                Console.WriteLine("Out of bounds!");
                return;
            }
            if (Map[newX, newY] != null)
            {
                bool canMove = Map[newX, newY].OnEnter(player, this);
                if (canMove && Map[newX, newY].IsConsumed)
                    Map[newX, newY] = null;
                player.X = newX;
                player.Y = newY;
            }
            else
            {
                player.X = newX;
                player.Y = newY;
            }
        }

        private bool IsBossDefeated()
        {
            foreach (var obj in Map)
            {
                if (obj is Enemy enemy && enemy.UnitType == "BigBoss")
                    return false;
            }
            return true;
        }
    }
}
