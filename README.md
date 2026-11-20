# EnterpriseManager

Plateforme de gestion d’entreprise multi-secteurs, conçue pour être installée sur Windows et Android.

## État du dépôt

Ce dépôt contient le cadrage technique, la documentation d’architecture et l’arborescence initiale. Aucun projet applicatif n’a encore été généré et aucune compilation n’a été effectuée.

## Décisions techniques

- **Langage :** C#
- **Runtime :** .NET 10 LTS
- **Interface :** Avalonia UI, XAML et MVVM (Windows et Android)
- **Serveur :** ASP.NET Core Web API
- **Persistance :** une base SQLite centrale par entreprise
- **Accès aux données :** exclusivement par l’API ; aucun accès direct au fichier SQLite depuis les clients
- **ORM :** Entity Framework Core
- **Tests prévus :** xUnit
- **Journalisation :** Serilog
- **Validation :** FluentValidation
- **PDF :** QuestPDF, sous réserve de validation de sa licence commerciale

## Architecture retenue

Chaque entreprise dispose de son propre serveur technique et de sa propre base SQLite centrale. Les clients Windows et Android communiquent avec ce serveur via l’API. Aucun stockage métier local ni mode hors connexion n’est prévu. Si le serveur est indisponible, les données ne sont pas accessibles depuis les clients.

L’accès distant doit passer par un réseau privé sécurisé, par exemple un VPN. Le serveur doit être démarré et joignable.

## Arborescence

Voir [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) et [docs/ROADMAP.md](docs/ROADMAP.md).

## Démarrage du projet

Le travail actuel est limité à la documentation et à la préparation du dépôt. Les projets .NET, fichiers de solution et builds seront réalisés dans une phase ultérieure, sur décision explicite.
