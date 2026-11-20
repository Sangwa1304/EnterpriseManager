# Données et persistance

## Modèle d’hébergement

Chaque entreprise possède une base SQLite centrale distincte. Le fichier est stocké sur la machine ou l’infrastructure qui héberge le serveur de cette entreprise.

Les appareils Windows et Android ne contiennent aucune base métier locale et n’accèdent jamais au fichier SQLite.

## Accès

Toute lecture et écriture passent par les cas d’utilisation applicatifs côté serveur et par l’API. Le client ne reçoit que les données nécessaires à ses écrans et à ses opérations autorisées.

## Contrat du schéma initial

Le détail des tables Identity et applicatives, des index, des relations et des points de vigilance de la première migration est décrit dans [DATABASE-SCHEMA.md](DATABASE-SCHEMA.md). Ce document est un contrat de revue, pas une migration exécutée.

## Domaines de données prévus

- entreprise et paramètres ;
- utilisateurs et appartenances ;
- appareils et sessions ;
- licences ;
- rôles et permissions ;
- départements ;
- clients et fournisseurs ;
- personnel ;
- projets, contrats et prestations ;
- matériaux et équipements ;
- caisses, banques et transactions ;
- devis, factures et reçus ;
- audit et historique.

## Principes de conception

- identifiants stables et contraintes d’intégrité ;
- montants monétaires stockés avec une représentation décimale adaptée, jamais en flottants binaires ;
- devise explicitement portée par les opérations financières ;
- dates et heures normalisées, avec distinction entre date métier et horodatage technique ;
- migrations de schéma versionnées ;
- indexation guidée par les requêtes réellement utilisées ;
- transactions pour les opérations métier atomiques.

## Sauvegarde SQLite

Ne pas copier simplement le fichier actif pendant que la base est utilisée. Utiliser l’API de sauvegarde SQLite ou un mécanisme transactionnel équivalent afin de produire une copie cohérente. Vérifier l’intégrité avant de considérer une sauvegarde comme valide.
