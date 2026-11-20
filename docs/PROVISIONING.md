# Initialisation d'une entreprise

## Modèle d'hébergement

Chaque entreprise possède un fichier SQLite central distinct, hébergé sur la machine qui exécute son serveur API. Les applications Avalonia Windows et Android n'embarquent aucune base métier et ne se connectent jamais directement au fichier SQLite.

## Étapes de provisioning

1. Installer le serveur et l'outil `EnterpriseManager.Provisioning` sur l'hôte central.
2. Choisir un emplacement protégé pour le fichier SQLite et définir `ENTERPRISEMANAGER_DB_CONNECTION` dans l'environnement du processus.
3. Appliquer les migrations EF Core versionnées avec l'outil de provisioning.
4. Créer l'entreprise et le compte Directeur dans une transaction atomique, avec les rôles initiaux et la licence OWNER.
5. Générer et remettre au Directeur les paramètres de connexion sécurisés du serveur.
6. Démarrer l'API et vérifier son état de santé avant d'autoriser les connexions clientes.

## Sécurité

- Ne jamais inclure de chaîne de connexion réelle, mot de passe, clé JWT ou clé de configuration dans Git.
- Le mot de passe initial du Directeur doit être saisi via un canal protégé et traité avec ASP.NET Core Identity.
- La clé de bootstrap doit être aléatoire, temporaire, à usage unique et retirée après initialisation.
- L'API doit refuser une seconde initialisation lorsque l'entreprise ou un compte existe déjà.
- Le fichier SQLite doit être accessible uniquement au compte système du serveur et aux sauvegardes autorisées.
- Les appareils clients passent exclusivement par l'API. Si le serveur est arrêté ou inaccessible, les données ne sont pas accessibles depuis les clients.

## État de livraison

La factory EF Core et le squelette de l'outil de provisioning sont présents. La migration initiale complète n'est pas encore générée et l'étape de création interactive du Directeur reste à implémenter. L'outil refuse explicitement de toucher à la base tant qu'aucune migration n'est disponible.

Aucune compilation, exécution ou migration de base de données n'a été lancée pendant cette étape.
