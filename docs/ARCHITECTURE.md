# Architecture générale

## Objectifs

EnterpriseManager est un logiciel de gestion d’entreprise généraliste, installable sur Windows et Android. Il doit pouvoir être configuré pour plusieurs entreprises indépendantes. MULTI VISION constitue un premier contexte de déploiement, pas une limite du produit.

## Vue d’ensemble

```text
Client Windows ─┐
Client Android ─┼── HTTPS / réseau privé ──> ASP.NET Core Web API
Autre Windows ──┘                              │
                                               ▼
                                      SQLite centrale
                                      de l’entreprise
```

Le serveur API et la base centrale sont hébergés sur l’ordinateur principal choisi par le Directeur, ou sur une infrastructure qu’il contrôle.

## Principes non négociables

1. Une base métier SQLite centrale par entreprise.
2. Les clients n’ouvrent jamais directement le fichier SQLite.
3. Toutes les lectures et écritures métier passent par l’API.
4. Aucun stockage métier local sur les appareils clients.
5. Aucun mode hors connexion, aucune synchronisation différée et aucune résolution de conflits.
6. Si le serveur est arrêté ou inaccessible, les clients ne peuvent pas consulter ni modifier les données.
7. Le rôle Directeur donne des privilèges métier, pas un accès technique local spécial depuis ses autres appareils.
8. Les entreprises sont isolées les unes des autres.

## Couches logicielles

- **Domain :** entités, objets-valeurs, règles métier et invariants.
- **Application :** cas d’utilisation, orchestration, contrats, validation et autorisation applicative.
- **Infrastructure :** EF Core, SQLite, stockage, chiffrement, sauvegarde, journalisation et adaptateurs.
- **Server :** hôte ASP.NET Core, endpoints API, authentification, autorisation et configuration réseau.
- **Client.Core :** services et logique de présentation partagés.
- **Client.Avalonia :** interface Avalonia, vues XAML, ViewModels et ressources multiplateformes.
- **Shared :** contrats et types explicitement partagés entre client et serveur, sans logique métier sensible.

## Flux de connexion

1. L’utilisateur saisit ses identifiants et l’adresse du serveur de son entreprise.
2. L’API vérifie le compte, l’appartenance à l’entreprise, la licence et les autorisations.
3. Le nouvel appareil est enregistré selon la politique de l’entreprise.
4. Le serveur crée une session et délivre les éléments d’authentification.
5. Les appels suivants vérifient la session, le compte, l’appareil et les droits.

## Réseau

Sur le réseau local, les clients communiquent avec le serveur sur un canal chiffré. Pour l’accès hors LAN, utiliser un VPN privé ou une solution de réseau privé équivalente. L’exposition directe de l’API à Internet n’est pas le choix par défaut.

## Décisions à documenter avant implémentation

- stratégie exacte de déploiement et de mise à jour du serveur ;
- gestion des certificats TLS ;
- durée et renouvellement des sessions ;
- stratégie de migration du schéma SQLite ;
- stratégie de chiffrement des sauvegardes et de gestion des clés ;
- politique de rétention des journaux et données d’audit.
