using MidgardStudio.Core.Schema;

namespace MidgardStudio.Core.Schemas;

/// <summary>
/// Static predefined value sets for the item database (and shared with other DBs where noted).
/// Values mirror rAthena's YAML enums exactly.
/// </summary>
public static class ItemEnums
{
    public static readonly EnumSource Type = EnumSource.Labeled("ItemType",
        ("Healing", "Healing Item"),
        ("Usable", "Usable"),
        ("Etc", "Etc / Miscellaneous"),
        ("Armor", "Armor"),
        ("Weapon", "Weapon"),
        ("Card", "Card"),
        ("PetEgg", "Pet Egg"),
        ("PetArmor", "Pet Equipment"),
        ("Ammo", "Ammunition"),
        ("DelayConsume", "Delayed Consumable"),
        ("ShadowGear", "Shadow Equipment"),
        ("Cash", "Cash Shop Item"));

    // Combined Weapon + Ammo + Card subtypes. Labels are display-only; the raw value is stored in YAML.
    public static readonly EnumSource SubType = EnumSource.Labeled("ItemSubType",
        // weapons
        ("Fist", "Bare Fist"),
        ("Dagger", "Dagger"),
        ("1hSword", "One-Handed Sword"),
        ("2hSword", "Two-Handed Sword"),
        ("1hSpear", "One-Handed Spear"),
        ("2hSpear", "Two-Handed Spear"),
        ("1hAxe", "One-Handed Axe"),
        ("2hAxe", "Two-Handed Axe"),
        ("Mace", "Mace"),
        ("Staff", "One-Handed Staff"),
        ("Bow", "Bow"),
        ("Knuckle", "Knuckle"),
        ("Musical", "Musical Instrument"),
        ("Whip", "Whip"),
        ("Book", "Book"),
        ("Katar", "Katar"),
        ("Revolver", "Revolver"),
        ("Rifle", "Rifle"),
        ("Gatling", "Gatling Gun"),
        ("Shotgun", "Shotgun"),
        ("Grenade", "Grenade Launcher"),
        ("Huuma", "Huuma Shuriken"),
        ("2hStaff", "Two-Handed Staff"),
        // ammo
        ("Arrow", "Arrow"),
        ("Bullet", "Bullet"),
        ("Shell", "Shell"),
        ("Shuriken", "Shuriken"),
        ("Kunai", "Kunai"),
        ("CannonBall", "Cannon Ball"),
        ("ThrowWeapon", "Throwing Weapon"),
        // card
        ("Normal", "Normal (Card)"),
        ("Enchant", "Enchant (Card)"));

    public static readonly EnumSource Gender = EnumSource.Static("Gender",
        "Female", "Male", "Both");

    public static readonly EnumSource Jobs = EnumSource.Static("Jobs",
        "All", "Acolyte", "Alchemist", "Archer", "Assassin", "BardDancer",
        "Blacksmith", "Crusader", "Gunslinger", "Hunter", "KagerouOboro",
        "Knight", "Mage", "Merchant", "Monk", "Ninja", "Novice", "Priest",
        "Rebellion", "Rogue", "Sage", "SoulLinker", "StarGladiator",
        "Summoner", "SuperNovice", "Swordman", "Taekwon", "Thief", "Wizard");

    public static readonly EnumSource Classes = EnumSource.Labeled("Classes",
        ("All", "All Classes"),
        ("Normal", "Normal"),
        ("Upper", "Transcendent"),
        ("Baby", "Baby"),
        ("Third", "Third Class"),
        ("Third_Upper", "Third Class (Trans)"),
        ("Third_Baby", "Third Class (Baby)"),
        ("Fourth", "Fourth Class"),
        ("All_Upper", "All Transcendent"),
        ("All_Baby", "All Baby"),
        ("All_Third", "All Third Class"));

    public static readonly EnumSource Locations = EnumSource.Labeled("Locations",
        ("Head_Top", "Upper Headgear"),
        ("Head_Mid", "Middle Headgear"),
        ("Head_Low", "Lower Headgear"),
        ("Armor", "Armor"),
        ("Right_Hand", "Right Hand (Weapon)"),
        ("Left_Hand", "Left Hand (Shield)"),
        ("Garment", "Garment"),
        ("Shoes", "Shoes"),
        ("Right_Accessory", "Right Accessory"),
        ("Left_Accessory", "Left Accessory"),
        ("Costume_Head_Top", "Costume Upper Headgear"),
        ("Costume_Head_Mid", "Costume Middle Headgear"),
        ("Costume_Head_Low", "Costume Lower Headgear"),
        ("Costume_Garment", "Costume Garment"),
        ("Ammo", "Ammunition"),
        ("Shadow_Armor", "Shadow Armor"),
        ("Shadow_Weapon", "Shadow Weapon"),
        ("Shadow_Shield", "Shadow Shield"),
        ("Shadow_Shoes", "Shadow Shoes"),
        ("Shadow_Right_Accessory", "Shadow Right Accessory"),
        ("Shadow_Left_Accessory", "Shadow Left Accessory"));

    public static readonly EnumSource DropEffect = EnumSource.Static("DropEffect",
        "None", "Client", "White", "Blue", "Yellow", "Purple", "Orange", "Green", "Red");

    /// <summary>Grade levels (renewal) used by Gradable/grade min-max fields.</summary>
    public static readonly EnumSource Grade = EnumSource.Static("Grade",
        "None", "D", "C", "B", "A");

    /// <summary>Bind types used across item groups/rewards.</summary>
    public static readonly EnumSource BindType = EnumSource.Static("BindType",
        "None", "Account", "Guild", "Party", "Character");
}
