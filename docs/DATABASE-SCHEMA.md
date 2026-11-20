# Contrat du schéma de la base centrale

## Portée

Une base SQLite correspond à une seule entreprise. Le cloisonnement entre entreprises est assuré par des fichiers de base distincts et des instances de serveur/configurations distinctes, pas par un champ `CompanyId` répété dans chaque table métier.

Les clients Windows et Android n'ouvrent jamais cette base. Ils passent exclusivement par l'API du serveur de leur entreprise.

Ce document décrit le modèle actuellement déclaré dans `CompanyDbContext` et les conventions Identity utilisées. Il sert de référence pour relire la migration initiale et le modèle EF Core ; il ne remplace pas les artefacts générés.

## Tables ASP.NET Core Identity

`CompanyDbContext` hérite de `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`. Le schéma Identity standard doit donc être inclus dans la migration initiale :

| Table | Rôle |
|---|---|
| `AspNetUsers` | Comptes, identifiants Identity, état actif, nom d'affichage et date de création |
| `AspNetRoles` | Rôles Identity |
| `AspNetUserClaims` | Claims attribués directement aux utilisateurs |
| `AspNetRoleClaims` | Claims associés aux rôles |
| `AspNetUserLogins` | Connexions externes, même si elles ne sont pas encore activées |
| `AspNetUserRoles` | Association utilisateurs-rôles |
| `AspNetUserTokens` | Jetons Identity |

Les clés Identity sont des GUID. `ApplicationUser` ajoute `DisplayName` (obligatoire, 160 caractères maximum), `IsActive` et `CreatedAtUtc`. Un index unique supplémentaire est déclaré sur `NormalizedEmail`. Les index et contraintes standards Identity, notamment l'unicité du nom d'utilisateur normalisé, doivent être conservés.

## Tables propres à l'application

Les propriétés héritées de `Entity` sont présentes sur les entités métier qui en dérivent : `Id` (GUID), `CreatedAtUtc` et `UpdatedAtUtc` nullable.

| Table | Colonnes spécifiques | Contraintes et relations déclarées |
|---|---|---|
| `Companies` | `LegalName` (200), `BusinessIdentifier` (80), `TimeZone` (100), `Status` (enum stocké en texte), dates de base | `BusinessIdentifier` unique |
| `CompanyMemberships` | `UserId`, `RoleName` (80), `Status` (texte), `JoinedAtUtc`, `SuspendedAtUtc` | FK vers `AspNetUsers`, suppression en cascade ; index unique sur `UserId` |
| `MembershipRequests` | `UserId`, `Status` (texte), `RequestedAtUtc`, `DecidedAtUtc`, `DecidedByUserId`, `RejectionReason` (500) | FK vers `AspNetUsers`, suppression en cascade ; index unique sur `UserId` |
| `UserLicenses` | `UserId`, `PlanCode` (40), `StartsAtUtc`, `ExpiresAtUtc` nullable, `IsLifetime`, `Status` (texte), `RevokedAtUtc` | FK vers `AspNetUsers`, suppression en cascade ; index composite (`UserId`, `Status`) |
| `Devices` | `UserId`, `DeviceKey`, `DisplayName` (100), `Platform` (texte), `Status` (texte), dates d'enregistrement/activité/révocation, `RevokedByUserId` | FK vers `AspNetUsers`, suppression en cascade ; index unique (`UserId`, `DeviceKey`) |
| `RefreshTokens` | `UserId`, `DeviceId`, `TokenHash` (64), `ConcurrencyStamp`, dates de création/expiration/révocation, hash de remplacement, IP de création/révocation (64) | Hash unique ; index (`UserId`, `ExpiresAtUtc`) et index `DeviceId` ; FK vers utilisateur et appareil, suppressions en cascade ; `ConcurrencyStamp` est un jeton de concurrence |
| `AuditEvents` | `ActorUserId` nullable, `Action` (80), `TargetType` (80), `TargetId` (120), `OccurredAtUtc`, `IpAddress` (64), `Details` (1000) | Index (`OccurredAtUtc`, `Id`) et index `ActorUserId` ; aucune FK configurée afin de conserver les événements même si un compte est ultérieurement supprimé |

Les statuts et plateformes déclarés comme enums sont actuellement convertis en chaînes dans le modèle EF. Les dates sont des `DateTimeOffset`. La migration devra être relue pour vérifier la représentation SQLite effective et les valeurs par défaut produites.

## Points de vigilance à traiter avant la migration

1. **Une demande par utilisateur :** l'index unique de `MembershipRequests.UserId` interdit de conserver plusieurs demandes historiques pour un même utilisateur. Si l'historique des demandes est souhaité, remplacer cette unicité par un index adapté (par exemple un index non unique ou une stratégie d'unicité des seules demandes en attente).
2. **Une appartenance par utilisateur dans cette base :** l'index unique de `CompanyMemberships.UserId` exprime une seule appartenance par utilisateur et par base d'entreprise. Cela correspond au modèle d'une base distincte par entreprise ; le partage d'un même compte entre plusieurs entreprises n'est pas pris en charge par ce schéma.
3. **Références de décision/révocation :** `DecidedByUserId`, `RevokedByUserId` et `RevokedByUserId` sont des GUID scalaires dans le modèle actuel. Aucune relation EF explicite n'est configurée pour ces propriétés ; ne pas inventer de FK dans la migration.
4. **Suppression en cascade :** la suppression d'un compte supprime ses appartenances, demandes, licences, appareils et jetons selon les relations configurées. Les règles de conservation/audit devront être définies avant d'autoriser la suppression physique de comptes.
5. **Rétention des jetons :** prévoir ultérieurement une politique de purge des refresh tokens expirés/révoqués, sans supprimer les données nécessaires à l'audit ou à la détection de réutilisation.
6. **Données multi-entreprises :** aucune table de ce périmètre ne doit devenir partagée entre plusieurs entreprises par simple changement de chaîne de connexion. L'isolation doit rester au niveau du déploiement et du fichier SQLite.

## Critères d'acceptation de la migration initiale

- Toutes les tables Identity et applicatives ci-dessus sont créées.
- Les clés GUID, index uniques, index composites, longueurs maximales, conversions d'enums, jetons de concurrence et FK correspondent au modèle EF.
- La migration est accompagnée du snapshot EF Core généré à partir du modèle réel.
- Le snapshot est cohérent avec `CompanyDbContext` et permet les migrations ultérieures.
- Une revue spécifique confirme les comportements de suppression en cascade et les deux index uniques par utilisateur.
- La migration est relue avant tout `--apply` sur une base réelle.

## État

La migration initiale `20261120025147_InitialCompanySchema` et le snapshot `CompanyDbContextModelSnapshot` ont été générés avec EF Core 10.0.0 sur la branche d'implémentation. Le workflow GitHub Actions a terminé avec succès, y compris la compilation requise par l'outillage EF Core. La génération a utilisé une base SQLite temporaire ; aucune base d'entreprise n'a été créée ni modifiée.

La migration et le snapshot doivent encore faire l'objet d'une revue statique complète avant toute application sur une base réelle. En particulier, les deux index uniques par `UserId` sur `CompanyMemberships` et `MembershipRequests` sont intentionnels dans le modèle actuel, mais leur adéquation avec les besoins d'historique des demandes doit être confirmée. Aucune migration n'a été appliquée à une base réelle.
