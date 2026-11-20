# EnterpriseManager.Provisioning

Outil en ligne de commande réservé à l'ordinateur qui héberge le serveur central d'une entreprise.

## Responsabilité

- vérifier une chaîne de connexion SQLite centrale, absolue et adossée à un fichier ;
- afficher les migrations en attente sans modifier la base ;
- exiger l'option explicite `--apply` pour toute migration ;
- créer une sauvegarde cohérente avant de modifier une base existante ;
- appliquer les migrations EF Core en attente ;
- exécuter `PRAGMA integrity_check` après migration.

Cet outil n'est pas exécuté sur les clients Windows ou Android. Il ne crée aucune base métier locale sur les appareils clients.

## Configuration

Définir `ENTERPRISEMANAGER_DB_CONNECTION` dans l'environnement du processus avec la chaîne SQLite centrale. Ne pas inscrire le chemin réel, les mots de passe ou d'autres secrets dans Git ou dans les arguments de commande.

Exemple de valeur locale :

```text
Data Source=/srv/enterprisemanager/companies/company.sqlite
```

## Utilisation

Sans argument, ou avec `--status`, l'outil affiche les migrations en attente sans modifier la base.

```text
EnterpriseManager.Provisioning --status
EnterpriseManager.Provisioning --apply
```

`--apply` est obligatoire pour effectuer les modifications. L'outil refuse les bases en mémoire et les chemins relatifs.

## Sauvegarde avant migration

Si le fichier existe déjà et n'est pas vide, l'outil crée une sauvegarde horodatée avec l'API SQLite Online Backup avant d'appliquer les migrations. Cette méthode produit une copie cohérente même si SQLite utilise son journal transactionnel. En cas d'échec du contrôle d'intégrité, le chemin de la sauvegarde est affiché et le fichier n'est pas supprimé.

La politique de rétention et le chiffrement des sauvegardes restent à implémenter dans le service de sauvegarde dédié. La sauvegarde pré-migration ne remplace pas les sauvegardes régulières sur un support distinct.

## État actuel et garde-fous

La factory de conception EF Core et l'outil de migration sont disponibles. La migration initiale n'est pas encore générée : l'outil s'arrête donc sans modifier la base tant qu'aucune migration n'est livrée.

La création du compte Directeur doit ensuite être intégrée à un parcours de provisioning sécurisé : mot de passe saisi de manière interactive ou fourni par un gestionnaire de secrets, transaction atomique, création des rôles par défaut, entreprise, appartenance et licence OWNER.

Ne pas utiliser `EnsureCreated()` : le schéma doit rester piloté par les migrations versionnées.
