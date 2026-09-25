# DMX

Application Windows (C# / .NET 10 / Avalonia) de pilotage d'éclairage DMX, alternative légère à Daslight 4,
avec, à terme, un mode automatique musical. Sortie DMX par Arduino Leonardo + shield (firmware compatible Enttec).

- Documentation (cahier des charges, feuille de route, règles) : [docs/README.md](docs/README.md)
- Règles de développement : [docs/03-regles-de-developpement.md](docs/03-regles-de-developpement.md)
- Démonstrations par phase : [docs/demos/](docs/demos/)
- Show de référence : [samples/Show de référence/](samples/Show%20de%20r%C3%A9f%C3%A9rence/JOURNAL.md)
- Firmware : [firmware/arduino-dmx/](firmware/arduino-dmx/README.md)

## Construire et tester

```bash
dotnet build Dmx.slnx
dotnet test --solution Dmx.slnx -- --filter-not-trait "Categorie=Materiel"
```

## Lancer

```bash
dotnet run --project src/Dmx.App
dotnet run --project tools/Dmx.Tools.Headless -- aide
```
