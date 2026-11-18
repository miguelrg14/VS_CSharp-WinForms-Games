# OOP Games in C# (Windows Forms)

[![Build & Test](https://github.com/miguelrg14/VS_OOP_Examples/actions/workflows/dotnet.yml/badge.svg)](https://github.com/miguelrg14/VS_OOP_Examples/actions/workflows/dotnet.yml)

Object-oriented programming coursework from my second year of Computer Science & Video Game Engineering at
ESNE (2020–2022), refactored into a single .NET 10 solution: a desktop launcher with five Windows Forms projects,
from inheritance basics to a two-player card duel, with every game rule kept in a separate, unit-tested library.

Part 2 of my university C# work. Part 1, console fundamentals and data structures:
[VS_CSharp-Fundamentals](https://github.com/miguelrg14/VS_CSharp-Fundamentals).

## Projects

All of them open from the launcher window, in the order they were made during the course.

| # | Project | What it shows |
| --- | --- | --- |
| 1 | **OOP basics** | Abstract classes and overriding (a character roster), message boxes and owned windows, Fibonacci and factorial with `BigInteger`, distance between two points, and a football squad table bound to a `DataGridView` |
| 2 | **Duel** | A striker with critical hits against a guardian with a shield. Both implement `IAttacker` and `IDamageable`, so one `DuelRules.Attack` method works for any pair |
| 3 | **Elemental tower** | Fight a queue of fire, water and grass enemies; the element chart doubles or halves your damage |
| 4 | **Weapon arena** | Three against three with the sword > axe > spear triangle. You choose who attacks whom; the enemy AI looks for targets it beats |
| 5 | **Twenty-one duel** | Final project. Two players draw cards 1–11 trying to get closest to 21 without going over. The round winner hits the loser, and coins buy trump cards that add damage or shield, return a card, raise the target to 24 or 27, or take away points |

## Structure

```
src/OopGames.Core       Game rules only (net10.0, no UI): characters, combat, battles, the 21 game
src/OopGames.Desktop    Windows Forms app (net10.0-windows): launcher and one form per project
tests/OopGames.Core.Tests   xUnit tests for every rule in OopGames.Core
```

The forms do not contain game logic: they call methods such as `TowerBattle.Attack` or `TwentyOneGame.Draw`
and redraw the state. Because the rules live in a plain class library, they are tested on every push, even
though the windows themselves only run on Windows.

The forms are built in code (no designer files), so each screen can be read top to bottom in one file.
The unit sprites are drawn at runtime with GDI+, so the project contains no third-party art or audio.

## Getting started

Requires Windows and the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/miguelrg14/VS_OOP_Examples.git
cd VS_OOP_Examples

dotnet run --project src/OopGames.Desktop   # opens the launcher
dotnet test                                  # runs the rule tests
```

Or open `OopGames.sln` in Visual Studio and run `OopGames.Desktop`.

## Design notes

- **Inheritance where it models a real "is a" relationship**: `Goalkeeper` and `OutfieldPlayer` both derive from
  `SquadMember`, and `Striker` and `Guardian` from `Fighter`. Subclasses override only what makes them different.
- **Interfaces for behaviour**: anything that is an `IAttacker` can hit anything that is an `IDamageable`.
- **Testable randomness**: every class that rolls dice receives a `Random`, so tests can pass a seeded or fixed one.
- **No impossible states**: a card is removed from the deck when it is drawn, so it can never be dealt twice;
  health never drops below zero; points are computed from the hand instead of being stored separately.

## License

[MIT](LICENSE)
