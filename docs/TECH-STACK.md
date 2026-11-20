# Stack technologique

| Domaine | Choix retenu | Notes |
|---|---|---|
| Langage | C# | Langage principal |
| Runtime | .NET 10 LTS | Cible de référence |
| UI | Avalonia UI | Windows et Android |
| Présentation | XAML + MVVM | Séparation vues / état / logique |
| API | ASP.NET Core Web API | Serveur technique, pas un site public |
| ORM | Entity Framework Core | Accès aux données côté serveur |
| Base | SQLite | Une base centrale par entreprise |
| Validation | FluentValidation | Validation des commandes et entrées |
| Logs | Serilog | Journaux structurés |
| PDF | QuestPDF | Vérifier les conditions de licence commerciale |
| Tests | xUnit | Tests unitaires et d’intégration |

## Règles

- Le client Avalonia ne référence pas l’implémentation Infrastructure.
- Le client ne contient ni fichier de base métier ni chaîne d’accès SQLite.
- Le serveur est seul responsable de l’accès à la base centrale.
- Les contrats exposés par l’API ne doivent pas exposer directement les entités EF Core.
- Les versions exactes des packages seront fixées au moment de l’initialisation technique, puis centralisées.
