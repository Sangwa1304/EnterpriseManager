# Gestion des appareils et sessions

## Enregistrement

Chaque nouvel appareil doit être associé à un utilisateur et à une entreprise après authentification réussie. Conserver les informations techniques strictement nécessaires à l’identification et à la gestion de l’appareil.

## Contrôles

À chaque session, vérifier :
- que le compte est actif ;
- que l’appartenance à l’entreprise est valide ;
- que la licence est valide ;
- que l’appareil n’a pas été révoqué ;
- que la session n’a pas expiré ;
- que les permissions couvrent l’opération demandée.

## Administration

Le Directeur peut consulter les appareils associés aux utilisateurs, révoquer un appareil perdu et définir une limite configurable d’appareils par utilisateur.

## Absence de privilège local

Le statut Directeur n’accorde aucun accès spécial au fichier SQLite ni aucune capacité hors ligne depuis un appareil secondaire.
