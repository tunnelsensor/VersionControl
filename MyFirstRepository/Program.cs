// ============================================================
//  ZOMBIE SURVIVAL - Een turn-based console game in C#
//  Gemaakt voor beginners: elk onderdeel is uitgelegd met comments
// ============================================================

using System;
using System.Collections.Generic;
using System.Threading;

// ─────────────────────────────────────────────────────────────
// ENUMS  (vaste keuzemogelijkheden)
// ─────────────────────────────────────────────────────────────

// Moeilijkheidsgraden van het spel
enum Difficulty { Easy, Normal, Hard }

// Alle soorten items die de speler kan hebben
enum ItemType { SmallPotion, BigPotion, Bomb, Shield, PoisonCure }

// ─────────────────────────────────────────────────────────────
// CLASS: Item
// Stelt één item voor in de inventory van de speler
// ─────────────────────────────────────────────────────────────
class Item
{
    public string Name { get; private set; }
    public ItemType Type { get; private set; }
    public string Description { get; private set; }

    public Item(ItemType type)
    {
        Type = type;
        // Stel naam en beschrijving in op basis van type
        switch (type)
        {
            case ItemType.SmallPotion:
                Name = "Kleine Potion";
                Description = "Herstelt 20 HP";
                break;
            case ItemType.BigPotion:
                Name = "Grote Potion";
                Description = "Herstelt 50 HP";
                break;
            case ItemType.Bomb:
                Name = "Explosief";
                Description = "Doet 40 schade aan alle zombies";
                break;
            case ItemType.Shield:
                Name = "Schild";
                Description = "Blokkeert de volgende aanval volledig";
                break;
            case ItemType.PoisonCure:
                Name = "Antidotum";
                Description = "Geneest vergiftiging";
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
    }

    // Speler ontvangt schade (houdt rekening met schild en vergif)
    public int TakeDamage(int damage)
    {
        if (IsShielded)
        {
            IsShielded = false;
            Display.WriteColored("⛊  Je schild absorbeert de aanval!", ConsoleColor.Cyan);
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
        Display.WriteColored($"☠  Vergif! Je verliest {poisonDamage} HP door het gif. ({PoisonTurns} beurten resterend)", ConsoleColor.DarkGreen);
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
        Inventory.RemoveAt(index);  // Item verdwijnt na gebruik

        switch (item.Type)
        {
            case ItemType.SmallPotion:
                Heal(20);
                Display.WriteColored($"🧪 Je drinkt een kleine potion en herstelt 20 HP. (HP: {HP}/{MaxHP})", ConsoleColor.Green);
                break;
            case ItemType.BigPotion:
                Heal(50);
                Display.WriteColored($"🧪 Je drinkt een grote potion en herstelt 50 HP. (HP: {HP}/{MaxHP})", ConsoleColor.Green);
                break;
            case ItemType.Bomb:
                Display.WriteColored("💣 BOEM! Je gooit een explosief naar de zombies!", ConsoleColor.Yellow);
                foreach (var z in zombies)
                    if (!z.IsDead) z.TakeDamage(40);
                break;
            case ItemType.Shield:
                IsShielded = true;
                Display.WriteColored("⛊  Je heft je schild - de volgende aanval wordt geblokkeerd!", ConsoleColor.Cyan);
                break;
            case ItemType.PoisonCure:
                IsPoisoned = false;
                PoisonTurns = 0;
                Display.WriteColored("💊 Je slikt het antidotum. De vergiftiging is verdwenen!", ConsoleColor.Green);
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
        Display.WriteColored($"⚔  Aanvalskracht verhoogd naar {AttackDamage}!", ConsoleColor.Yellow);
    }

    // Verhoog maximale HP
    public void UpgradeMaxHP(int amount)
    {
        MaxHP += amount;
        HP = Math.Min(HP + amount, MaxHP);
        Display.WriteColored($"❤  Maximale HP verhoogd naar {MaxHP}!", ConsoleColor.Green);
    }

    // Voeg score toe
    public void AddScore(int points)
    {
        Score += points;
        KillCount++;
    }

    // Toon spelersstatus
    public void ShowStats()
    {
        Console.WriteLine();
        Display.WriteColored("╔══════════════════════════════╗", ConsoleColor.White);
        Display.WriteColored($"║  🧍 {Name,-24} ║", ConsoleColor.White);
        Display.WriteColored($"║  HP:     {Display.HPBar(HP, MaxHP),20} ║", ConsoleColor.White);
        Display.WriteColored($"║  ❤  {HP}/{MaxHP,-24} ║", ConsoleColor.Green);
        Display.WriteColored($"║  ⚔  Aanval: {AttackDamage,-17} ║", ConsoleColor.Yellow);
        if (IsPoisoned)
            Display.WriteColored($"║  ☠  Vergiftigd ({PoisonTurns} beurten)     ║", ConsoleColor.DarkGreen);
        if (IsShielded)
            Display.WriteColored($"║  ⛊  Schild actief              ║", ConsoleColor.Cyan);
        Display.WriteColored($"║  🏆 Score: {Score,-18} ║", ConsoleColor.Magenta);
        Display.WriteColored("╚══════════════════════════════╝", ConsoleColor.White);
        Console.WriteLine();
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
    protected Random rng = new Random();

    // Constructor
    public Zombie(string name, int maxHP, int attackDamage)
    {
        Name = name;
        MaxHP = maxHP;
        HP = maxHP;
        AttackDamage = attackDamage;
    }

    // Zombie ontvangt schade
    public virtual void TakeDamage(int damage)
    {
        HP -= damage;
        if (HP < 0) HP = 0;

        if (IsDead)
            Display.WriteColored($"💀 {Name} valt uit elkaar!", ConsoleColor.DarkRed);
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

        Display.WriteColored($"🧟 {Name} strompelt naar je toe...", ConsoleColor.DarkYellow);
        Thread.Sleep(600);

        int dealt = player.TakeDamage(damage);
        if (dealt > 0)
            Display.WriteColored($"   Je ontvangt {dealt} schade! (HP: {player.HP}/{player.MaxHP})", ConsoleColor.Red);
        return dealt;
    }

    // Toon zombie stats
    public virtual void ShowStats()
    {
        string hpBar = Display.HPBar(HP, MaxHP);
        Display.WriteColored($"  🧟 {Name,-18} {hpBar} {HP}/{MaxHP} HP", ConsoleColor.DarkYellow);
    }
}

// ─────────────────────────────────────────────────────────────
// CLASS: FastZombie
// Weinig HP, maar valt TWEE keer per ronde aan
// ─────────────────────────────────────────────────────────────
class FastZombie : Zombie
{
    public FastZombie(string name) : base(name, maxHP: 35, attackDamage: 8) { }

    public override int Attack(Player player)
    {
        if (IsDead) return 0;
        Display.WriteColored($"💨 {Name} schiet razendsnel op je af...", ConsoleColor.Yellow);
        Thread.Sleep(400);

        int total = 0;
        for (int i = 0; i < 2; i++)  // Twee aanvallen!
        {
            int damage = rng.Next(4, 12);
            int dealt = player.TakeDamage(damage);
            if (dealt > 0)
            {
                Display.WriteColored($"   Aanval {i + 1}: {dealt} schade! (HP: {player.HP}/{player.MaxHP})", ConsoleColor.Red);
                total += dealt;
            }
            Thread.Sleep(300);
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
    public TankZombie(string name) : base(name, maxHP: 120, attackDamage: 6) { }

    public override void TakeDamage(int damage)
    {
        // Tankt 20% van alle schade weg
        int reduced = (int)(damage * 0.8);
        base.TakeDamage(reduced);
        if (!IsDead)
            Display.WriteColored($"   (De dikke huid absorbeert {damage - reduced} schade)", ConsoleColor.Gray);
    }
}

// ─────────────────────────────────────────────────────────────
// CLASS: PoisonZombie
// Vergiftigt de speler bij aanval
// ─────────────────────────────────────────────────────────────
class PoisonZombie : Zombie
{
    public PoisonZombie(string name) : base(name, maxHP: 50, attackDamage: 9) { }

    public override int Attack(Player player)
    {
        if (IsDead) return 0;
        Display.WriteColored($"🟢 {Name} spuwt giftig slijm...", ConsoleColor.DarkGreen);
        Thread.Sleep(600);

        int damage = rng.Next(6, 13);
        int dealt = player.TakeDamage(damage);
        if (dealt > 0)
        {
            Display.WriteColored($"   Je ontvangt {dealt} schade!", ConsoleColor.Red);
            // 60% kans op vergiftiging
            if (rng.Next(100) < 60)
            {
                player.Poison(3);
                Display.WriteColored("   ☠  Je bent vergiftigd! (3 beurten)", ConsoleColor.DarkGreen);
            }
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
    private int turnCount = 0;  // Telt hoeveel beurten de boss al leeft

    public BossZombie(string name) : base(name, maxHP: 200, attackDamage: 18) { }

    public override int Attack(Player player)
    {
        if (IsDead) return 0;
        turnCount++;

        // Elke 3 beurten: boss geneest zichzelf
        if (turnCount % 3 == 0)
        {
            Display.WriteColored($"💀 {Name} brult luid en herstelt 20 HP!", ConsoleColor.Magenta);
            Heal(20);
            Thread.Sleep(600);
        }

        // Speciale aanval om de 4 beurten
        if (turnCount % 4 == 0)
        {
            Display.WriteColored($"💀 {Name} gooit je de lucht in met een VERPLETTERENDE KLAP!", ConsoleColor.Magenta);
            Thread.Sleep(600);
            int damage = rng.Next(25, 35);
            int dealt = player.TakeDamage(damage);
            Display.WriteColored($"   KRITIEKE AANVAL! {dealt} schade! (HP: {player.HP}/{player.MaxHP})", ConsoleColor.Red);
            return dealt;
        }

        // Normale aanval
        Display.WriteColored($"👹 {Name} stormt op je af...", ConsoleColor.Magenta);
        Thread.Sleep(600);
        int dmg = rng.Next(12, 22);
        int d = player.TakeDamage(dmg);
        if (d > 0)
            Display.WriteColored($"   Je ontvangt {d} schade! (HP: {player.HP}/{player.MaxHP})", ConsoleColor.Red);
        return d;
    }

    public override void ShowStats()
    {
        string hpBar = Display.HPBar(HP, MaxHP);
        Display.WriteColored($"  👹 {Name,-18} {hpBar} {HP}/{MaxHP} HP  ← BOSS!", ConsoleColor.Magenta);
    }
}

// ─────────────────────────────────────────────────────────────
// CLASS: Display (hulpklasse voor mooie output)
// ─────────────────────────────────────────────────────────────
static class Display
{
    private static Random rng = new Random();

    // Schrijf gekleurde tekst
    public static void WriteColored(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    // Genereer een HP-balk
    public static string HPBar(int current, int max)
    {
        int barLength = 10;
        int filled = max > 0 ? (int)((double)current / max * barLength) : 0;
        return "[" + new string('█', filled) + new string('░', barLength - filled) + "]";
    }

    // Kleine pauze voor dramatisch effect
    public static void Pause(int ms = 800) => Thread.Sleep(ms);

    // Druk op enter om door te gaan
    public static void PressEnter()
    {
        WriteColored("\n[Druk op ENTER om door te gaan...]", ConsoleColor.DarkGray);
        Console.ReadLine();
    }

    // Toon het startscherm met ASCII art
    public static void ShowTitle()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine(@"
  ███████╗ ██████╗ ███╗   ███╗██████╗ ██╗███████╗
  ╚══███╔╝██╔═══██╗████╗ ████║██╔══██╗██║██╔════╝
    ███╔╝ ██║   ██║██╔████╔██║██████╔╝██║█████╗  
   ███╔╝  ██║   ██║██║╚██╔╝██║██╔══██╗██║██╔══╝  
  ███████╗╚██████╔╝██║ ╚═╝ ██║██████╔╝██║███████╗
  ╚══════╝ ╚═════╝ ╚═╝     ╚═╝╚═════╝ ╚═╝╚══════╝
");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("            ★  S U R V I V A L  ★");
        Console.ResetColor();
        Console.WriteLine();
        WriteColored("  Overleef de golven. Verslaan de boss. Ontsnappe levend.", ConsoleColor.Gray);
        Console.WriteLine();
    }

    // Toon wave-aankondiging
    public static void ShowWaveBanner(int wave, string label = "")
    {
        Console.WriteLine();
        WriteColored($"══════════════════════════════════════════", ConsoleColor.DarkYellow);
        WriteColored($"         ⚠  WAVE {wave}  {label}  ⚠", ConsoleColor.Yellow);
        WriteColored($"══════════════════════════════════════════", ConsoleColor.DarkYellow);
        Console.WriteLine();
        Pause(1000);
    }

    // Toon game over scherm
    public static void ShowGameOver(Player player)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine(@"
   ██████╗  █████╗ ███╗   ███╗███████╗
  ██╔════╝ ██╔══██╗████╗ ████║██╔════╝
  ██║  ███╗███████║██╔████╔██║█████╗  
  ██║   ██║██╔══██║██║╚██╔╝██║██╔══╝  
  ╚██████╔╝██║  ██║██║ ╚═╝ ██║███████╗
   ╚═════╝ ╚═╝  ╚═╝╚═╝     ╚═╝╚══════╝

              O V E R
");
        Console.ResetColor();
        WriteColored($"  De zombies hebben gewonnen...", ConsoleColor.Gray);
        Console.WriteLine();
        ShowEndStats(player);
    }

    // Toon victory scherm
    public static void ShowVictory(Player player)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(@"
  ██╗   ██╗ ██████╗ ████████╗ ██████╗ ██████╗ ██╗
  ██║   ██║██╔════╝ ╚══██╔══╝██╔═══██╗██╔══██╗██║
  ██║   ██║██║         ██║   ██║   ██║██████╔╝██║
  ╚██╗ ██╔╝██║         ██║   ██║   ██║██╔══██╗╚═╝
   ╚████╔╝ ╚██████╗    ██║   ╚██████╔╝██║  ██║██╗
    ╚═══╝   ╚═════╝    ╚═╝    ╚═════╝ ╚═╝  ╚═╝╚═╝
");
        Console.ResetColor();
        WriteColored("  🎉 Je hebt alle waves overleefd! GEWELDIG!", ConsoleColor.Green);
        Console.WriteLine();
        ShowEndStats(player);
    }

    // Eindscore scherm
    private static void ShowEndStats(Player player)
    {
        WriteColored("╔══════════════════════════════════╗", ConsoleColor.White);
        WriteColored("║       EINDSTATISTIEKEN           ║", ConsoleColor.White);
        WriteColored($"║  Naam:       {player.Name,-20} ║", ConsoleColor.White);
        WriteColored($"║  Score:      {player.Score,-20} ║", ConsoleColor.Magenta);
        WriteColored($"║  Zombies:    {player.KillCount,-20} ║", ConsoleColor.Red);
        WriteColored($"║  HP over:    {player.HP}/{player.MaxHP,-17} ║", ConsoleColor.Green);
        WriteColored("╚══════════════════════════════════╝", ConsoleColor.White);
        Console.WriteLine();
    }
}

// ─────────────────────────────────────────────────────────────
// CLASS: Game (de hoofdklasse die alles bestuurt)
// ─────────────────────────────────────────────────────────────
class Game
{
    private Player player;
    private Random rng = new Random();
    private Difficulty difficulty;
    private int currentWave = 0;
    private const int TOTAL_WAVES = 5;

    // Start het spel
    public void Run()
    {
        Display.ShowTitle();
        Setup();
        GameLoop();
    }

    // Initialiseer speler en moeilijkheidsgraad
    private void Setup()
    {
        Console.WriteLine();
        Display.WriteColored("  Voer je naam in:", ConsoleColor.White);
        Console.Write("  > ");
        string name = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(name)) name = "Overlever";

        Console.WriteLine();
        Display.WriteColored("  Kies moeilijkheidsgraad:", ConsoleColor.White);
        Display.WriteColored("  1. Makkelijk  (meer HP, minder zombieschade)", ConsoleColor.Green);
        Display.WriteColored("  2. Normaal    (standaard)", ConsoleColor.Yellow);
        Display.WriteColored("  3. Moeilijk   (minder HP, meer zombieschade)", ConsoleColor.Red);
        Console.Write("  > ");
        string choice = Console.ReadLine();

        // Stel speler in op basis van moeilijkheid
        switch (choice)
        {
            case "1":
                difficulty = Difficulty.Easy;
                player = new Player(name, maxHP: 150, attackDamage: 20);
                player.AddItem(new Item(ItemType.BigPotion));
                player.AddItem(new Item(ItemType.SmallPotion));
                player.AddItem(new Item(ItemType.Shield));
                Display.WriteColored("\n  Je speelt op MAKKELIJK. Veel succes!", ConsoleColor.Green);
                break;
            case "3":
                difficulty = Difficulty.Hard;
                player = new Player(name, maxHP: 70, attackDamage: 14);
                player.AddItem(new Item(ItemType.SmallPotion));
                Display.WriteColored("\n  Je speelt op MOEILIJK. Vaarwel...", ConsoleColor.Red);
                break;
            default:
                difficulty = Difficulty.Normal;
                player = new Player(name, maxHP: 100, attackDamage: 17);
                player.AddItem(new Item(ItemType.SmallPotion));
                player.AddItem(new Item(ItemType.SmallPotion));
                Display.WriteColored("\n  Je speelt op NORMAAL. Succes!", ConsoleColor.Yellow);
                break;
        }

        Display.PressEnter();
    }

    // Hoofd game loop: doorloop alle waves
    private void GameLoop()
    {
        for (int wave = 1; wave <= TOTAL_WAVES; wave++)
        {
            currentWave = wave;
            List<Zombie> zombies = SpawnWave(wave);

            Display.ShowWaveBanner(wave, wave == TOTAL_WAVES ? "⚡ BOSS FIGHT ⚡" : "");

            bool ranAway = RunCombat(zombies);

            if (player.IsDead)
            {
                Display.ShowGameOver(player);
                return;
            }

            if (!ranAway)
            {
                Display.WriteColored($"\n✅ Wave {wave} overleefd! Uitstekend werk, {player.Name}!", ConsoleColor.Green);
                player.AddScore(wave * 50);

                if (wave < TOTAL_WAVES)
                {
                    Display.PressEnter();
                    WaveReward(wave);
                }
            }
            else
            {
                Display.WriteColored("\n🏃 Je bent weggevlucht! Het spel is over.", ConsoleColor.DarkYellow);
                Display.ShowGameOver(player);
                return;
            }
        }

        Display.ShowVictory(player);
    }

    // Maak zombies aan voor een bepaalde wave
    private List<Zombie> SpawnWave(int wave)
    {
        var zombies = new List<Zombie>();
        int diffMultiplier = difficulty == Difficulty.Easy ? 1 : difficulty == Difficulty.Hard ? 2 : 1;

        switch (wave)
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
                Display.WriteColored("De grond trilt... ZOMBIE LORD MORTIS staat op!", ConsoleColor.Magenta);
                Display.Pause(1500);
                break;
        }

        // Verdubbel HP op Hard
        if (difficulty == Difficulty.Hard)
            foreach (var z in zombies)
                z.Heal(z.MaxHP / 2);  // Extra HP voor hard mode

        return zombies;
    }

    // Voer het gevecht uit voor één wave
    private bool RunCombat(List<Zombie> zombies)
    {
        bool ranAway = false;

        while (!player.IsDead && zombies.Exists(z => !z.IsDead))
        {
            // Toon situatie
            Console.Clear();
            Display.WriteColored($"══ WAVE {currentWave}/{TOTAL_WAVES} ══════════════════════════", ConsoleColor.DarkYellow);
            player.ShowStats();

            Display.WriteColored("VIJANDEN:", ConsoleColor.DarkRed);
            foreach (var z in zombies)
                if (!z.IsDead) z.ShowStats();

            // Toon inventory als er items zijn
            if (player.Inventory.Count > 0)
            {
                Console.WriteLine();
                Display.WriteColored("RUGZAK:", ConsoleColor.DarkCyan);
                for (int i = 0; i < player.Inventory.Count; i++)
                    Display.WriteColored($"  {i + 1}. {player.Inventory[i].Name} - {player.Inventory[i].Description}", ConsoleColor.Cyan);
            }

            // Speler kiest actie
            Console.WriteLine();
            Display.WriteColored("╔══════════════════════════╗", ConsoleColor.White);
            Display.WriteColored("║  WAT DOE JE?             ║", ConsoleColor.White);
            Display.WriteColored("║  1. Aanvallen            ║", ConsoleColor.White);
            Display.WriteColored("║  2. Zware aanval         ║", ConsoleColor.White);
            Display.WriteColored("║  3. Genezen              ║", ConsoleColor.White);
            Display.WriteColored("║  4. Item gebruiken       ║", ConsoleColor.White);
            Display.WriteColored("║  5. Stats tonen          ║", ConsoleColor.White);
            Display.WriteColored("║  6. Vluchten             ║", ConsoleColor.White);
            Display.WriteColored("╚══════════════════════════╝", ConsoleColor.White);
            Console.Write("  Keuze: ");

            string input = Console.ReadLine();
            Console.WriteLine();

            bool playerActed = true;

            switch (input)
            {
                case "1": // Normale aanval
                    AttackZombie(zombies, false);
                    break;

                case "2": // Zware aanval (kans op missen)
                    AttackZombie(zombies, true);
                    break;

                case "3": // Genezen (kost een beurt)
                    int healAmount = rng.Next(15, 30);
                    player.Heal(healAmount);
                    Display.WriteColored($"🩹 Je verbindt je wonden en herstelt {healAmount} HP. (HP: {player.HP}/{player.MaxHP})", ConsoleColor.Green);
                    break;

                case "4": // Item gebruiken
                    if (player.Inventory.Count == 0)
                    {
                        Display.WriteColored("Je rugzak is leeg!", ConsoleColor.DarkGray);
                        playerActed = false;
                        break;
                    }
                    Console.Write("  Welk item (nummer)? ");
                    if (int.TryParse(Console.ReadLine(), out int itemIdx))
                        player.UseItem(itemIdx - 1, zombies);
                    else
                        playerActed = false;
                    break;

                case "5": // Stats tonen (geen beurt)
                    player.ShowStats();
                    Display.PressEnter();
                    playerActed = false;
                    break;

                case "6": // Vluchten
                    // 40% kans om te vluchten
                    if (rng.Next(100) < 40)
                    {
                        Display.WriteColored("🏃 Je rent weg! Je overleeft, maar de missie is mislukt.", ConsoleColor.DarkYellow);
                        return true;
                    }
                    else
                    {
                        Display.WriteColored("🏃 Je probeert te vluchten maar de zombies zijn te snel!", ConsoleColor.Red);
                    }
                    break;

                default:
                    Display.WriteColored("Ongeldige keuze.", ConsoleColor.DarkGray);
                    playerActed = false;
                    break;
            }

            // Verwerk vergiftiging na speler-beurt
            if (playerActed)
                player.ProcessPoison();

            if (player.IsDead) break;

            // Check of alle zombies dood zijn
            if (!zombies.Exists(z => !z.IsDead)) break;

            // Zombies vallen terug aan
            Display.Pause(600);
            Display.WriteColored("\n--- ZOMBIES VALLEN AAN ---", ConsoleColor.DarkRed);
            Display.Pause(400);
            foreach (var z in zombies)
                if (!z.IsDead) z.Attack(player);

            Display.PressEnter();
        }

        return ranAway;
    }

    // Verwerk een aanval van de speler op een zombie
    private void AttackZombie(List<Zombie> zombies, bool heavy)
    {
        // Kies doelwit als er meerdere zombies zijn
        List<Zombie> aliveZombies = zombies.FindAll(z => !z.IsDead);
        Zombie target;

        if (aliveZombies.Count > 1)
        {
            Display.WriteColored("Kies een doelwit:", ConsoleColor.White);
            for (int i = 0; i < aliveZombies.Count; i++)
                Display.WriteColored($"  {i + 1}. {aliveZombies[i].Name}", ConsoleColor.DarkYellow);
            Console.Write("  > ");

            if (int.TryParse(Console.ReadLine(), out int targetIdx) && targetIdx >= 1 && targetIdx <= aliveZombies.Count)
                target = aliveZombies[targetIdx - 1];
            else
                target = aliveZombies[0];
        }
        else
        {
            target = aliveZombies[0];
        }

        Console.WriteLine();

        if (heavy)
        {
            // Zware aanval: 30% kans te missen, maar 2x schade bij treffer
            if (rng.Next(100) < 30)
            {
                Display.WriteColored("💨 Je mist je aanval! De zombie duwt je opzij.", ConsoleColor.DarkGray);
                return;
            }
            int damage = player.AttackDamage * 2;
            // Critical hit kans
            if (rng.Next(100) < 20)
            {
                damage = (int)(damage * 1.5);
                Display.WriteColored("⚡ CRITICAL HIT!", ConsoleColor.Yellow);
            }
            Display.WriteColored($"🪓 ZWARE AANVAL! Je slaat {target.Name} voor {damage} schade!", ConsoleColor.Yellow);
            target.TakeDamage(damage);
        }
        else
        {
            // Normale aanval met kleine variatie
            int damage = rng.Next(player.AttackDamage - 3, player.AttackDamage + 5);
            if (damage < 1) damage = 1;

            // 15% kans op critical hit
            bool crit = rng.Next(100) < 15;
            if (crit)
            {
                damage = (int)(damage * 1.5);
                Display.WriteColored("⚡ CRITICAL HIT!", ConsoleColor.Yellow);
            }

            Display.WriteColored($"⚔  Je valt {target.Name} aan voor {damage} schade!", ConsoleColor.White);
            target.TakeDamage(damage);
        }

        // Voeg score toe bij kill
        if (target.IsDead)
        {
            int points = target is BossZombie ? 500 : target is TankZombie ? 150 : 100;
            player.AddScore(points);
            Display.WriteColored($"  +{points} punten!", ConsoleColor.Magenta);
            Display.Pause(500);

            // Kans op loot
            DropLoot(target);
        }
    }

    // Willekeurige loot na het verslaan van een zombie
    private void DropLoot(Zombie zombie)
    {
        int chance = rng.Next(100);
        if (chance < 35)  // 35% kans op loot
        {
            ItemType[] possibleLoot = { ItemType.SmallPotion, ItemType.SmallPotion,
                                         ItemType.BigPotion, ItemType.Bomb,
                                         ItemType.PoisonCure, ItemType.Shield };
            Item loot = new Item(possibleLoot[rng.Next(possibleLoot.Length)]);
            player.AddItem(loot);
            Display.WriteColored($"🎁 Je vindt een {loot.Name} op het lijk!", ConsoleColor.Cyan);
        }
    }

    // Beloning na een wave: speler mag een upgrade kiezen
    private void WaveReward(int wave)
    {
        Console.Clear();
        Display.WriteColored("══════════════════════════════════════", ConsoleColor.Yellow);
        Display.WriteColored($"  🏅 WAVE {wave} COMPLEET - Kies je beloning:", ConsoleColor.Yellow);
        Display.WriteColored("══════════════════════════════════════", ConsoleColor.Yellow);
        Console.WriteLine();
        Display.WriteColored("  1. ❤  HP volledig herstellen", ConsoleColor.Green);
        Display.WriteColored("  2. ⚔  Aanval +5", ConsoleColor.Yellow);
        Display.WriteColored("  3. 💪 Max HP +20", ConsoleColor.Cyan);
        Display.WriteColored("  4. 🧪 Ontvang een Grote Potion", ConsoleColor.Magenta);
        Console.WriteLine();
        Console.Write("  Keuze: ");

        string choice = Console.ReadLine();
        Console.WriteLine();

        switch (choice)
        {
            case "1":
                player.Heal(player.MaxHP);
                Display.WriteColored("  ❤  Je HP is volledig hersteld!", ConsoleColor.Green);
                break;
            case "2":
                player.UpgradeAttack(5);
                break;
            case "3":
                player.UpgradeMaxHP(20);
                break;
            case "4":
                player.AddItem(new Item(ItemType.BigPotion));
                Display.WriteColored("  🧪 Grote Potion toegevoegd aan rugzak!", ConsoleColor.Magenta);
                break;
            default:
                player.Heal(player.MaxHP);
                Display.WriteColored("  ❤  HP hersteld (standaard keuze).", ConsoleColor.Green);
                break;
        }

        Display.PressEnter();
    }
}

// ─────────────────────────────────────────────────────────────
// PROGRAM: Ingangspunt van de applicatie
// ─────────────────────────────────────────────────────────────
class Program
{
    static void Main(string[] args)
    {
        // Zet de console op voor unicode tekens (symbolen)
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "Zombie Survival";

        bool playAgain = true;
        while (playAgain)
        {
            Game game = new Game();
            game.Run();

            Console.WriteLine();
            Display.WriteColored("Wil je opnieuw spelen? (j/n)", ConsoleColor.White);
            Console.Write("> ");
            string again = Console.ReadLine()?.Trim().ToLower();
            playAgain = again == "j" || again == "ja" || again == "y" || again == "yes";
        }

        Display.WriteColored("Tot ziens, overlever. Blijf leven. 🧟", ConsoleColor.DarkGray);
    }
}