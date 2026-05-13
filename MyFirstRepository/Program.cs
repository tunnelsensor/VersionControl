// ============================================================
//  ZOMBIE SURVIVAL - VISUELE GAME MET RAYLIB-CS
//  Omgezet van console naar grafisch venster
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;

// ─────────────────────────────────────────────────────────────
// ENUMS  (vaste keuzemogelijkheden)
// ─────────────────────────────────────────────────────────────

// Moeilijkheidsgraden van het spel
enum Difficulty { Easy, Normal, Hard }

// Alle soorten items die de speler kan hebben
enum ItemType { SmallPotion, BigPotion, Bomb, Shield, PoisonCure }

// Game states voor de visuele versie
enum GameState { Menu, Playing, Combat, GameOver, Victory, WaveReward }

// ─────────────────────────────────────────────────────────────
// CLASS: Item
// Stelt één item voor in de inventory van de speler
// ─────────────────────────────────────────────────────────────
class Item
{
    public string Name { get; private set; }
    public ItemType Type { get; private set; }
    public string Description { get; private set; }
    public Color Color { get; private set; }

    public Item(ItemType type)
    {
        Type = type;
        switch (type)
        {
            case ItemType.SmallPotion:
                Name = "Kleine Potion";
                Description = "Herstelt 20 HP";
                Color = new Color(0, 228, 48, 255);
                break;
            case ItemType.BigPotion:
                Name = "Grote Potion";
                Description = "Herstelt 50 HP";
                Color = new Color(0, 117, 44, 255);
                break;
            case ItemType.Bomb:
                Name = "Explosief";
                Description = "Doet 40 schade aan alle zombies";
                Color = new Color(255, 161, 0, 255);
                break;
            case ItemType.Shield:
                Name = "Schild";
                Description = "Blokkeert de volgende aanval";
                Color = new Color(0, 121, 241, 255);
                break;
            case ItemType.PoisonCure:
                Name = "Antidotum";
                Description = "Geneest vergiftiging";
                Color = new Color(200, 122, 255, 255);
                break;
        }
    }
}

// ─────────────────────────────────────────────────────────────
// CLASS: Player
// De speler met HP, aanval, inventory en status
// ─────────────────────────────────────────────────────────────
class Player
{
    public string Name { get; private set; }
    public int MaxHP { get; private set; }
    public int HP { get; private set; }
    public int AttackDamage { get; private set; }
    public bool IsShielded { get; private set; }   // Actief schild?
    public bool IsPoisoned { get; private set; }   // Vergiftigd?
    public int PoisonTurns { get; private set; }   // Hoeveel beurten vergif nog duurt
    public bool IsDead => HP <= 0;
    public List<Item> Inventory { get; private set; }
    public int Score { get; private set; }
    public int KillCount { get; private set; }
    public Rectangle Position { get; set; }

    // Constructor: maak een nieuwe speler aan
    public Player(string name, int maxHP, int attackDamage)
    {
        Name = name;
        MaxHP = maxHP;
        HP = maxHP;
        AttackDamage = attackDamage;
        Inventory = new List<Item>();
        Score = 0;
        KillCount = 0;
        Position = new Rectangle(100, 300, 60, 80);
    }

    // Speler ontvangt schade (houdt rekening met schild en vergif)
    public int TakeDamage(int damage)
    {
        if (IsShielded)
        {
            IsShielded = false;
            return 0;
        }
        HP -= damage;
        if (HP < 0) HP = 0;
        return damage;
    }

    // Verwerk vergiftiging aan het einde van de beurt
    public void ProcessPoison()
    {
        if (!IsPoisoned) return;
        int poisonDamage = 5;
        HP -= poisonDamage;
        if (HP < 0) HP = 0;
        PoisonTurns--;
        if (PoisonTurns <= 0) IsPoisoned = false;
    }

    // Genees een bepaald aantal HP
    public void Heal(int amount)
    {
        HP += amount;
        if (HP > MaxHP) HP = MaxHP;
    }

    // Gebruik een item uit de inventory
    public bool UseItem(int index, List<Zombie> zombies)
    {
        if (index < 0 || index >= Inventory.Count) return false;

        Item item = Inventory[index];
        Inventory.RemoveAt(index);

        switch (item.Type)
        {
            case ItemType.SmallPotion:
                Heal(20);
                break;
            case ItemType.BigPotion:
                Heal(50);
                break;
            case ItemType.Bomb:
                foreach (var z in zombies)
                    if (!z.IsDead) z.TakeDamage(40);
                break;
            case ItemType.Shield:
                IsShielded = true;
                break;
            case ItemType.PoisonCure:
                IsPoisoned = false;
                PoisonTurns = 0;
                break;
        }
        return true;
    }

    // Vergiftig de speler
    public void Poison(int turns)
    {
        if (!IsPoisoned)
        {
            IsPoisoned = true;
            PoisonTurns = turns;
        }
        else
        {
            // Vergiftiging wordt verlengd
            PoisonTurns = Math.Max(PoisonTurns, turns);
        }
    }

    // Voeg een item toe aan de inventory
    public void AddItem(Item item) => Inventory.Add(item);

    // Upgrade aanvalskracht
    public void UpgradeAttack(int amount)
    {
        AttackDamage += amount;
    }

    // Verhoog maximale HP
    public void UpgradeMaxHP(int amount)
    {
        MaxHP += amount;
        HP = Math.Min(HP + amount, MaxHP);
    }

    // Voeg score toe
    public void AddScore(int points)
    {
        Score += points;
        KillCount++;
    }
}

// ─────────────────────────────────────────────────────────────
// CLASS: Zombie (basisklasse)
// Alle zombie-typen erven van deze klasse
// ─────────────────────────────────────────────────────────────
class Zombie
{
    public string Name { get; protected set; }
    public int MaxHP { get; protected set; }
    public int HP { get; protected set; }
    public int AttackDamage { get; protected set; }
    public bool IsDead => HP <= 0;
    public Rectangle Position { get; set; }
    protected Random rng = new Random();
    public Color Color { get; protected set; }

    // Constructor
    public Zombie(string name, int maxHP, int attackDamage)
    {
        Name = name;
        MaxHP = maxHP;
        HP = maxHP;
        AttackDamage = attackDamage;
        Position = new Rectangle(600, 300, 60, 80);
        Color = new Color(127, 106, 79, 255);
    }

    // Zombie ontvangt schade
    public virtual void TakeDamage(int damage)
    {
        HP -= damage;
        if (HP < 0) HP = 0;
    }

    // Zombie geneest HP (voor boss)
    public void Heal(int amount)
    {
        HP = Math.Min(HP + amount, MaxHP);
    }

    // Zombie valt speler aan — returns hoeveel schade gedaan
    public virtual int Attack(Player player)
    {
        if (IsDead) return 0;
        int damage = rng.Next(AttackDamage - 3, AttackDamage + 4);
        if (damage < 1) damage = 1;
        return player.TakeDamage(damage);
    }
}

// ─────────────────────────────────────────────────────────────
// CLASS: FastZombie
// Weinig HP, maar valt TWEE keer per ronde aan
// ─────────────────────────────────────────────────────────────
class FastZombie : Zombie
{
    public FastZombie(string name) : base(name, maxHP: 35, attackDamage: 8) 
    { 
        Color = new Color(230, 41, 55, 255);
    }

    public override int Attack(Player player)
    {
        if (IsDead) return 0;
        int total = 0;
        for (int i = 0; i < 2; i++)
        {
            int damage = rng.Next(4, 12);
            total += player.TakeDamage(damage);
        }
        return total;
    }
}

// ─────────────────────────────────────────────────────────────
// CLASS: TankZombie
// Veel HP, weinig schade
// ─────────────────────────────────────────────────────────────
class TankZombie : Zombie
{
    public TankZombie(string name) : base(name, maxHP: 120, attackDamage: 6) 
    { 
        Color = new Color(80, 80, 80, 255);
    }

    public override void TakeDamage(int damage)
    {
        int reduced = (int)(damage * 0.8);
        base.TakeDamage(reduced);
    }
}

// ─────────────────────────────────────────────────────────────
// CLASS: PoisonZombie
// Vergiftigt de speler bij aanval
// ─────────────────────────────────────────────────────────────
class PoisonZombie : Zombie
{
    public PoisonZombie(string name) : base(name, maxHP: 50, attackDamage: 9) 
    { 
        Color = new Color(0, 117, 44, 255);
    }

    public override int Attack(Player player)
    {
        if (IsDead) return 0;
        int damage = rng.Next(6, 13);
        int dealt = player.TakeDamage(damage);
        if (dealt > 0 && rng.Next(100) < 60)
        {
            player.Poison(3);
        }
        return dealt;
    }
}

// ─────────────────────────────────────────────────────────────
// CLASS: BossZombie
// De eindbaas: sterk, geneest zichzelf, speciale aanvallen
// ─────────────────────────────────────────────────────────────
class BossZombie : Zombie
{
    private int turnCount = 0;
    public BossZombie(string name) : base(name, maxHP: 200, attackDamage: 18) 
    { 
        Color = new Color(112, 31, 126, 255);
    }

    public override int Attack(Player player)
    {
        if (IsDead) return 0;
        turnCount++;

        if (turnCount % 3 == 0)
        {
            Heal(20);
        }

        if (turnCount % 4 == 0)
        {
            int damage = rng.Next(25, 35);
            return player.TakeDamage(damage);
        }

        int dmg = rng.Next(12, 22);
        return player.TakeDamage(dmg);
    }
}

// ─────────────────────────────────────────────────────────────
// CLASS: VisualGame (hoofdklasse voor visuele game)
// ─────────────────────────────────────────────────────────────
class VisualGame
{
    private Player player;
    private List<Zombie> zombies;
    private Random rng = new Random();
    private Difficulty difficulty;
    private int currentWave = 0;
    private const int TOTAL_WAVES = 5;
    private GameState gameState = GameState.Menu;
    private int selectedZombie = 0;
    private int selectedItem = 0;
    private string playerName = "Overlever";
    private string message = "";
    private int messageTimer = 0;
    private int selectedReward = 0;

    public void Run()
    {
        const int screenWidth = 1000;
        const int screenHeight = 700;
        
        Raylib.InitWindow(screenWidth, screenHeight, "Zombie Survival - Visual Edition");
        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            Update();
            Draw();
        }

        Raylib.CloseWindow();
    }

    private void Update()
    {
        switch (gameState)
        {
            case GameState.Menu:
                UpdateMenu();
                break;
            case GameState.Playing:
                UpdateGame();
                break;
            case GameState.Combat:
                UpdateCombat();
                break;
            case GameState.WaveReward:
                UpdateWaveReward();
                break;
            case GameState.GameOver:
            case GameState.Victory:
                UpdateEndScreen();
                break;
        }

        // Update message timer
        if (messageTimer > 0) messageTimer--;
    }

    private void UpdateMenu()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.One))
        {
            difficulty = Difficulty.Easy;
            StartGame();
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Two))
        {
            difficulty = Difficulty.Normal;
            StartGame();
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Three))
        {
            difficulty = Difficulty.Hard;
            StartGame();
        }
    }

    private void StartGame()
    {
        // Maak speler aan op basis van moeilijkheid
        switch (difficulty)
        {
            case Difficulty.Easy:
                player = new Player(playerName, 150, 20);
                player.AddItem(new Item(ItemType.BigPotion));
                player.AddItem(new Item(ItemType.SmallPotion));
                player.AddItem(new Item(ItemType.Shield));
                break;
            case Difficulty.Hard:
                player = new Player(playerName, 70, 14);
                player.AddItem(new Item(ItemType.SmallPotion));
                break;
            default:
                player = new Player(playerName, 100, 17);
                player.AddItem(new Item(ItemType.SmallPotion));
                player.AddItem(new Item(ItemType.SmallPotion));
                break;
        }

        currentWave = 1;
        gameState = GameState.Playing;
        SpawnWave();
        SetMessage($"Wave {currentWave} begint!");
    }

    private void SpawnWave()
    {
        zombies = new List<Zombie>();
        int diffMultiplier = difficulty == Difficulty.Easy ? 1 : difficulty == Difficulty.Hard ? 2 : 1;

        switch (currentWave)
        {
            case 1:
                zombies.Add(new Zombie("Kreunende Bob", 40 + diffMultiplier * 5, 8 + diffMultiplier * 2));
                break;
            case 2:
                zombies.Add(new Zombie("Rotte Rick", 45, 9));
                zombies.Add(new FastZombie("Snelle Sanne"));
                break;
            case 3:
                zombies.Add(new TankZombie("Dikke Dirk"));
                zombies.Add(new PoisonZombie("Giftige Gina"));
                break;
            case 4:
                zombies.Add(new FastZombie("Razende Rob"));
                zombies.Add(new PoisonZombie("Slijmerige Sam"));
                zombies.Add(new TankZombie("Zware Zwarte Zombie"));
                break;
            case 5:
                zombies.Add(new BossZombie("⚡ ZOMBIE LORD MORTIS ⚡"));
                break;
        }

        // Verdubbel HP op Hard
        if (difficulty == Difficulty.Hard)
            foreach (var z in zombies)
                z.Heal(z.MaxHP / 2);

        // Positioneer zombies
        for (int i = 0; i < zombies.Count; i++)
        {
            zombies[i].Position = new Rectangle(600 + i * 80, 200 + i * 100, 60, 80);
        }

        gameState = GameState.Combat;
    }

    private void UpdateGame()
    {
        // Game logica tussen waves
        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            currentWave++;
            if (currentWave > TOTAL_WAVES)
            {
                gameState = GameState.Victory;
            }
            else
            {
                SpawnWave();
                SetMessage($"Wave {currentWave} begint!");
            }
        }
    }

    private void UpdateCombat()
    {
        // Selecteer zombie met pijltjestoetsen
        if (Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            selectedZombie = (selectedZombie - 1 + zombies.Count) % zombies.Count;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            selectedZombie = (selectedZombie + 1) % zombies.Count;
        }

        // Combat acties
        if (Raylib.IsKeyPressed(KeyboardKey.One))
        {
            AttackZombie(false);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Two))
        {
            AttackZombie(true);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Three))
        {
            // Genezen
            int healAmount = rng.Next(15, 30);
            player.Heal(healAmount);
            SetMessage($"Je herstelt {healAmount} HP!");
            EndPlayerTurn();
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Four))
        {
            // Item gebruiken
            if (player.Inventory.Count > 0)
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Left))
                {
                    selectedItem = (selectedItem - 1 + player.Inventory.Count) % player.Inventory.Count;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Right))
                {
                    selectedItem = (selectedItem + 1) % player.Inventory.Count;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    player.UseItem(selectedItem, zombies);
                    SetMessage($"Gebruikt item!");
                    EndPlayerTurn();
                }
            }
            else
            {
                SetMessage("Geen items in inventory!");
            }
        }

        // Check of alle zombies dood zijn
        if (zombies.All(z => z.IsDead))
        {
            player.AddScore(currentWave * 50);
            gameState = GameState.WaveReward;
            selectedReward = 0;
        }

        // Check of speler dood is
        if (player.IsDead)
        {
            gameState = GameState.GameOver;
        }
    }

    private void AttackZombie(bool heavy)
    {
        var aliveZombies = zombies.Where(z => !z.IsDead).ToList();
        if (aliveZombies.Count == 0) return;

        Zombie target = aliveZombies[selectedZombie % aliveZombies.Count];

        if (heavy)
        {
            // Zware aanval: 30% kans te missen
            if (rng.Next(100) < 30)
            {
                SetMessage("Je mist je aanval!");
                EndPlayerTurn();
                return;
            }
            int damage = player.AttackDamage * 2;
            if (rng.Next(100) < 20) damage = (int)(damage * 1.5); // Critical hit
            target.TakeDamage(damage);
            SetMessage($"ZWARE AANVAL! {damage} schade!");
        }
        else
        {
            int damage = rng.Next(player.AttackDamage - 3, player.AttackDamage + 5);
            if (damage < 1) damage = 1;
            if (rng.Next(100) < 15) damage = (int)(damage * 1.5); // Critical hit
            target.TakeDamage(damage);
            SetMessage($"Aanval! {damage} schade!");
        }

        // Voeg score toe bij kill
        if (target.IsDead)
        {
            int points = target is BossZombie ? 500 : target is TankZombie ? 150 : 100;
            player.AddScore(points);
            SetMessage($"{target.Name} verslagen! +{points} punten!");
            
            // Kans op loot
            if (rng.Next(100) < 35)
            {
                ItemType[] possibleLoot = { ItemType.SmallPotion, ItemType.SmallPotion,
                                             ItemType.BigPotion, ItemType.Bomb,
                                             ItemType.PoisonCure, ItemType.Shield };
                Item loot = new Item(possibleLoot[rng.Next(possibleLoot.Length)]);
                player.AddItem(loot);
                SetMessage($"Gevonden: {loot.Name}!");
            }
        }

        EndPlayerTurn();
    }

    private void EndPlayerTurn()
    {
        // Verwerk vergiftiging
        player.ProcessPoison();

        // Zombies vallen aan
        foreach (var zombie in zombies.Where(z => !z.IsDead))
        {
            zombie.Attack(player);
        }
    }

    private void UpdateWaveReward()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Up))
        {
            selectedReward = (selectedReward - 1 + 4) % 4;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Down))
        {
            selectedReward = (selectedReward + 1) % 4;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.Enter))
        {
            switch (selectedReward)
            {
                case 0:
                    player.Heal(player.MaxHP);
                    SetMessage("HP volledig hersteld!");
                    break;
                case 1:
                    player.UpgradeAttack(5);
                    SetMessage("Aanval +5!");
                    break;
                case 2:
                    player.UpgradeMaxHP(20);
                    SetMessage("Max HP +20!");
                    break;
                case 3:
                    player.AddItem(new Item(ItemType.BigPotion));
                    SetMessage("Grote Potion ontvangen!");
                    break;
            }
            
            currentWave++;
            if (currentWave > TOTAL_WAVES)
            {
                gameState = GameState.Victory;
            }
            else
            {
                SpawnWave();
                SetMessage($"Wave {currentWave} begint!");
            }
        }
    }

    private void UpdateEndScreen()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            gameState = GameState.Menu;
        }
    }

    private void SetMessage(string msg)
    {
        message = msg;
        messageTimer = 120; // 2 seconden bij 60 FPS
    }

    private void Draw()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(0, 0, 0, 255));

        switch (gameState)
        {
            case GameState.Menu:
                DrawMenu();
                break;
            case GameState.Combat:
                DrawCombat();
                break;
            case GameState.WaveReward:
                DrawWaveReward();
                break;
            case GameState.GameOver:
                DrawGameOver();
                break;
            case GameState.Victory:
                DrawVictory();
                break;
        }

        Raylib.EndDrawing();
    }

    private void DrawMenu()
    {
        // Titel
        Raylib.DrawText("ZOMBIE SURVIVAL", 250, 100, 60, new Color(230, 41, 55, 255));
        Raylib.DrawText("VISUAL EDITION", 300, 170, 30, new Color(255, 255, 255, 255));

        // Moeilijkheidskeuze
        Raylib.DrawText("Kies moeilijkheidsgraad:", 300, 250, 25, new Color(255, 255, 255, 255));
        
        Raylib.DrawText("1. Makkelijk", 350, 320, 20, new Color(0, 228, 48, 255));
        Raylib.DrawText("2. Normaal", 350, 360, 20, new Color(255, 255, 255, 255));
        Raylib.DrawText("3. Moeilijk", 350, 400, 20, new Color(230, 41, 55, 255));

        // Instructies
        Raylib.DrawText("Druk op 1, 2 of 3 om te starten", 250, 500, 18, new Color(130, 130, 130, 255));
    }

    private void DrawCombat()
    {
        // Wave info
        Raylib.DrawText($"Wave {currentWave}/{TOTAL_WAVES}", 10, 10, 24, new Color(255, 255, 255, 255));

        // Speler
        DrawPlayer();

        // Zombies
        for (int i = 0; i < zombies.Count; i++)
        {
            DrawZombie(zombies[i], i == selectedZombie);
        }

        // UI Panel
        DrawUIPanel();

        // Message
        if (messageTimer > 0)
        {
            Raylib.DrawRectangle(200, 50, 600, 40, new Color(80, 80, 80, 255));
            Raylib.DrawText(message, 220, 60, 20, new Color(255, 255, 255, 255));
        }
    }

    private void DrawPlayer()
    {
        // Speler sprite (eenvoudige rechthoek)
        Raylib.DrawRectangleRec(player.Position, player.IsPoisoned ? new Color(0, 117, 44, 255) : new Color(0, 121, 241, 255));
        
        // Schild indicator
        if (player.IsShielded)
        {
            Raylib.DrawRectangleLinesEx(player.Position, 3, new Color(0, 255, 255, 255));
        }

        // Naam
        Raylib.DrawText(player.Name, (int)player.Position.X, (int)player.Position.Y - 25, 16, new Color(255, 255, 255, 255));

        // Health bar
        DrawHealthBar(player.Position.X, player.Position.Y + player.Position.Height + 5, 
                     player.Position.Width, player.HP, player.MaxHP, new Color(0, 228, 48, 255));
    }

    private void DrawZombie(Zombie zombie, bool selected)
    {
        if (zombie.IsDead) return;

        // Zombie sprite
        Raylib.DrawRectangleRec(zombie.Position, zombie.Color);

        // Selectie indicator
        if (selected)
        {
            Raylib.DrawRectangleLinesEx(zombie.Position, 3, new Color(255, 255, 0, 255));
        }

        // Naam
        Raylib.DrawText(zombie.Name, (int)zombie.Position.X, (int)zombie.Position.Y - 25, 14, new Color(255, 255, 255, 255));

        // Health bar
        DrawHealthBar(zombie.Position.X, zombie.Position.Y + zombie.Position.Height + 5, 
                     zombie.Position.Width, zombie.HP, zombie.MaxHP, new Color(230, 41, 55, 255));
    }

    private void DrawHealthBar(float x, float y, float width, int current, int max, Color color)
    {
        float barHeight = 8;
        float filledWidth = max > 0 ? (width * current / max) : 0;

        // Background
        Raylib.DrawRectangle((int)x, (int)y, (int)width, (int)barHeight, new Color(80, 80, 80, 255));
        // Fill
        Raylib.DrawRectangle((int)x, (int)y, (int)filledWidth, (int)barHeight, color);
        // Border
        Raylib.DrawRectangleLines((int)x, (int)y, (int)width, (int)barHeight, new Color(255, 255, 255, 255));

        // HP text
        Raylib.DrawText($"{current}/{max}", (int)(x + width/2 - 20), (int)(y - 15), 12, new Color(255, 255, 255, 255));
    }

    private void DrawUIPanel()
    {
        // Panel achtergrond
        Raylib.DrawRectangle(10, 500, 980, 180, new Color(80, 80, 80, 255));

        // Speler stats
        Raylib.DrawText($"Speler: {player.Name}", 20, 510, 18, new Color(255, 255, 255, 255));
        Raylib.DrawText($"HP: {player.HP}/{player.MaxHP}", 20, 535, 16, new Color(0, 228, 48, 255));
        Raylib.DrawText($"Aanval: {player.AttackDamage}", 20, 560, 16, new Color(255, 255, 255, 255));
        Raylib.DrawText($"Score: {player.Score}", 20, 585, 16, new Color(255, 255, 0, 255));
        Raylib.DrawText($"Kills: {player.KillCount}", 20, 610, 16, new Color(230, 41, 55, 255));

        // Status effects
        if (player.IsPoisoned)
        {
            Raylib.DrawText($"VERGIFTIGD ({player.PoisonTurns} beurten)", 200, 535, 14, new Color(0, 117, 44, 255));
        }
        if (player.IsShielded)
        {
            Raylib.DrawText("SCHILD ACTIEF", 200, 555, 14, new Color(0, 255, 255, 255));
        }

        // Inventory
        Raylib.DrawText("Inventory:", 400, 510, 16, new Color(255, 255, 255, 255));
        for (int i = 0; i < Math.Min(player.Inventory.Count, 8); i++)
        {
            Item item = player.Inventory[i];
            Color itemColor = i == selectedItem ? new Color(255, 255, 0, 255) : item.Color;
            Raylib.DrawRectangle(400 + i * 60, 540, 50, 30, itemColor);
            Raylib.DrawText($"{i+1}", 415 + i * 60, 545, 12, new Color(255, 255, 255, 255));
        }

        // Acties
        Raylib.DrawText("Acties:", 20, 640, 16, new Color(255, 255, 255, 255));
        Raylib.DrawText("1: Aanval  2: Zware aanval  3: Genezen  4: Item", 20, 660, 14, new Color(130, 130, 130, 255));
        Raylib.DrawText("Pijltjes: Selecteer zombie  Links/Rechts: Selecteer item", 400, 660, 14, new Color(130, 130, 130, 255));
    }

    private void DrawWaveReward()
    {
        Raylib.DrawText($"Wave {currentWave-1} Voltooid!", 300, 150, 40, new Color(255, 255, 0, 255));
        Raylib.DrawText("Kies je beloning:", 350, 250, 24, new Color(255, 255, 255, 255));

        string[] rewards = { "HP volledig herstellen", "Aanval +5", "Max HP +20", "Grote Potion" };
        for (int i = 0; i < rewards.Length; i++)
        {
            Color color = i == selectedReward ? new Color(255, 255, 0, 255) : new Color(255, 255, 255, 255);
            Raylib.DrawText($"{i+1}. {rewards[i]}", 350, 320 + i * 40, 18, color);
        }

        Raylib.DrawText("Pijltjes: Selecteren  Enter: Bevestigen", 300, 550, 16, new Color(130, 130, 130, 255));
    }

    private void DrawGameOver()
    {
        Raylib.DrawText("GAME OVER", 350, 200, 60, new Color(230, 41, 55, 255));
        Raylib.DrawText($"Score: {player.Score}", 400, 300, 24, new Color(255, 255, 255, 255));
        Raylib.DrawText($"Kills: {player.KillCount}", 400, 340, 24, new Color(255, 255, 255, 255));
        Raylib.DrawText("Druk op SPATIE om terug naar menu", 250, 450, 18, new Color(130, 130, 130, 255));
    }

    private void DrawVictory()
    {
        Raylib.DrawText("GEWONNEN!", 350, 200, 60, new Color(255, 255, 0, 255));
        Raylib.DrawText($"Score: {player.Score}", 400, 300, 24, new Color(255, 255, 255, 255));
        Raylib.DrawText($"Kills: {player.KillCount}", 400, 340, 24, new Color(255, 255, 255, 255));
        Raylib.DrawText("Druk op SPATIE om terug naar menu", 250, 450, 18, new Color(130, 130, 130, 255));
    }
}

// ─────────────────────────────────────────────────────────────
// PROGRAM: Ingangspunt van de applicatie
// ─────────────────────────────────────────────────────────────
class Program
{
    static void Main(string[] args)
    {
        VisualGame game = new VisualGame();
        game.Run();
    }
}