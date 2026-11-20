# Sauvegarde, export et restauration

## Objectifs

Le Directeur doit pouvoir protéger les données de l’entreprise et restaurer son environnement après une panne de l’ordinateur serveur.

## Sauvegardes

Prévoir :
- sauvegardes automatiques selon une fréquence configurable ;
- sauvegardes manuelles à la demande ;
- export vers clé USB ou disque externe ;
- plusieurs versions avec politique de rétention ;
- chiffrement des fichiers de sauvegarde ;
- vérification d’intégrité et journalisation du résultat.

## Cohérence

Utiliser l’API de sauvegarde SQLite ou un mécanisme transactionnel adapté. Une simple copie du fichier de base actif n’est pas considérée comme une sauvegarde fiable.

## Restauration

Avant restauration :
1. vérifier le format et l’intégrité du fichier ;
2. vérifier la compatibilité de version du schéma ;
3. sauvegarder l’état actuel si cela est possible ;
4. restaurer dans un emplacement contrôlé ;
5. vérifier les données et les identifiants.

La restauration sur un nouvel ordinateur doit conserver l’identifiant unique de l’entreprise, les comptes, licences, rôles, permissions et historiques.

## Sécurité des clés

La gestion des clés de chiffrement doit être définie séparément. Une sauvegarde chiffrée sans procédure de récupération des clés serait une très jolie façon de perdre définitivement les données.
