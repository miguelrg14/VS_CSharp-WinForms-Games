using OopGames.Core.Arena;
using OopGames.Core.ElementalTower;
using OopGames.Desktop.Forms;
using OopGames.Desktop.Ui;

namespace OopGames.Desktop.Tests;

/// <summary>
/// Builds every window and lays it out without showing it. This catches runtime errors in the
/// form-building code (invalid property values, null controls...) that the compiler cannot see.
/// </summary>
public class FormSmokeTests
{
    public static TheoryData<string> FormNames => ["Launcher", "Basics", "Duel", "ElementalTower", "Arena", "Blackjack"];

    [Theory]
    [MemberData(nameof(FormNames))]
    public void Form_CanBeBuiltAndLaidOut(string name)
    {
        using Form form = name switch
        {
            "Launcher" => new LauncherForm(),
            "Basics" => new BasicsForm(),
            "Duel" => new DuelForm(),
            "ElementalTower" => new ElementalTowerForm(),
            "Arena" => new ArenaForm(),
            _ => new BlackjackForm(),
        };

        form.PerformLayout();
        Size size = form.GetPreferredSize(Size.Empty);

        Assert.InRange(size.Width, 200, 2000);
        Assert.InRange(size.Height, 150, 2000);
    }

    [Fact]
    public void Sprites_AreDrawnForEveryWeaponAndElement()
    {
        foreach (Weapon weapon in Enum.GetValues<Weapon>())
        {
            using Bitmap sprite = Sprites.ForWeapon(weapon, Team.Player);
            Assert.Equal(Sprites.Size, sprite.Width);
        }

        foreach (Element element in Enum.GetValues<Element>())
        {
            using Bitmap sprite = Sprites.ForElement(element);
            Assert.Equal(Sprites.Size, sprite.Height);
        }

        using Bitmap grave = Sprites.Grave();
        Assert.Equal(Sprites.Size, grave.Width);
    }
}
