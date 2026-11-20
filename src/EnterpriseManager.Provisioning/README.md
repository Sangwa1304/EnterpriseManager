# EnterpriseManager.Provisioning

Outil en ligne de commande réservé à l'ordinateur qui héberge le serveur central d'une entreprise.

## Responsabilité actuelle

- vérifier qu'une chaîne de connexion centrale est configurée ;
- refuser de modifier une base si aucune migration EF Core n'est livrée ;
- appliquer les migrations EF Core en attente à la base SQLite centrale.

Cet outil n'est pas exécuté sur les clients Windows ou Android. Il ne crée aucune base métier locale sur les appareils clients.

## Configuration

Définir la variable d'environnement `ENTERPRISEMANAGER_DB_CONNECTION` avec la chaîne de connexion SQLite de l'entreprise. Le secret et le chemin de la base ne doivent pas être inscrits dans le dépôt ni passés en argument de ligne de commande.

Exemple de valeur locale pour un environnement de développement :

```text
Data Source=/srv/enterprisemanager/companies/company.sqlite
```

## État actuel et garde-fous

La factory de conception EF Core est disponible dans Infrastructure. La migration initiale n'est pas encore générée : le projet s'arrête donc volontairement avant toute modification tant qu'aucune migration n'existe.

La création du compte Directeur doit ensuite être intégrée à un parcours de provisioning sécurisé : mot de passe fourni de manière interactive ou via un gestionnaire de secrets, transaction atomique, création des rôles par défaut, entreprise, appartenance et licence OWNER. Ne jamais stocker un mot de passe en clair dans un fichier de configuration.

Ne pas utiliser `EnsureCreated()` : le schéma doit rester piloté par les migrations versionnées.
