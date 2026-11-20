# Structure technique de la solution

## Principes

- Domain ne dépend d'aucun autre projet.
- Application dépend de Domain et porte les cas d'usage et validations.
- Infrastructure implémente la persistance et les adaptateurs techniques.
- Server compose Application et Infrastructure et expose l'API HTTP.
- Shared ne contient que les contrats de transport indépendants de l'interface.
- Client.Core contient les services client, sans accès à SQLite.
- Client.Avalonia contient exclusivement l'interface et ses ViewModels.
- Les clients ne référencent jamais Infrastructure et ne reçoivent jamais de chemin de base SQLite.

## Projets

| Projet | Responsabilité |
|---|---|
| Domain | Entités, invariants métier, énumérations et événements métier |
| Application | Cas d'usage, interfaces, validation et orchestration |
| Infrastructure | EF Core, SQLite, stockage sécurisé et adaptateurs |
| Server | API, authentification, autorisation, composition et hébergement |
| Shared | DTO et contrats API versionnés |
| Client.Core | Authentification côté client, appels API et état de session en mémoire |
| Client.Avalonia | UI Windows/Android, navigation, vues et ViewModels |
| Tests | Tests unitaires, intégration API et tests de persistance |

## Isolation des entreprises

Chaque instance serveur est configurée pour une seule entreprise et une seule base SQLite centrale. Le chemin de la base est une configuration serveur, jamais une donnée fournie par le client HTTP. Une instance ne doit jamais accepter un identifiant de tenant arbitraire pour sélectionner un fichier. La séparation physique des bases doit être renforcée par les contrôles d'identité et d'autorisation de l'API.

## Sécurité

Les endpoints métier ne doivent pas être exposés avant l'implémentation de l'authentification, de l'autorisation et de la gestion des sessions. Les endpoints de santé ne retournent aucune information sensible. Les secrets et clés ne sont jamais stockés dans appsettings.json ou dans le dépôt.

## État d'implémentation

Cette première tranche pose la solution, les projets Domain/Application/Infrastructure/Server/Shared/Client.Core, le shell Avalonia Windows/Android, le client HTTP initial, les entités initiales, la configuration EF Core et les premiers tests du domaine. Elle ne constitue pas encore un produit déployable : l'authentification, les migrations, le provisioning des entreprises, les licences, les appareils, les parcours UI complets, les tests d'intégration et le packaging restent à implémenter.
