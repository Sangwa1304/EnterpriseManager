# Sécurité

## Principes

- TLS pour les communications client-serveur.
- Aucun accès direct au fichier SQLite depuis les clients.
- Authentification et autorisation côté serveur.
- Contrôle des permissions à chaque opération sensible.
- Hachage robuste des mots de passe avec un mécanisme reconnu.
- Secrets exclus du dépôt et des journaux.
- Validation de toutes les entrées côté serveur.
- Journalisation des événements de sécurité et opérations administratives.
- Sauvegardes chiffrées et procédure de restauration contrôlée.

## Réseau

L’accès distant doit être établi via VPN privé ou solution équivalente. Ne pas considérer qu’un ordinateur allumé implique que le serveur est disponible : le service API doit fonctionner et être joignable.

## Appareils

Chaque appareil autorisé est enregistré. Le Directeur peut consulter et révoquer les appareils. Une limite d’appareils par utilisateur pourra être configurée.

## Sessions

Prévoir expiration, révocation, rotation des jetons lorsque pertinent, déconnexion et invalidation lors de la suspension d’un compte ou d’une licence.

## Audit

Tracer au minimum les connexions, échecs d’authentification, changements de rôles et permissions, décisions d’adhésion, actions sur les licences, révocations d’appareils, sauvegardes et restaurations.

## À ne pas faire

- stocker des mots de passe en clair ;
- inclure des secrets dans Git ;
- faire confiance à un rôle déclaré par le client ;
- exposer SQLite sur le réseau ;
- désactiver la validation TLS en production.
