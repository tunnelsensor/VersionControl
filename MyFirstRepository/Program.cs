Zombie zombie1 = new Zombie(10);

zombie1.damage(1);
zombie1.showStats();
zombie1.heal(2);
zombie1.showStats();
zombie1.damage(200);
zombie1.showStats();

class Zombie
{
    int maxHP = 0;
    int hp = 0;

    public Zombie(int maxhp)
    {
        this.maxHP = maxhp;
        this.hp = maxhp;
    }

    public void heal(int healHP)
    {
        hp += healHP;
        if (hp > maxHP)
        {
            hp = maxHP;
        }
    }
    public void damage(int damage)
    {
        hp -= damage;
        if (hp < 0)
        {
            dead();
        }
    }

    public void dead()
    {
        Console.WriteLine("Dead");
    }

    public void showStats()
    {
        Console.WriteLine("------------------");
        Console.WriteLine("MaxHP: " + maxHP);
        Console.WriteLine("Hp: " + hp);
        Console.WriteLine("------------------");

    }
}