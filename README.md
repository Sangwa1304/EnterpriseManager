# EnterpriseManager

Plateforme native de gestion d’entreprise multi-secteurs pour Windows et Android.

## État du dépôt

Le dépôt contient maintenant la première tranche de fondations : solution .NET, projets métier, application, infrastructure, serveur ASP.NET Core, contrats partagés, socle client Avalonia et premiers tests du domaine. Cette tranche est en cours d'implémentation et n'est pas encore un produit prêt à déployer.

## Stack technique

- C# et .NET 10
- Avalonia UI, XAML et MVVM pour les clients natifs
- ASP.NET Core Web API pour le serveur privé
- Entity Framework Core et SQLite centrale
- FluentValidation et Serilog
- xUnit pour les tests

## Architecture

Chaque installation serveur est dédiée à une entreprise et utilise sa base SQLite centrale. Les clients Windows et Android accèdent aux données exclusivement par l'API. Aucun stockage métier local, accès direct à SQLite ou mode hors connexion n'est prévu.

Le serveur doit être démarré et joignable. L'accès distant doit passer par un réseau privé sécurisé, par exemple un VPN.

## Structure

- `src/EnterpriseManager.Domain` : modèle et règles métier
- `src/EnterpriseManager.Application` : cas d'usage et validations
- `src/EnterpriseManager.Infrastructure` : persistance et adaptateurs
- `src/EnterpriseManager.Server` : API et hébergement
- `src/EnterpriseManager.Shared` : contrats API
- `src/EnterpriseManager.Client.Core` : services partagés des clients
- `src/EnterpriseManager.Client.Avalonia` : application native
- `tests` : tests automatisés

Consulter [la structure détaillée](docs/PROJECT-STRUCTURE.md), [l'architecture](docs/ARCHITECTURE.md) et [la feuille de route](docs/ROADMAP.md).

## Identité et authentification

Le socle Identity est en cours d'implémentation : création initiale contrôlée du Directeur, authentification par JWT, renouvellement par jeton opaque rotatif et limitation des tentatives de connexion. La documentation est disponible dans [IDENTITY-AUTHENTICATION.md](docs/IDENTITY-AUTHENTICATION.md).

Le schéma SQLite doit encore être initialisé par l'outil de provisioning et les migrations avant de pouvoir utiliser ces endpoints. Les demandes d'adhésion, l'approbation par le Directeur, l'attribution initiale de rôles/licences et la suspension/révocation des membres sont maintenant présentes dans le socle API. Voir [MEMBERSHIP-LICENSING.md](docs/MEMBERSHIP-LICENSING.md). Le Directeur peut aussi changer le rôle, renouveler une licence, suspendre, réactiver ou révoquer un membre. La validation des JWT vérifie l'état actuel du compte, de l'adhésion et de la licence en base centrale. Le Directeur peut aussi gérer les appareils enregistrés et révoquer un appareil perdu. Les jetons de renouvellement sont liés à un appareil, et les requêtes authentifiées vérifient son état actuel. Voir [DEVICE-MANAGEMENT.md](docs/DEVICE-MANAGEMENT.md). Les jetons de renouvellement sont associés à un appareil enregistré. Le Directeur peut consulter les appareils et révoquer un appareil perdu, ce qui bloque aussi ses jetons d'accès actifs. Le client devra encore stocker son identifiant d'installation dans le stockage sécurisé Windows/Android. Voir [DEVICE-MANAGEMENT.md](docs/DEVICE-MANAGEMENT.md). Les permissions détaillées et l'interface complète restent à développer.

## État de validation

Aucune compilation, aucun build, aucun test et aucune exécution d'application n'ont été effectués. Le dépôt reste en phase d'implémentation et ne doit pas encore être déployé en production.
